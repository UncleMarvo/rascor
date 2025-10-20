using Microsoft.EntityFrameworkCore;
using SiteAttendance.Domain;
using SiteAttendance.Infrastructure.Data;

namespace SiteAttendance.Infrastructure.Repositories;

public class EfSiteRepository : ISiteRepository
{
    private readonly SiteAttendanceDbContext _db;

    public EfSiteRepository(SiteAttendanceDbContext db)
    {
        _db = db;
    }

    public async Task<Site?> GetByIdAsync(string siteId, CancellationToken ct = default)
    {
        return await _db.Sites
            .FirstOrDefaultAsync(s => s.Id == siteId, ct);
    }

    public async Task<List<Site>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Sites.ToListAsync(ct);
    }

    public async Task AddAsync(Site site, CancellationToken ct = default)
    {
        _db.Sites.Add(site);
        await _db.SaveChangesAsync(ct);
    }
}
