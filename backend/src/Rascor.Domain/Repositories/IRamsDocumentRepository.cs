using Rascor.Domain.Entities;

namespace Rascor.Domain.Repositories;

public interface IRamsDocumentRepository
{
    Task<RamsDocument?> GetCurrentVersionAsync(string workTypeId);
    Task<RamsDocument?> GetByIdAsync(string id);
    Task<IEnumerable<RamsDocument>> GetAllVersionsAsync(string workTypeId);
    Task<RamsDocument> CreateAsync(RamsDocument document);
    Task<RamsDocument> UpdateAsync(RamsDocument document);
    Task DeactivateOldVersionsAsync(string workTypeId, int newVersion);
}