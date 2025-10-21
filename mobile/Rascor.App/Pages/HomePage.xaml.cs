using Microsoft.Extensions.Logging;
using Rascor.App.Services;
using Shiny.Locations;

namespace Rascor.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly ILogger<HomePage> _logger;
    private readonly ConfigService _configService;
    private readonly IGpsManager _gpsManager;
    private GpsReading? _lastReading;
    private IDisposable? _gpsSubscription;

    public HomePage(
        ILogger<HomePage> logger,
        ConfigService configService,
        IGpsManager gpsManager)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
        _gpsManager = gpsManager;
        
        // Subscribe to GPS updates
        _gpsSubscription = _gpsManager.WhenReading().Subscribe(reading =>
        {
            _lastReading = reading;
            MainThread.BeginInvokeOnMainThread(() => UpdateLocationStatus());
        });
        
        LoadDashboard();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshDashboard();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _gpsSubscription?.Dispose();
    }

    private void LoadDashboard()
    {
        // Set greeting based on time of day
        var hour = DateTime.Now.Hour;
        var greeting = hour < 12 ? "Good morning! 👋" : hour < 18 ? "Good afternoon! 👋" : "Good evening! 👋";
        GreetingLabel.Text = greeting;

        // Load real data from services
        UpdateLocationStatus();
        UpdateWorkAssignments();
        UpdateSyncStatus();
        UpdateWeeklyStats();
    }

    private void RefreshDashboard()
    {
        // Refresh data when page appears
        LoadDashboard();
    }

    private void UpdateLocationStatus()
    {
        try
        {
            // Use the last reading from our subscription
            var reading = _lastReading;
            
            if (reading == null)
            {
                LocationLabel.Text = "Location unavailable";
                CheckInTimeLabel.IsVisible = false;
                return;
            }

            var sites = _configService.Sites;
            if (sites.Count == 0)
            {
                LocationLabel.Text = "No sites configured";
                CheckInTimeLabel.IsVisible = false;
                return;
            }

            // Check if inside any site
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
                    currentSiteName = site.Name;
                    break;
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }

            if (currentSiteName != null)
            {
                LocationLabel.Text = $"📍 At: {currentSiteName}";
                LocationLabel.TextColor = Colors.Green;
                CheckInTimeLabel.Text = $"Last update: {DateTime.Now:HH:mm}";
                CheckInTimeLabel.IsVisible = true;
            }
            else
            {
                LocationLabel.Text = $"Not at any site (closest: {closestDistance:F0}m)";
                LocationLabel.TextColor = Colors.Orange;
                CheckInTimeLabel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update location status");
            LocationLabel.Text = "Location error";
            CheckInTimeLabel.IsVisible = false;
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
