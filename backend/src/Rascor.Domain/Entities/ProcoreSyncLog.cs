namespace Rascor.Domain.Entities;

/// <summary>
/// Logs each Procore sync operation
/// </summary>
public class ProcoreSyncLog
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "running"; // running, success, failed
    public int? SitesAdded { get; set; }
    public int? SitesUpdated { get; set; }
    public int? SitesDeactivated { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Details { get; set; } // JSON
}
