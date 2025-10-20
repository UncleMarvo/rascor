using Rascor.Domain.Entities;

namespace Rascor.Domain.Repositories;

public interface IWorkAssignmentRepository
{
    Task<IEnumerable<WorkAssignment>> GetByUserIdAsync(string userId);
    Task<IEnumerable<WorkAssignment>> GetBySiteIdAsync(string siteId);
    Task<WorkAssignment?> GetByIdAsync(string id);
    Task<WorkAssignment?> GetByUserSiteWorkAsync(string userId, string siteId, string workTypeId);
    Task<IEnumerable<WorkAssignment>> GetAvailableWorkAtSiteAsync(string siteId);
    Task<WorkAssignment> CreateAsync(WorkAssignment assignment);
    Task<WorkAssignment> UpdateAsync(WorkAssignment assignment);
    Task DeleteAsync(string id);
}