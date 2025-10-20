namespace Rascor.Application.DTOs;

public record WorkTypeDto(
    string Id,
    string Name,
    string? Description,
    bool IsActive
);

public record CreateWorkTypeRequest(
    string Name,
    string? Description
);

public record UpdateWorkTypeRequest(
    string Name,
    string? Description,
    bool IsActive
);