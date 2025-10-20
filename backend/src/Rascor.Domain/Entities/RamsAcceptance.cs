namespace Rascor.Domain.Entities;

/// <summary>
/// Records that a user has read, understood, and signed a RAMS document
/// Includes digital signature and checklist responses
/// </summary>
public class RamsAcceptance
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string SiteId { get; set; } = null!;
    public string? WorkAssignmentId { get; set; }
    public string RamsDocumentId { get; set; } = null!;
    
    /// <summary>
    /// Base64 encoded signature image
    /// </summary>
    public string SignatureData { get; set; } = null!;
    
    public string? IpAddress { get; set; }
    public string? DeviceInfo { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// JSON containing user's responses to interactive checklist
    /// </summary>
    public string? ChecklistResponses { get; set; }

    // Navigation properties
    public Site Site { get; set; } = null!;
    public WorkAssignment? WorkAssignment { get; set; }
    public RamsDocument RamsDocument { get; set; } = null!;
}