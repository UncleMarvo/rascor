using SiteAttendance.Domain;

namespace SiteAttendance.Infrastructure;

public class InMemorySettingsRepository : ISettingsRepository
{
    private RemoteConfig _config = RemoteConfig.Default;

    public Task<RemoteConfig> GetConfigAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_config);
    }

    public Task UpdateConfigAsync(RemoteConfig config, CancellationToken ct = default)
    {
        _config = config;
        return Task.CompletedTask;
    }
}
