using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rascor.Domain.Repositories;
using Rascor.Infrastructure.Data;
using System.Text.Json;

namespace Rascor.Infrastructure.ExternalServices;

public class ZohoSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ZohoSyncService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(15);

    public ZohoSyncService(
        IServiceProvider serviceProvider,
        ILogger<ZohoSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Zoho Sync Service started");

        // Wait 30 seconds before first sync to allow app to fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformSyncAsync(stoppingToken);
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Zoho sync");
                // Wait 5 minutes on error before retrying
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task PerformSyncAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RascorDbContext>();
        var syncTracker = scope.ServiceProvider.GetRequiredService<ISyncTracker>();
        var zohoClient = scope.ServiceProvider.GetRequiredService<ZohoCreatorClient>();

        var lastSync = await syncTracker.GetLastSyncTimeAsync("geofence_events");
        var now = DateTime.UtcNow;

        _logger.LogInformation("Starting sync from {LastSync} to {Now}", lastSync, now);

        try
        {
            // Sync each entity type
            await SyncGeofenceEventsAsync(dbContext, zohoClient, lastSync, now, ct);
            // Update sync timestamp
            await syncTracker.UpdateLastSyncTimeAsync("geofence_events", now);

            _logger.LogInformation("Sync completed successfully");
        }
        catch (Exception ex)
        {
            await syncTracker.RecordSyncErrorAsync("geofence_events", ex.Message);
            throw;
        }
    }

    private async Task SyncGeofenceEventsAsync(
        RascorDbContext db,
        ZohoCreatorClient zohoClient,
        DateTime lastSync,
        DateTime now,
        CancellationToken ct)
    {
        // Add timeout and limit records
        var newEvents = await db.GeofenceEvents
            .Where(e => e.Timestamp > lastSync && e.Timestamp <= now)
            .OrderBy(e => e.Timestamp)
            .Take(100) // Limit to 100 records per sync
            .AsNoTracking() // Performance improvement
            .ToListAsync(ct);

        if (!newEvents.Any())
        {
            _logger.LogInformation("No new geofence events to sync");
            return;
        }

        _logger.LogInformation("Syncing {Count} geofence events", newEvents.Count);

        var batches = newEvents.Chunk(10); // Smaller batches

        foreach (var batch in batches)
        {
            var zohoRecords = batch.Select(e => new
            {
                ID = $"evt_{e.Id.ToString().Replace("-", "").Substring(0, 15)}",
                User_ID = e.UserId,
                User_Name2 = e.UserId ?? "",
                Site_ID = e.SiteId,  // Empty this too
                Site_Name1 = e.SiteId ?? "",  // Put the ID in the name field instead
                Event_Type1 = e.EventType ?? "",
                Timestamp = e.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
                Latitude1 = Math.Round(e.Latitude, 4),
                Longitude1 = Math.Round(e.Longitude, 4),
                Trigger_Method1 = e.TriggerMethod ?? ""
            }).ToList<object>();

            // ADD THIS LINE:
            _logger.LogWarning("🚀🚀🚀 SYNC STARTING AT 095211 - BATCH SIZE: {Count}", zohoRecords.Count);
            _logger.LogWarning("Sending to Zoho: {Record}", JsonSerializer.Serialize(zohoRecords.First()));

            await zohoClient.UpsertRecordsAsync("Sample_Activity_Data_Report1", zohoRecords, ct);
            _logger.LogInformation("Synced batch of {Count} events", batch.Length);
        }
    }
}

