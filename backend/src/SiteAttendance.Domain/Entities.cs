namespace SiteAttendance.Domain;

// ============================================================================
// ENTITIES
// ============================================================================

public record Site(
    string Id,
    string Name,
    double Latitude,
    double Longitude,
    int RadiusMeters
);

public record GeofenceEvent(
    string Id,
    string UserId,
    string SiteId,
    string EventType, // "Enter" | "Exit"
    DateTimeOffset Timestamp,
    double? Latitude,
    double? Longitude
);

public record UserAssignment(
    string UserId,
    string SiteId,
    DateTimeOffset AssignedAt
);

public record MobileBootstrapResponse(
    RemoteConfig Config,
    List<Site> Sites,
    string Etag
);
