using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rascor.Application.Interfaces.Procore;
using Rascor.Infrastructure.Configuration.Procore;
using Rascor.Infrastructure.ExternalServices.Procore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rascor.Infrastructure.BackgroundServices;

public class ProcoreSyncHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProcoreConfiguration _config;
    private readonly ILogger<ProcoreSyncHostedService> _logger;

    public ProcoreSyncHostedService(
        IServiceProvider serviceProvider,
        IOptions<ProcoreConfiguration> config,
        ILogger<ProcoreSyncHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.SyncEnabled)
        {
            _logger.LogInformation("Procore sync is disabled in configuration");
            return;
        }

        _logger.LogInformation(
            "Procore sync service started. Sync interval: {Interval} minutes",
            _config.SyncIntervalMinutes);

        // Wait a bit before the first sync to let the app start up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformSyncAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Procore sync");
            }

            // Wait for the next sync interval
            var delay = TimeSpan.FromMinutes(_config.SyncIntervalMinutes);
            _logger.LogInformation("Next Procore sync in {Minutes} minutes", _config.SyncIntervalMinutes);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when service is stopping
                break;
            }
        }

        _logger.LogInformation("Procore sync service stopped");
    }

    private async Task PerformSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IProcoreSitesSync>();

        try
        {
            _logger.LogInformation("Starting scheduled Procore sync...");
            var result = await syncService.SyncSitesAsync(cancellationToken);

            _logger.LogInformation(
                "Scheduled sync completed. Added: {Added}, Updated: {Updated}, Deactivated: {Deactivated}",
                result.SitesAdded, result.SitesUpdated, result.SitesDeactivated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled Procore sync failed");
            // Don't rethrow - let the service continue running
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Procore sync service is stopping...");
        await base.StopAsync(cancellationToken);
    }
}