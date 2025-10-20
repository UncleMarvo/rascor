using Rascor.Domain.Entities;

namespace Rascor.Domain.Repositories;

public interface IRamsAcceptanceRepository
{
    Task<RamsAcceptance?> GetTodaysAcceptanceAsync(string userId, string siteId, string workAssignmentId);
    Task<IEnumerable<RamsAcceptance>> GetByUserIdAsync(string userId);
    Task<IEnumerable<RamsAcceptance>> GetBySiteIdAsync(string siteId);
    Task<bool> HasSignedTodayAsync(string userId, string siteId, string workAssignmentId);
    Task<RamsAcceptance> CreateAsync(RamsAcceptance acceptance);
    Task<IEnumerable<RamsAcceptance>> GetComplianceReportAsync(DateTime startDate, DateTime endDate);
}