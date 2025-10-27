namespace Rascor.Domain.Entities;

public record UserAssignment(
    string UserId,
    string SiteId,
    DateTimeOffset AssignedAt
);

