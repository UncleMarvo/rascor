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
    private readonly IGpsManager _gpsManager;
    private readonly IGeofenceManager _geofenceManager;

    public SettingsPage(
        ILogger<SettingsPage> logger,
        ConfigService configService,
        DeviceIdentityService deviceIdentity,
        IGpsManager gpsManager,
        IGeofenceManager geofenceManager)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
        _deviceIdentity = deviceIdentity;
        _gpsManager = gpsManager;
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

    private async void UpdateGeofenceStatus()
    {
        try
        {
            var monitoredRegions = await _geofenceManager.GetMonitorRegions();
            var count = monitoredRegions?.Count() ?? 0;
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

            _logger.LogInformation("Geofence status: {Count} monitored, {SiteCount} configured", count, siteCount);
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
            var button = (Button)sender;
            button.IsEnabled = false;
            button.Text = "Starting...";

            await _configService.InitializeAsync();
            
            // Refresh geofence status after initialization
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
        try
        {
            var button = (Button)sender;
            button.IsEnabled = false;
            button.Text = "Checking...";

            // Get current location
            var reading = await _gpsManager.GetCurrentPosition(TimeSpan.FromSeconds(10));
            
            if (reading == null)
            {
                await DisplayAlert("Location", "Could not get current location. Check GPS permissions.", "OK");
                button.Text = "Check Current Location";
                button.IsEnabled = true;
                return;
            }

            var lat = reading.Position.Latitude;
            var lon = reading.Position.Longitude;
            var accuracy = reading.PositionAccuracy;

            // Check if inside any site
            var sites = _configService.Sites;
            if (sites.Count == 0)
            {
                await DisplayAlert("Location", 
                    $"📍 Current Location:\n" +
                    $"Lat: {lat:F6}\n" +
                    $"Lon: {lon:F6}\n" +
                    $"Accuracy: {accuracy:F0}m\n\n" +
                    $"⚠️ No sites configured yet. Run 'Start Monitoring' first.",
                    "OK");
                button.Text = "Check Current Location";
                button.IsEnabled = true;
                return;
            }

            // Find which site(s) user is in or nearest
            var insideSites = new List<string>();
            string? nearestSite = null;
            double nearestDistance = double.MaxValue;

            foreach (var site in sites)
            {
                var distance = CalculateDistance(lat, lon, site.Latitude, site.Longitude);
                
                if (distance <= site.RadiusMeters)
                {
                    insideSites.Add($"{site.Name} ({distance:F0}m from center)");
                }

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSite = site.Name;
                }
            }

            var message = $"📍 Current Location:\n" +
                         $"Lat: {lat:F6}\n" +
                         $"Lon: {lon:F6}\n" +
                         $"Accuracy: {accuracy:F0}m\n\n";

            if (insideSites.Count > 0)
            {
                message += $"✅ Inside {insideSites.Count} site(s):\n" + string.Join("\n", insideSites);
            }
            else
            {
                message += $"❌ Not inside any site\n" +
                          $"Nearest: {nearestSite} ({nearestDistance:F0}m away)";
            }

            await DisplayAlert("Current Location", message, "OK");

            button.Text = "Check Current Location";
            button.IsEnabled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check current location");
            await DisplayAlert("Error", $"Failed to get location: {ex.Message}", "OK");
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
