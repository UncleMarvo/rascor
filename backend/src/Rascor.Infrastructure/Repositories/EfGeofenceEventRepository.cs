using Microsoft.EntityFrameworkCore;
using Rascor.Domain;
using Rascor.Domain.Entities;
using Rascor.Infrastructure.Data;

namespace Rascor.Infrastructure.Repositories;

public class EfGeofenceEventRepository : IGeofenceEventRepository
{
    private readonly RascorDbContext _db;

    public EfGeofenceEventRepository(RascorDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(GeofenceEvent evt, CancellationToken ct = default)
    {
        _db.GeofenceEvents.Add(evt);
        await _db.SaveChangesAsync(ct);
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
