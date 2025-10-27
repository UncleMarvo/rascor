namespace Rascor.App.Core.Models;

public class RamsPhoto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public string LocalFilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public bool IsUploaded { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string? ServerUrl { get; set; } // URL if uploaded to server
}
