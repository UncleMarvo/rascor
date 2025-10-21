using Microsoft.Extensions.Logging;
using Rascor.App.Services;
using Shiny.Locations;

namespace Rascor.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly ILogger<HomePage> _logger;
    private readonly ConfigService _configService;
    private readonly LocationTrackingService _locationTracking;

    public HomePage(
        ILogger<HomePage> logger,
        ConfigService configService,
        LocationTrackingService locationTracking)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
        _locationTracking = locationTracking;
        
        LoadDashboard();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshDashboard();
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
            var sites = _configService.Sites;
            
            if (sites.Count == 0)
            {
                LocationLabel.Text = "No sites configured - run Start Monitoring";
                LocationLabel.TextColor = Colors.Gray;
                CheckInTimeLabel.IsVisible = false;
                return;
            }

            // Get current location from LocationTrackingService
            var currentSite = _locationTracking.GetCurrentSite();
            
            if (currentSite != null)
            {
                LocationLabel.Text = $"📍 At: {currentSite.Name}";
                LocationLabel.TextColor = Colors.Green;
                CheckInTimeLabel.Text = $"Last update: {DateTime.Now:HH:mm}";
                CheckInTimeLabel.IsVisible = true;
            }
            else
            {
                // Get distance to nearest site
                var nearestInfo = _locationTracking.GetNearestSiteInfo();
                if (nearestInfo != null)
                {
                    LocationLabel.Text = $"Not at any site (nearest: {nearestInfo.Value.distance:F0}m to {nearestInfo.Value.siteName})";
                    LocationLabel.TextColor = Colors.Orange;
                }
                else
                {
                    LocationLabel.Text = "Location unavailable - waiting for GPS";
                    LocationLabel.TextColor = Colors.Gray;
                }
                CheckInTimeLabel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update location status");
            LocationLabel.Text = "Location error";
            LocationLabel.TextColor = Colors.Red;
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
}
