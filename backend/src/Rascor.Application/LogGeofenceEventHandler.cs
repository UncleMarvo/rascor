using Rascor.Domain;
using Rascor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Rascor.Application;

public class LogGeofenceEventHandler
{
    private readonly IGeofenceEventRepository _eventRepo;
    private readonly ISiteRepository _siteRepo;
    private readonly IClock _clock;
    private readonly ILogger<LogGeofenceEventHandler> _logger;

    public LogGeofenceEventHandler(
        IGeofenceEventRepository eventRepo,
        ISiteRepository siteRepo,
        IClock clock,
        ILogger<LogGeofenceEventHandler> logger)
    {
        _eventRepo = eventRepo;
        _siteRepo = siteRepo;
        _clock = clock;
        _logger = logger;
    }

    public async Task<GeofenceEvent> HandleAsync(
        string userId,
        string siteId,
        string eventType,
        double? latitude,
        double? longitude,
        CancellationToken ct = default)
    {
        // Validate site exists
        var site = await _siteRepo.GetByIdAsync(siteId, ct);
        if (site == null)
        {
            _logger.LogWarning("Geofence event for unknown site {SiteId}", siteId);
            throw new InvalidOperationException($"Site {siteId} not found");
        }

        // TODO: Add debounce logic using RemoteConfig.DebounceEnterMinutes/DebounceExitMinutes
        // For MVP, just log every event
        var evt = new GeofenceEvent
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            SiteId = siteId,
            EventType = eventType,
            TriggerMethod = "auto", // This is an auto-triggered event from geofencing
            Timestamp = _clock.UtcNow.UtcDateTime,
            Latitude = latitude ?? 0,
            Longitude = longitude ?? 0
        };

        // Save event to database
        await _eventRepo.AddAsync(evt, ct);

        // Log success with event ID for tracking
        _logger.LogInformation(
            "✅ Geofence {EventType} event saved to database: EventId={EventId}, User={UserId}, Site={SiteName} ({SiteId}), Timestamp={Timestamp}, Lat={Lat}, Lon={Lon}",
            eventType, evt.Id, userId, site.Name, siteId, evt.Timestamp, latitude, longitude);

        return evt;
    }
}
