using Microsoft.EntityFrameworkCore;
using SiteAttendance.Domain;
using SiteAttendance.Infrastructure.Data;

namespace SiteAttendance.Infrastructure.Repositories;

public class EfAssignmentRepository : IAssignmentRepository
{
    private readonly SiteAttendanceDbContext _db;

    public EfAssignmentRepository(SiteAttendanceDbContext db)
    {
        _db = db;
    }

    public async Task<List<Site>> GetAssignedSitesAsync(string userId, CancellationToken ct = default)
    {
        // Get site IDs assigned to this user
        var siteIds = await _db.Assignments
            .Where(a => a.UserId == userId)
            .Select(a => a.SiteId)
            .ToListAsync(ct);

        // Get the actual site objects
        return await _db.Sites
            .Where(s => siteIds.Contains(s.Id))
            .ToListAsync(ct);
    }

    public async Task AssignAsync(string userId, string siteId, CancellationToken ct = default)
    {
        // Check if assignment already exists
        var exists = await _db.Assignments
            .AnyAsync(a => a.UserId == userId && a.SiteId == siteId, ct);

        if (!exists)
        {
            var assignment = new UserAssignment(userId, siteId, DateTimeOffset.UtcNow);
            _db.Assignments.Add(assignment);
            await _db.SaveChangesAsync(ct);
        }
    }
}
