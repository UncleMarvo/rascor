namespace SiteAttendance.Domain;

// ============================================================================
// REMOTE CONFIG
// ============================================================================

public record RemoteConfig(
    int DefaultRadiusMeters,
    int MaxConcurrentSites,
    int DebounceEnterMinutes,
    int DebounceExitMinutes
)
{
    public static RemoteConfig Default => new(
        DefaultRadiusMeters: 150,
        MaxConcurrentSites: 20,
        DebounceEnterMinutes: 5,
        DebounceExitMinutes: 3
    );
}
