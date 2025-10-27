using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Locations;
using Rascor.App.Core;

namespace Rascor.App.Services;

/// <summary>
/// Wrapper service that implements IGeofenceService using Shiny.NET
/// </summary>
public class ShinyGeofenceService : IGeofenceService
{
    private readonly IGeofenceManager _geofenceManager;
    private readonly ILogger<ShinyGeofenceService> _logger;

    public ShinyGeofenceService(
        IGeofenceManager geofenceManager,
        ILogger<ShinyGeofenceService> logger)
    {
        _geofenceManager = geofenceManager;
        _logger = logger;
        
        _logger.LogInformation("🎯 ShinyGeofenceService CONSTRUCTOR called");
    }

    public async Task<bool> RequestPermissionsAsync()
    {
        _logger.LogWarning("🔐 Requesting location permissions...");

        // Request access through Shiny's geofence manager
        var status = await _geofenceManager.RequestAccess();
        
        _logger.LogWarning("🔐 Shiny permission status: {Status}", status);

        // Check if we got the required permissions
        if (status == AccessState.Available)
        {
            _logger.LogWarning("✅ All location permissions granted!");
            return true;
        }

        // Log specific permission issues
        _logger.LogError("❌ Location permissions not fully granted. Status: {Status}", status);
        
        if (status == AccessState.Restricted)
        {
            _logger.LogError("⚠️ Background location is RESTRICTED. User needs to grant 'Allow all the time' in Settings!");
        }

        return false;
    }

    public async Task RegisterGeofencesAsync(List<Site> sites)
    {
        if (sites == null || sites.Count == 0)
        {
            _logger.LogWarning("⚠️ No sites to register for geofencing");
            return;
        }

        _logger.LogWarning("📍 Registering {Count} geofences...", sites.Count);

        // Clear existing geofences first
        await _geofenceManager.StopAllMonitoring();
        _logger.LogInformation("🧹 Cleared all existing geofences");

        // Register each site as a geofence
        int successCount = 0;
        foreach (var site in sites)
        {
            try
            {
                // Shiny GeofenceRegion constructor: (identifier, center, radius)
                var region = new GeofenceRegion(
                    site.Id, // identifier
                    new Position(site.Latitude, site.Longitude), // center
                    Distance.FromMeters(site.AutoTriggerRadiusMeters) // radius
                )
                {
                    NotifyOnEntry = true,
                    NotifyOnExit = true,
                    SingleUse = false // Keep monitoring indefinitely
                };

                await _geofenceManager.StartMonitoring(region);
                successCount++;
                _logger.LogWarning("✅ Registered geofence: {SiteId} ({SiteName}) at ({Lat}, {Lon}) radius {Radius}m, NotifyOnEntry={Entry}, NotifyOnExit={Exit}",
                    site.Id, site.Name, site.Latitude, site.Longitude, site.AutoTriggerRadiusMeters, region.NotifyOnEntry, region.NotifyOnExit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to register geofence for site {SiteId}", site.Id);
            }
        }

        _logger.LogWarning("✅ Geofence registration complete! {Success}/{Total} geofences registered", 
            successCount, sites.Count);
        
        _logger.LogWarning("⚠️ REMINDER: Geofences require significant movement (200m+) and time (2-5 min) to trigger!");
        _logger.LogWarning("💡 TIP: Use 'Simulate' buttons to test offline queue immediately!");
    }

    public async Task UnregisterAllGeofencesAsync()
    {
        _logger.LogInformation("🧹 Unregistering all geofences");
        await _geofenceManager.StopAllMonitoring();
        _logger.LogInformation("✅ All geofences unregistered");
    }
}
