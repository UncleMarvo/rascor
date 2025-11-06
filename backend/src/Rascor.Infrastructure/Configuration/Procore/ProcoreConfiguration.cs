namespace Rascor.Infrastructure.Configuration.Procore;

/// <summary>
/// Configuration for Procore Infrastructure integration
/// </summary>
public class ProcoreConfiguration
{
    public const string SectionName = "Procore";

    /// <summary>
    /// OAuth Client ID from Procore Developer Portal
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth Client Secret from Procore Developer Portal
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Procore Company ID
    /// </summary>
    public long CompanyId { get; set; }

    /// <summary>
    /// Procore API base URL (default: https://api.procore.com)
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.procore.com";

    /// <summary>
    /// Procore OAuth token URL
    /// </summary>
    public string TokenUrl { get; set; } = "https://login.procore.com/oauth/token";

    /// <summary>
    /// API version to use (default: v1.0)
    /// </summary>
    public string ApiVersion { get; set; } = "v1.0";

    /// <summary>
    /// How many minutes before expiry to refresh the token
    /// </summary>
    public int TokenRefreshBufferMinutes { get; set; } = 5;

    /// <summary>
    /// Sync interval in minutes (default: 30)
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Whether to sync only active projects (default: true)
    /// </summary>
    public bool SyncOnlyActiveProjects { get; set; } = true;

    /// <summary>
    /// Whether the sync service is enabled
    /// </summary>
    public bool SyncEnabled { get; set; } = true;
}
