using Microsoft.Extensions.Logging;
using Shiny.Locations;

namespace Rascor.App.Services;

/// <summary>
/// Handles geofence enter/exit events from Shiny
/// </summary>
public class RascorGeofenceDelegate : IGeofenceDelegate
{
    private readonly ILogger<RascorGeofenceDelegate> _logger;
    private readonly DeviceIdentityService _deviceIdentity;
    private readonly EventQueueService _eventQueue;
    private readonly LocalNotificationService _notificationService;

    public RascorGeofenceDelegate(
        ILogger<RascorGeofenceDelegate> logger,
        DeviceIdentityService deviceIdentity,
        EventQueueService eventQueue,
        LocalNotificationService notificationService)
    {
        _logger = logger;
        _deviceIdentity = deviceIdentity;
        _eventQueue = eventQueue;
        _notificationService = notificationService;
        
        _logger.LogInformation("🎯 RascorGeofenceDelegate CONSTRUCTOR called - delegate is registered!");
    }

    public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
        _logger.LogWarning("🚨 GEOFENCE EVENT TRIGGERED! State={State}, Region={RegionId}", newStatus, region.Identifier);
        
        var eventType = newStatus switch
        {
            GeofenceState.Entered => "Enter",
            GeofenceState.Exited => "Exit",
            _ => null
        };

        if (eventType == null)
        {
            _logger.LogWarning("Unknown geofence state: {State}", newStatus);
            return;
        }

        _logger.LogWarning("🎯 Processing {EventType} event for site {SiteId} at {Timestamp}", 
            eventType, region.Identifier, DateTime.UtcNow);

        try
        {
            var userId = _deviceIdentity.GetUserId();
            
            // Create event request
            var request = new Core.GeofenceEventRequest(
                UserId: userId,
                SiteId: region.Identifier,
                EventType: eventType,
                Latitude: region.Center.Latitude,
                Longitude: region.Center.Longitude
            );

            _logger.LogWarning("📡 Posting event to backend for user {UserId}...", userId);
            
            // Try to post, queue if offline
            var success = await _eventQueue.PostOrQueueEventAsync(request);
            
            if (success)
            {
                _logger.LogWarning("✅ Event posted successfully!");
                _notificationService.ShowNotification(
                    title: $"Site {eventType}",
                    message: $"You {eventType.ToLower()}ed site: {region.Identifier}"
                );
            }
            else
            {
                _logger.LogWarning("📦 Event queued offline, will sync later");
                _notificationService.ShowNotification(
                    title: $"Site {eventType} (Offline)",
                    message: $"You {eventType.ToLower()}ed site: {region.Identifier}\nWill sync when online"
                );
            }

            _logger.LogInformation("Successfully processed geofence event");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to process geofence event for {SiteId}", region.Identifier);
            
            // Still show notification even if processing fails
            _notificationService.ShowNotification(
                title: "Geofence Event (Error)",
                message: $"Site {eventType}: {region.Identifier} - Error occurred"
            );
        }
    }
}
