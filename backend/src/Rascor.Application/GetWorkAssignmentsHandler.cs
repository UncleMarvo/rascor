using Rascor.Domain.Repositories;
using Rascor.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Rascor.Application;

public class GetWorkAssignmentsHandler
{
    private readonly IWorkAssignmentRepository _repo;
    private readonly ILogger<GetWorkAssignmentsHandler> _logger;

    public GetWorkAssignmentsHandler(
        IWorkAssignmentRepository repo,
        ILogger<GetWorkAssignmentsHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<List<WorkAssignmentDto>> ExecuteAsync(
        string userId, 
        CancellationToken ct = default)
    {
        var assignments = await _repo.GetByUserIdAsync(userId);
        
        _logger.LogInformation(
            "Retrieved {Count} work assignments for user {UserId}", 
            assignments.Count(), 
            userId);
        
        return assignments.Select(a => new WorkAssignmentDto(
            a.Id,
            a.UserId,
            a.SiteId,
            a.Site?.Name ?? "",
            a.WorkTypeId,
            a.WorkType?.Name ?? "",
            a.AssignedBy,
            a.AssignedAt,
            a.ExpectedStartDate,
            a.ExpectedEndDate,
            a.Status,
            a.Notes
        )).ToList();
    }
}
