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

        // URL-encode OwnerName and AppName to handle special characters (e.g., @, spaces, etc.)
        var encodedOwnerName = Uri.EscapeDataString(ownerName ?? "");
        var encodedAppName = Uri.EscapeDataString(appName ?? "");
        var encodedFormName = Uri.EscapeDataString(formName);

        var url = $"https://creator.zoho.{dataCenter.ToLowerInvariant()}/api/v2/{encodedOwnerName}/{encodedAppName}/form/{encodedFormName}";

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

        // URL-encode OwnerName and AppName to handle special characters (e.g., @, spaces, etc.)
        var encodedOwnerName = Uri.EscapeDataString(ownerName ?? "");
        var encodedAppName = Uri.EscapeDataString(appName ?? "");
        var encodedFormName = Uri.EscapeDataString(formName);

        var url = $"https://creator.zoho.{dataCenter.ToLowerInvariant()}/api/v2/{encodedOwnerName}/{encodedAppName}/form/{encodedFormName}";

        var payload = new { data = records };
        var json = JsonSerializer.Serialize(payload);

        // Log the exact payload being sent to Zoho for debugging
        // This will show us the exact timestamp format being sent
        _logger.LogWarning("=== ZOHO REQUEST PAYLOAD ===");
        _logger.LogWarning("URL: {Url}", url);
        if (records != null && records.Count > 0)
        {
            _logger.LogWarning("Payload (first record): {FirstRecord}", 
                JsonSerializer.Serialize(records[0]));
        }
        else
        {
            _logger.LogWarning("No records to send");
        }
        
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");

        var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        _logger.LogWarning("=== ZOHO RESPONSE ===");
        _logger.LogWarning("Status Code: {StatusCode}", response.StatusCode);
        _logger.LogWarning("Body: {Body}", responseBody);

        if (!response.IsSuccessStatusCode)
        {
            // Log detailed error information for non-success status codes
            _logger.LogError(
                "❌ Zoho API returned error status {StatusCode}. URL: {Url}, Response: {Response}",
                response.StatusCode, url, responseBody);
            
            // Try to parse error details if available
            try
            {
                var errorJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
                if (errorJson.TryGetProperty("code", out var code))
                {
                    _logger.LogError("Zoho error code: {Code}", code.GetInt32());
                }
                if (errorJson.TryGetProperty("description", out var desc))
                {
                    _logger.LogError("Zoho error description: {Description}", desc.GetString());
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }
            
            return false;
        }

        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

        // Check for errors in the response
        // Note: Zoho may return errors in the "error" field even when records are added successfully
        // (e.g., script errors in "On Add - On Success" scripts). Check the "code" and "message" fields
        // to determine if records were actually rejected or just have script errors.
        if (jsonResponse.TryGetProperty("result", out var result))
        {
            var hasRejectionErrors = false;
            var hasScriptErrors = false;
            
            foreach (var item in result.EnumerateArray())
            {
                // Get error code and message for detailed logging
                var errorCode = item.TryGetProperty("code", out var codeElement) 
                    ? codeElement.GetInt32() 
                    : 0;
                var message = item.TryGetProperty("message", out var messageElement) 
                    ? messageElement.GetString() 
                    : "Unknown";
                
                // Check if record was added successfully (has ID)
                string recordId = "unknown";
                bool hasId = false;
                if (item.TryGetProperty("data", out var data) && 
                    data.TryGetProperty("ID", out var idElement))
                {
                    recordId = idElement.ToString() ?? "unknown";
                    hasId = true;
                }
                
                // Check if there's an error
                if (item.TryGetProperty("error", out var errors))
                {
                    // Parse error details - can be object (field-specific) or array (general errors)
                    string errorDetails = "";
                    if (errors.ValueKind == JsonValueKind.Object)
                    {
                        // Field-specific errors: {"Timestamp":"Enter a valid date format", "Field2":"Error 2"}
                        var fieldErrors = new List<string>();
                        foreach (var errorProp in errors.EnumerateObject())
                        {
                            fieldErrors.Add($"{errorProp.Name}: {errorProp.Value.GetString()}");
                        }
                        errorDetails = string.Join(", ", fieldErrors);
                    }
                    else if (errors.ValueKind == JsonValueKind.Array)
                    {
                        // Array of error strings
                        var errorList = new List<string>();
                        foreach (var errorItem in errors.EnumerateArray())
                        {
                            errorList.Add(errorItem.GetString() ?? errorItem.ToString());
                        }
                        errorDetails = string.Join(" | ", errorList);
                    }
                    else
                    {
                        errorDetails = errors.ToString();
                    }
                    
                    var fullErrorText = errors.ToString();
                    
                    // Script errors (like "On Add - On Success" script errors) don't mean rejection
                    // Records are still added successfully even with script errors
                    if (fullErrorText.Contains("On Add") || fullErrorText.Contains("On Success") || 
                        fullErrorText.Contains("script") || fullErrorText.Contains("pushNotification"))
                    {
                        hasScriptErrors = true;
                        _logger.LogWarning(
                            "⚠️ Zoho script error (record still added successfully): Code={ErrorCode}, Message={Message}, Details={ErrorDetails}, Record ID={RecordId}",
                            errorCode, message, errorDetails, recordId);
                    }
                    else
                    {
                        // This is a real rejection error - log detailed information
                        hasRejectionErrors = true;
                        _logger.LogError(
                            "❌ Zoho rejected record: Code={ErrorCode}, Message={Message}, Error Details={ErrorDetails}, Record ID={RecordId}",
                            errorCode, message, errorDetails, recordId);
                    }
                }
                else if (hasId)
                {
                    // No errors, record added successfully
                    _logger.LogInformation("✅ Zoho record added successfully: Code={ErrorCode}, Message={Message}, ID={RecordId}", 
                        errorCode, message, recordId);
                }
            }

            if (hasRejectionErrors)
            {
                _logger.LogError("Some records were rejected by Zoho");
                return false;
            }
            
            if (hasScriptErrors)
            {
                _logger.LogWarning(
                    "⚠️ Zoho script errors detected (records were added successfully, but scripts failed). " +
                    "Fix the 'On Add - On Success' script in Zoho Creator to resolve script errors.");
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
