using Rascor.Domain.Entities;
using Rascor.Domain.Repositories;
using Rascor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Rascor.Infrastructure.Repositories;

public class DatabaseSyncTracker : ISyncTracker
{
    private readonly RascorDbContext _dbContext;

    public DatabaseSyncTracker(RascorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DateTime> GetLastSyncTimeAsync(string entityName)
    {
        var tracker = await _dbContext.SyncTrackers
            .FirstOrDefaultAsync(s => s.EntityName == entityName);

        // If no tracker exists, default to 30 days ago
        // This means the first sync will pick up all events from the last 30 days
        var defaultLastSync = DateTime.UtcNow.AddDays(-30);
        var lastSync = tracker?.LastSyncTime ?? defaultLastSync;
        
        // Log if using default (no tracker record exists)
        if (tracker == null)
        {
            // Note: Can't use ILogger here without DI, but this is logged in the sync service
        }
        
        return lastSync;
    }

    public async Task UpdateLastSyncTimeAsync(string entityName, DateTime syncTime)
    {
        var tracker = await _dbContext.SyncTrackers
            .FirstOrDefaultAsync(s => s.EntityName == entityName);

        if (tracker == null)
        {
            tracker = new SyncTracker
            {
                EntityName = entityName,
                LastSyncTime = syncTime,
                LastSuccessfulSync = syncTime
            };
            _dbContext.SyncTrackers.Add(tracker);
        }
        else
        {
            tracker.LastSyncTime = syncTime;
            tracker.LastSuccessfulSync = syncTime;
            tracker.LastError = null;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task RecordSyncErrorAsync(string entityName, string error)
    {
        var tracker = await _dbContext.SyncTrackers
            .FirstOrDefaultAsync(s => s.EntityName == entityName);

        if (tracker != null)
        {
            tracker.LastError = error;
            await _dbContext.SaveChangesAsync();
        }
    }
}
