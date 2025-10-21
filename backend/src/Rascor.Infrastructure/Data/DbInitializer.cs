using Microsoft.EntityFrameworkCore;
using Rascor.Domain;
using Rascor.Domain.Entities;

namespace Rascor.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(RascorDbContext db)
    {
        // Seed sites if they don't exist
        if (!await db.Sites.AnyAsync())
        {
            var testSites = new[]
            {
                new Site(
                    Id: "site-001",
                    Name: "Dublin Office",
                    Latitude: 53.3498,
                    Longitude: -6.2603,
                    RadiusMeters: 100
                ),
                new Site(
                    Id: "site-002",
                    Name: "Dublin Warehouse",
                    Latitude: 53.3520,
                    Longitude: -6.2570,
                    RadiusMeters: 150
                ),
                new Site(
                    Id: "site-003",
                    Name: "Cork Office",
                    Latitude: 51.8985,
                    Longitude: -8.4756,
                    RadiusMeters: 120
                ),
                new Site(
                    Id: "site-004",
                    Name: "Wexford Home",
                    Latitude: 52.5103567,
                    Longitude: -6.5767854,
                    RadiusMeters: 50
                ),
                new Site(
                    Id: "site-005",
                    Name: "Wexford GreenTechHQ",
                    Latitude: 52.4930494,
                    Longitude: -6.5640803,
                    RadiusMeters: 150
                )
            };

            db.Sites.AddRange(testSites);

            // Add test assignment (user-demo → all sites)
            var assignments = new[]
            {
                new UserAssignment("user-demo", "site-001", DateTimeOffset.UtcNow),
                new UserAssignment("user-demo", "site-002", DateTimeOffset.UtcNow),
                new UserAssignment("user-demo", "site-003", DateTimeOffset.UtcNow),
                new UserAssignment("user-demo", "site-004", DateTimeOffset.UtcNow),
                new UserAssignment("user-demo", "site-005", DateTimeOffset.UtcNow)
            };

            db.Assignments.AddRange(assignments);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ Sites seeded successfully!");
        }

        // Check if RAMS data already exists
        if (await db.WorkTypes.AnyAsync())
        {
            Console.WriteLine("ℹ️ RAMS data already exists, skipping seed.");
            return;
        }

        Console.WriteLine("🌱 Seeding RAMS data...");

        // ========================================================================
        // SEED WORK TYPES
        // ========================================================================
        var workTypes = new[]
        {
            new WorkType
            {
                Id = "wt-electrical",
                Name = "Electrical Work",
                Description = "All electrical installations, repairs, and maintenance work",
                IsActive = true
            },
            new WorkType
            {
                Id = "wt-plumbing",
                Name = "Plumbing & Drainage",
                Description = "Plumbing installations, water systems, and drainage work",
                IsActive = true
            },
            new WorkType
            {
                Id = "wt-scaffolding",
                Name = "Scaffolding",
                Description = "Scaffold erection, modification, and dismantling",
                IsActive = true
            },
            new WorkType
            {
                Id = "wt-groundworks",
                Name = "Groundworks",
                Description = "Excavation, foundations, and site preparation",
                IsActive = true
            }
        };

        db.WorkTypes.AddRange(workTypes);
        await db.SaveChangesAsync();
        Console.WriteLine("  ✓ Work types seeded");

        // ========================================================================
        // SEED RAMS DOCUMENTS WITH CHECKLISTS
        // ========================================================================

        // Electrical RAMS
        var electricalRams = new RamsDocument
        {
            Id = "rams-electrical-001",
            WorkTypeId = "wt-electrical",
            Version = 1,
            Title = "Electrical Safety Method Statement v1.0",
            ContentType = "checklist",
            Content = null,
            PdfBlobUrl = null,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow,
            EffectiveTo = null,
            CreatedBy = "system"
        };

        db.RamsDocuments.Add(electricalRams);
        await db.SaveChangesAsync();

        var electricalChecklist = new[]
        {
            new RamsChecklistItem
            {
                Id = "cl-elec-001",
                RamsDocumentId = "rams-electrical-001",
                Section = "PPE Requirements",
                DisplayOrder = 1,
                ItemType = "checkbox",
                Label = "I am wearing appropriate PPE (hard hat, safety boots, hi-vis vest, insulated gloves)",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-elec-002",
                RamsDocumentId = "rams-electrical-001",
                Section = "Tools & Equipment",
                DisplayOrder = 2,
                ItemType = "checkbox",
                Label = "All tools and test equipment have been inspected and are in good working condition",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-elec-003",
                RamsDocumentId = "rams-electrical-001",
                Section = "Isolation & Testing",
                DisplayOrder = 3,
                ItemType = "checkbox",
                Label = "I have verified the circuit is properly isolated and locked out",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-elec-004",
                RamsDocumentId = "rams-electrical-001",
                Section = "Isolation & Testing",
                DisplayOrder = 4,
                ItemType = "checkbox",
                Label = "Dead testing has been completed and documented",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-elec-005",
                RamsDocumentId = "rams-electrical-001",
                Section = "Emergency Procedures",
                DisplayOrder = 5,
                ItemType = "checkbox",
                Label = "I know the location of the nearest first aid kit and fire extinguisher",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-elec-006",
                RamsDocumentId = "rams-electrical-001",
                Section = "Emergency Procedures",
                DisplayOrder = 6,
                ItemType = "checkbox",
                Label = "I have reviewed the emergency evacuation procedure for this site",
                IsRequired = true,
                ValidationRules = null
            }
        };

        db.RamsChecklistItems.AddRange(electricalChecklist);
        Console.WriteLine("  ✓ Electrical RAMS seeded (6 checklist items)");

        // Plumbing RAMS
        var plumbingRams = new RamsDocument
        {
            Id = "rams-plumbing-001",
            WorkTypeId = "wt-plumbing",
            Version = 1,
            Title = "Plumbing & Drainage Safety Method Statement v1.0",
            ContentType = "checklist",
            Content = null,
            PdfBlobUrl = null,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow,
            EffectiveTo = null,
            CreatedBy = "system"
        };

        db.RamsDocuments.Add(plumbingRams);
        await db.SaveChangesAsync();

        var plumbingChecklist = new[]
        {
            new RamsChecklistItem
            {
                Id = "cl-plumb-001",
                RamsDocumentId = "rams-plumbing-001",
                Section = "PPE Requirements",
                DisplayOrder = 1,
                ItemType = "checkbox",
                Label = "I am wearing appropriate PPE (hard hat, safety boots, hi-vis vest, waterproof gloves)",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-plumb-002",
                RamsDocumentId = "rams-plumbing-001",
                Section = "Water Isolation",
                DisplayOrder = 2,
                ItemType = "checkbox",
                Label = "Water supply has been isolated and drained where required",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-plumb-003",
                RamsDocumentId = "rams-plumbing-001",
                Section = "Confined Spaces",
                DisplayOrder = 3,
                ItemType = "checkbox",
                Label = "If working in confined space, permit to work has been obtained",
                IsRequired = false,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-plumb-004",
                RamsDocumentId = "rams-plumbing-001",
                Section = "Hot Works",
                DisplayOrder = 4,
                ItemType = "checkbox",
                Label = "Hot work permit obtained if using blow torches or welding equipment",
                IsRequired = false,
                ValidationRules = null
            }
        };

        db.RamsChecklistItems.AddRange(plumbingChecklist);
        Console.WriteLine("  ✓ Plumbing RAMS seeded (4 checklist items)");

        // Scaffolding RAMS
        var scaffoldingRams = new RamsDocument
        {
            Id = "rams-scaffolding-001",
            WorkTypeId = "wt-scaffolding",
            Version = 1,
            Title = "Scaffolding Safety Method Statement v1.0",
            ContentType = "checklist",
            Content = null,
            PdfBlobUrl = null,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow,
            EffectiveTo = null,
            CreatedBy = "system"
        };

        db.RamsDocuments.Add(scaffoldingRams);
        await db.SaveChangesAsync();

        var scaffoldingChecklist = new[]
        {
            new RamsChecklistItem
            {
                Id = "cl-scaff-001",
                RamsDocumentId = "rams-scaffolding-001",
                Section = "Qualifications",
                DisplayOrder = 1,
                ItemType = "checkbox",
                Label = "I hold a valid CISRS scaffolding card or equivalent",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-scaff-002",
                RamsDocumentId = "rams-scaffolding-001",
                Section = "PPE Requirements",
                DisplayOrder = 2,
                ItemType = "checkbox",
                Label = "I am wearing full PPE including harness and hard hat with chin strap",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-scaff-003",
                RamsDocumentId = "rams-scaffolding-001",
                Section = "Equipment Check",
                DisplayOrder = 3,
                ItemType = "checkbox",
                Label = "All scaffolding components have been inspected for damage",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-scaff-004",
                RamsDocumentId = "rams-scaffolding-001",
                Section = "Weather Conditions",
                DisplayOrder = 4,
                ItemType = "checkbox",
                Label = "Weather conditions are suitable for working at height (no high winds or rain)",
                IsRequired = true,
                ValidationRules = null
            },
            new RamsChecklistItem
            {
                Id = "cl-scaff-005",
                RamsDocumentId = "rams-scaffolding-001",
                Section = "Exclusion Zone",
                DisplayOrder = 5,
                ItemType = "checkbox",
                Label = "Exclusion zone has been established and marked below work area",
                IsRequired = true,
                ValidationRules = null
            }
        };

        db.RamsChecklistItems.AddRange(scaffoldingChecklist);
        await db.SaveChangesAsync();
        Console.WriteLine("  ✓ Scaffolding RAMS seeded (5 checklist items)");

        // ========================================================================
        // SEED WORK ASSIGNMENTS
        // ========================================================================
        var workAssignments = new[]
        {
            new WorkAssignment
            {
                Id = "wa-001",
                UserId = "user-demo",
                SiteId = "site-001",
                WorkTypeId = "wt-electrical",
                AssignedBy = "system",
                AssignedAt = DateTime.UtcNow,
                ExpectedStartDate = DateTime.UtcNow,
                ExpectedEndDate = DateTime.UtcNow.AddDays(30),
                Status = "Active",
                Notes = "Electrical maintenance and lighting upgrades"
            },
            new WorkAssignment
            {
                Id = "wa-002",
                UserId = "user-demo",
                SiteId = "site-002",
                WorkTypeId = "wt-plumbing",
                AssignedBy = "system",
                AssignedAt = DateTime.UtcNow,
                ExpectedStartDate = DateTime.UtcNow,
                ExpectedEndDate = DateTime.UtcNow.AddDays(14),
                Status = "Active",
                Notes = "Install new drainage system in warehouse"
            },
            new WorkAssignment
            {
                Id = "wa-003",
                UserId = "user-demo",
                SiteId = "site-003",
                WorkTypeId = "wt-scaffolding",
                AssignedBy = "system",
                AssignedAt = DateTime.UtcNow,
                ExpectedStartDate = DateTime.UtcNow.AddDays(7),
                ExpectedEndDate = DateTime.UtcNow.AddDays(21),
                Status = "Pending",
                Notes = "Erect scaffolding for exterior maintenance work"
            }
        };

        db.WorkAssignments.AddRange(workAssignments);
        await db.SaveChangesAsync();
        Console.WriteLine("  ✓ Work assignments seeded (3 assignments)");

        Console.WriteLine("✅ RAMS data seeded successfully!");
    }
}
