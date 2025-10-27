using Microsoft.EntityFrameworkCore;
using Rascor.Application;
using Rascor.Application.DTOs;
using Rascor.Application.Services;
using Rascor.Domain;
using Rascor.Domain.Repositories;
using Rascor.Infrastructure;
using Rascor.Infrastructure.Data;
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
        )
    ));

// Repositories (EF Core + PostgreSQL)
builder.Services.AddScoped<IGeofenceEventRepository, EfGeofenceEventRepository>();
builder.Services.AddScoped<ISiteRepository, EfSiteRepository>();
builder.Services.AddScoped<IAssignmentRepository, EfAssignmentRepository>();
builder.Services.AddSingleton<ISettingsRepository, InMemorySettingsRepository>(); // Keep in-memory for now

// RAMS Repositories
builder.Services.AddScoped<IWorkTypeRepository, WorkTypeRepository>();
builder.Services.AddScoped<IRamsDocumentRepository, RamsDocumentRepository>();
builder.Services.AddScoped<IWorkAssignmentRepository, WorkAssignmentRepository>();
builder.Services.AddScoped<IRamsAcceptanceRepository, RamsAcceptanceRepository>();

// Providers (swappable)
builder.Services.AddSingleton<IPushProvider, MockPushProvider>();
builder.Services.AddSingleton<IEmailProvider, ConsoleEmailProvider>();

// Application handlers
builder.Services.AddScoped<LogGeofenceEventHandler>();
builder.Services.AddScoped<GetMobileBootstrap>();

// RAMS Application handlers
builder.Services.AddScoped<GetWorkTypesHandler>();
builder.Services.AddScoped<GetWorkAssignmentsHandler>();
builder.Services.AddScoped<GetRamsDocumentHandler>();
builder.Services.AddScoped<SubmitRamsAcceptanceHandler>();
builder.Services.AddScoped<IRamsPhotoRepository, EfRamsPhotoRepository>();

builder.Services.AddScoped<RamsPhotoService>(sp =>
{
    var repository = sp.GetRequiredService<IRamsPhotoRepository>();
    var logger = sp.GetRequiredService<ILogger<RamsPhotoService>>();
    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "rams-photos");
    return new RamsPhotoService(repository, logger, uploadsDir);
});

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
    Version = "2.0.0-RAMS",
    Endpoints = new[]
    {
        "GET /config/mobile?userId={id}",
        "POST /events/geofence",
        "GET /api/work-types",
        "GET /api/assignments?userId={id}",
        "GET /api/rams/{id}",
        "GET /api/rams/work-type/{workTypeId}",
        "POST /api/rams/accept",
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
// RAMS ENDPOINTS
// ============================================================================

// GET /api/work-types - List all active work types
app.MapGet("/api/work-types", async (
    GetWorkTypesHandler handler,
    CancellationToken ct) =>
{
    var workTypes = await handler.ExecuteAsync(ct);
    return Results.Ok(workTypes);
})
.WithName("GetWorkTypes")
.WithOpenApi()
.WithTags("RAMS");

// GET /api/assignments - Get user's work assignments
app.MapGet("/api/assignments", async (
    string userId,
    GetWorkAssignmentsHandler handler,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.BadRequest(new { error = "userId is required" });
    }

    var assignments = await handler.ExecuteAsync(userId, ct);
    return Results.Ok(assignments);
})
.WithName("GetUserWorkAssignments")
.WithOpenApi()
.WithTags("RAMS");

// GET /api/rams/{id} - Get RAMS document with checklist
app.MapGet("/api/rams/{id}", async (
    string id,
    GetRamsDocumentHandler handler,
    CancellationToken ct) =>
{
    var document = await handler.ExecuteAsync(id, ct);
    
    if (document == null)
    {
        return Results.NotFound(new { error = $"RAMS document {id} not found" });
    }

    return Results.Ok(document);
})
.WithName("GetRamsDocument")
.WithOpenApi()
.WithTags("RAMS");

// GET /api/rams/work-type/{workTypeId} - Get current RAMS version for work type
app.MapGet("/api/rams/work-type/{workTypeId}", async (
    string workTypeId,
    GetRamsDocumentHandler handler,
    CancellationToken ct) =>
{
    var document = await handler.GetCurrentVersionAsync(workTypeId, ct);
    
    if (document == null)
    {
        return Results.NotFound(new { 
            error = $"No current RAMS document found for work type {workTypeId}" 
        });
    }

    return Results.Ok(document);
})
.WithName("GetCurrentRamsDocumentByWorkType")
.WithOpenApi()
.WithTags("RAMS");

// POST /api/rams/accept - Submit RAMS acceptance
app.MapPost("/api/rams/accept", async (
    CreateRamsAcceptanceRequest request,
    SubmitRamsAcceptanceHandler handler,
    CancellationToken ct) =>
{
    try
    {
        var acceptance = await handler.HandleAsync(request, ct);
        return Results.Created($"/api/rams/acceptances/{acceptance.Id}", acceptance);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("SubmitRamsAcceptance")
.WithOpenApi()
.WithTags("RAMS");


// ============================================================================
// RAMS PHOTO ENDPOINTS
// ============================================================================
app.MapPost("/api/rams-photos/upload", async (
    HttpRequest request,
    RamsPhotoService photoService,
    ILogger<Program> logger) =>
{
    try
    {
        var form = await request.ReadFormAsync();

        var userId = form["userId"].ToString();
        var siteId = form["siteId"].ToString();
        var capturedAtStr = form["capturedAt"].ToString();
        var file = form.Files["photo"];

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { success = false, message = "No file uploaded" });
        }

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(siteId))
        {
            return Results.BadRequest(new { success = false, message = "Missing userId or siteId" });
        }

        // Parse captured date
        if (!DateTime.TryParse(capturedAtStr, out var capturedAt))
        {
            capturedAt = DateTime.UtcNow;
        }

        // Delegate to service
        using var stream = file.OpenReadStream();
        var result = await photoService.UploadPhotoAsync(
            userId,
            siteId,
            capturedAt,
            stream,
            file.FileName,
            file.Length
        );

        if (result.Success)
        {
            return Results.Ok(new
            {
                success = true,
                message = result.Message,
                photoId = result.PhotoId,
                uploadedAt = result.UploadedAt
            });
        }
        else
        {
            return Results.BadRequest(new { success = false, message = result.Message });
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to upload RAMS photo");
        return Results.StatusCode(500);
    }
})
.WithName("UploadRamsPhoto")
.DisableAntiforgery()
.WithOpenApi();

// Get RAMS photos for a user
app.MapGet("/api/rams-photos/user/{userId}", async (
    string userId,
    RamsPhotoService photoService) =>
{
    try
    {
        var photos = await photoService.GetUserPhotosAsync(userId);

        return Results.Ok(photos.Select(p => new
        {
            p.Id,
            p.SiteId,
            p.CapturedAt,
            p.UploadedAt,
            p.FileSizeBytes,
            p.OriginalFilename
        }));
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
})
.WithName("GetUserRamsPhotos")
.WithOpenApi();

// Get RAMS photos for a site
app.MapGet("/api/rams-photos/site/{siteId}", async (
    string siteId,
    RamsPhotoService photoService) =>
{
    try
    {
        var photos = await photoService.GetSitePhotosAsync(siteId);

        return Results.Ok(photos.Select(p => new
        {
            p.Id,
            p.UserId,
            p.CapturedAt,
            p.UploadedAt,
            p.FileSizeBytes,
            p.OriginalFilename
        }));
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
})
.WithName("GetSiteRamsPhotos")
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
