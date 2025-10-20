namespace Rascor.Domain.Entities;

/// <summary>
/// Assigns specific work to a user at a site
/// Can be assigned by PM or self-selected by user
/// </summary>
public class WorkAssignment
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string SiteId { get; set; } = null!;
    public string WorkTypeId { get; set; } = null!;
    
    /// <summary>
    /// PM/Admin who assigned the work (null if self-selected)
    /// </summary>
    public string? AssignedBy { get; set; }
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedStartDate { get; set; }
    public DateTime? ExpectedEndDate { get; set; }
    
    /// <summary>
    /// Status: 'assigned', 'in_progress', 'completed', 'cancelled'
    /// </summary>
    public string Status { get; set; } = "assigned";
    
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Site Site { get; set; } = null!;
    public WorkType WorkType { get; set; } = null!;
    public ICollection<RamsAcceptance> RamsAcceptances { get; set; } = new List<RamsAcceptance>();
}