using Rascor.Domain.Entities;

namespace Rascor.Domain.Repositories;

public interface IWorkTypeRepository
{
    Task<IEnumerable<WorkType>> GetAllActiveAsync();
    Task<WorkType?> GetByIdAsync(string id);
    Task<WorkType> CreateAsync(WorkType workType);
    Task<WorkType> UpdateAsync(WorkType workType);
    Task DeleteAsync(string id);
}