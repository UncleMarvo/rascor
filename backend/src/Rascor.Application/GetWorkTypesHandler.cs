using Rascor.Domain.Repositories;
using Rascor.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Rascor.Application;

public class GetWorkTypesHandler
{
    private readonly IWorkTypeRepository _repo;
    private readonly ILogger<GetWorkTypesHandler> _logger;

    public GetWorkTypesHandler(
        IWorkTypeRepository repo,
        ILogger<GetWorkTypesHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<List<WorkTypeDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var workTypes = await _repo.GetAllActiveAsync();
        
        _logger.LogInformation("Retrieved {Count} work types", workTypes.Count());
        
        return workTypes.Select(w => new WorkTypeDto(
            w.Id,
            w.Name,
            w.Description,
            w.IsActive
        )).ToList();
    }
}
