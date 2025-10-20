namespace Rascor.Application.DTOs;

/// <summary>
/// Extended bootstrap response including work assignments and RAMS status
/// </summary>
public record MobileBootstrapDto(
    RemoteConfigDto Config,
    List<SiteDto> Sites,
    List<WorkAssignmentDto>? WorkAssignments,
    Dictionary<string, bool>? RamsSignedToday
);

public record RemoteConfigDto(
    int PollIntervalSeconds,
    bool EnableOfflineMode
);

public record SiteDto(
    string Id,
    string Name,
    double Latitude,
    double Longitude,
    int RadiusMeters
);