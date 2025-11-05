using Microsoft.Extensions.Logging;
using Rascor.App.Core;
using Rascor.App.Core.Services;
using Shiny.Locations;

namespace Rascor.App.Services;

/// <summary>
/// Active location tracking for faster geofence detection
/// Complements passive geofencing with more frequent updates
/// </summary>
public class LocationTrackingService
{
    public event EventHandler<GpsReading>? LocationUpdated;
    public event EventHandler? GeofenceStateChanged;

    private readonly IGpsManager _gpsManager;
    private readonly EventQueueService _eventQueue;
    private readonly LocalNotificationService _notificationService;
    private readonly DeviceIdentityService _deviceIdentity;
    private readonly ILogger<LocationTrackingService> _logger;
    private readonly Dictionary<string, bool> _currentSiteStatus = new(); // Track which sites user is inside
    private List<Site> _sites = new();
    private Position? _lastLoggedPosition;
    private GpsReading? _lastReading;
    private bool _isListening = false;
    private const double MinimumLogDistanceMeters = 50; // Only log if moved 50+ meters
    private DateTime _lastLocationUpdateTime = DateTime.MinValue;
    private const int MinimumUpdateIntervalSeconds = 20; // Only process location updates every 20 seconds
    // Track when state changes started to implement dwell time
    private Dictionary<string, DateTime> _potentialEnterTimes = new();
    private Dictionary<string, DateTime> _potentialExitTimes = new();
    private const int DwellTimeSeconds = 90; // Must be inside/outside for 90 seconds

    private readonly GeofenceStateService _stateService;

    public LocationTrackingService(
        IGpsManager gpsManager,
        EventQueueService eventQueue,
        LocalNotificationService notificationService,
        DeviceIdentityService deviceIdentity,
        ILogger<LocationTrackingService> logger,
        GeofenceStateService stateService)
    {
        _gpsManager = gpsManager;
        _eventQueue = eventQueue;
        _notificationService = notificationService;
        _deviceIdentity = deviceIdentity;
        _logger = logger;
        _stateService = stateService;

    }

    /// <summary>
    /// Initialize with sites to monitor (called by ConfigService after it loads sites)
    /// </summary>
    public void SetSites(List<Site> sites)
    {
        _sites = sites;
        _logger.LogInformation("Location tracking configured for {Count} sites", sites.Count);
    }

    /// <summary>
    /// Get the site the user is currently inside (if any)
    /// </summary>
    public Site? GetCurrentSite()
    {
        if (_lastReading == null || _sites.Count == 0)
            return null;

        foreach (var site in _sites)
        {
            var distance = CalculateDistance(
                _lastReading.Position.Latitude,
                _lastReading.Position.Longitude,
                site.Latitude,
                site.Longitude
            );

            if (distance <= site.AutoTriggerRadiusMeters)
                return site;
        }

        return null;
    }

    /// <summary>
    /// Get distance to nearest site and site name
    /// </summary>
    public (string siteName, double distance)? GetNearestSiteInfo()
    {
        if (_lastReading == null || _sites.Count == 0)
            return null;

        string? nearestSite = null;
        double nearestDistance = double.MaxValue;

        foreach (var site in _sites)
        {
            var distance = CalculateDistance(
                _lastReading.Position.Latitude,
                _lastReading.Position.Longitude,
                site.Latitude,
                site.Longitude
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSite = site.Name;
            }
        }

        return nearestSite != null ? (nearestSite, nearestDistance) : null;
    }

