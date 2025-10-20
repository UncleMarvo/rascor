using Microsoft.EntityFrameworkCore;
using Rascor.Domain;

namespace Rascor.Infrastructure.Data;

public class RascorDbContext : DbContext
{
    public RascorDbContext(DbContextOptions<RascorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Site> Sites => Set<Site>();
    public DbSet<GeofenceEvent> GeofenceEvents => Set<GeofenceEvent>();
    public DbSet<UserAssignment> Assignments => Set<UserAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Site entity
        modelBuilder.Entity<Site>(entity =>
        {
            entity.ToTable("sites");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.Latitude)
                .HasColumnName("latitude")
                .HasPrecision(10, 8)
                .IsRequired();
            
            entity.Property(e => e.Longitude)
                .HasColumnName("longitude")
                .HasPrecision(11, 8)
                .IsRequired();
            
            entity.Property(e => e.RadiusMeters)
                .HasColumnName("radius_meters")
                .IsRequired();
        });

        // Configure GeofenceEvent entity
        modelBuilder.Entity<GeofenceEvent>(entity =>
        {
            entity.ToTable("geofence_events");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.SiteId)
                .HasColumnName("site_id")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(10)
                .IsRequired();
            
            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp")
                .IsRequired();
            
            entity.Property(e => e.Latitude)
                .HasColumnName("latitude")
                .HasPrecision(10, 8);
            
            entity.Property(e => e.Longitude)
                .HasColumnName("longitude")
                .HasPrecision(11, 8);

            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SiteId);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure UserAssignment entity
        modelBuilder.Entity<UserAssignment>(entity =>
        {
            entity.ToTable("assignments");
            entity.HasKey(e => new { e.UserId, e.SiteId }); // Composite key
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.SiteId)
                .HasColumnName("site_id")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.AssignedAt)
                .HasColumnName("assigned_at")
                .IsRequired();
        });
    }
}
