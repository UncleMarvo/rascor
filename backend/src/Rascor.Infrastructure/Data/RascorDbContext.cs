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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureGeofenceEntities(modelBuilder);
        ConfigureExistingEntities(modelBuilder);
        ConfigureSyncTrackerEntities(modelBuilder);
    }

    private void ConfigureSyncTrackerEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncTracker>(entity =>
        {
            entity.HasKey(e => e.EntityName);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.LastError).HasMaxLength(1000);
        });
    }


    private void ConfigureGeofenceEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GeofenceEvent>(entity =>
        {
            entity.ToTable("geofence_events");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SiteId).HasColumnName("site_id");
            entity.Property(e => e.EventType).HasColumnName("event_type");
            entity.Property(e => e.TriggerMethod).HasColumnName("trigger_method");  // ADD THIS
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
        });
    }

    private void ConfigureExistingEntities(ModelBuilder modelBuilder)
    {
        // Configure Site entity
        modelBuilder.Entity<Site>(entity =>
        {
            entity.ToTable("sites");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasPrecision(10, 8).IsRequired();
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasPrecision(11, 8).IsRequired();
            // entity.Property(e => e.RadiusMeters).HasColumnName("radius_meters").IsRequired();
            entity.Property(e => e.AutoTriggerRadiusMeters).HasColumnName("auto_trigger_radius_meters").IsRequired().HasDefaultValue(50);
            entity.Property(e => e.ManualTriggerRadiusMeters).HasColumnName("manual_trigger_radius_meters").IsRequired().HasDefaultValue(150);
        });

        // Configure GeofenceEvent entity
        modelBuilder.Entity<GeofenceEvent>(entity =>
        {
            entity.ToTable("geofence_events");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.SiteId).HasColumnName("site_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Timestamp).HasColumnName("timestamp").IsRequired();
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasPrecision(10, 8);
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasPrecision(11, 8);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SiteId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
