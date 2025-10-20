using Microsoft.Extensions.Logging;
using SQLite;
using Rascor.App.Core;
using Rascor.App.Data;

namespace Rascor.App.Services;

/// <summary>
/// Manages offline event queue with SQLite
/// </summary>
public class EventQueueService
{
    private readonly ILogger<EventQueueService> _logger;
    private readonly BackendApi _api;
    private readonly string _dbPath;
    private SQLiteAsyncConnection? _database;

    public EventQueueService(ILogger<EventQueueService> logger, BackendApi api)
    {
        _logger = logger;
        _api = api;
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "events.db");
    }

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database != null)
            return _database;

        _database = new SQLiteAsyncConnection(_dbPath);
        await _database.CreateTableAsync<QueuedGeofenceEvent>();
        _logger.LogInformation("📦 SQLite database initialized at {Path}", _dbPath);
        return _database;
    }

    /// <summary>
    /// Queue an event for later sync
    /// </summary>
    public async Task QueueEventAsync(GeofenceEventRequest request)
    {
        var db = await GetDatabaseAsync();
        
        var queuedEvent = new QueuedGeofenceEvent
        {
            UserId = request.UserId,
            SiteId = request.SiteId,
            EventType = request.EventType,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Timestamp = DateTime.UtcNow,
            RetryCount = 0,
            IsSynced = false
        };

        await db.InsertAsync(queuedEvent);
        _logger.LogWarning("💾 Queued event offline: {EventType} for site {SiteId}", request.EventType, request.SiteId);
    }

    /// <summary>
    /// Try to post event, queue if offline
    /// </summary>
    public async Task<bool> PostOrQueueEventAsync(GeofenceEventRequest request)
    {
        try
        {
            // Try to post directly
            await _api.PostGeofenceEventAsync(request);
            _logger.LogInformation("✅ Event posted directly to backend");
            return true;
        }
        catch (Exception ex)
        {
            // Any error (network, timeout, etc.) - queue for later
            _logger.LogWarning(ex, "⚠️ Cannot reach server, queueing event offline");
            await QueueEventAsync(request);
            return false;
        }
    }

    /// <summary>
    /// Sync all queued events to backend
    /// </summary>
    public async Task<(int synced, int failed)> SyncQueuedEventsAsync()
    {
        var db = await GetDatabaseAsync();
        var pendingEvents = await db.Table<QueuedGeofenceEvent>()
            .Where(e => !e.IsSynced)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        if (pendingEvents.Count == 0)
        {
            _logger.LogInformation("📭 No queued events to sync");
            return (0, 0);
        }

        _logger.LogInformation("📤 Syncing {Count} queued events...", pendingEvents.Count);

        int synced = 0;
        int failed = 0;

        foreach (var queuedEvent in pendingEvents)
        {
            try
            {
                var request = new GeofenceEventRequest(
                    queuedEvent.UserId,
                    queuedEvent.SiteId,
                    queuedEvent.EventType,
                    queuedEvent.Latitude,
                    queuedEvent.Longitude
                );

                await _api.PostGeofenceEventAsync(request);

                // Mark as synced
                queuedEvent.IsSynced = true;
                await db.UpdateAsync(queuedEvent);
                synced++;
                
                _logger.LogInformation("✅ Synced queued event {Id}: {EventType} for {SiteId}", 
                    queuedEvent.Id, queuedEvent.EventType, queuedEvent.SiteId);
            }
            catch (Exception ex)
            {
                queuedEvent.RetryCount++;
                queuedEvent.LastRetryAt = DateTime.UtcNow;
                await db.UpdateAsync(queuedEvent);
                failed++;
                
                _logger.LogWarning(ex, "❌ Failed to sync event {Id} (retry {RetryCount})", 
                    queuedEvent.Id, queuedEvent.RetryCount);
            }
        }

        _logger.LogInformation("📊 Sync complete: {Synced} synced, {Failed} failed", synced, failed);
        return (synced, failed);
    }

    /// <summary>
    /// Get count of pending events
    /// </summary>
    public async Task<int> GetPendingCountAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<QueuedGeofenceEvent>()
            .Where(e => !e.IsSynced)
            .CountAsync();
    }

    /// <summary>
    /// Clear old synced events (older than 30 days)
    /// </summary>
    public async Task CleanupOldEventsAsync()
    {
        var db = await GetDatabaseAsync();
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        
        var deleted = await db.ExecuteAsync(
            "DELETE FROM queued_events WHERE IsSynced = 1 AND Timestamp < ?",
            cutoffDate);

        if (deleted > 0)
        {
            _logger.LogInformation("🧹 Cleaned up {Count} old synced events", deleted);
        }
    }
}
