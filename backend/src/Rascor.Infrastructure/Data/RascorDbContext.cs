using Microsoft.EntityFrameworkCore;
using Rascor.Domain;
using Rascor.Domain.Entities;

namespace Rascor.Infrastructure.Data;

public class RascorDbContext : DbContext
{
    public RascorDbContext(DbContextOptions<RascorDbContext> options)
        : base(options)
    {
    }

    // Existing tables
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<GeofenceEvent> GeofenceEvents => Set<GeofenceEvent>();
    public DbSet<SyncTracker> SyncTrackers => Set<SyncTracker>();
    public DbSet<ProcoreToken> ProcoreTokens { get; set; }
    public DbSet<ProcoreSyncLog> ProcoreSyncLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure each entity type in separate methods for clarity
        ConfigureSiteEntity(modelBuilder);
        ConfigureGeofenceEventEntity(modelBuilder);
        ConfigureSyncTrackerEntity(modelBuilder);

        ConfigureProcoreToken(modelBuilder);
        ConfigureProcoreSyncLog(modelBuilder);
    }

    private void ConfigureSiteEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Site>(entity =>
        {
            entity.ToTable("sites");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasPrecision(10, 8).IsRequired();
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasPrecision(11, 8).IsRequired();
            entity.Property(e => e.AutoTriggerRadiusMeters).HasColumnName("auto_trigger_radius_meters").IsRequired().HasDefaultValue(50);
            entity.Property(e => e.ManualTriggerRadiusMeters).HasColumnName("manual_trigger_radius_meters").IsRequired().HasDefaultValue(150);

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
            // ADD these indexes
            entity.HasIndex(e => e.ProcoreProjectId).IsUnique().HasFilter("procore_project_id IS NOT NULL");

            entity.HasIndex(e => e.ProjectNumber);
            entity.HasIndex(e => e.IsActive);
        });
    }

    private void ConfigureGeofenceEventEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GeofenceEvent>(entity =>
        {
            entity.ToTable("geofence_events");
            entity.HasKey(e => e.Id);
            
            // Column mappings with constraints
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.SiteId).HasColumnName("site_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(10).IsRequired();
            entity.Property(e => e.TriggerMethod).HasColumnName("trigger_method").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Timestamp).HasColumnName("timestamp").IsRequired();
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasPrecision(10, 8);
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasPrecision(11, 8);

            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SiteId);
            entity.HasIndex(e => e.Timestamp);
        });
    }

    private void ConfigureSyncTrackerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncTracker>(entity =>
        {
            entity.HasKey(e => e.EntityName);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.LastError).HasMaxLength(1000);
        });
    }

    private void ConfigureProcoreToken(ModelBuilder modelBuilder)
    {
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
    }

    private void ConfigureProcoreSyncLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcoreSyncLog>(entity =>
        {
            entity.ToTable("procore_sync_logs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").IsRequired();
            entity.Property(e => e.StartedAt).HasColumnName("started_at").IsRequired();
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.SitesAdded).HasColumnName("sites_added");
            entity.Property(e => e.SitesUpdated).HasColumnName("sites_updated");
            entity.Property(e => e.SitesDeactivated).HasColumnName("sites_deactivated");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.Details).HasColumnName("details");
        });
    }
}
