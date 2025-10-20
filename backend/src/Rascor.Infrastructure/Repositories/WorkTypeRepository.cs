using Microsoft.EntityFrameworkCore;
using Rascor.Domain.Entities;
using Rascor.Domain.Repositories;
using Rascor.Infrastructure.Data;

namespace Rascor.Infrastructure.Repositories;

public class WorkTypeRepository : IWorkTypeRepository
{
    private readonly RascorDbContext _context;

    public WorkTypeRepository(RascorDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkType>> GetAllActiveAsync()
    {
        return await _context.WorkTypes
            .Where(w => w.IsActive)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<WorkType?> GetByIdAsync(string id)
    {
        return await _context.WorkTypes.FindAsync(id);
    }

    public async Task<WorkType> CreateAsync(WorkType workType)
    {
        _context.WorkTypes.Add(workType);
        await _context.SaveChangesAsync();
        return workType;
    }

    public async Task<WorkType> UpdateAsync(WorkType workType)
    {
        workType.UpdatedAt = DateTime.UtcNow;
        _context.WorkTypes.Update(workType);
        await _context.SaveChangesAsync();
        return workType;
    }

    public async Task DeleteAsync(string id)
    {
        var workType = await GetByIdAsync(id);
        if (workType != null)
        {
            workType.IsActive = false;
            await UpdateAsync(workType);
        }
    }
}