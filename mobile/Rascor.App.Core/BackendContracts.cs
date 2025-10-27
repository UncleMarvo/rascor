namespace Rascor.App.Core;

// ============================================================================
// BACKEND API CONTRACTS (matching backend DTOs)
// ============================================================================

public record Site(
    string Id,
    string Name,
    double Latitude,
    double Longitude,
    int AutoTriggerRadiusMeters,
    int ManualTriggerRadiusMeters
);

public record RemoteConfig(
    int DefaultRadiusMeters,
    int MaxConcurrentSites,
    int DebounceEnterMinutes,
    int DebounceExitMinutes
);

public record MobileBootstrapResponse(
    RemoteConfig Config,
    List<Site> Sites,
    string Etag
);

public record GeofenceEventRequest(
    string UserId,
    string SiteId,
    string EventType, // "Enter" or "Exit"
    double? Latitude,
    double? Longitude
);
