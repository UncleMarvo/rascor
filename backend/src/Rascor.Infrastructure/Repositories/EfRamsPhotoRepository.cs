using Microsoft.EntityFrameworkCore;
using Rascor.Domain;
using Rascor.Domain.Entities;
using Rascor.Infrastructure.Data;

namespace Rascor.Infrastructure.Repositories;

public class EfRamsPhotoRepository : IRamsPhotoRepository
{
    private readonly RascorDbContext _context;

    public EfRamsPhotoRepository(RascorDbContext context)
    {
        _context = context;
    }

    public async Task<RamsPhoto> AddAsync(RamsPhoto photo, CancellationToken ct = default)
    {
        _context.RamsPhotos.Add(photo);
        await _context.SaveChangesAsync(ct);
        return photo;
    }

    public async Task<List<RamsPhoto>> GetByUserIdAsync(string userId, int limit = 50, CancellationToken ct = default)
    {
        return await _context.RamsPhotos
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CapturedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<RamsPhoto>> GetBySiteIdAsync(string siteId, CancellationToken ct = default)
    {
        return await _context.RamsPhotos
            .Where(p => p.SiteId == siteId)
            .OrderByDescending(p => p.CapturedAt)
            .ToListAsync(ct);
    }
}