using Microsoft.EntityFrameworkCore;
using Rascor.Domain;
using Rascor.Domain.Entities;

namespace Rascor.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(RascorDbContext db)
    {
        // Check if data already exists
        if (await db.Sites.AnyAsync())
            return; // Already seeded

        // ========================================================================
        // SEED SITES
        // ========================================================================
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
            )
        };

        db.Sites.AddRange(testSites);

        // Add test assignment (user-demo → all sites)
        var assignments = new[]
        {
            new UserAssignment("user-demo", "site-001", DateTimeOffset.UtcNow),
            new UserAssignment("user-demo", "site-002", DateTimeOffset.UtcNow),
            new UserAssignment("user-demo", "site-003", DateTimeOffset.UtcNow)
        };

        db.Assignments.AddRange(assignments);
        await db.SaveChangesAsync();

        // ========================================================================
        // SEED WORK TYPES
        // ========================================================================
        var workTypes = new[]
        {
            new WorkType(
                "wt-electrical",
                "Electrical Work",
                "All electrical installations, repairs, and maintenance work",
                true
            ),
            new WorkType(
                "wt-plumbing",
                "Plumbing & Drainage",
                "Plumbing installations, water systems, and drainage work",
                true
            ),
            new WorkType(
                "wt-scaffolding",
                "Scaffolding",
                "Scaffold erection, modification, and dismantling",
                true
            ),
            new WorkType(
                "wt-groundworks",
                "Groundworks",
                "Excavation, foundations, and site preparation",
                true
            )
        };

        db.WorkTypes.AddRange(workTypes);
        await db.SaveChangesAsync();

        // ========================================================================
        // SEED RAMS DOCUMENTS WITH CHECKLISTS
        // ========================================================================

        // Electrical RAMS
        var electricalRams = new RamsDocument(
            "rams-electrical-001",
            "wt-electrical",
            1,
            "Electrical Safety Method Statement v1.0",
            "checklist",
            null, // No JSON content
            null, // No PDF URL yet
            true,
            DateTimeOffset.UtcNow,
            null,
            "system"
        );

        db.RamsDocuments.Add(electricalRams);
        await db.SaveChangesAsync();

        var electricalChecklist = new[]
        {
            new RamsChecklistItem(
                "cl-elec-001",
                "rams-electrical-001",
                "PPE Requirements",
                1,
                "checkbox",
                "I am wearing appropriate PPE (hard hat, safety boots, hi-vis vest, insulated gloves)",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-elec-002",
                "rams-electrical-001",
                "Tools & Equipment",
                2,
                "checkbox",
                "All tools and test equipment have been inspected and are in good working condition",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-elec-003",
                "rams-electrical-001",
                "Isolation & Testing",
                3,
                "checkbox",
                "I have verified the circuit is properly isolated and locked out",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-elec-004",
                "rams-electrical-001",
                "Isolation & Testing",
                4,
                "checkbox",
                "Dead testing has been completed and documented",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-elec-005",
                "rams-electrical-001",
                "Emergency Procedures",
                5,
                "checkbox",
                "I know the location of the nearest first aid kit and fire extinguisher",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-elec-006",
                "rams-electrical-001",
                "Emergency Procedures",
                6,
                "checkbox",
                "I have reviewed the emergency evacuation procedure for this site",
                true,
                null
            )
        };

        db.RamsChecklistItems.AddRange(electricalChecklist);

        // Plumbing RAMS
        var plumbingRams = new RamsDocument(
            "rams-plumbing-001",
            "wt-plumbing",
            1,
            "Plumbing & Drainage Safety Method Statement v1.0",
            "checklist",
            null,
            null,
            true,
            DateTimeOffset.UtcNow,
            null,
            "system"
        );

        db.RamsDocuments.Add(plumbingRams);
        await db.SaveChangesAsync();

        var plumbingChecklist = new[]
        {
            new RamsChecklistItem(
                "cl-plumb-001",
                "rams-plumbing-001",
                "PPE Requirements",
                1,
                "checkbox",
                "I am wearing appropriate PPE (hard hat, safety boots, hi-vis vest, waterproof gloves)",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-plumb-002",
                "rams-plumbing-001",
                "Water Isolation",
                2,
                "checkbox",
                "Water supply has been isolated and drained where required",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-plumb-003",
                "rams-plumbing-001",
                "Confined Spaces",
                3,
                "checkbox",
                "If working in confined space, permit to work has been obtained",
                false,
                null
            ),
            new RamsChecklistItem(
                "cl-plumb-004",
                "rams-plumbing-001",
                "Hot Works",
                4,
                "checkbox",
                "Hot work permit obtained if using blow torches or welding equipment",
                false,
                null
            )
        };

        db.RamsChecklistItems.AddRange(plumbingChecklist);

        // Scaffolding RAMS
        var scaffoldingRams = new RamsDocument(
            "rams-scaffolding-001",
            "wt-scaffolding",
            1,
            "Scaffolding Safety Method Statement v1.0",
            "checklist",
            null,
            null,
            true,
            DateTimeOffset.UtcNow,
            null,
            "system"
        );

        db.RamsDocuments.Add(scaffoldingRams);
        await db.SaveChangesAsync();

        var scaffoldingChecklist = new[]
        {
            new RamsChecklistItem(
                "cl-scaff-001",
                "rams-scaffolding-001",
                "Qualifications",
                1,
                "checkbox",
                "I hold a valid CISRS scaffolding card or equivalent",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-scaff-002",
                "rams-scaffolding-001",
                "PPE Requirements",
                2,
                "checkbox",
                "I am wearing full PPE including harness and hard hat with chin strap",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-scaff-003",
                "rams-scaffolding-001",
                "Equipment Check",
                3,
                "checkbox",
                "All scaffolding components have been inspected for damage",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-scaff-004",
                "rams-scaffolding-001",
                "Weather Conditions",
                4,
                "checkbox",
                "Weather conditions are suitable for working at height (no high winds or rain)",
                true,
                null
            ),
            new RamsChecklistItem(
                "cl-scaff-005",
                "rams-scaffolding-001",
                "Exclusion Zone",
                5,
                "checkbox",
                "Exclusion zone has been established and marked below work area",
                true,
                null
            )
        };

        db.RamsChecklistItems.AddRange(scaffoldingChecklist);
        await db.SaveChangesAsync();

        // ========================================================================
        // SEED WORK ASSIGNMENTS
        // ========================================================================
        var workAssignments = new[]
        {
            // User has electrical work at Dublin Office
            new WorkAssignment(
                "wa-001",
                "user-demo",
                "site-001",
                "wt-electrical",
                "system",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(30),
                "Active",
                "Electrical maintenance and lighting upgrades"
            ),
            // User has plumbing work at Dublin Warehouse
            new WorkAssignment(
                "wa-002",
                "user-demo",
                "site-002",
                "wt-plumbing",
                "system",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(14),
                "Active",
                "Install new drainage system in warehouse"
            ),
            // User has scaffolding work at Cork Office
            new WorkAssignment(
                "wa-003",
                "user-demo",
                "site-003",
                "wt-scaffolding",
                "system",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(7),
                DateTimeOffset.UtcNow.AddDays(21),
                "Pending",
                "Erect scaffolding for exterior maintenance work"
            )
        };

        db.WorkAssignments.AddRange(workAssignments);
        await db.SaveChangesAsync();

        Console.WriteLine("✅ Database seeded successfully with RAMS demo data!");
    }
}
