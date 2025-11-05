namespace Rascor.Domain.Entities;

public class SyncTracker
{
    public string EntityName { get; set; } = string.Empty;
    public DateTime LastSyncTime { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public string? LastError { get; set; }
}
