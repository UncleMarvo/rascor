using SQLite;

namespace Rascor.App.Data;

/// <summary>
/// SQLite entity for offline event queue
/// </summary>
[Table("queued_events")]
public class QueuedGeofenceEvent
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string UserId { get; set; } = string.Empty;

    [Indexed]
    public string SiteId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateTime Timestamp { get; set; }

    public int RetryCount { get; set; }

    public DateTime? LastRetryAt { get; set; }

    public bool IsSynced { get; set; }
}
