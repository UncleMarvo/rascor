using Microsoft.EntityFrameworkCore;
using Rascor.Domain.Entities;
using Rascor.Domain.Repositories;
using Rascor.Infrastructure.Data;

namespace Rascor.Infrastructure.Repositories;

public class RamsDocumentRepository : IRamsDocumentRepository
{
    private readonly RascorDbContext _context;

    public RamsDocumentRepository(RascorDbContext context)
    {
        _context = context;
    }

    public async Task<RamsDocument?> GetCurrentVersionAsync(string workTypeId)
    {
        return await _context.RamsDocuments
            .Include(r => r.ChecklistItems.OrderBy(c => c.DisplayOrder))
            .Where(r => r.WorkTypeId == workTypeId && r.IsActive)
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<RamsDocument?> GetByIdAsync(string id)
    {
        return await _context.RamsDocuments
            .Include(r => r.ChecklistItems.OrderBy(c => c.DisplayOrder))
            .Include(r => r.WorkType)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<RamsDocument>> GetAllVersionsAsync(string workTypeId)
    {
        return await _context.RamsDocuments
            .Where(r => r.WorkTypeId == workTypeId)
            .OrderByDescending(r => r.Version)
            .ToListAsync();
    }

    public async Task<RamsDocument> CreateAsync(RamsDocument document)
    {
        _context.RamsDocuments.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<RamsDocument> UpdateAsync(RamsDocument document)
    {
        _context.RamsDocuments.Update(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task DeactivateOldVersionsAsync(string workTypeId, int newVersion)
    {
        var oldDocuments = await _context.RamsDocuments
            .Where(r => r.WorkTypeId == workTypeId && r.Version < newVersion)
            .ToListAsync();

        foreach (var doc in oldDocuments)
        {
            doc.IsActive = false;
            doc.EffectiveTo = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}