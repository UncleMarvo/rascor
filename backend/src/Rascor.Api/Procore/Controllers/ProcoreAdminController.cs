using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rascor.Application.Interfaces.Procore;
using Rascor.Infrastructure.Data;

namespace Rascor.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/procore")]
public class ProcoreAdminController : ControllerBase
{
    private readonly IProcoreSitesSync _syncService;
    private readonly RascorDbContext _dbContext;
    private readonly ILogger<ProcoreAdminController> _logger;

    public ProcoreAdminController(
        IProcoreSitesSync syncService,
        RascorDbContext dbContext,
        ILogger<ProcoreAdminController> logger)
    {
        _syncService = syncService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("sync-now")]
    public async Task<IActionResult> TriggerSyncNow(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Manual Procore sync triggered via API");
            var result = await _syncService.SyncSitesAsync(cancellationToken);

            return Ok(new
            {
                success = result.Success,
                message = "Sync completed successfully",
                details = new
                {
                    sitesAdded = result.SitesAdded,
                    sitesUpdated = result.SitesUpdated,
                    sitesDeactivated = result.SitesDeactivated,
                    startedAt = result.StartedAt,
                    completedAt = result.CompletedAt,
                    durationSeconds = (result.CompletedAt.Value - result.StartedAt).TotalSeconds
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual Procore sync failed");
            return StatusCode(500, new
            {
                success = false,
                message = "Sync failed",
                error = ex.Message
            });
        }
    }

    [HttpGet("sync-status")]
    public async Task<IActionResult> GetSyncStatus(CancellationToken cancellationToken)
    {
        var lastSync = await _dbContext.ProcoreSyncLogs
            .OrderByDescending(l => l.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var totalSites = await _dbContext.Sites.CountAsync(cancellationToken);
        var procoreSites = await _dbContext.Sites.CountAsync(s => s.ProcoreProjectId != null, cancellationToken);
        var activeSites = await _dbContext.Sites.CountAsync(s => s.IsActive, cancellationToken);

        return Ok(new
        {
            lastSync = lastSync != null ? new
            {
                startedAt = lastSync.StartedAt,
                completedAt = lastSync.CompletedAt,
                status = lastSync.Status,
                sitesAdded = lastSync.SitesAdded,
                sitesUpdated = lastSync.SitesUpdated,
                sitesDeactivated = lastSync.SitesDeactivated,
                errorMessage = lastSync.ErrorMessage
            } : null,
            statistics = new
            {
                totalSites,
                procoreSites,
                activeSites,
                inactiveSites = totalSites - activeSites,
                manualSites = totalSites - procoreSites
            }
        });
    }

    [HttpGet("sync-history")]
    public async Task<IActionResult> GetSyncHistory(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 100)
        {
            return BadRequest("Limit must be between 1 and 100");
        }

        var logs = await _dbContext.ProcoreSyncLogs
            .OrderByDescending(l => l.StartedAt)
            .Take(limit)
            .Select(l => new
            {
                id = l.Id,
                startedAt = l.StartedAt,
                completedAt = l.CompletedAt,
                status = l.Status,
                sitesAdded = l.SitesAdded,
                sitesUpdated = l.SitesUpdated,
                sitesDeactivated = l.SitesDeactivated,
                errorMessage = l.ErrorMessage
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            count = logs.Count,
            history = logs
        });
    }

    [HttpGet("sites")]
    public async Task<IActionResult> GetProcoreSites(
        [FromQuery] bool? active = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 500)
        {
            return BadRequest("Limit must be between 1 and 500");
        }

        var query = _dbContext.Sites
            .Where(s => s.ProcoreProjectId != null);

        if (active.HasValue)
        {
            query = query.Where(s => s.IsActive == active.Value);
        }

        var sites = await query
            .OrderByDescending(s => s.LastSyncedAt)
            .Take(limit)
            .Select(s => new
            {
                id = s.Id,
                name = s.Name,
                displayName = s.DisplayName,
                projectNumber = s.ProjectNumber,
                procoreProjectId = s.ProcoreProjectId,
                address = s.Address,
                city = s.City,
                latitude = s.Latitude,
                longitude = s.Longitude,
                isActive = s.IsActive,
                lastSyncedAt = s.LastSyncedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            count = sites.Count,
            sites
        });
    }
}