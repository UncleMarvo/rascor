namespace Rascor.Domain.Entities;

public class Site
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int AutoTriggerRadiusMeters { get; set; } = 100;
    public int ManualTriggerRadiusMeters { get; set; } = 150;

    // PROCORE SETTINGS
    public long? ProcoreProjectId { get; set; }
    public string? ProjectNumber { get; set; }
    public string? DisplayName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public string? CountryCode { get; set; }
    public string? Zip { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}