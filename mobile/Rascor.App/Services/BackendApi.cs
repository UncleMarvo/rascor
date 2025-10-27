using Microsoft.Extensions.Logging;
using Rascor.App.Core;
using Rascor.App.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Rascor.App.Services;

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

    public void TestMethod()
    {
        Console.WriteLine("🟢🔵🔵 TestMethod called!");
        _logger.LogWarning("🟢🔵🔵 TestMethod executed");
    }

    public async Task<bool> UploadRamsPhotoAsync(RamsPhoto photo)
    {
        try
        {
            // ========================================================================
            _logger.LogWarning("🔵 HttpClient BaseAddress: {Base}", _http.BaseAddress);
            _logger.LogWarning("🔵 Full URL will be: {Url}", new Uri(_http.BaseAddress, "/api/rams-photos/upload"));

            // Test if we can reach ANY endpoint
            try
            {
                _logger.LogWarning("🧪 Quick test: GET /api/rams-photos/upload");
                var testGet = await _http.GetAsync("/api/rams-photos/upload");
                _logger.LogWarning("🧪 GET result: {Status}", testGet.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🧪 GET test failed");
            }
            // ========================================================================



            _logger.LogWarning("🔵 Step 1: Check file exists: {Path}", photo.LocalFilePath);

            if (!File.Exists(photo.LocalFilePath))
            {
                _logger.LogError("Photo file not found: {Path}", photo.LocalFilePath);
                return false;
            }

            _logger.LogWarning("🔵 Step 2: Reading file bytes...");

            // Load image and compress more aggressively
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(photo.LocalFilePath);

            // Resize to max 1280px (smaller than before)
            int maxDimension = 1280;
            double ratio = Math.Min((double)maxDimension / image.Width, (double)maxDimension / image.Height);

            if (ratio < 1)
            {
                int newWidth = (int)(image.Width * ratio);
                int newHeight = (int)(image.Height * ratio);
                image.Mutate(x => x.Resize(newWidth, newHeight));
                _logger.LogWarning("🔵 Image resized to {Width}x{Height}", newWidth, newHeight);
            }

            // Save to memory stream with lower quality
            using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
            {
                Quality = 70  // Lower quality = smaller file
            });

            var fileBytes = ms.ToArray();

            _logger.LogWarning("🔵 Step 3: Compressed to {Size}KB", fileBytes.Length / 1024);

            _logger.LogWarning("🔵 Step 4: Converting to base64...");

            var base64String = Convert.ToBase64String(fileBytes);

            _logger.LogWarning("🔵 Step 5: Base64 length: {Length} (~{Size}KB)", base64String.Length, base64String.Length / 1024);

            // Create JSON payload
            var payload = new
            {
                userId = photo.UserId,
                siteId = photo.SiteId,
                capturedAt = photo.CapturedAt.ToString("O"),
                fileName = Path.GetFileName(photo.LocalFilePath),
                photoBase64 = base64String
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);

            _logger.LogWarning("🔵 Step 8: JSON size: {Size}KB", json.Length / 1024);

            // Only proceed if under 500KB
            if (json.Length > 500 * 1024)
            {
                _logger.LogError("🔴 Payload too large: {Size}KB, max 500KB", json.Length / 1024);
                return false;
            }

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogWarning("🔵 Step 9: Sending POST request...");

            // ========================================================================

            // TEST WITH TINY PAYLOAD FIRST
            try
            {
                _logger.LogWarning("🧪 Testing with tiny payload...");
                var tinyPayload = new { userId = "test", test = "data" };
                var tinyJson = System.Text.Json.JsonSerializer.Serialize(tinyPayload);
                var tinyContent = new StringContent(tinyJson, System.Text.Encoding.UTF8, "application/json");

                using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var testResponse = await _http.PostAsync("/api/rams-photos/upload", tinyContent, testCts.Token);
                _logger.LogWarning("🧪 Tiny test result: {Status}", testResponse.StatusCode);
            }
            catch (Exception testEx)
            {
                _logger.LogError(testEx, "🧪 Tiny test failed: {Message}", testEx.Message);
            }

            //=========================================================================



            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await _http.PostAsync("/api/rams-photos/upload", content, cts.Token);

            _logger.LogWarning("🔵 Step 10: Response received: {Status}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ RAMS photo uploaded successfully");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("❌ Upload failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("🔴 Upload timed out after 15 seconds");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔴 Exception: {Message}", ex.Message);
            return false;
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

    public class GeofenceEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TriggerMethod { get; set; } = string.Empty;
    }
}
