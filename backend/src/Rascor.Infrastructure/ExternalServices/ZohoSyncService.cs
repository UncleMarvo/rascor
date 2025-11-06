using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(15);

    public ZohoSyncService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ZohoSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
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
                _logger.LogInformation("⏰ Zoho Sync Service: Starting sync cycle at {Time}", DateTime.UtcNow);
                await PerformSyncAsync(stoppingToken);
                _logger.LogInformation("⏰ Zoho Sync Service: Sync cycle completed. Next sync in {Interval} minutes at approximately {NextSyncTime}", 
                    _syncInterval.TotalMinutes, DateTime.UtcNow.Add(_syncInterval));
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during Zoho sync - will retry in 5 minutes");
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

        // Log database connection info to verify we're using the same database
        var databaseName = dbContext.Database.GetDbConnection().Database;
        var dataSource = dbContext.Database.GetDbConnection().DataSource;
        _logger.LogWarning(
            "🔄 Zoho Sync starting. Database: {Database}, DataSource: {DataSource}",
            databaseName, dataSource);

        var lastSync = await syncTracker.GetLastSyncTimeAsync("geofence_events");
        var now = DateTime.UtcNow;
        
        // Check if sync tracker exists in database (reuse existing scope)
        var trackerExists = await dbContext.SyncTrackers
            .AnyAsync(s => s.EntityName == "geofence_events", ct);
        
        if (!trackerExists)
        {
            _logger.LogWarning(
                "⚠️ No sync tracker record found in database - using default lastSync of 30 days ago. " +
                "This will sync ALL events from the last 30 days. LastSync={LastSync}, Now={Now}, TimeRange={TimeRange} hours",
                lastSync, now, (now - lastSync).TotalHours);
        }
        else
        {
            _logger.LogWarning(
                "🔄 Starting sync from {LastSync} to {Now} (time range: {TimeRange} hours, {TimeRangeMinutes} minutes)", 
                lastSync, now, (now - lastSync).TotalHours, (now - lastSync).TotalMinutes);
        }

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

    private string GetZohoDateFormat()
    {
        // Get the date-time format from configuration, with fallback to common formats
        // Format must match the format configured in Zoho Creator app settings
        // Common formats: "dd-MMM-yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "MM/dd/yyyy HH:mm:ss"
        // To find your app's format: Zoho Creator > Your App > Settings > Date and Time Format
        var format = _configuration["Zoho:DateTimeFormat"];
        
        if (string.IsNullOrWhiteSpace(format))
        {
            // Default fallback - try the most common format first
            // If this doesn't work, set Zoho:DateTimeFormat in app settings to match your Zoho app
            _logger.LogWarning(
                "⚠️ Zoho:DateTimeFormat not configured. Using default format 'dd-MMM-yyyy HH:mm:ss'. " +
                "To set the correct format: Add 'Zoho:DateTimeFormat' to app settings matching your Zoho Creator app's date-time format.");
            return "dd-MMM-yyyy HH:mm:ss";
        }
        
        _logger.LogInformation("Using Zoho date-time format: {Format}", format);
        return format;
    }

    private async Task SyncGeofenceEventsAsync(
        RascorDbContext db,
        ZohoCreatorClient zohoClient,
        DateTime lastSync,
        DateTime now,
        CancellationToken ct)
    {
        // Log detailed sync information for debugging
        _logger.LogWarning(
            "🔍 SYNC QUERY: LastSync={LastSync}, Now={Now}, TimeRange={TimeRange} hours",
            lastSync, now, (now - lastSync).TotalHours);
        
        // First, get total count of events in database for debugging
        var totalEventsInDb = await db.GeofenceEvents.CountAsync(ct);
        _logger.LogWarning("📊 Total events in database: {Count}", totalEventsInDb);
        
        // Get events in the sync time range
        var newEvents = await db.GeofenceEvents
            .Where(e => e.Timestamp > lastSync && e.Timestamp <= now)
            .OrderBy(e => e.Timestamp)
            .Take(100) // Limit to 100 records per sync
            .AsNoTracking() // Performance improvement
            .ToListAsync(ct);

        if (!newEvents.Any())
        {
            // Log why no events were found
            var oldestEvent = await db.GeofenceEvents
                .OrderBy(e => e.Timestamp)
                .Select(e => e.Timestamp)
                .FirstOrDefaultAsync(ct);
            
            var newestEvent = await db.GeofenceEvents
                .OrderByDescending(e => e.Timestamp)
                .Select(e => e.Timestamp)
                .FirstOrDefaultAsync(ct);
            
            _logger.LogInformation(
                "No new geofence events to sync. LastSync={LastSync}, Now={Now}. " +
                "Database event range: Oldest={Oldest}, Newest={Newest}",
                lastSync, now, oldestEvent, newestEvent);
            return;
        }

        // Log detailed information about events being synced
        // Include event IDs and timestamps to verify these events exist in the database
        _logger.LogWarning(
            "📤 Syncing {Count} geofence events. Event IDs: {EventIds}. " +
            "Event timestamps range: {Oldest} to {Newest}. " +
            "First 3 Event Details: {EventDetails}",
            newEvents.Count,
            string.Join(", ", newEvents.Select(e => e.Id).Take(5)),
            newEvents.First().Timestamp,
            newEvents.Last().Timestamp,
            string.Join(" | ", newEvents.Take(3).Select(e => 
                $"Id={e.Id}, UserId={e.UserId}, SiteId={e.SiteId}, Type={e.EventType}, Time={e.Timestamp:yyyy-MM-dd HH:mm:ss}")));
        
        // CRITICAL: Verify these events still exist in the database after querying
        // This helps diagnose if events are being deleted or if there's a transaction issue
        var eventIds = newEvents.Select(e => e.Id).ToList();
        var stillExists = await db.GeofenceEvents
            .AsNoTracking()
            .Where(e => eventIds.Contains(e.Id))
            .CountAsync(ct);
        
        if (stillExists != newEvents.Count)
        {
            _logger.LogError(
                "⚠️ CRITICAL: Query found {FoundCount} events but only {StillExistsCount} still exist in database! " +
                "This suggests events are being deleted or there's a transaction isolation issue. " +
                "Event IDs queried: {EventIds}",
                newEvents.Count, stillExists, string.Join(", ", eventIds.Take(10)));
        }

        // Get the date-time format that matches Zoho Creator app settings
        var dateFormat = GetZohoDateFormat();
        
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
                // Format timestamp to match Zoho Creator app's date-time format setting
                // Format is configurable via Zoho:DateTimeFormat app setting
                Timestamp = e.Timestamp.ToString(dateFormat),
                Latitude1 = Math.Round(e.Latitude, 4),
                Longitude1 = Math.Round(e.Longitude, 4),
                Trigger_Method1 = e.TriggerMethod ?? ""
            }).ToList<object>();

            var syncSuccess = await zohoClient.UpsertRecordsAsync("Sample_Activity_Data_Report1", zohoRecords, ct);
            if (syncSuccess)
            {
                _logger.LogInformation("✅ Successfully synced batch of {Count} events to Zoho", batch.Length);
            }
            else
            {
                _logger.LogError("❌ Failed to sync batch of {Count} events to Zoho - check logs above for details", batch.Length);
                throw new Exception($"Failed to sync batch to Zoho - Zoho API returned error. Check logs for details.");
            }
        }
    }
}

