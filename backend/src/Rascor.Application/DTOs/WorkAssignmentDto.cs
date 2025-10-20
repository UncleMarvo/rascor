namespace Rascor.Application.DTOs;

public record WorkAssignmentDto(
    string Id,
    string UserId,
    string SiteId,
    string SiteName,
    string WorkTypeId,
    string WorkTypeName,
    string? AssignedBy,
    DateTime AssignedAt,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string Status,
    string? Notes
);

public record CreateWorkAssignmentRequest(
    string UserId,
    string SiteId,
    string WorkTypeId,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? Notes
);

public record UpdateWorkAssignmentRequest(
    string Status,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? Notes
);

public record AvailableWorkDto(
    string WorkTypeId,
    string WorkTypeName,
    string? Description,
    bool HasSignedRamsToday
);