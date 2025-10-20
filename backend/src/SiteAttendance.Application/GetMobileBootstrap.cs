using SiteAttendance.Domain;
using Microsoft.Extensions.Logging;

namespace SiteAttendance.Application;

public class GetMobileBootstrap
{
    private readonly IAssignmentRepository _assignmentRepo;
    private readonly ISettingsRepository _settingsRepo;
    private readonly ILogger<GetMobileBootstrap> _logger;

    public GetMobileBootstrap(
        IAssignmentRepository assignmentRepo,
        ISettingsRepository settingsRepo,
        ILogger<GetMobileBootstrap> logger)
    {
        _assignmentRepo = assignmentRepo;
        _settingsRepo = settingsRepo;
        _logger = logger;
    }

    public async Task<MobileBootstrapResponse> ExecuteAsync(string userId, CancellationToken ct = default)
    {
        var config = await _settingsRepo.GetConfigAsync(ct);
        var assignedSites = await _assignmentRepo.GetAssignedSitesAsync(userId, ct);

        // Trim to MaxConcurrentSites
        var sites = assignedSites.Take(config.MaxConcurrentSites).ToList();

        var etag = $"{userId}-{DateTime.UtcNow.Ticks}";

        _logger.LogInformation(
            "Mobile bootstrap for user {UserId}: {SiteCount} sites, config={@Config}",
            userId, sites.Count, config);

        return new MobileBootstrapResponse(config, sites, etag);
    }
}
