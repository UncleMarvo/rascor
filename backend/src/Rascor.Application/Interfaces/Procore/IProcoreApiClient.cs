using Rascor.Application.Models.Procore;

namespace Rascor.Application.Interfaces.Procore;

public interface IProcoreApiClient
{
    Task<List<ProcoreProject>> GetProjectsAsync(DateTime? updatedSince = null, CancellationToken cancellationToken = default);
}
