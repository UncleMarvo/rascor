using Rascor.Application.Models.Procore;

namespace Rascor.Application.Interfaces.Procore;

public interface IProcoreSitesSync
{
    Task<ProcoreSyncResult> SyncSitesAsync(CancellationToken cancellationToken = default);
}
