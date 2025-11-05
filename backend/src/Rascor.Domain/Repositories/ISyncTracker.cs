namespace Rascor.Domain.Repositories;

public interface ISyncTracker
{
    Task<DateTime> GetLastSyncTimeAsync(string entityName);
    Task UpdateLastSyncTimeAsync(string entityName, DateTime syncTime);
    Task RecordSyncErrorAsync(string entityName, string error);
}
