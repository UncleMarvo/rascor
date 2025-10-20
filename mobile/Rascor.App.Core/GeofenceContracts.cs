namespace Rascor.App.Core;

// ============================================================================
// GEOFENCE SERVICE CONTRACTS (platform-agnostic)
// ============================================================================

public interface IGeofenceService
{
    Task<bool> RequestPermissionsAsync();
    Task RegisterGeofencesAsync(List<Site> sites);
    Task UnregisterAllGeofencesAsync();
}

public record GeofenceTransition(
    string SiteId,
    string TransitionType, // "Enter" or "Exit"
    double? Latitude,
    double? Longitude,
    DateTimeOffset Timestamp
);
