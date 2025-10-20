namespace SiteAttendance.Domain;

// ============================================================================
// PORTS (Interfaces for dependency injection)
// ============================================================================

/// <summary>
/// Repository for geofence events
/// </summary>
public interface IGeofenceEventRepository
{
    Task AddAsync(GeofenceEvent evt, CancellationToken ct = default);
    Task<List<GeofenceEvent>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<List<GeofenceEvent>> GetBySiteIdAsync(string siteId, CancellationToken ct = default);
}

/// <summary>
/// Repository for sites
/// </summary>
public interface ISiteRepository
{
    Task<Site?> GetByIdAsync(string siteId, CancellationToken ct = default);
    Task<List<Site>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Site site, CancellationToken ct = default);
}

/// <summary>
/// Repository for user-site assignments
/// </summary>
public interface IAssignmentRepository
{
    Task<List<Site>> GetAssignedSitesAsync(string userId, CancellationToken ct = default);
    Task AssignAsync(string userId, string siteId, CancellationToken ct = default);
}

/// <summary>
/// Repository for remote settings
/// </summary>
public interface ISettingsRepository
{
    Task<RemoteConfig> GetConfigAsync(CancellationToken ct = default);
    Task UpdateConfigAsync(RemoteConfig config, CancellationToken ct = default);
}

/// <summary>
/// Push notification provider (swappable: Mock, OneSignal, FCM, APNs)
/// </summary>
public interface IPushProvider
{
    Task SendAsync(string userId, string title, string message, CancellationToken ct = default);
}

/// <summary>
/// Email provider (swappable: Console, AWS SES, SendGrid)
/// </summary>
public interface IEmailProvider
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}

/// <summary>
/// System clock abstraction for testing
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
