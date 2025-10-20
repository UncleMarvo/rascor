using Microsoft.Extensions.Logging;
using Rascor.App.Services;
using Rascor.App.Core;

namespace Rascor.App.Pages;

public partial class PermissionGuidePage : ContentPage
{
    private readonly ILogger<PermissionGuidePage> _logger;
    private readonly IGeofenceService _geofenceService;
    private readonly LocalNotificationService _notificationService;
    private bool _locationGranted = false;
    private bool _notificationGranted = false;

    public PermissionGuidePage(
        ILogger<PermissionGuidePage> logger,
        IGeofenceService geofenceService,
        LocalNotificationService notificationService)
    {
        InitializeComponent();
        _logger = logger;
        _geofenceService = geofenceService;
        _notificationService = notificationService;
        
        _ = CheckPermissionsAsync();
    }

    private async Task CheckPermissionsAsync()
    {
        try
        {
            // Check location permission
            _locationGranted = await _geofenceService.RequestPermissionsAsync();
            
            // Check notification permission
            _notificationGranted = await _notificationService.RequestPermissionAsync();
            
            // Update UI based on status
            UpdatePermissionUI();
            
            // If both granted, auto-continue
            if (_locationGranted && _notificationGranted)
            {
                StatusLabel.Text = "✅ All permissions granted!";
                await Task.Delay(1500);
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check permissions");
        }
    }

    private void UpdatePermissionUI()
    {
        // Hide/show location guide
        LocationBorder.IsVisible = !_locationGranted;
        
        // Hide/show notification guide
        NotificationBorder.IsVisible = !_notificationGranted;
        
        // Update status
        if (_locationGranted && _notificationGranted)
        {
            StatusLabel.Text = "✅ All permissions granted!";
            StatusLabel.TextColor = Colors.Green;
        }
        else if (_locationGranted)
        {
            StatusLabel.Text = "Location ✅ | Notifications ❌";
        }
        else if (_notificationGranted)
        {
            StatusLabel.Text = "Location ❌ | Notifications ✅";
        }
        else
        {
            StatusLabel.Text = "Location ❌ | Notifications ❌";
        }
    }

    private async void OnOpenSettingsClicked(object sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Opening app settings");
            AppInfo.Current.ShowSettingsUI();
            
            await DisplayAlert(
                "Settings Opened",
                "After updating permissions, return to this app and tap 'Check Again'.",
                "OK"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open settings");
            await DisplayAlert("Error", "Could not open settings. Please open Settings manually.", "OK");
        }
    }

    private async void OnCheckAgainClicked(object sender, EventArgs e)
    {
        CheckAgainBtn.Text = "Checking...";
        CheckAgainBtn.IsEnabled = false;
        
        await CheckPermissionsAsync();
        
        CheckAgainBtn.Text = "I've Updated Permissions - Check Again";
        CheckAgainBtn.IsEnabled = true;
    }

    private async void OnContinueAnywayClicked(object sender, EventArgs e)
    {
        var proceed = await DisplayAlert(
            "Warning",
            "Without proper permissions, the app cannot automatically track RASCOR. Are you sure you want to continue?",
            "Yes, Continue",
            "No, Go Back"
        );
        
        if (proceed)
        {
            _logger.LogWarning("User continued without full permissions");
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}