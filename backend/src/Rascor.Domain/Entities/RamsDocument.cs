namespace Rascor.Domain.Entities;

/// <summary>
/// Risk Assessment and Method Statement document with versioning
/// </summary>
public class RamsDocument
{
    public string Id { get; set; } = null!;
    public string WorkTypeId { get; set; } = null!;
    public int Version { get; set; }
    public string Title { get; set; } = null!;
    
    /// <summary>
    /// Type of content: 'checklist', 'pdf', 'html'
    /// </summary>
    public string ContentType { get; set; } = null!;
    
    /// <summary>
    /// JSON for interactive checklist or HTML content
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    /// Azure Blob URL if PDF document
    /// </summary>
    public string? PdfBlobUrl { get; set; }
    
    /// <summary>
    /// Is this the currently active version?
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public WorkType WorkType { get; set; } = null!;
    public ICollection<RamsChecklistItem> ChecklistItems { get; set; } = new List<RamsChecklistItem>();
    public ICollection<RamsAcceptance> Acceptances { get; set; } = new List<RamsAcceptance>();
}