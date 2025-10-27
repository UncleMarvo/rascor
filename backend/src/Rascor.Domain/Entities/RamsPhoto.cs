using System.ComponentModel.DataAnnotations.Schema;

namespace Rascor.Domain.Entities;

[Table("rams_photos")]
public class RamsPhoto
{
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("site_id")]
    public string SiteId { get; set; } = string.Empty;

    [Column("captured_at")]
    public DateTime CapturedAt { get; set; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; }

    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [Column("file_size_bytes")]
    public long FileSizeBytes { get; set; }

    [Column("original_filename")]
    public string? OriginalFilename { get; set; }
}