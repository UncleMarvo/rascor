using Microsoft.EntityFrameworkCore;
using Rascor.Application;
using Rascor.Application.DTOs;
using Rascor.Application.Services;
using Rascor.Domain;
using Rascor.Domain.Repositories;
using Rascor.Infrastructure;
using Rascor.Infrastructure.Data;
using Rascor.Infrastructure.ExternalServices;
using Rascor.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpClient factory (required by Infrastructure)
builder.Services.AddHttpClient();

// Domain services
builder.Services.AddSingleton<IClock, SystemClock>();

// Get connection string - Azure App Service connection strings are automatically added to Configuration
// They appear as ConnectionStrings:DefaultConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


Console.WriteLine($"🔍 Connection String: {connectionString}");
Console.WriteLine($"🔍 Connection String Length: {connectionString?.Length}");

// Log what we found (without exposing password)
var logger = builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
if (!string.IsNullOrEmpty(connectionString))
{
    // Mask password for logging
    var safeConnectionString = System.Text.RegularExpressions.Regex.Replace(
        connectionString, 
        @"Password=([^;]+)", 
        "Password=***");
    logger.LogInformation("Using connection string: {ConnectionString}", safeConnectionString);
}
else
{
    logger.LogError("No connection string found!");
    
    // Debug: List all configuration keys
    var allKeys = builder.Configuration.AsEnumerable()
        .Where(kv => kv.Key.Contains("Connection", StringComparison.OrdinalIgnoreCase))
        .Select(kv => kv.Key);
    logger.LogInformation("Available connection-related config keys: {Keys}", string.Join(", ", allKeys));
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string 'DefaultConnection' not found. Check Azure App Service connection strings configuration.");
}

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<RascorDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        ).CommandTimeout(120)
    ));

// Repositories (EF Core + PostgreSQL)
builder.Services.AddScoped<IGeofenceEventRepository, EfGeofenceEventRepository>();
builder.Services.AddScoped<ISiteRepository, EfSiteRepository>();
builder.Services.AddSingleton<ISettingsRepository, InMemorySettingsRepository>(); // Keep in-memory for now

// Providers (swappable)
builder.Services.AddSingleton<IPushProvider, MockPushProvider>();
builder.Services.AddSingleton<IEmailProvider, ConsoleEmailProvider>();

// Application handlers
builder.Services.AddScoped<LogGeofenceEventHandler>();
builder.Services.AddScoped<GetMobileBootstrap>();

// Zoho Integration Services
builder.Services.AddHttpClient<ZohoCreatorClient>();
builder.Services.AddSingleton<ZohoCreatorClient>();

// Sync Services
builder.Services.AddScoped<ISyncTracker, DatabaseSyncTracker>();

// Background Service (only if Zoho sync is enabled)
var enableZohoSync = builder.Configuration.GetValue<bool>("Sync:EnableSync", false);
if (enableZohoSync)
{
    builder.Services.AddHostedService<ZohoSyncService>();
}

// Manual Checks
builder.Services.AddScoped<IGeofenceService, GeofenceService>();

var app = builder.Build();

// Initialize database and seed data (with error handling for production)
// Only seed if ENABLE_DATABASE_SEED environment variable is set to "true"
var shouldSeed = builder.Configuration.GetValue<string>("ENABLE_DATABASE_SEED")?.ToLowerInvariant() == "true";

