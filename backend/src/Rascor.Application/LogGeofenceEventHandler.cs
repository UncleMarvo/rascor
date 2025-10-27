using Rascor.Domain;
using Microsoft.Extensions.Logging;
using Rascor.Domain.Entities;

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

        await _eventRepo.AddAsync(evt, ct);

        _logger.LogInformation(
            "Geofence {EventType} event logged: User={UserId}, Site={SiteName} ({SiteId}), Lat={Lat}, Lon={Lon}",
            eventType, userId, site.Name, siteId, latitude, longitude);

        return evt;
    }
}
