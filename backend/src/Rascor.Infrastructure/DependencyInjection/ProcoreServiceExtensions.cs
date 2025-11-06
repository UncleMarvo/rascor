using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Rascor.Application.Interfaces.Procore;
using Rascor.Domain.Entities;
using Rascor.Infrastructure.BackgroundServices;
using Rascor.Infrastructure.Configuration.Procore;
using Rascor.Infrastructure.ExternalServices.Procore;

namespace Rascor.Infrastructure.DepedencyInjection;

/// <summary>
/// Extension methods for registering Procore services
/// Add this to your Program.cs or Startup.cs
/// </summary>
public static class ProcoreServiceExtensions
{
    /// <summary>
    /// Adds Procore integration services to the service collection
    /// </summary>
    public static IServiceCollection AddProcoreIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<ProcoreConfiguration>(
            configuration.GetSection(ProcoreConfiguration.SectionName));

        // Register HttpClient with retry policy for Token Manager
        services.AddHttpClient<IProcoreTokenManager, ProcoreTokenManager>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy());

        // Register HttpClient with retry policy for API Client
        services.AddHttpClient<IProcoreApiClient, ProcoreApiClient>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy());

        // Register services
        services.AddScoped<IProcoreTokenManager, ProcoreTokenManager>();
        services.AddScoped<IProcoreApiClient, ProcoreApiClient>();
        services.AddScoped<IProcoreSitesSync, ProcoreSitesSync>();

        // Register background service
        services.AddHostedService<ProcoreSyncHostedService>();

        return services;
    }

    /// <summary>
    /// Gets a retry policy with exponential backoff for HTTP requests
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // Handles 5xx and 408 errors
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempt (you can inject ILogger if needed)
                    Console.WriteLine(
                        $"Retry {retryCount} after {timespan.TotalSeconds}s due to: {outcome.Result?.StatusCode}");
                });
    }

    /// <summary>
    /// Adds Procore entities to the DbContext
    /// Add this to your ApplicationDbContext OnModelCreating method
    /// </summary>
    public static void ConfigureProcoreEntities(ModelBuilder modelBuilder)
    {
        // Configure ProcoreToken entity
        modelBuilder.Entity<ProcoreToken>(entity =>
        {
            entity.ToTable("procore_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccessToken).HasColumnName("access_token").IsRequired();
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token").IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        // Configure ProcoreSyncLog entity
        modelBuilder.Entity<ProcoreSyncLog>(entity =>
        {
            entity.ToTable("procore_sync_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StartedAt).HasColumnName("started_at").IsRequired();
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
            entity.Property(e => e.SitesAdded).HasColumnName("sites_added");
            entity.Property(e => e.SitesUpdated).HasColumnName("sites_updated");
            entity.Property(e => e.SitesDeactivated).HasColumnName("sites_deactivated");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.Details).HasColumnName("details");
        });

        // Configure Site entity (update existing configuration)
        modelBuilder.Entity<Site>(entity =>
        {
            entity.ToTable("sites");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50);
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Latitude).HasColumnName("latitude").IsRequired();
            entity.Property(e => e.Longitude).HasColumnName("longitude").IsRequired();
            entity.Property(e => e.AutoTriggerRadiusMeters).HasColumnName("auto_trigger_radius_meters").HasDefaultValue(50);
            entity.Property(e => e.ManualTriggerRadiusMeters).HasColumnName("manual_trigger_radius_meters").HasDefaultValue(150);

            // Procore fields
            entity.Property(e => e.ProcoreProjectId).HasColumnName("procore_project_id");
            entity.Property(e => e.ProjectNumber).HasColumnName("project_number").HasMaxLength(50);
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(300);
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.County).HasColumnName("county").HasMaxLength(100);
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(10);
            entity.Property(e => e.Zip).HasColumnName("zip").HasMaxLength(20);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.LastSyncedAt).HasColumnName("last_synced_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => e.ProcoreProjectId).IsUnique().HasFilter("procore_project_id IS NOT NULL");
            entity.HasIndex(e => e.ProjectNumber);
            entity.HasIndex(e => e.IsActive);
        });
    }
}