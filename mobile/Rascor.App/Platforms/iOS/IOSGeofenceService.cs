using CoreLocation;
using Foundation;
using Microsoft.Extensions.Logging;
using Rascor.App.Core;
using UIKit;

namespace Rascor.App.Platforms.iOS;

public class IOSGeofenceService : IGeofenceService
{
    private readonly ILogger<IOSGeofenceService> _logger;
    private readonly CLLocationManager _locationManager;

    public IOSGeofenceService(ILogger<IOSGeofenceService> logger)
    {
        _logger = logger;
        _locationManager = new CLLocationManager();
        _locationManager.RegionEntered += OnRegionEntered;
        _locationManager.RegionLeft += OnRegionLeft;
    }

    public Task<bool> RequestPermissionsAsync()
    {
        var tcs = new TaskCompletionSource<bool>();

        try
        {
            var status = CLLocationManager.Status;

            if (status == CLAuthorizationStatus.AuthorizedAlways)
            {
                _logger.LogInformation("Location permissions already granted (Always)");
                tcs.SetResult(true);
                return tcs.Task;
            }

            // Request permissions
            _locationManager.AuthorizationChanged += (sender, args) =>
            {
                if (args.Status == CLAuthorizationStatus.AuthorizedAlways || 
                    args.Status == CLAuthorizationStatus.AuthorizedWhenInUse)
                {
                    _logger.LogInformation("Location permissions granted: {Status}", args.Status);
                    tcs.TrySetResult(true);
                }
                else
                {
                    _logger.LogWarning("Location permissions denied: {Status}", args.Status);
                    tcs.TrySetResult(false);
                }
            };

            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                _locationManager.RequestAlwaysAuthorization();
            }
            else
            {
                _locationManager.RequestWhenInUseAuthorization();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request location permissions");
            tcs.SetResult(false);
        }

        return tcs.Task;
    }

    public Task RegisterGeofencesAsync(List<Site> sites)
    {
        try
        {
            // Clear existing geofences
            if (_locationManager.MonitoredRegions != null)
            {
                foreach (var region in _locationManager.MonitoredRegions)
                {
                    if (region is CLRegion clRegion)
                    {
                        _locationManager.StopMonitoring(clRegion);
                    }
                }
            }

            // Register new geofences
            foreach (var site in sites)
            {
                var coordinate = new CLLocationCoordinate2D(site.Latitude, site.Longitude);
                var region = new CLCircularRegion(coordinate, site.AutoTriggerRadiusMeters, site.Id)
                {
                    NotifyOnEntry = true,
                    NotifyOnExit = true
                };

                _locationManager.StartMonitoring(region);
                _logger.LogInformation("Registered geofence: {SiteName} ({SiteId})", site.Name, site.Id);
            }

            _logger.LogInformation("Registered {Count} iOS geofences", sites.Count);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register iOS geofences");
            throw;
        }
    }

    public Task UnregisterAllGeofencesAsync()
    {
        try
        {
            if (_locationManager.MonitoredRegions != null)
            {
                foreach (var region in _locationManager.MonitoredRegions)
                {
                    if (region is CLRegion clRegion)
                    {
                        _locationManager.StopMonitoring(clRegion);
                    }
                }
            }

            _logger.LogInformation("Unregistered all iOS geofences");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister iOS geofences");
            throw;
        }
    }

    private void OnRegionEntered(object? sender, CLRegionEventArgs e)
    {
        var siteId = e.Region.Identifier;
        _logger.LogInformation("Geofence Enter: {SiteId}", siteId);

        // TODO: Post event to backend via background fetch
        // For MVP, just log. In production, use background fetch or push notifications
    }

    private void OnRegionLeft(object? sender, CLRegionEventArgs e)
    {
        var siteId = e.Region.Identifier;
        _logger.LogInformation("Geofence Exit: {SiteId}", siteId);

        // TODO: Post event to backend
    }
}
