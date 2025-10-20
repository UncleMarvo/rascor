namespace Rascor.Domain.Entities;

/// <summary>
/// Represents a category of work that requires specific RAMS
/// Examples: Electrical Installation, Plumbing, General Construction, Site Inspection
/// </summary>
public class WorkType
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<RamsDocument> RamsDocuments { get; set; } = new List<RamsDocument>();
    public ICollection<WorkAssignment> WorkAssignments { get; set; } = new List<WorkAssignment>();
}