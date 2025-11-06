namespace Rascor.Application.Interfaces.Procore;

public interface IProcoreTokenManager
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task RefreshTokenAsync(CancellationToken cancellationToken = default);
}
