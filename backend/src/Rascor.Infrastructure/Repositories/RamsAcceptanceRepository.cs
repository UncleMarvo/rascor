using Microsoft.EntityFrameworkCore;
using Rascor.Domain.Entities;
using Rascor.Domain.Repositories;
using Rascor.Infrastructure.Data;

namespace Rascor.Infrastructure.Repositories;

public class RamsAcceptanceRepository : IRamsAcceptanceRepository
{
    private readonly RascorDbContext _context;

    public RamsAcceptanceRepository(RascorDbContext context)
    {
        _context = context;
    }

    public async Task<RamsAcceptance?> GetTodaysAcceptanceAsync(string userId, string siteId, string workAssignmentId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.RamsAcceptances
            .Include(r => r.RamsDocument)
            .Where(r => 
                r.UserId == userId && 
                r.SiteId == siteId && 
                r.WorkAssignmentId == workAssignmentId &&
                r.AcceptedAt >= today)
            .OrderByDescending(r => r.AcceptedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<RamsAcceptance>> GetByUserIdAsync(string userId)
    {
        return await _context.RamsAcceptances
            .Include(r => r.Site)
            .Include(r => r.RamsDocument)
            .Include(r => r.WorkAssignment)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.AcceptedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<RamsAcceptance>> GetBySiteIdAsync(string siteId)
    {
        return await _context.RamsAcceptances
            .Include(r => r.RamsDocument)
            .Include(r => r.WorkAssignment)
            .Where(r => r.SiteId == siteId)
            .OrderByDescending(r => r.AcceptedAt)
            .ToListAsync();
    }

    public async Task<bool> HasSignedTodayAsync(string userId, string siteId, string workAssignmentId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.RamsAcceptances
            .AnyAsync(r => 
                r.UserId == userId && 
                r.SiteId == siteId && 
                r.WorkAssignmentId == workAssignmentId &&
                r.AcceptedAt >= today);
    }

    public async Task<RamsAcceptance> CreateAsync(RamsAcceptance acceptance)
    {
        _context.RamsAcceptances.Add(acceptance);
        await _context.SaveChangesAsync();
        return acceptance;
    }

    public async Task<IEnumerable<RamsAcceptance>> GetComplianceReportAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.RamsAcceptances
            .Include(r => r.Site)
            .Include(r => r.RamsDocument)
            .Include(r => r.WorkAssignment)
            .Where(r => r.AcceptedAt >= startDate && r.AcceptedAt <= endDate)
            .OrderBy(r => r.AcceptedAt)
            .ToListAsync();
    }
}