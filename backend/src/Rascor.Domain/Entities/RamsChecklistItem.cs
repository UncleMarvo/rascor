namespace Rascor.Domain.Entities;

/// <summary>
/// Individual checklist item within an interactive RAMS document
/// </summary>
public class RamsChecklistItem
{
    public string Id { get; set; } = null!;
    public string RamsDocumentId { get; set; } = null!;
    
    /// <summary>
    /// Section name for grouping items (e.g., "Personal Protective Equipment")
    /// </summary>
    public string? Section { get; set; }
    
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// Type of form item: 'checkbox', 'text', 'signature', 'heading'
    /// </summary>
    public string ItemType { get; set; } = null!;
    
    public string Label { get; set; } = null!;
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// JSON validation rules: regex, min/max length, etc.
    /// </summary>
    public string? ValidationRules { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public RamsDocument RamsDocument { get; set; } = null!;
}