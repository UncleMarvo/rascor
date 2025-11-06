namespace Rascor.Application.Models.Procore;

public class ProcoreSyncResult
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Success { get; set; }
    public int SitesAdded { get; set; }
    public int SitesUpdated { get; set; }
    public int SitesDeactivated { get; set; }
    public string? ErrorMessage { get; set; }
}
