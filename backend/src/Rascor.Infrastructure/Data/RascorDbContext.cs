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
    public DbSet<UserAssignment> Assignments => Set<UserAssignment>();

    // New RAMS tables
    public DbSet<WorkType> WorkTypes => Set<WorkType>();
    public DbSet<RamsDocument> RamsDocuments => Set<RamsDocument>();
    public DbSet<RamsChecklistItem> RamsChecklistItems => Set<RamsChecklistItem>();
    public DbSet<WorkAssignment> WorkAssignments => Set<WorkAssignment>();
    public DbSet<RamsAcceptance> RamsAcceptances => Set<RamsAcceptance>();

    public DbSet<RamsPhoto> RamsPhotos => Set<RamsPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureGeofenceEntities(modelBuilder);
        ConfigureExistingEntities(modelBuilder);
        ConfigureRamsEntities(modelBuilder);
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

        // Configure UserAssignment entity
        modelBuilder.Entity<UserAssignment>(entity =>
        {
            entity.ToTable("assignments");
            entity.HasKey(e => new { e.UserId, e.SiteId });
            
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.SiteId).HasColumnName("site_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at").IsRequired();
        });
    }

    private void ConfigureRamsEntities(ModelBuilder modelBuilder)
    {
        // Configure WorkType
        modelBuilder.Entity<WorkType>(entity =>
        {
            entity.ToTable("work_types");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        // Configure RamsDocument
        modelBuilder.Entity<RamsDocument>(entity =>
        {
            entity.ToTable("rams_documents");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.WorkTypeId).HasColumnName("work_type_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            entity.Property(e => e.ContentType).HasColumnName("content_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.PdfBlobUrl).HasColumnName("pdf_blob_url").HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from").IsRequired();
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasOne(e => e.WorkType)
                .WithMany(w => w.RamsDocuments)
                .HasForeignKey(e => e.WorkTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.WorkTypeId, e.Version }).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // Configure RamsChecklistItem
        modelBuilder.Entity<RamsChecklistItem>(entity =>
        {
            entity.ToTable("rams_checklist_items");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.RamsDocumentId).HasColumnName("rams_document_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Section).HasColumnName("section").HasMaxLength(200);
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order").IsRequired();
            entity.Property(e => e.ItemType).HasColumnName("item_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Label).HasColumnName("label").HasMaxLength(500).IsRequired();
            entity.Property(e => e.IsRequired).HasColumnName("is_required").IsRequired();
            entity.Property(e => e.ValidationRules).HasColumnName("validation_rules");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasOne(e => e.RamsDocument)
                .WithMany(r => r.ChecklistItems)
                .HasForeignKey(e => e.RamsDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.RamsDocumentId, e.DisplayOrder });
        });

        // Configure WorkAssignment
        modelBuilder.Entity<WorkAssignment>(entity =>
        {
            entity.ToTable("work_assignments");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.SiteId).HasColumnName("site_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.WorkTypeId).HasColumnName("work_type_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by").HasMaxLength(100);
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at").IsRequired();
            entity.Property(e => e.ExpectedStartDate).HasColumnName("expected_start_date");
            entity.Property(e => e.ExpectedEndDate).HasColumnName("expected_end_date");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.WorkType)
                .WithMany(w => w.WorkAssignments)
                .HasForeignKey(e => e.WorkTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.UserId, e.SiteId });
            entity.HasIndex(e => e.Status);
        });

        // Configure RamsAcceptance
        modelBuilder.Entity<RamsAcceptance>(entity =>
        {
            entity.ToTable("rams_acceptances");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.SiteId).HasColumnName("site_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.WorkAssignmentId).HasColumnName("work_assignment_id").HasMaxLength(50);
            entity.Property(e => e.RamsDocumentId).HasColumnName("rams_document_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.SignatureData).HasColumnName("signature_data").IsRequired();
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
            entity.Property(e => e.DeviceInfo).HasColumnName("device_info").HasMaxLength(500);
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasPrecision(10, 8);
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasPrecision(11, 8);
            entity.Property(e => e.AcceptedAt).HasColumnName("accepted_at").IsRequired();
            entity.Property(e => e.ChecklistResponses).HasColumnName("checklist_responses");

            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.WorkAssignment)
                .WithMany(w => w.RamsAcceptances)
                .HasForeignKey(e => e.WorkAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RamsDocument)
                .WithMany(r => r.Acceptances)
                .HasForeignKey(e => e.RamsDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.UserId, e.SiteId, e.AcceptedAt });
            entity.HasIndex(e => new { e.UserId, e.WorkAssignmentId, e.RamsDocumentId }).IsUnique();
        });
    }
}
