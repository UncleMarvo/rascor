using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rascor.Infrastructure.ExternalServices;

public class ZohoCreatorClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ZohoCreatorClient> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiry;

    public ZohoCreatorClient(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<ZohoCreatorClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(5))
        {
            return; // Token still valid
        }

        await RefreshAccessTokenAsync(ct);
    }

    // In ZohoCreatorClient.cs - RefreshAccessTokenAsync method

    private async Task RefreshAccessTokenAsync(CancellationToken ct)
    {
        var clientId = _config["Zoho:ClientId"];
        var clientSecret = _config["Zoho:ClientSecret"];
        var refreshToken = _config["Zoho:RefreshToken"];
        var dataCenter = _config["Zoho:DataCenter"] ?? "eu";

        var tokenUrl = $"https://accounts.zoho.{dataCenter}/oauth/v2/token";
        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "refresh_token"
        });

        request.Content = content;

        var response = await _httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        _logger.LogWarning("Token refresh response: {Body}", body);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Token refresh failed: {StatusCode} - {Body}", response.StatusCode, body);
            throw new Exception($"Token refresh failed: {body}");
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(body);

        if (!tokenData.TryGetProperty("access_token", out var accessTokenElement))
        {
            _logger.LogError("No access_token in response: {Body}", body);
            throw new Exception($"Invalid token response: {body}");
        }

        _accessToken = accessTokenElement.GetString();
        var expiresIn = tokenData.TryGetProperty("expires_in", out var expiresElement)
            ? expiresElement.GetInt32()
            : 3600;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

        _logger.LogInformation("Token refreshed successfully");
    }

    public async Task<bool> UpsertRecordAsync(
        string formName,
        object record,
        CancellationToken ct)
    {
        await EnsureAuthenticatedAsync(ct);

        var ownerName = _config["Zoho:OwnerName"];
        var appName = _config["Zoho:AppName"];
        var dataCenter = _config["Zoho:DataCenter"] ?? "eu";

        // FIXED: Add /data/ to the URL!
        var url = $"https://creator.zoho.{dataCenter}/api/v2/{ownerName}/{appName}/form/{formName}";

        var payload = new { data = new[] { record } };
        var json = JsonSerializer.Serialize(payload);

        _logger.LogInformation("Zoho API Request: POST {Url}", url);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");

        var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Zoho API error: {StatusCode} - {Response}",
                response.StatusCode, responseBody);
            return false;
        }

        _logger.LogInformation("Zoho API Success: {Response}", responseBody);
        return true;
    }

    public async Task<bool> UpsertRecordsAsync(
        string formName,
        List<object> records,
        CancellationToken ct)
    {
        await EnsureAuthenticatedAsync(ct);

        var ownerName = _config["Zoho:OwnerName"];
        var appName = _config["Zoho:AppName"];
        var dataCenter = _config["Zoho:DataCenter"] ?? "eu";

        var url = $"https://creator.zoho.{dataCenter}/api/v2/{ownerName}/{appName}/form/{formName}";

        var payload = new { data = records };
        var json = JsonSerializer.Serialize(payload);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");

        var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        _logger.LogWarning("=== ZOHO RESPONSE ===");
        _logger.LogWarning("Body: {Body}", responseBody);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

        // Check for ANY errors in the response
        if (jsonResponse.TryGetProperty("result", out var result))
        {
            var hasErrors = false;
            foreach (var item in result.EnumerateArray())
            {
                if (item.TryGetProperty("error", out var errors))
                {
                    _logger.LogError("Zoho rejected record: {Errors}", errors.ToString());
                    hasErrors = true;
                }
            }

            if (hasErrors)
            {
                _logger.LogError("Some records were rejected by Zoho");
                return false;
            }
        }

        _logger.LogInformation("All records accepted by Zoho");
        return true;
    }

    private class ZohoTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
