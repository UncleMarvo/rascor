using Microsoft.EntityFrameworkCore;
using SiteAttendance.Domain;
using SiteAttendance.Infrastructure.Data;

namespace SiteAttendance.Infrastructure.Repositories;

public class EfGeofenceEventRepository : IGeofenceEventRepository
{
    private readonly SiteAttendanceDbContext _db;

    public EfGeofenceEventRepository(SiteAttendanceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(GeofenceEvent evt, CancellationToken ct = default)
    {
        _db.GeofenceEvents.Add(evt);
        await _db.SaveChangesAsync(ct);
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
