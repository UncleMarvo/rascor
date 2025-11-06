using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rascor.Application.Interfaces.Procore;
using Rascor.Application.Models.Procore;
using Rascor.Infrastructure.Configuration.Procore;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Rascor.Infrastructure.ExternalServices.Procore;

public class ProcoreApiClient : IProcoreApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IProcoreTokenManager _tokenManager;
    private readonly ProcoreConfiguration _config;
    private readonly ILogger<ProcoreApiClient> _logger;

    public ProcoreApiClient(
        HttpClient httpClient,
        IProcoreTokenManager tokenManager,
        IOptions<ProcoreConfiguration> config,
        ILogger<ProcoreApiClient> logger)
    {
        _httpClient = httpClient;
        _tokenManager = tokenManager;
        _config = config.Value;
        _logger = logger;

        // Configure HttpClient base address
        _httpClient.BaseAddress = new Uri(_config.ApiBaseUrl);
    }

    /// <summary>
    /// Fetches all projects from Procore, optionally filtering by update date
    /// </summary>
    /// <param name="updatedSince">Only fetch projects updated after this date (for incremental sync)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of Procore projects</returns>
    public async Task<List<ProcoreProject>> GetProjectsAsync(
        DateTime? updatedSince = null,
        CancellationToken cancellationToken = default)
    {
        var projects = new List<ProcoreProject>();
        var page = 1;
        var perPage = 100;
        var hasMorePages = true;

        _logger.LogInformation(
            "Fetching projects from Procore (Company ID: {CompanyId}, Updated Since: {UpdatedSince})",
            _config.CompanyId, updatedSince?.ToString("o") ?? "all time");

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var pageProjects = await FetchProjectPageAsync(page, perPage, updatedSince, cancellationToken);

                if (pageProjects.Count == 0)
                {
                    hasMorePages = false;
                }
                else
                {
                    projects.AddRange(pageProjects);
                    _logger.LogDebug("Fetched page {Page} with {Count} projects", page, pageProjects.Count);

                    // Check if we got a full page (indicates more pages might exist)
                    hasMorePages = pageProjects.Count == perPage;
                    page++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching projects page {Page}", page);
                throw;
            }
        }

        _logger.LogInformation("Successfully fetched {TotalCount} projects from Procore", projects.Count);
        return projects;
    }

    private async Task<List<ProcoreProject>> FetchProjectPageAsync(
        int page,
        int perPage,
        DateTime? updatedSince,
        CancellationToken cancellationToken)
    {
        var accessToken = await _tokenManager.GetAccessTokenAsync(cancellationToken);

        var url = $"/rest/{_config.ApiVersion}/projects?company_id={_config.CompanyId}&page={page}&per_page={perPage}";

        // CHANGE THIS - Procore wants a range, not a single date
        if (updatedSince.HasValue)
        {
            var startDate = updatedSince.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // THREE dots, and the whole thing needs to be quoted
            var dateRange = $"\"{startDate}...{endDate}\"";
            url += $"&filters[updated_at]={Uri.EscapeDataString(dateRange)}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        _logger.LogWarning("🔵🔵🔵 PROCORE API CALL 🔵🔵🔵 URL: {Url}", url);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "❌❌❌ PROCORE API FAILED ❌❌❌ Status: {StatusCode}, Error: {Error}",
                response.StatusCode, errorContent);

            throw new HttpRequestException(
                $"Procore API request failed with status {response.StatusCode}: {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        _logger.LogWarning("🟢 PROCORE RESPONSE RECEIVED");
        _logger.LogWarning("📏 Length: {Length} characters", content.Length);
        _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        _logger.LogWarning("📄 RESPONSE CONTENT (first 3000 chars):\n{Content}",
            content.Substring(0, Math.Min(3000, content.Length)));

        _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // Try to parse and log structure
        try
        {
            using var jsonDoc = JsonDocument.Parse(content);
            _logger.LogWarning("✅ JSON IS VALID - Root type: {Type}", jsonDoc.RootElement.ValueKind);

            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var arrayLength = jsonDoc.RootElement.GetArrayLength();
                _logger.LogWarning("📊 JSON Array has {Count} elements", arrayLength);

                if (arrayLength > 0)
                {
                    var firstElement = jsonDoc.RootElement[0];
                    var firstJson = firstElement.GetRawText();
                    _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    _logger.LogWarning("🎯 FIRST PROJECT RAW JSON:");
                    _logger.LogWarning("{Json}", firstJson.Substring(0, Math.Min(1500, firstJson.Length)));
                    _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                }
            }
            else
            {
                _logger.LogError("⚠️⚠️⚠️ JSON ROOT IS NOT AN ARRAY! Type: {Type}", jsonDoc.RootElement.ValueKind);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥💥💥 FAILED TO PARSE JSON");
        }

        // Try deserialization
        List<ProcoreProject>? projects = null;
        try
        {
            _logger.LogWarning("🔄 ATTEMPTING DESERIALIZATION...");

            projects = JsonSerializer.Deserialize<List<ProcoreProject>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            _logger.LogWarning("✅ DESERIALIZATION SUCCESS - Projects count: {Count}", projects?.Count ?? 0);
            _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            if (projects != null && projects.Count > 0)
            {
                var first = projects[0];
                _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogWarning("🎯 FIRST DESERIALIZED PROJECT:");
                _logger.LogWarning("   ID: {Id}", first.Id);
                _logger.LogWarning("   Name: '{Name}'", first.Name ?? "❌ NULL");
                _logger.LogWarning("   DisplayName: '{DisplayName}'", first.DisplayName ?? "❌ NULL");
                _logger.LogWarning("   ProjectNumber: '{ProjectNumber}'", first.ProjectNumber ?? "❌ NULL");
                _logger.LogWarning("   Latitude: {Lat}", first.Latitude?.ToString() ?? "❌ NULL");
                _logger.LogWarning("   Longitude: {Long}", first.Longitude?.ToString() ?? "❌ NULL");
                _logger.LogWarning("   Active: {Active}", first.Active);
                _logger.LogWarning("   City: '{City}'", first.City ?? "❌ NULL");
                _logger.LogWarning("   Address: '{Address}'", first.Address ?? "❌ NULL");
                _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            else
            {
                _logger.LogError("❌❌❌ DESERIALIZATION RETURNED NULL OR EMPTY LIST!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥💥💥 DESERIALIZATION EXCEPTION");
        }

        _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        _logger.LogWarning("🏁 END OF PROCORE API PROCESSING");
        _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

        return projects ?? new List<ProcoreProject>();
    }
}