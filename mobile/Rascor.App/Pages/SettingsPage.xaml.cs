using Microsoft.Extensions.Logging;
using Rascor.App.Core;
using Rascor.App.Services;

namespace Rascor.App.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly ILogger<SettingsPage> _logger;
    private readonly ConfigService _configService;
    private readonly DeviceIdentityService _deviceIdentity;

    public SettingsPage(
        ILogger<SettingsPage> logger,
        ConfigService configService,
        DeviceIdentityService deviceIdentity)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
        _deviceIdentity = deviceIdentity;
        
        LoadSettings();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshSettings();
    }

    private void LoadSettings()
    {
        // Load user info from DeviceIdentityService
        UserIdLabel.Text = $"User ID: {_configService.CurrentUserId}";
        DeviceIdLabel.Text = $"Device ID: {_deviceIdentity.GetDeviceId()}";
        
        // Load sync status
        UpdateSyncStatus();
        
        // Load app version
        VersionLabel.Text = $"Version: {AppInfo.VersionString}";
    }

    private void RefreshSettings()
    {
        LoadSettings();
    }

    private void UpdateSyncStatus()
    {
        // TODO: Get actual pending count from queue
        var pendingCount = 0;
        
        PendingItemsLabel.Text = $"Pending items: {pendingCount}";
        
        if (pendingCount > 0)
        {
            SyncStatusLabel.Text = $"Status: {pendingCount} items pending";
            SyncStatusLabel.TextColor = Colors.Orange;
        }
        else
        {
            SyncStatusLabel.Text = "Status: All synced ✓";
            SyncStatusLabel.TextColor = Colors.Green;
        }
    }

    private async void OnCopyDeviceIdClicked(object sender, EventArgs e)
    {
        var deviceId = _deviceIdentity.GetDeviceId();
        await Clipboard.SetTextAsync(deviceId);
        await DisplayAlert("Copied", $"Device ID copied to clipboard", "OK");
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        button.IsEnabled = false;
        button.Text = "Syncing...";

        try
        {
            // TODO: Trigger actual sync from queue
            await Task.Delay(1000); // Simulate sync
            
            UpdateSyncStatus();
            await DisplayAlert("Success", "All data synced successfully", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
            await DisplayAlert("Error", "Sync failed. Please try again.", "OK");
        }
        finally
        {
            button.Text = "Sync Offline Events";
            button.IsEnabled = true;
        }
    }

    private void OnNotificationsToggled(object sender, ToggledEventArgs e)
    {
        // TODO: Save preference to local storage
        _logger.LogInformation("Notifications toggled: {IsEnabled}", e.Value);
    }

    private void OnWiFiOnlyToggled(object sender, ToggledEventArgs e)
    {
        // TODO: Save preference to local storage
        _logger.LogInformation("WiFi-only sync toggled: {IsEnabled}", e.Value);
    }

    // ========== Testing Tools (from old MainPage) ==========

    private async void OnResetOnboardingClicked(object sender, EventArgs e)
    {
        Preferences.Remove("OnboardingCompleted");
        await DisplayAlert("Reset", "Onboarding has been reset. Restart the app to see it again.", "OK");
    }

    private async void OnStartMonitoringClicked(object sender, EventArgs e)
    {
        try
        {
            await _configService.InitializeAsync();
            await DisplayAlert("Success", "Geofence monitoring started", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start monitoring");
            await DisplayAlert("Error", $"Failed to start monitoring: {ex.Message}", "OK");
        }
    }

    private async void OnTestNotificationClicked(object sender, EventArgs e)
    {
        try
        {
            // TODO: Trigger test notification
            await DisplayAlert("Test", "Notification triggered", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test notification failed");
            await DisplayAlert("Error", $"Test failed: {ex.Message}", "OK");
        }
    }

    private async void OnSimulateEnterClicked(object sender, EventArgs e)
    {
        try
        {
            var site = _configService.Sites.FirstOrDefault();
            if (site == null)
            {
                await DisplayAlert("Error", "No sites configured. Run 'Start Monitoring' first.", "OK");
                return;
            }

            _logger.LogInformation("Simulating ENTER event for site: {SiteId}", site.Id);
            var success = await _configService.SimulateGeofenceEventAsync(
                site.Id, 
                "Enter", 
                site.Latitude, 
                site.Longitude
            );

            if (success)
                await DisplayAlert("Success", $"Simulated ENTER event for {site.Name}", "OK");
            else
                await DisplayAlert("Queued", $"Event queued for sync (offline)", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Simulate enter failed");
            await DisplayAlert("Error", $"Simulation failed: {ex.Message}", "OK");
        }
    }

    private async void OnSimulateExitClicked(object sender, EventArgs e)
    {
        try
        {
            var site = _configService.Sites.FirstOrDefault();
            if (site == null)
            {
                await DisplayAlert("Error", "No sites configured. Run 'Start Monitoring' first.", "OK");
                return;
            }

            _logger.LogInformation("Simulating EXIT event for site: {SiteId}", site.Id);
            var success = await _configService.SimulateGeofenceEventAsync(
                site.Id, 
                "Exit", 
                site.Latitude, 
                site.Longitude
            );

            if (success)
                await DisplayAlert("Success", $"Simulated EXIT event for {site.Name}", "OK");
            else
                await DisplayAlert("Queued", $"Event queued for sync (offline)", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Simulate exit failed");
            await DisplayAlert("Error", $"Simulation failed: {ex.Message}", "OK");
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert(
            "Logout", 
            "Are you sure you want to logout?", 
            "Yes", 
            "No");

        if (result)
        {
            // TODO: Clear user session, stop monitoring, navigate to login
            _logger.LogInformation("User logged out");
            await DisplayAlert("Logged Out", "You have been logged out", "OK");
        }
    }
}
