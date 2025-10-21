using Microsoft.Extensions.Logging;
using Rascor.App.Services;
using Shiny.Locations;

namespace Rascor.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly ILogger<HomePage> _logger;
    private readonly ConfigService _configService;
    private readonly LocationTrackingService _locationTrackingService;
    private readonly IGpsManager _gpsManager;
    private System.Timers.Timer? _locationTimer;

    public HomePage(
        ILogger<HomePage> logger,
        ConfigService configService,
        LocationTrackingService locationTrackingService,
        IGpsManager gpsManager)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
        _locationTrackingService = locationTrackingService;
        _gpsManager = gpsManager;
        
        LoadDashboard();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshDashboard();
        StartLocationPolling();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopLocationPolling();
    }

    private void StartLocationPolling()
    {
        // Poll current location every 5 seconds while on HomePage
        _locationTimer = new System.Timers.Timer(5000);
        _locationTimer.Elapsed += async (s, e) =>
        {
            await UpdateLocationStatusAsync();
        };
        _locationTimer.Start();
        _logger.LogInformation("📍 Started location polling on HomePage");
    }

    private void StopLocationPolling()
    {
        _locationTimer?.Stop();
        _locationTimer?.Dispose();
        _locationTimer = null;
        _logger.LogInformation("🛑 Stopped location polling on HomePage");
    }

    private void LoadDashboard()
    {
        // Set greeting based on time of day
        var hour = DateTime.Now.Hour;
        var greeting = hour < 12 ? "Good morning! 👋" : hour < 18 ? "Good afternoon! 👋" : "Good evening! 👋";
        GreetingLabel.Text = greeting;

        // Load real data from services
        _ = UpdateLocationStatusAsync();
        UpdateWorkAssignments();
        UpdateSyncStatus();
        UpdateWeeklyStats();
    }

    private void RefreshDashboard()
    {
        // Refresh data when page appears
        LoadDashboard();
    }

    private async Task UpdateLocationStatusAsync()
    {
        try
        {
            // Get current location from Shiny GPS
            var lastReading = _gpsManager.GetLastReading();
            
            GpsReading? reading = null;
            if (lastReading == null)
            {
                // Try to get a fresh reading with CancellationToken
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    reading = await _gpsManager.GetCurrentPosition(new GpsRequest(), cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("GPS timeout - no location available");
                }
            }
            else
            {
                reading = lastReading;
            }

            if (reading == null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LocationLabel.Text = "Location unavailable";
                    CheckInTimeLabel.IsVisible = false;
                });
                return;
            }

            // Check if inside any site
            var sites = _configService.Sites;
            if (sites.Count == 0)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LocationLabel.Text = "No sites configured";
                    CheckInTimeLabel.IsVisible = false;
                });
                return;
            }

            // Find closest/inside site
            string? currentSiteId = null;
            string? currentSiteName = null;
            double closestDistance = double.MaxValue;

            foreach (var site in sites)
            {
                var distance = CalculateDistance(
                    reading.Position.Latitude,
                    reading.Position.Longitude,
                    site.Latitude,
                    site.Longitude
                );

                if (distance <= site.RadiusMeters)
                {
                    // Inside this site
                    currentSiteId = site.Id;
                    currentSiteName = site.Name;
                    break;
                }

                // Track closest even if not inside
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (currentSiteId != null)
                {
                    LocationLabel.Text = $"📍 At: {currentSiteName}";
                    LocationLabel.TextColor = Colors.Green;
                    CheckInTimeLabel.Text = $"Checked in at {DateTime.Now:HH:mm}";
                    CheckInTimeLabel.IsVisible = true;
                    _logger.LogInformation("✅ User is at site: {SiteName}", currentSiteName);
                }
                else
                {
                    LocationLabel.Text = $"Not at any site (closest: {closestDistance:F0}m away)";
                    LocationLabel.TextColor = Colors.Orange;
                    CheckInTimeLabel.IsVisible = false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update location status");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LocationLabel.Text = "Location error";
                CheckInTimeLabel.IsVisible = false;
            });
        }
    }

    private void UpdateWorkAssignments()
    {
        // TODO: Load from API/local storage
        WorkItemsContainer.Clear();
        TodaysWorkLabel.Text = "No work assignments today";
        TodaysWorkLabel.TextColor = Colors.Gray;
    }

    private void UpdateSyncStatus()
    {
        // TODO: Check pending queue
        SyncStatusLabel.Text = "All synced ✓";
        SyncStatusLabel.TextColor = Colors.Green;
    }

    private void UpdateWeeklyStats()
    {
        // TODO: Query local database
        SitesVisitedLabel.Text = "0";
        RamsSignedLabel.Text = "0";
    }

    private async void OnSignNowClicked(object sender, EventArgs e)
    {
        // TODO: Navigate to RAMS viewer
        await DisplayAlert("Sign RAMS", "This will navigate to RAMS signing", "OK");
    }

    private async void OnSyncNowClicked(object sender, EventArgs e)
    {
        SyncButton.IsEnabled = false;
        SyncButton.Text = "Syncing...";

        // TODO: Trigger actual sync
        await Task.Delay(1000); // Simulate sync

        SyncButton.Text = "Sync Now";
        SyncButton.IsEnabled = true;
        
        await DisplayAlert("Sync Complete", "All data synced successfully", "OK");
    }

    /// <summary>
    /// Calculate distance between two GPS coordinates using Haversine formula
    /// </summary>
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMeters = 6371000;
        
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return EarthRadiusMeters * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
