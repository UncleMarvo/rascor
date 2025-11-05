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
            var testSites = new List<Site>
            {
                new Site
                {
                    Id = "SITE001",
                    Name = "RASCOR HQ",
                    Latitude = 52.691793,
                    Longitude = -6.270275
                },
                new Site
                {
                    Id = "SITE002",
                    Name = "South West Gate",
                    Latitude = 53.325525,
                    Longitude = -6.341526
                },
                new Site
                {
                    Id = "SITE003",
                    Name = "Marmalade Lane",
                    Latitude = 53.280824,
                    Longitude = -6.240092
                },
                new Site
                {
                    Id = "SITE004",
                    Name = "Rathbourne Crossing",
                    Latitude = 53.377030,
                    Longitude = -6.331707
                },
                new Site
                {
                    Id = "SITE005",
                    Name = "Castleforbes Prem Inn",
                    Latitude = 53.350367,
                    Longitude = -6.234467
                },
                new Site
                {
                    Id = "SITE006",
                    Name = "Angem",
                    Latitude = 53.272313,
                    Longitude = -6.152010
                },
                new Site
                {
                    Id = "SITE007",
                    Name = "Oscar Trainer Road",
                    Latitude = 53.397126,
                    Longitude = -6.233258
                },
                new Site
                {
                    Id = "SITE008",
                    Name = "Eden",
                    Latitude = 51.894092,
                    Longitude = -8.413677
                },
                new Site
                {
                    Id = "SITE009",
                    Name = "Jacobs Island",
                    Latitude = 51.883780,
                    Longitude = -8.392919
                },
                new Site
                {
                    Id = "SITE010",
                    Name = "Ford",
                    Latitude = 51.899808,
                    Longitude = -8.440358
                },
                new Site
                {
                    Id = "SITE011",
                    Name = "Tile 6",
                    Latitude = 53.332341,
                    Longitude = -6.426592
                },
                new Site
                {
                    Id = "SITE012",
                    Name = "Montrose",
                    Latitude = 53.317609,
                    Longitude = -6.227131
                },
                new Site
                {
                    Id = "SITE013",
                    Name = "Donore",
                    Latitude = 53.335169,
                    Longitude = -6.285713
                }
            };

            db.Sites.AddRange(testSites);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ Sites seeded successfully!");
        }
    }
}
