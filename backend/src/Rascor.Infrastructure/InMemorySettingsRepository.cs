using Rascor.Domain;
using Rascor.Domain.Entities;

namespace Rascor.Infrastructure;

public class InMemorySettingsRepository : ISettingsRepository
{
    private GeofenceConfig _config = GeofenceConfig.Default;

    public Task<GeofenceConfig> GetConfigAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_config);
    }

    public Task UpdateConfigAsync(GeofenceConfig config, CancellationToken ct = default)
    {
        _config = config;
        return Task.CompletedTask;
    }
}
