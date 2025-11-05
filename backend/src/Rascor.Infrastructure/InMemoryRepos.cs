using Rascor.Domain;
using Rascor.Domain.Entities;
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

    public Task<GeofenceEvent?> GetLastEventForDeviceAtSiteAsync(string userId, string siteId)
    {
        var lastEvent = _events
            .Where(e => e.UserId == userId && e.SiteId == siteId)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefault();

        return Task.FromResult(lastEvent);
    }
}

public class InMemorySiteRepository : ISiteRepository
{
    private readonly ConcurrentDictionary<string, Site> _sites = new();

    public InMemorySiteRepository()
    {
        // Seed demo site (San Francisco)
        var demoSite = new Site
        {
            Id = "site-001",
            Name = "SF Office",
            Latitude = 37.7749,
            Longitude = -122.4194
        };
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

    public Task<List<Site>> GetSitesByDeviceIdAsync(string userId)
    {
        // In-memory implementation: return all sites
        // In real implementation, this would filter by device assignments
        return Task.FromResult(_sites.Values.ToList());
    }
}