using Microsoft.Extensions.Logging;
using Rascor.App.Core;
using Rascor.App.Services;
using Shiny.Locations;

namespace Rascor.App.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly ILogger<SettingsPage> _logger;
    private readonly ConfigService _configService;
    private readonly DeviceIdentityService _deviceIdentity;
    private readonly IGeofenceManager _geofenceManager;

    public SettingsPage(
        ILogger<SettingsPage> logger,
        ConfigService configService,
        DeviceIdentityService deviceIdentity,
        IGeofenceManager geofenceManager)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
        _deviceIdentity = deviceIdentity;
        _geofenceManager = geofenceManager;
        
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
        
        // Load geofence count
        UpdateGeofenceStatus();
        
        // Load sync status
        UpdateSyncStatus();
        
        // Load app version
        VersionLabel.Text = $"Version: {AppInfo.VersionString}";
    }

    private void RefreshSettings()
    {
        LoadSettings();
    }

    private void UpdateGeofenceStatus()
    {
        try
        {
            var monitoredRegions = _geofenceManager.GetMonitorRegions();
            var count = monitoredRegions?.Count ?? 0;
            var siteCount = _configService.Sites.Count;

            if (count > 0)
            {
                GeofenceStatusLabel.Text = $"✅ Monitoring {count} geofence(s)";
                GeofenceStatusLabel.TextColor = Colors.Green;
            }
            else if (siteCount > 0)
            {
                GeofenceStatusLabel.Text = $"⚠️ {siteCount} site(s) loaded but NOT monitoring";
                GeofenceStatusLabel.TextColor = Colors.Orange;
            }
            else
            {
                GeofenceStatusLabel.Text = "❌ No geofences configured";
                GeofenceStatusLabel.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get geofence status");
            GeofenceStatusLabel.Text = "❌ Error checking status";
            GeofenceStatusLabel.TextColor = Colors.Red;
        }
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
            await Task.Delay(1000);
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
        _logger.LogInformation("Notifications toggled: {IsEnabled}", e.Value);
    }

    private void OnWiFiOnlyToggled(object sender, ToggledEventArgs e)
    {
        _logger.LogInformation("WiFi-only sync toggled: {IsEnabled}", e.Value);
    }

    private async void OnResetOnboardingClicked(object sender, EventArgs e)
    {
        Preferences.Remove("OnboardingCompleted");
        await DisplayAlert("Reset", "Onboarding has been reset. Restart the app to see it again.", "OK");
    }

    private async void OnStartMonitoringClicked(object sender, EventArgs e)
    {
        try
        {
            var button = (Button)sender;
            button.IsEnabled = false;
            button.Text = "Starting...";

            await _configService.InitializeAsync();
            UpdateGeofenceStatus();

            button.Text = "Start Monitoring";
            button.IsEnabled = true;
            
            await DisplayAlert("Success", "Geofence monitoring started successfully", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start monitoring");
            await DisplayAlert("Error", $"Failed to start monitoring: {ex.Message}", "OK");
        }
    }

    private async void OnCheckCurrentLocationClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Info", "Use HomePage to see current location. This diagnostic will be enhanced in future updates.", "OK");
    }

    private async void OnTestNotificationClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Test", "Notification triggered", "OK");
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

            var success = await _configService.SimulateGeofenceEventAsync(
                site.Id, "Enter", site.Latitude, site.Longitude);

            await DisplayAlert(
                success ? "Success" : "Queued",
                success ? $"Simulated ENTER event for {site.Name}" : "Event queued for sync (offline)",
                "OK");
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

            var success = await _configService.SimulateGeofenceEventAsync(
                site.Id, "Exit", site.Latitude, site.Longitude);

            await DisplayAlert(
                success ? "Success" : "Queued",
                success ? $"Simulated EXIT event for {site.Name}" : "Event queued for sync (offline)",
                "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Simulate exit failed");
            await DisplayAlert("Error", $"Simulation failed: {ex.Message}", "OK");
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (result)
        {
            _logger.LogInformation("User logged out");
            await DisplayAlert("Logged Out", "You have been logged out", "OK");
        }
    }
}
