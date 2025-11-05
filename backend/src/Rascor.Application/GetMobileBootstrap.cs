using Rascor.Application.DTOs;
using Rascor.Domain;
using Rascor.Domain.Repositories;

namespace Rascor.Application;

public class GetMobileBootstrap
{
    private readonly ISiteRepository _siteRepo;

    public GetMobileBootstrap(ISiteRepository siteRepo)
    {
        _siteRepo = siteRepo;
    }

    public async Task<MobileBootstrapResponse> ExecuteAsync(string userId, CancellationToken ct)
    {
        // Return ALL sites - no assignment filtering needed
        var allSites = await _siteRepo.GetAllAsync(ct);

        var siteDtos = allSites.Select(s => new SiteDto(
            s.Id,
            s.Name,
            s.Latitude,
            s.Longitude,
            s.AutoTriggerRadiusMeters,
            s.ManualTriggerRadiusMeters
        )).ToList();

        var config = new RemoteConfig(
            PollIntervalSeconds: 300,
            EnableOfflineMode: true
        );

        return new MobileBootstrapResponse(
            config,
            siteDtos
        );
    }
}