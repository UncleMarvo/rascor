namespace Rascor.Application.DTOs;

public record RamsAcceptanceDto(
    string Id,
    string UserId,
    string SiteId,
    string SiteName,
    string WorkAssignmentId,
    string RamsDocumentId,
    string RamsTitle,
    DateTime AcceptedAt,
    string? IpAddress,
    double? Latitude,
    double? Longitude
);

public record CreateRamsAcceptanceRequest(
    string UserId,
    string SiteId,
    string WorkAssignmentId,
    string RamsDocumentId,
    string SignatureData,
    string? IpAddress,
    string? DeviceInfo,
    double? Latitude,
    double? Longitude,
    Dictionary<string, object>? ChecklistResponses
);

public record RamsComplianceReportDto(
    int TotalAcceptances,
    int UniqueUsers,
    int UniqueSites,
    List<RamsAcceptanceDto> Acceptances
);