using System.Net.Http.Json;
using Rascor.App.Core;
using Microsoft.Extensions.Logging;

namespace Rascor.App.Services;

public class BackendApi
{
    private readonly HttpClient _http;
    private readonly ILogger<BackendApi> _logger;

    // Use existing Azure backend URL (siteattendance deployment)
    private const string BaseUrl = "https://siteattendance-api-1411956859.azurewebsites.net";

    public BackendApi(ILogger<BackendApi> logger)
    {
        _logger = logger;
        
        // Create HttpClient with proper timeout and configuration
        var handler = new HttpClientHandler
        {
            // Allow automatic decompression for better performance
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30) // 30 second timeout for mobile networks
        };
        
        // Set default headers
        _http.DefaultRequestHeaders.Add("User-Agent", "Rascor-Mobile/1.0");
        
        _logger.LogInformation("BackendApi initialized with base URL: {BaseUrl}, Timeout: {Timeout}s", 
            BaseUrl, _http.Timeout.TotalSeconds);
    }

    public async Task<MobileBootstrapResponse?> GetConfigAsync(string userId, CancellationToken ct = default)
    {
        const int maxRetries = 2;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Fetching config for user {UserId} from {Url} (attempt {Attempt}/{Max})", 
                    userId, $"{BaseUrl}/config/mobile?userId={userId}", attempt, maxRetries);
                
                // Use CancellationTokenSource to enforce our own timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(30));
                
                var response = await _http.GetFromJsonAsync<MobileBootstrapResponse>(
                    $"/config/mobile?userId={userId}", cts.Token);
                
                _logger.LogInformation("Fetched config for user {UserId}: {SiteCount} sites", 
                    userId, response?.Sites.Count ?? 0);
                
                return response;
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                lastException = ex;
                _logger.LogWarning("Request timed out (attempt {Attempt}/{Max})", attempt, maxRetries);
                if (attempt < maxRetries) await Task.Delay(1000, ct); // Wait 1s before retry
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                lastException = ex;
                _logger.LogWarning("Request timed out (attempt {Attempt}/{Max})", attempt, maxRetries);
                if (attempt < maxRetries) await Task.Delay(1000, ct);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Network error (attempt {Attempt}/{Max}): {Message}", attempt, maxRetries, ex.Message);
                if (attempt < maxRetries) await Task.Delay(1000, ct);
            }
        }

        // All retries failed
        _logger.LogError(lastException, "Failed to fetch config after {MaxRetries} attempts", maxRetries);
        throw new Exception($"Could not connect to server after {maxRetries} attempts. {lastException?.Message}", lastException);
    }

    public async Task PostGeofenceEventAsync(GeofenceEventRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Posting {EventType} event for site {SiteId}", 
                request.EventType, request.SiteId);
            
            // Use CancellationTokenSource to enforce our own timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            
            var response = await _http.PostAsJsonAsync("/events/geofence", request, cts.Token);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Posted geofence {EventType} event for site {SiteId}", 
                request.EventType, request.SiteId);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Request timed out posting geofence event");
            throw new Exception("Connection timed out.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Request timed out posting geofence event");
            throw new Exception("Connection timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error posting geofence event. Message: {Message}", ex.Message);
            throw new Exception($"Cannot reach server. Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post geofence event");
            throw;
        }
    }
}
