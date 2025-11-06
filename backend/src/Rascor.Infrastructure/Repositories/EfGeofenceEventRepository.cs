using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rascor.Domain;
using Rascor.Domain.Entities;
using Rascor.Infrastructure.Data;

namespace Rascor.Infrastructure.Repositories;

public class EfGeofenceEventRepository : IGeofenceEventRepository
{
    private readonly RascorDbContext _db;
    private readonly ILogger<EfGeofenceEventRepository>? _logger;

    public EfGeofenceEventRepository(RascorDbContext db, ILogger<EfGeofenceEventRepository>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AddAsync(GeofenceEvent evt, CancellationToken ct = default)
    {
        _db.GeofenceEvents.Add(evt);
        
        try
        {
            var rowsAffected = await _db.SaveChangesAsync(ct);
            
            // Verify the event was actually saved by querying it back
            // This helps diagnose if events are being saved but not visible to other queries
            var savedEvent = await _db.GeofenceEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == evt.Id, ct);
            
            if (savedEvent == null)
            {
                _logger?.LogError(
                    "⚠️ CRITICAL: Event was saved (RowsAffected={RowsAffected}) but cannot be queried back! " +
                    "EventId={EventId}, UserId={UserId}, SiteId={SiteId}. " +
                    "This suggests a transaction isolation or connection issue.",
                    rowsAffected, evt.Id, evt.UserId, evt.SiteId);
            }
            else
            {
                _logger?.LogInformation(
                    "✅ GeofenceEvent saved to database: EventId={EventId}, UserId={UserId}, SiteId={SiteId}, " +
                    "RowsAffected={RowsAffected}, Timestamp={Timestamp}, Verified in DB={Verified}",
                    evt.Id, evt.UserId, evt.SiteId, rowsAffected, savedEvent.Timestamp, savedEvent != null);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "❌ Failed to save GeofenceEvent to database: EventId={EventId}, UserId={UserId}, SiteId={SiteId}",
                evt.Id, evt.UserId, evt.SiteId);
            throw;
        }
    }

    public async Task<GeofenceEvent?> GetLastEventForDeviceAtSiteAsync(string userId, string siteId)
    {
        return await _db.GeofenceEvents
            .Where(e => e.UserId == userId && e.SiteId == siteId)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<List<GeofenceEvent>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _db.GeofenceEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<List<GeofenceEvent>> GetBySiteIdAsync(string siteId, CancellationToken ct = default)
    {
        return await _db.GeofenceEvents
            .Where(e => e.SiteId == siteId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);
    }
}
