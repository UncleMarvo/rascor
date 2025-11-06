using Microsoft.Extensions.Logging;
using Rascor.App.Core;
using System.Net.Http.Json;

namespace Rascor.App.Services;

/// <summary>
/// Exception thrown when API returns a validation error (400 Bad Request)
/// These errors should NOT be queued because they will never succeed on retry
/// </summary>
public class ApiValidationException : Exception
{
    public int StatusCode { get; }
    public string? ErrorResponse { get; }

    public ApiValidationException(int statusCode, string message, string? errorResponse = null) 
        : base(message)
    {
        StatusCode = statusCode;
        ErrorResponse = errorResponse;
    }
}

public class ManualCheckInResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? EventId { get; set; }
    public double? Distance { get; set; }
    public int? RequiredDistance { get; set; }
}

public class ManualCheckOutResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? EventId { get; set; }
}

public class BackendApi
{
    private readonly HttpClient _http;
    private readonly ILogger<BackendApi> _logger;
    private readonly Guid _instanceId = Guid.NewGuid();


    // =========================================================
    // Environment BaseUrl Settings
    // =========================================================
    // Local dev (emulator):
    // private const string BaseUrl = "http://10.0.2.2:5000";

    // Local dev (physical device - use your PC's IP):
    // private const string BaseUrl = "http://192.168.1.4:5000";

    // Production:
    private const string BaseUrl = "https://rascor-api-1761048156.azurewebsites.net";


    public BackendApi(ILogger<BackendApi> logger)
    {
        Console.WriteLine("🔵🔵🔵 BackendApi constructor STARTED");
        Console.WriteLine($"🔵🔵🔵 BackendApi constructor STARTED - Instance ID: {_instanceId}");

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
        
        _logger.LogInformation("🔵 BackendApi initialized with base URL: {BaseUrl}, Timeout: {Timeout}s",
            BaseUrl, _http.Timeout.TotalSeconds);

        Console.WriteLine("🔵🔵🔵 BackendApi constructor COMPLETED");
    }

