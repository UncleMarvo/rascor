using Rascor.Domain;
using System.Collections.Concurrent;

namespace Rascor.Infrastructure;

public class InMemoryGeofenceEventRepository : IGeofenceEventRepository
{
    private readonly ConcurrentBag<GeofenceEvent> _events = new();

    public Task AddAsync(GeofenceEvent evt, CancellationToken ct = default)
    {
        _events.Add(evt);
        return Task.CompletedTask;
    }

    public Task<List<GeofenceEvent>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return Task.FromResult(_events.Where(e => e.UserId == userId).ToList());
    }

    public Task<List<GeofenceEvent>> GetBySiteIdAsync(string siteId, CancellationToken ct = default)
    {
        return Task.FromResult(_events.Where(e => e.SiteId == siteId).ToList());
    }
}

public class InMemorySiteRepository : ISiteRepository
{
    private readonly ConcurrentDictionary<string, Site> _sites = new();

    public InMemorySiteRepository()
    {
        // Seed demo site (San Francisco)
        var demoSite = new Site(
            Id: "site-001",
            Name: "SF Office",
            Latitude: 37.7749,
            Longitude: -122.4194,
            RadiusMeters: 150
        );
        _sites.TryAdd(demoSite.Id, demoSite);
    }

    public Task<Site?> GetByIdAsync(string siteId, CancellationToken ct = default)
    {
        _sites.TryGetValue(siteId, out var site);
        return Task.FromResult(site);
    }

    public Task<List<Site>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_sites.Values.ToList());
    }

    public Task AddAsync(Site site, CancellationToken ct = default)
    {
        _sites.TryAdd(site.Id, site);
        return Task.CompletedTask;
    }
}

public class InMemoryAssignmentRepository : IAssignmentRepository
{
    private readonly ConcurrentBag<UserAssignment> _assignments = new();
    private readonly ISiteRepository _siteRepo;

    public InMemoryAssignmentRepository(ISiteRepository siteRepo)
    {
        _siteRepo = siteRepo;
        
        // Seed demo assignment for user-demo
        _assignments.Add(new UserAssignment(
            UserId: "user-demo",
            SiteId: "site-001",
            AssignedAt: DateTimeOffset.UtcNow
        ));
    }

    public async Task<List<Site>> GetAssignedSitesAsync(string userId, CancellationToken ct = default)
    {
        var assignedSiteIds = _assignments
            .Where(a => a.UserId == userId)
            .Select(a => a.SiteId)
            .Distinct()
            .ToList();

        var sites = new List<Site>();
        foreach (var siteId in assignedSiteIds)
        {
            var site = await _siteRepo.GetByIdAsync(siteId, ct);
            if (site != null)
                sites.Add(site);
        }
        return sites;
    }

    public Task AssignAsync(string userId, string siteId, CancellationToken ct = default)
    {
        _assignments.Add(new UserAssignment(userId, siteId, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}
