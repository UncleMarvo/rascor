using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rascor.Application.Interfaces.Procore;
using Rascor.Application.Models.Procore;
using Rascor.Domain.Entities;
using Rascor.Infrastructure.Configuration.Procore;
using Rascor.Infrastructure.Data;

namespace Rascor.Infrastructure.ExternalServices.Procore;

public class ProcoreSitesSync : IProcoreSitesSync
{
    private readonly IProcoreApiClient _apiClient;
    private readonly RascorDbContext _dbContext;
    private readonly ProcoreConfiguration _config;
    private readonly ILogger<ProcoreSitesSync> _logger;

    public ProcoreSitesSync(
        IProcoreApiClient apiClient,
        RascorDbContext dbContext,
        IOptions<ProcoreConfiguration> config,
        ILogger<ProcoreSitesSync> logger)
    {
        _apiClient = apiClient;
        _dbContext = dbContext;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Syncs projects from Procore to the sites table
    /// </summary>
    public async Task<ProcoreSyncResult> SyncSitesAsync(CancellationToken cancellationToken = default)
    {
        var result = new ProcoreSyncResult
        {
            StartedAt = DateTime.UtcNow
        };

        // Create sync log entry
        var syncLog = new ProcoreSyncLog
        {
            StartedAt = result.StartedAt,
            Status = "running"
        };
        _dbContext.ProcoreSyncLogs.Add(syncLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Starting Procore sites sync...");

            // Get the last successful sync time for incremental sync
            var lastSync = await _dbContext.Sites
                .Where(s => s.LastSyncedAt != null)
                .OrderByDescending(s => s.LastSyncedAt)
                .Select(s => s.LastSyncedAt)
                .FirstOrDefaultAsync(cancellationToken);

            // Fetch projects from Procore
            var projects = await _apiClient.GetProjectsAsync(lastSync, cancellationToken);

            // Filter active projects if configured
            if (_config.SyncOnlyActiveProjects)
            {
                projects = projects.Where(p => p.Active).ToList();
                _logger.LogInformation(
                    "Filtered to {ActiveCount} active projects out of {TotalCount}",
                    projects.Count, projects.Count);
            }

            // Process each project
            foreach (var project in projects)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await UpsertSiteFromProjectAsync(project, result, cancellationToken);
            }

            // Mark inactive sites (projects that were active but are no longer in Procore)
            if (_config.SyncOnlyActiveProjects)
            {
                await DeactivateMissingSitesAsync(projects, result, cancellationToken);
            }

            result.CompletedAt = DateTime.UtcNow;
            result.Success = true;

            // Update sync log
            syncLog.CompletedAt = result.CompletedAt;
            syncLog.Status = "success";
            syncLog.SitesAdded = result.SitesAdded;
            syncLog.SitesUpdated = result.SitesUpdated;
            syncLog.SitesDeactivated = result.SitesDeactivated;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Procore sync completed successfully. Added: {Added}, Updated: {Updated}, Deactivated: {Deactivated}, Duration: {Duration}s",
                result.SitesAdded, result.SitesUpdated, result.SitesDeactivated,
                (result.CompletedAt.Value - result.StartedAt).TotalSeconds);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;

            // Update sync log with error
            syncLog.CompletedAt = result.CompletedAt;
            syncLog.Status = "failed";
            syncLog.ErrorMessage = ex.Message;
            syncLog.Details = ex.ToString();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Procore sync failed");
            throw;
        }
    }

    private async Task UpsertSiteFromProjectAsync(
        ProcoreProject project,
        ProcoreSyncResult result,
        CancellationToken cancellationToken)
    {
        var existingSite = await _dbContext.Sites
            .FirstOrDefaultAsync(s => s.ProcoreProjectId == project.Id, cancellationToken);

        if (existingSite != null)
        {
            // Update existing site
            existingSite.Name = project.Name;
            existingSite.DisplayName = project.DisplayName;
            existingSite.ProjectNumber = project.ProjectNumber;
            existingSite.Address = project.Address;
            existingSite.City = project.City;
            existingSite.County = project.County;
            existingSite.CountryCode = project.CountryCode;
            existingSite.Zip = project.Zip;
            existingSite.Latitude = project.Latitude ?? existingSite.Latitude;
            existingSite.Longitude = project.Longitude ?? existingSite.Longitude;
            existingSite.IsActive = project.Active;
            existingSite.LastSyncedAt = DateTime.UtcNow;
            existingSite.UpdatedAt = DateTime.UtcNow;

            result.SitesUpdated++;

            _logger.LogDebug(
                "Updated site {SiteId} from Procore project {ProjectId}",
                existingSite.Id, project.Id);
        }
        else
        {
            if (!project.Latitude.HasValue || !project.Longitude.HasValue)
            {
                _logger.LogWarning(
                    "Skipping Procore project {ProjectId} ({Name}) - missing coordinates",
                    project.Id, project.Name ?? "(null)");
                return;
            }

            // Generate next SITEXXX ID
            var nextSiteId = await GetNextSiteIdAsync(cancellationToken);

            var newSite = new Site
            {
                Id = nextSiteId,  // Use SITEXXX format
                Name = project.Name,
                DisplayName = project.DisplayName,
                ProjectNumber = project.ProjectNumber,
                Address = project.Address,
                City = project.City,
                County = project.County,
                CountryCode = project.CountryCode,
                Zip = project.Zip,
                Latitude = project.Latitude.Value,
                Longitude = project.Longitude.Value,
                ProcoreProjectId = project.Id,
                IsActive = project.Active,
                AutoTriggerRadiusMeters = 50,
                ManualTriggerRadiusMeters = 150,
                LastSyncedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Sites.Add(newSite);
            result.SitesAdded++;

            _logger.LogDebug(
                "Added new site {SiteId} from Procore project {ProjectId}",
                newSite.Id, project.Id);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GetNextSiteIdAsync(CancellationToken cancellationToken)
    {
        // Get all site IDs that match SITEXXX pattern
        var siteIds = await _dbContext.Sites
            .Where(s => s.Id.StartsWith("SITE"))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        // Extract numeric parts and find the max
        var maxNumber = 0;
        foreach (var id in siteIds)
        {
            // Remove "SITE" prefix and try to parse the number
            var numericPart = id.Substring(4);
            if (int.TryParse(numericPart, out var number))
            {
                if (number > maxNumber)
                {
                    maxNumber = number;
                }
            }
        }

        // Increment and format with leading zeros
        var nextNumber = maxNumber + 1;
        var nextId = $"SITE{nextNumber:D3}";  // D3 = 3 digits with leading zeros

        _logger.LogDebug("Generated next site ID: {SiteId} (previous max was {MaxNumber})", nextId, maxNumber);

        return nextId;
    }

    private async Task DeactivateMissingSitesAsync(
        List<ProcoreProject> projects,
        ProcoreSyncResult result,
        CancellationToken cancellationToken)
    {
        // Get all sites with Procore IDs
        var procoreSites = await _dbContext.Sites
            .Where(s => s.ProcoreProjectId != null && s.IsActive)
            .ToListAsync(cancellationToken);

        var activeProjectIds = projects.Select(p => p.Id).ToHashSet();

        foreach (var site in procoreSites)
        {
            if (!activeProjectIds.Contains(site.ProcoreProjectId!.Value))
            {
                // This site is no longer in the active projects list - deactivate it
                site.IsActive = false;
                site.UpdatedAt = DateTime.UtcNow;
                result.SitesDeactivated++;

                _logger.LogDebug(
                    "Deactivated site {SiteId} - Procore project {ProjectId} no longer active",
                    site.Id, site.ProcoreProjectId);
            }
        }

        if (result.SitesDeactivated > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}