    public async Task<MobileBootstrapResponse?> GetConfigAsync(string userId, CancellationToken ct = default)
    {
        const int maxRetries = 2;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = $"/config/mobile?userId={userId}";
                var fullUrl = $"{BaseUrl}{url}";

                _logger.LogInformation("Fetching config for user {UserId} from {Url} (attempt {Attempt}/{Max})", 
                    userId, $"{fullUrl}", attempt, maxRetries);
                
                // Use CancellationTokenSource to enforce our own timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                var httpResponse = await _http.GetAsync(fullUrl, cts.Token);
                var rawContent = await httpResponse.Content.ReadAsStringAsync();
                //var response = await _http.GetFromJsonAsync<MobileBootstrapResponse>(
                //    $"{BaseUrl}/config/mobile?userId={userId}", cts.Token);

                _logger.LogWarning("URL: {URL}", url);
                _logger.LogWarning("User ID: {UserId}", fullUrl);
                _logger.LogWarning("Raw response: {Content}", rawContent);
                _logger.LogInformation("Status: {Status}", httpResponse.StatusCode);

                httpResponse.EnsureSuccessStatusCode();

                var response = await httpResponse.Content.ReadFromJsonAsync<MobileBootstrapResponse>(cancellationToken: cts.Token);

                _logger.LogInformation("Deserialized: {SiteCount} sites", response?.Sites?.Count ?? 0);

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
            
            // Log response details before checking status code
            // This helps diagnose API errors by showing what the server actually returned
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                var errorMessage = $"API returned error status {response.StatusCode} when posting geofence event. " +
                                 $"SiteId: {request.SiteId}, EventType: {request.EventType}, UserId: {request.UserId}. " +
                                 $"Response body: {errorBody}";
                
                _logger.LogError(errorMessage);
                
                // Report error to backend for production monitoring
                ReportErrorToBackend(
                    errorType: "ApiError",
                    userId: request.UserId,
                    siteId: request.SiteId,
                    eventType: request.EventType,
                    message: errorMessage);
                
                // Validation errors (400 Bad Request) should NOT be queued - they indicate permanent problems
                // Examples: "Site not found", invalid data format, etc.
                // These errors will never succeed on retry, so throw a special exception that won't be queued
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new ApiValidationException(
                        statusCode: (int)response.StatusCode,
                        message: $"Validation error: {errorBody}",
                        errorResponse: errorBody);
                }
            }
            
            // This will throw HttpRequestException if status code is not 2xx
            // (500 errors, 502, 503, etc. are transient and can be queued)
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Posted geofence {EventType} event for site {SiteId}", 
                request.EventType, request.SiteId);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            var errorMessage = $"Request timed out posting geofence event for site {request.SiteId}";
            _logger.LogError(ex, errorMessage);
            
            // Report timeout error to backend
            ReportErrorToBackend(
                errorType: "Timeout",
                userId: request.UserId,
                siteId: request.SiteId,
                eventType: request.EventType,
                message: errorMessage,
                exception: ex);
            
            throw new Exception("Connection timed out.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            var errorMessage = $"Request timed out posting geofence event for site {request.SiteId}";
            _logger.LogError(ex, errorMessage);
            
            // Report timeout error to backend
            ReportErrorToBackend(
                errorType: "Timeout",
                userId: request.UserId,
                siteId: request.SiteId,
                eventType: request.EventType,
                message: errorMessage,
                exception: ex);
            
            throw new Exception("Connection timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            // This exception is thrown by EnsureSuccessStatusCode() when status code is not 2xx
            // The error details should already be logged above, but we log here too for completeness
            var errorMessage = $"HTTP error posting geofence event. SiteId: {request.SiteId}, EventType: {request.EventType}, Message: {ex.Message}";
            _logger.LogError(ex, errorMessage);
            
            // Report HTTP error to backend
            ReportErrorToBackend(
                errorType: "NetworkError",
                userId: request.UserId,
                siteId: request.SiteId,
                eventType: request.EventType,
                message: errorMessage,
                exception: ex);
            
            throw new Exception($"Cannot reach server. Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Unexpected error posting geofence event. SiteId: {request.SiteId}, EventType: {request.EventType}";
            _logger.LogError(ex, errorMessage);
            
            // Report unexpected error to backend
            ReportErrorToBackend(
                errorType: "Unexpected",
                userId: request.UserId,
                siteId: request.SiteId,
                eventType: request.EventType,
                message: errorMessage,
                exception: ex);
            
            throw;
        }
    }

    public async Task<ManualCheckInResponse> ManualCheckInAsync(string userId, string siteId, double latitude, double longitude, double accuracy)
    {
        try
        {
            var request = new
            {
                userId = userId,
                siteId = siteId,
                latitude = latitude,
                longitude = longitude,
                accuracy = accuracy
            };

            _logger.LogInformation("Manual check-in attempt for site {SiteId}", siteId);

            var response = await _http.PostAsJsonAsync("/api/geofence-events/manual-checkin", request);  // Changed from _httpClient to _http

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ManualCheckInResponse>();
                _logger.LogInformation("Manual check-in successful: {Message}", result?.Message);
                return result ?? new ManualCheckInResponse { Success = false, Message = "Invalid response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Manual check-in failed: {Error}", errorContent);

                try
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<ManualCheckInResponse>();
                    return errorResult ?? new ManualCheckInResponse { Success = false, Message = errorContent };
                }
                catch
                {
                    return new ManualCheckInResponse { Success = false, Message = errorContent };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual check-in exception");
            return new ManualCheckInResponse { Success = false, Message = "Connection error" };
        }
    }

    public async Task<ManualCheckOutResponse> ManualCheckOutAsync(string userId, string siteId, double latitude, double longitude)
    {
        try
        {
            var request = new
            {
                userId = userId,  // Changed from _deviceId to userId parameter
                siteId = siteId,
                latitude = latitude,
                longitude = longitude
            };

            _logger.LogInformation("Manual check-out attempt for site {SiteId}", siteId);

            var response = await _http.PostAsJsonAsync("/api/geofence-events/manual-checkout", request);  // Changed from _httpClient to _http

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ManualCheckOutResponse>();
                _logger.LogInformation("Manual check-out successful: {Message}", result?.Message);
                return result ?? new ManualCheckOutResponse { Success = false, Message = "Invalid response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Manual check-out failed: {Error}", errorContent);

                try
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<ManualCheckOutResponse>();
                    return errorResult ?? new ManualCheckOutResponse { Success = false, Message = errorContent };
                }
                catch
                {
                    return new ManualCheckOutResponse { Success = false, Message = errorContent };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual check-out exception");
            return new ManualCheckOutResponse { Success = false, Message = "Connection error" };
        }
    }

    public async Task<List<GeofenceEventDto>> GetTodaysEventsAsync(string userId)
    {
        try
        {
            var today = DateTime.Today;
            var response = await _http.GetAsync($"/api/geofence-events/user/{userId}/today?date={today:yyyy-MM-dd}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return System.Text.Json.JsonSerializer.Deserialize<List<GeofenceEventDto>>(json, options) ?? new();
            }

            return new List<GeofenceEventDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get today's events");
            return new List<GeofenceEventDto>();
        }
    }

    /// <summary>
    /// Reports an error from the mobile app to the backend for production monitoring
    /// This is fire-and-forget - errors in reporting won't affect the app
    /// </summary>
    private void ReportErrorToBackend(
        string errorType,
        string userId,
        string? siteId,
        string? eventType,
        string message,
        Exception? exception = null)
    {
        // Fire-and-forget: Don't await, don't block the calling thread
        // If error reporting fails, we don't want it to affect the app
        _ = Task.Run(async () =>
        {
            try
            {
                var errorReport = new
                {
                    ErrorType = errorType,
                    UserId = userId,
                    SiteId = siteId,
                    EventType = eventType,
                    Message = message,
                    StackTrace = exception?.StackTrace,
                    Timestamp = DateTime.UtcNow
                };

                // Use a short timeout for error reporting (5 seconds)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _http.PostAsJsonAsync("/api/errors/report", errorReport, cts.Token);
                
                // Don't log success - we don't want to spam logs
                // Only log if reporting itself fails
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to report error to backend: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // Silently fail - we don't want error reporting errors to cause issues
                // Only log in debug scenarios
                _logger.LogDebug(ex, "Error reporting to backend failed (this is expected if offline)");
            }
        });
    }

    public class GeofenceEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TriggerMethod { get; set; } = string.Empty;
    }
}
