using Rascor.Domain;
using Rascor.Domain.Repositories;
using Rascor.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Rascor.Application;

public class GetMobileBootstrap
{
    private readonly IAssignmentRepository _assignmentRepo;
    private readonly ISettingsRepository _settingsRepo;
    private readonly IWorkAssignmentRepository _workAssignmentRepo;
    private readonly IRamsAcceptanceRepository _ramsAcceptanceRepo;
    private readonly ILogger<GetMobileBootstrap> _logger;

    public GetMobileBootstrap(
        IAssignmentRepository assignmentRepo,
        ISettingsRepository settingsRepo,
        IWorkAssignmentRepository workAssignmentRepo,
        IRamsAcceptanceRepository ramsAcceptanceRepo,
        ILogger<GetMobileBootstrap> logger)
    {
        _assignmentRepo = assignmentRepo;
        _settingsRepo = settingsRepo;
        _workAssignmentRepo = workAssignmentRepo;
        _ramsAcceptanceRepo = ramsAcceptanceRepo;
        _logger = logger;
    }

    public async Task<MobileBootstrapDto> ExecuteAsync(
        string userId, 
        CancellationToken ct = default)
    {
        // Get basic config and sites
        var config = await _settingsRepo.GetConfigAsync(ct);
        var assignedSites = await _assignmentRepo.GetAssignedSitesAsync(userId, ct);
        var sites = assignedSites
            .Take(config.MaxConcurrentSites)
            .Select(s => new SiteDto(
                s.Id,
                s.Name,
                s.Latitude,
                s.Longitude,
                s.RadiusMeters
            ))
            .ToList();

        // Get work assignments
        var workAssignments = await _workAssignmentRepo.GetByUserIdAsync(userId, ct);
        var workAssignmentDtos = workAssignments
            .Select(a => new WorkAssignmentDto(
                a.Id,
                a.UserId,
                a.SiteId,
                a.WorkTypeId,
                a.Status,
                a.AssignedAt,
                a.ExpectedStartDate,
                a.ExpectedEndDate,
                a.Notes
            ))
            .ToList();

        // Check which RAMS have been signed today
        var ramsSigned = new Dictionary<string, bool>();
        foreach (var assignment in workAssignments)
        {
            var hasSigned = await _ramsAcceptanceRepo.HasAcceptedTodayAsync(
                userId,
                assignment.WorkTypeId,
                ct);
            ramsSigned[assignment.WorkTypeId] = hasSigned;
        }

        var remoteConfig = new RemoteConfigDto(
            config.PollIntervalSeconds,
            config.EnableOfflineMode
        );

        _logger.LogInformation(
            "Mobile bootstrap for user {UserId}: {SiteCount} sites, {AssignmentCount} assignments",
            userId, 
            sites.Count, 
            workAssignments.Count);

        return new MobileBootstrapDto(
            remoteConfig,
            sites,
            workAssignmentDtos,
            ramsSigned
        );
    }
}
