using Microsoft.EntityFrameworkCore;
using SiteAttendance.Domain;

namespace SiteAttendance.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(SiteAttendanceDbContext db)
    {
        // Check if data already exists
        if (await db.Sites.AnyAsync())
            return; // Already seeded

        // Add test sites
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
    }
}