try
{
    using (var scope = app.Services.CreateScope())
    {
        var scopeLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var db = scope.ServiceProvider.GetRequiredService<RascorDbContext>();
        
        scopeLogger.LogInformation("Starting database migration...");
        
        // Apply any pending migrations
        await db.Database.MigrateAsync();
        
        scopeLogger.LogInformation("Database migration complete.");
        
        // Seed initial data only if enabled
        if (shouldSeed)
        {
            scopeLogger.LogInformation("ENABLE_DATABASE_SEED is set to 'true'. Seeding data...");
            await DbInitializer.SeedAsync(db);
            scopeLogger.LogInformation("Database seeding complete!");
        }
        else
        {
            scopeLogger.LogInformation("Database seeding skipped (ENABLE_DATABASE_SEED not set to 'true').");
            scopeLogger.LogInformation("To enable seeding, set the ENABLE_DATABASE_SEED environment variable to 'true'.");
        }
        
        scopeLogger.LogInformation("Database initialization complete!");
    }
}
catch (Exception ex)
{
    var errorLogger = app.Services.GetRequiredService<ILogger<Program>>();
    errorLogger.LogError(ex, "An error occurred while migrating or seeding the database.");
    
    // In production, we want the app to start even if migration fails
    // The error will be logged and can be fixed later
    if (app.Environment.IsDevelopment())
    {
        throw; // In development, fail fast
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Enable Swagger in production for testing
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ============================================================================
// ENDPOINTS
// ============================================================================

app.MapGet("/", () => new
{
    Service = "Rascor API",
    Version = "3.0.0-Simplified",
    Endpoints = new[]
    {
        "GET /config/mobile?userId={id}",
        "POST /events/geofence",
        "GET /api/geofence-events/user/{userId}/today",
        "POST /api/geofence-events/manual-checkin",
        "POST /api/geofence-events/manual-checkout",
        "GET /swagger"
    }
});

app.MapGet("/config/mobile", async (
    string userId,
    GetMobileBootstrap handler,
    CancellationToken ct) =>
{
    var response = await handler.ExecuteAsync(userId, ct);
    return Results.Ok(response);
})
.WithName("GetMobileConfig")
.WithOpenApi();

app.MapPost("/events/geofence", async (
    GeofenceEventRequest request,
    LogGeofenceEventHandler handler,
    CancellationToken ct) =>
{
    var evt = await handler.HandleAsync(
        request.UserId,
        request.SiteId,
        request.EventType,
        request.Latitude,
        request.Longitude,
        ct);
    
    return Results.Ok(evt);
})
.WithName("LogGeofenceEvent")
.WithOpenApi();

// ============================================================================
// CHECK IN & OUT FALLBACK
// ============================================================================

// Manual Check-In
app.MapPost("/api/geofence-events/manual-checkin", async (
    ManualCheckInRequest request,
    IGeofenceService geofenceService) =>
{
    var result = await geofenceService.ManualCheckInAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("ManualCheckIn")
.WithOpenApi();

// Manual Check-Out
app.MapPost("/api/geofence-events/manual-checkout", async (
    ManualCheckOutRequest request,
    IGeofenceService geofenceService) =>
{
    var result = await geofenceService.ManualCheckOutAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("ManualCheckOut")
.WithOpenApi();


// ============================================================================
// HISTORY - Get today's geofence events for a user
// ============================================================================
app.MapGet("/api/geofence-events/user/{userId}/today", async (
    string userId,
    RascorDbContext dbContext) =>
{
    try
    {
        var today = DateTime.UtcNow.Date;  // CHANGED: Use UtcNow instead of Today
        var tomorrow = today.AddDays(1);

        var events = await dbContext.GeofenceEvents
            .Where(e => e.UserId == userId &&
                        e.Timestamp >= today &&
                        e.Timestamp < tomorrow)
            .Join(dbContext.Sites,
                  e => e.SiteId,
                  s => s.Id,
                  (e, s) => new
                  {
                      EventType = e.EventType,
                      SiteName = s.Name,
                      Timestamp = e.Timestamp,
                      TriggerMethod = e.TriggerMethod
                  })
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();

        return Results.Ok(events);
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
})
.WithName("GetTodaysEvents")
.WithOpenApi();

// ==============================================
// TEST SYNC ENDPOINTS
// ==============================================
app.MapGet("/api/test/sync-now", async (
    IServiceProvider serviceProvider) =>
{
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RascorDbContext>();

    // Count events
    var totalEvents = await dbContext.GeofenceEvents.CountAsync();
    var lastSync = new DateTime(2025, 10, 28, 14, 55, 49, DateTimeKind.Utc);
    var eventsSinceLastSync = await dbContext.GeofenceEvents
        .Where(e => e.Timestamp > lastSync)
        .CountAsync();

    return Results.Ok(new
    {
        totalEventsInDatabase = totalEvents,
        eventsSinceLastSync = eventsSinceLastSync,
        lastSyncTime = lastSync,
        message = totalEvents == 0
            ? "No events in database to sync"
            : $"Found {eventsSinceLastSync} events to sync"
    });
});

app.MapPost("/api/test/force-sync", async (
    IServiceProvider serviceProvider,
    ILogger<Program> logger) =>
{
    try
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RascorDbContext>();
        var syncTracker = scope.ServiceProvider.GetRequiredService<ISyncTracker>();
        var zohoClient = scope.ServiceProvider.GetRequiredService<ZohoCreatorClient>();

        var lastSync = await syncTracker.GetLastSyncTimeAsync("geofence_events");
        var now = DateTime.UtcNow;

        logger.LogInformation("Force sync triggered. Last sync: {LastSync}", lastSync);

        // Removed .Include() calls
        var events = await dbContext.GeofenceEvents
            .Where(e => e.Timestamp > lastSync && e.Timestamp <= now)
            .OrderBy(e => e.Timestamp)
            .Take(100)
            .ToListAsync();

        if (!events.Any())
        {
            var totalEvents = await dbContext.GeofenceEvents.CountAsync();
            return Results.Ok(new
            {
                success = false,
                message = "No new events to sync",
                lastSync = lastSync,
                totalEventsInDatabase = totalEvents,
                searchedFrom = lastSync,
                searchedTo = now
            });
        }

        logger.LogInformation("Found {Count} events to sync to Zoho", events.Count);

        // Send IDs only, no names
        var zohoRecords = events.Select(e => new
        {
            ID = e.Id,
            User_ID = e.UserId,
            Site_ID = e.SiteId,
            Event_Type1 = e.EventType,
            Timestamp = e.Timestamp.ToString("dd-MMM-yyyy HH:mm:ss"),
            Latitude1 = e.Latitude,
            Longitude1 = e.Longitude,
            Trigger_Method1 = e.TriggerMethod
        }).ToList<object>();

        logger.LogInformation("Syncing to Zoho form: Sample_Activity_Data_Report1");

        var success = await zohoClient.UpsertRecordsAsync(
            "Sample_Activity_Data_Report1",
            zohoRecords,
            default);

        if (success)
        {
            await syncTracker.UpdateLastSyncTimeAsync("geofence_events", now);
            logger.LogInformation("✅ Sync successful! Synced {Count} records", events.Count);

            return Results.Ok(new
            {
                success = true,
                message = "Sync successful to Sample_Activity_Data_Report1 form!",
                recordsSynced = events.Count,
                formName = "Sample_Activity_Data_Report1",
                lastSyncTime = now,
                syncedEventIds = events.Select(e => e.Id).Take(10).ToList()
            });
        }
        else
        {
            await syncTracker.RecordSyncErrorAsync("geofence_events", "Zoho API returned false");
            logger.LogError("❌ Zoho sync failed");

            return Results.Ok(new
            {
                success = false,
                message = "Sync failed - Zoho API returned false. Check form name and credentials."
            });
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Exception during force sync");
        return Results.Ok(new
        {
            success = false,
            error = ex.Message,
            stackTrace = ex.StackTrace?.Split('\n').Take(10).ToArray()
        });
    }
})
.WithName("ForceSyncNow")
.AllowAnonymous();


app.MapGet("/api/test/show-zoho-url", (IConfiguration config) =>
{
    var ownerName = config["Zoho:OwnerName"];
    var appName = config["Zoho:AppName"];
    var dataCenter = config["Zoho:DataCenter"] ?? "eu";

    var urlConstructed = $"https://creator.zoho.{dataCenter}/api/v2.1/data/{ownerName}/{appName}/form/Sample_Activity_Data_Report1";

    return Results.Ok(new
    {
        ownerName = ownerName,
        appName = appName,
        dataCenter = dataCenter,
        formName = "Sample_Activity_Data_Report1",
        fullUrl = urlConstructed,
        yourAppUrl = "https://creatorapp.zoho.eu/quantumbuild1/rascor-site-attendance"
    });
}).AllowAnonymous();

Console.WriteLine("🔥🔥🔥 CANARY BUILD: 2025-10-30 08:15 🔥🔥🔥");

app.Run();

// ============================================================================
// DTOs
// ============================================================================

public record GeofenceEventRequest(
    string UserId,
    string SiteId,
    string EventType, // "Enter" or "Exit"
    double? Latitude,
    double? Longitude
);

