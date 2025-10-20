using Rascor.Application;
using Rascor.Application.DTOs;
using Rascor.Domain;
using Rascor.Domain.Repositories;
using Rascor.Infrastructure;
using Rascor.Infrastructure.Data;
using Rascor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();

// Initialize database and seed data (with error handling for production)
try
{
    using (var scope = app.Services.CreateScope())
    {
        var scopeLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var db = scope.ServiceProvider.GetRequiredService<RascorDbContext>();
        
        scopeLogger.LogInformation("Starting database migration...");
        
        // Apply any pending migrations
        await db.Database.MigrateAsync();
        
        scopeLogger.LogInformation("Database migration complete. Seeding data...");
        
        // Seed initial data
        await DbInitializer.SeedAsync(db);
        
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
