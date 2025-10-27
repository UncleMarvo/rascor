using Microsoft.EntityFrameworkCore;
using Rascor.Domain;
using Rascor.Domain.Entities;
using Rascor.Infrastructure.Data;

namespace Rascor.Infrastructure.Repositories;

public class EfSiteRepository : ISiteRepository
{
    private readonly RascorDbContext _db;

    public EfSiteRepository(RascorDbContext db)
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

    public async Task<List<Site>> GetSitesByDeviceIdAsync(string userId)
    {
        // For now, return all sites
        // TODO: Filter by device assignments when UserAssignment entity is added

        return await _db.Sites.ToListAsync();
    }
}
