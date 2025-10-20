using Microsoft.EntityFrameworkCore;
using Rascor.Domain.Entities;
using Rascor.Domain.Repositories;
using Rascor.Infrastructure.Data;

namespace Rascor.Infrastructure.Repositories;

public class WorkAssignmentRepository : IWorkAssignmentRepository
{
    private readonly RascorDbContext _context;

    public WorkAssignmentRepository(RascorDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkAssignment>> GetByUserIdAsync(string userId)
    {
        return await _context.WorkAssignments
            .Include(w => w.Site)
            .Include(w => w.WorkType)
            .Where(w => w.UserId == userId && w.Status != "cancelled")
            .OrderByDescending(w => w.AssignedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<WorkAssignment>> GetBySiteIdAsync(string siteId)
    {
        return await _context.WorkAssignments
            .Include(w => w.WorkType)
            .Where(w => w.SiteId == siteId && w.Status != "cancelled")
            .ToListAsync();
    }

    public async Task<WorkAssignment?> GetByIdAsync(string id)
    {
        return await _context.WorkAssignments
            .Include(w => w.Site)
            .Include(w => w.WorkType)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<WorkAssignment?> GetByUserSiteWorkAsync(string userId, string siteId, string workTypeId)
    {
        return await _context.WorkAssignments
            .Include(w => w.Site)
            .Include(w => w.WorkType)
            .FirstOrDefaultAsync(w => 
                w.UserId == userId && 
                w.SiteId == siteId && 
                w.WorkTypeId == workTypeId &&
                w.Status != "cancelled");
    }

    public async Task<IEnumerable<WorkAssignment>> GetAvailableWorkAtSiteAsync(string siteId)
    {
        // Get distinct work types available at this site
        return await _context.WorkAssignments
            .Include(w => w.WorkType)
            .Where(w => w.SiteId == siteId && w.Status != "cancelled")
            .GroupBy(w => w.WorkTypeId)
            .Select(g => g.First())
            .ToListAsync();
    }

    public async Task<WorkAssignment> CreateAsync(WorkAssignment assignment)
    {
        _context.WorkAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<WorkAssignment> UpdateAsync(WorkAssignment assignment)
    {
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.WorkAssignments.Update(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task DeleteAsync(string id)
    {
        var assignment = await GetByIdAsync(id);
        if (assignment != null)
        {
            assignment.Status = "cancelled";
            await UpdateAsync(assignment);
        }
    }
}