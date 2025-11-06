using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rascor.Application.Interfaces.Procore;
using Rascor.Application.Models.Procore;
using Rascor.Infrastructure.Configuration.Procore;
using Rascor.Infrastructure.Data;
using System.Net.Http.Json;

namespace Rascor.Infrastructure.ExternalServices.Procore;

public class ProcoreTokenManager : IProcoreTokenManager
{
    private readonly HttpClient _httpClient;
    private readonly RascorDbContext _dbContext;
    private readonly ProcoreConfiguration _config;
    private readonly ILogger<ProcoreTokenManager> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ProcoreTokenManager(
        HttpClient httpClient,
        RascorDbContext dbContext,
        IOptions<ProcoreConfiguration> config,
        ILogger<ProcoreTokenManager> logger)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.ProcoreTokens
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (token == null)
        {
            throw new InvalidOperationException(
                "No Procore tokens found in database. Please insert initial tokens from n8n.");
        }

        var bufferTime = TimeSpan.FromMinutes(_config.TokenRefreshBufferMinutes);
        if (token.ExpiresAt <= DateTime.UtcNow.Add(bufferTime))
        {
            _logger.LogInformation(
                "Procore token expired or expiring soon (expires at {ExpiresAt}), refreshing...",
                token.ExpiresAt);

            await RefreshTokenAsync(cancellationToken);

            token = await _dbContext.ProcoreTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstAsync(cancellationToken);
        }

        return token.AccessToken;
    }

    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);

        try
        {
            var currentToken = await _dbContext.ProcoreTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (currentToken == null)
            {
                throw new InvalidOperationException("No tokens available to refresh");
            }

            var bufferTime = TimeSpan.FromMinutes(_config.TokenRefreshBufferMinutes);
            if (currentToken.ExpiresAt > DateTime.UtcNow.Add(bufferTime))
            {
                _logger.LogInformation("Token was already refreshed by another process");
                return;
            }

            _logger.LogInformation("Refreshing Procore access token...");

            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _config.ClientId,
                ["client_secret"] = _config.ClientSecret,
                ["refresh_token"] = currentToken.RefreshToken
            });

            var response = await _httpClient.PostAsync(_config.TokenUrl, requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to refresh Procore token. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException(
                    $"Failed to refresh Procore token: {response.StatusCode}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<ProcoreTokenResponse>(
                cancellationToken: cancellationToken);

            if (tokenResponse == null)
            {
                throw new InvalidOperationException("Received null token response from Procore");
            }

            currentToken.AccessToken = tokenResponse.AccessToken;
            currentToken.RefreshToken = tokenResponse.RefreshToken;
            currentToken.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            currentToken.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully refreshed Procore token. New expiry: {ExpiresAt}",
                currentToken.ExpiresAt);
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}