namespace Rascor.Domain;

// ============================================================================
// REMOTE CONFIG
// ============================================================================

public record GeofenceConfig(
    int DefaultRadiusMeters,
    int MaxConcurrentSites,
    int DebounceEnterMinutes,
    int DebounceExitMinutes
)
{
    public static GeofenceConfig Default => new(
        DefaultRadiusMeters: 150,
        MaxConcurrentSites: 20,
        DebounceEnterMinutes: 5,
        DebounceExitMinutes: 3
    );
}