    /// <summary>
    /// Start active location tracking for faster updates
    /// </summary>
    public async Task<bool> StartTrackingAsync()
    {
        try
        {
            // Create GPS request with 2-minute intervals
            var request = new GpsRequest
            {
                BackgroundMode = GpsBackgroundMode.Realtime,    // Realtime - more aggressive on battery but better event triggers
            };

            // Request GPS permissions
            var access = await _gpsManager.RequestAccess(request);

            if (access != Shiny.AccessState.Available)
            {
                _logger.LogWarning("GPS access not available: {Status}", access);
                return false;
            }

            // Start listening to location updates
            _gpsManager.WhenReading().Subscribe(OnLocationUpdate);

            // Try to start GPS tracking
            try
            {
                await _gpsManager.StartListener(request);
                _isListening = true;
                _logger.LogWarning("✅ Location tracking started - checking every ~2 minutes for faster detection");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already a GPS listener"))
            {
                // GPS already running - that's fine, we're subscribed to updates
                _isListening = true;
                _logger.LogInformation("✅ GPS listener already running - subscribed to updates");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start location tracking");
            return false;
        }
    }

    /// <summary>
    /// Stop active location tracking
    /// </summary>
    public async Task StopTrackingAsync()
    {
        try
        {
            if (_isListening)
            {
                await _gpsManager.StopListener();
                _isListening = false;
                _logger.LogInformation("Location tracking stopped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop location tracking");
        }
    }

    /// <summary>
    /// Handle each location update and check for geofence transitions
    /// </summary>
    private void OnLocationUpdate(GpsReading reading)
    {
        try
        {
            // Store last reading for GetCurrentSite()
            _lastReading = reading;

            // Throttle updates to reduce CPU/battery usage
            var timeSinceLastUpdate = (DateTime.Now - _lastLocationUpdateTime).TotalSeconds;
            if (timeSinceLastUpdate < MinimumUpdateIntervalSeconds)
            {
                return; // Skip this update, too soon
            }
            _lastLocationUpdateTime = DateTime.Now;

            // Ignore low-accuracy readings to prevent false events
            if (reading.PositionAccuracy > 100) // More than 100m error
            {
                _logger.LogInformation("Ignoring low-accuracy reading: {Accuracy}m", reading.PositionAccuracy);
                return;
            }

            // FIRE THE EVENT
            LocationUpdated?.Invoke(this, reading);

            if (_sites.Count == 0)
            {
                return; // Silently skip if no sites configured
            }

            // Only log if moved significantly (reduces spam)
            var shouldLog = _lastLoggedPosition == null ||
                CalculateDistance(
                    _lastLoggedPosition.Latitude,
                    _lastLoggedPosition.Longitude,
                    reading.Position.Latitude,
                    reading.Position.Longitude
                ) >= MinimumLogDistanceMeters;

            if (shouldLog)
            {
                _logger.LogInformation("📍 Location update: ({Lat}, {Lon}) accuracy: {Accuracy}m",
                    reading.Position.Latitude, reading.Position.Longitude, reading.PositionAccuracy);
                _lastLoggedPosition = reading.Position;
            }

            // Check each site to see if user entered or exited
            foreach (var site in _sites)
            {
                var distance = CalculateDistance(
                    reading.Position.Latitude,
                    reading.Position.Longitude,
                    site.Latitude,
                    site.Longitude
                );

                var wasInside = _currentSiteStatus.GetValueOrDefault(site.Id, false);
                var bufferMeters = 20; // 20m buffer to prevent bouncing

                var isInside = wasInside
                    ? distance <= (site.AutoTriggerRadiusMeters + bufferMeters)  // Must go further out to exit
                    : distance <= (site.AutoTriggerRadiusMeters - bufferMeters); // Must go further in to enter

                // Detect transition
                if (isInside && !wasInside)
                {
                    // ENTER event
                    // User appears to be entering - start dwell timer
                    if (!_potentialEnterTimes.ContainsKey(site.Id))
                    {
                        _potentialEnterTimes[site.Id] = DateTime.Now;
                        _logger.LogInformation("Potential ENTER for {SiteName}, waiting {Dwell}s...", site.Name, DwellTimeSeconds);
                    }
                    else
                    {
                        // Check if dwell time elapsed
                        var dwellTime = (DateTime.Now - _potentialEnterTimes[site.Id]).TotalSeconds;
                        if (dwellTime >= DwellTimeSeconds)
                        {
                            // Confirmed ENTER
                            _logger.LogWarning("ENTER confirmed: {SiteName} (distance: {Distance}m)", site.Name, (int)distance);
                            _ = HandleEnterEventAsync(site, reading.Position.Latitude, reading.Position.Longitude);
                            _currentSiteStatus[site.Id] = true;
                            _potentialEnterTimes.Remove(site.Id);
                        }
                    }
                }
                else if (!isInside && wasInside)
                {
                    // EXIT event
                    // User appears to be exiting - start dwell timer
                    if (!_potentialExitTimes.ContainsKey(site.Id))
                    {
                        _potentialExitTimes[site.Id] = DateTime.Now;
                        _logger.LogInformation("Potential EXIT for {SiteName}, waiting {Dwell}s...", site.Name, DwellTimeSeconds);
                    }
                    else
                    {
                        // Check if dwell time elapsed
                        var dwellTime = (DateTime.Now - _potentialExitTimes[site.Id]).TotalSeconds;
                        if (dwellTime >= DwellTimeSeconds)
                        {
                            // Confirmed EXIT
                            _logger.LogWarning("EXIT confirmed: {SiteName} (distance: {Distance}m)", site.Name, (int)distance);
                            _ = HandleExitEventAsync(site, reading.Position.Latitude, reading.Position.Longitude);
                            _currentSiteStatus[site.Id] = false;
                            _potentialExitTimes.Remove(site.Id);
                        }
                    }
                }
                else
                {
                    // Clear any pending transitions if state is stable
                    _potentialEnterTimes.Remove(site.Id);
                    _potentialExitTimes.Remove(site.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing location update");
        }
    }

    private async Task HandleEnterEventAsync(Site site, double latitude, double longitude)
    {
        try
        {
            var userId = _deviceIdentity.GetUserId();
            
            var request = new GeofenceEventRequest(
                UserId: userId,
                SiteId: site.Id,
                EventType: "Enter",
                Latitude: latitude,
                Longitude: longitude
            );

            var success = await _eventQueue.PostOrQueueEventAsync(request);
            
            await _notificationService.ShowNotificationAsync(
                success ? "Site Enter" : "Site Enter (Offline)",
                success 
                    ? $"You entered {site.Name}"
                    : $"You entered {site.Name} - will sync when online"
            );

            // UPDATE STATE SERVICE
            _stateService.SetCheckedIn(site.Id);

            // FIRE THE EVENT
            GeofenceStateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle enter event for {SiteId}", site.Id);
        }
    }

    private async Task HandleExitEventAsync(Site site, double latitude, double longitude)
    {
        try
        {
            var userId = _deviceIdentity.GetUserId();
            
            var request = new GeofenceEventRequest(
                UserId: userId,
                SiteId: site.Id,
                EventType: "Exit",
                Latitude: latitude,
                Longitude: longitude
            );

            var success = await _eventQueue.PostOrQueueEventAsync(request);
            
            await _notificationService.ShowNotificationAsync(
                success ? "Site Exit" : "Site Exit (Offline)",
                success 
                    ? $"You exited {site.Name}"
                    : $"You exited {site.Name} - will sync when online"
            );

            // UPDATE STATE SERVICE
            _stateService.SetNotAtSite();

            // FIRE THE EVENT
            GeofenceStateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle exit event for {SiteId}", site.Id);
        }
    }

    /// <summary>
    /// Calculate distance between two GPS coordinates using Haversine formula
    /// </summary>
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMeters = 6371000;
        
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return EarthRadiusMeters * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}