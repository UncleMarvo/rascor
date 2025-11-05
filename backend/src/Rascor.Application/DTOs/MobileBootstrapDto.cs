namespace Rascor.Application.DTOs;

/// <summary>
/// Bootstrap response with sites and basic config
/// </summary>
public record MobileBootstrapResponse(
    RemoteConfig Config,
    List<SiteDto> Sites
);

public record RemoteConfig(
    int PollIntervalSeconds,
    bool EnableOfflineMode
);

public record SiteDto(
    string Id,
    string Name,
    double Latitude,
    double Longitude,
    int AutoTriggerRadiusMeters,
    int ManualTriggerRadiusMeters
);