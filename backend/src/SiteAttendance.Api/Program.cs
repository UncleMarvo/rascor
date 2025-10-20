using SiteAttendance.Application;
using SiteAttendance.Domain;
using SiteAttendance.Infrastructure;
using SiteAttendance.Infrastructure.Data;
using SiteAttendance.Infrastructure.Repositories;
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
builder.Services.AddDbContext<SiteAttendanceDbContext>(options =>
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

// Providers (swappable)
builder.Services.AddSingleton<IPushProvider, MockPushProvider>();
builder.Services.AddSingleton<IEmailProvider, ConsoleEmailProvider>();

// Application handlers
builder.Services.AddScoped<LogGeofenceEventHandler>();
builder.Services.AddScoped<GetMobileBootstrap>();

var app = builder.Build();

// Initialize database and seed data (with error handling for production)
try
{
    using (var scope = app.Services.CreateScope())
    {
        var scopeLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var db = scope.ServiceProvider.GetRequiredService<SiteAttendanceDbContext>();
        
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
    Service = "SiteAttendance API",
    Version = "1.0.0-MVP",
    Endpoints = new[]
    {
        "GET /config/mobile?userId={id}",
        "POST /events/geofence",
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
