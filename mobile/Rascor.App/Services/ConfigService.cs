using Rascor.App.Core;
using Microsoft.Extensions.Logging;

namespace Rascor.App.Services;

public class ConfigService
{
    private readonly BackendApi _api;
    private readonly IGeofenceService _geofenceService;
    private readonly DeviceIdentityService _deviceIdentity;
    private readonly EventQueueService _eventQueue;
    private readonly LocationTrackingService _locationTracking;
    private readonly ILogger<ConfigService> _logger;

    public RemoteConfig? Config { get; private set; }
    public List<Site> Sites { get; private set; } = new();
    public string CurrentUserId => _deviceIdentity.GetUserId();

    public ConfigService(
        BackendApi api,
        IGeofenceService geofenceService,
        DeviceIdentityService deviceIdentity,
        EventQueueService eventQueue,
        LocationTrackingService locationTracking,
        ILogger<ConfigService> logger)
    {
        _api = api;
        _geofenceService = geofenceService;
        _deviceIdentity = deviceIdentity;
        _eventQueue = eventQueue;
        _locationTracking = locationTracking;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var userId = _deviceIdentity.GetUserId();
        _logger.LogWarning("🔑 Initializing for user ID: {UserId}", userId);
        
        // FIRST: Clear ALL existing geofences to prevent stale data
        _logger.LogInformation("🧹 Clearing all existing geofences before initialization");
        await _geofenceService.UnregisterAllGeofencesAsync();
        
        // Request permissions
        var granted = await _geofenceService.RequestPermissionsAsync();
        if (!granted)
        {
            throw new InvalidOperationException("Location permissions not granted. Please enable 'Allow all the time' in Settings.");
        }

        // Fetch config and sites for actual user ID
        _logger.LogInformation("📡 Fetching sites for user: {UserId}", userId);
        var bootstrap = await _api.GetConfigAsync(userId);
        
        if (bootstrap != null && bootstrap.Sites.Count > 0)
        {
            _logger.LogWarning("✅ Found {Count} sites assigned to user {UserId}", bootstrap.Sites.Count, userId);
            Config = bootstrap.Config;
            Sites = bootstrap.Sites;
        }
        else
        {
            // No sites found for this device - use demo fallback
            _logger.LogWarning("⚠️ No sites assigned to user {UserId}. Using demo user as fallback.", userId);
            _logger.LogWarning("💡 To assign sites: UPDATE user_site_assignments SET user_id = '{UserId}' WHERE site_id IN ('site-XXX', ...);", userId);
            
            bootstrap = await _api.GetConfigAsync("user-demo");
            
            if (bootstrap == null || bootstrap.Sites.Count == 0)
            {
                throw new InvalidOperationException($"No sites assigned to device {userId}. Please contact admin to assign sites.");
            }
            
            _logger.LogInformation("✅ Using demo user sites for testing ({Count} sites)", bootstrap.Sites.Count);
            Config = bootstrap.Config;
            Sites = bootstrap.Sites;
        }

        // Register geofences (passive background monitoring)
        await _geofenceService.RegisterGeofencesAsync(Sites);

        // Configure location tracking with sites
        _locationTracking.SetSites(Sites);

        // Start active location tracking (checks frequently for faster detection)
        var trackingStarted = await _locationTracking.StartTrackingAsync();
        if (trackingStarted)
        {
            _logger.LogWarning("⚡ Active location tracking enabled - faster detection");
        }
        else
        {
            _logger.LogWarning("⚠️ Active tracking not available - relying on passive geofences only");
        }

        _logger.LogInformation("Initialized with {SiteCount} geofences for user {UserId}", Sites.Count, userId);
    }

    public async Task<bool> SimulateGeofenceEventAsync(
        string siteId,
        string eventType,
        double? latitude,
        double? longitude)
    {
        var userId = _deviceIdentity.GetUserId();
        
        var request = new GeofenceEventRequest(
            UserId: userId,
            SiteId: siteId,
            EventType: eventType,
            Latitude: latitude,
            Longitude: longitude
        );

        // Use EventQueueService so it queues offline events
        return await _eventQueue.PostOrQueueEventAsync(request);
    }
}
