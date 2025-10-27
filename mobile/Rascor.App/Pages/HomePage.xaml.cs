using Microsoft.Extensions.Logging;
using Rascor.App.Core.Models;
using Rascor.App.Core.Services;
using Rascor.App.Services;
using Shiny.Locations;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

using AppGeofenceState = Rascor.App.Core.Services.GeofenceState;

namespace Rascor.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly ILogger<HomePage> _logger;
    private readonly ConfigService _configService;
    private readonly LocationTrackingService _locationTracking;
    private readonly BackendApi _backendApi;
    private readonly RamsPhotoService _ramsPhotoService;
    private readonly DeviceIdentityService _deviceIdentity;
    private readonly GeofenceStateService _stateService;


    public HomePage(
        ILogger<HomePage> logger,
        ConfigService configService,
        LocationTrackingService locationTracking,
        BackendApi backendApi,
        RamsPhotoService ramsPhotoService,
        DeviceIdentityService deviceIdentity,
        GeofenceStateService stateService)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
        _locationTracking = locationTracking;
        _backendApi = backendApi;
        _ramsPhotoService = ramsPhotoService;
        _deviceIdentity = deviceIdentity;
        _stateService = stateService;

        LoadDashboard();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to location updates
        _locationTracking.LocationUpdated += OnLocationUpdated;
        _locationTracking.GeofenceStateChanged += OnGeofenceStateChanged;
        _stateService.StateChanged += OnStateChanged;

        RefreshDashboard();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe to prevent memory leaks
        _locationTracking.LocationUpdated -= OnLocationUpdated;
        _locationTracking.GeofenceStateChanged -= OnGeofenceStateChanged;
        _stateService.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged(object? sender, AppGeofenceState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateLocationStatus();
        });
    }

    private void OnLocationUpdated(object? sender, GpsReading location)
    {
        // Update UI on main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateLocationStatus();
        });
    }

    private void OnGeofenceStateChanged(object? sender, EventArgs e)
    {
        // Update UI on main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateLocationStatus();
        });
    }

    private void LoadDashboard()
    {
        // Set greeting based on time of day
        var hour = DateTime.Now.Hour;
        var greeting = hour < 12 ? "Good morning! 👋" : hour < 18 ? "Good afternoon! 👋" : "Good evening! 👋";
        GreetingLabel.Text = greeting;

        // Load real data from services
        UpdateLocationStatus();
        //UpdateWorkAssignments();
        //UpdateSyncStatus();
        //UpdateWeeklyStats();
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
            
            if (sites == null || sites.Count == 0)
            {
                LocationLabel.Text = "No sites assigned";
                LocationLabel.TextColor = Colors.Gray;
                CheckInTimeLabel.IsVisible = false;
                WarningLabel.IsVisible = false;
                CheckInButton.IsVisible = false;
                CheckOutButton.IsVisible = false;
                TakeRamsPhotoButton.IsVisible = false;
                return;
            }

            // Get current location from LocationTrackingService
            var currentSite = _locationTracking.GetCurrentSite();
            var state = _stateService.CurrentState;


            if (state == AppGeofenceState.AtSite && _stateService.CurrentSiteId != null)
            {
                // User is checked in
                var site = sites.FirstOrDefault(s => s.Id == _stateService.CurrentSiteId);
                if (site != null)
                {
                    var checkInTime = _stateService.CheckInTime?.ToString("h:mm tt");
                    LocationLabel.Text = $"✅ {site.Name}\nChecked in at {checkInTime}";
                    LocationLabel.TextColor = Colors.Green;
                    CheckInTimeLabel.IsVisible = false;

                    // Check if they're away from site
                    if (currentSite == null)
                    {
                        var awayDuration = _stateService.AwayFromSiteStartTime.HasValue
                            ? DateTime.Now - _stateService.AwayFromSiteStartTime.Value
                            : TimeSpan.Zero;

                        if (awayDuration.TotalMinutes >= 2)
                        {
                            WarningLabel.Text = "⚠️ Still checked in\nYou've left the site";
                            WarningLabel.IsVisible = true;
                            CheckOutButton.IsVisible = true;
                        }
                        else
                        {
                            WarningLabel.IsVisible = false;
                            CheckOutButton.IsVisible = false;
                        }
                    }
                    else
                    {
                        WarningLabel.IsVisible = false;
                        CheckOutButton.IsVisible = false;
                    }

                    CheckInButton.IsVisible = false;
                    TakeRamsPhotoButton.IsVisible = true;
                }
            }
            else
            {
                // User is NOT checked in
                WarningLabel.IsVisible = false;
                CheckOutButton.IsVisible = false;

                if (currentSite != null)
                {
                    LocationLabel.Text = $"📍 At: {currentSite.Name}\nNot checked in";
                    LocationLabel.TextColor = Colors.Orange;
                    CheckInTimeLabel.IsVisible = false;
                    // Check wait time before showing button
                    var notAtSiteDuration = _stateService.NotAtSiteDisplayStartTime.HasValue
                        ? DateTime.Now - _stateService.NotAtSiteDisplayStartTime.Value
                        : TimeSpan.Zero;

                    CheckInButton.IsVisible = notAtSiteDuration.TotalMinutes >= 2;
                    TakeRamsPhotoButton.IsVisible = false;
                }
                else
                {
                    // Not at any site
                    var nearestInfo = _locationTracking.GetNearestSiteInfo();
                    if (nearestInfo != null)
                    {
                        var distance = nearestInfo.Value.distance;
                        var siteName = nearestInfo.Value.siteName;

                        LocationLabel.Text = $"Not at any site\n(nearest: {siteName}, {distance:F0}m away)";
                        LocationLabel.TextColor = Colors.Orange;
                    }
                    else
                    {
                        LocationLabel.Text = "Getting your location...";
                        LocationLabel.TextColor = Colors.Gray;
                    }

                    CheckInTimeLabel.IsVisible = false;
                    CheckInButton.IsVisible = false;
                    TakeRamsPhotoButton.IsVisible = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update location status");
            LocationLabel.Text = "Location error";
            LocationLabel.TextColor = Colors.Red;
            CheckInTimeLabel.IsVisible = false;
            WarningLabel.IsVisible = false;
            CheckInButton.IsVisible = false;
            CheckOutButton.IsVisible = false;
            TakeRamsPhotoButton.IsVisible = false;
        }
    }

    private async void OnCheckInClicked(object sender, EventArgs e)
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        CheckInButton.IsVisible = false;

        try
        {
            var location = _locationTracking.GetCurrentSite();
            if (location == null)
            {
                await DisplayAlert("Error", "No GPS location available", "OK");
                return;
            }

            var nearestSite = _configService.Sites?.FirstOrDefault(s =>
            {
                var distance = CalculateDistance(
                    _locationTracking.GetCurrentSite()?.Latitude ?? 0,
                    _locationTracking.GetCurrentSite()?.Longitude ?? 0,
                    s.Latitude,
                    s.Longitude
                );
                return distance <= s.ManualTriggerRadiusMeters;
            });

            if (nearestSite == null)
            {
                await DisplayAlert("Error", "No site within check-in range", "OK");
                CheckInButton.IsVisible = true;
                return;
            }

            var userId = _deviceIdentity.GetUserId();

            // Get last known GPS reading from location tracking
            var gpsAccuracy = 50.0; // Default reasonable accuracy

            var result = await _backendApi.ManualCheckInAsync(
                userId,
                nearestSite.Id,
                nearestSite.Latitude,
                nearestSite.Longitude,
                gpsAccuracy
            );

            if (result.Success)
            {
                _stateService.SetCheckedIn(nearestSite.Id);
                await DisplayAlert("Success", result.Message, "OK");
            }
            else
            {
                await DisplayAlert("Check-In Failed", result.Message, "OK");
                CheckInButton.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check-in error");
            await DisplayAlert("Error", "Check-in failed. Please try again.", "OK");
            CheckInButton.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            UpdateLocationStatus();
        }
    }

    private async void OnCheckOutClicked(object sender, EventArgs e)
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        CheckOutButton.IsVisible = false;

        try
        {
            var siteId = _stateService.CurrentSiteId;
            if (string.IsNullOrEmpty(siteId))
            {
                await DisplayAlert("Error", "Not currently checked in", "OK");
                return;
            }

            var userId = _deviceIdentity.GetUserId();

            var result = await _backendApi.ManualCheckOutAsync(
                userId,
                siteId,
                0, // Latitude - not critical for check-out
                0  // Longitude - not critical for check-out
            );

            if (result.Success)
            {
                _stateService.SetNotAtSite();
                await DisplayAlert("Success", result.Message, "OK");
            }
            else
            {
                await DisplayAlert("Check-Out Failed", result.Message, "OK");
                CheckOutButton.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check-out error");
            await DisplayAlert("Error", "Check-out failed. Please try again.", "OK");
            CheckOutButton.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            UpdateLocationStatus();
        }
    }

    private async void OnTakeRamsPhotoClicked(object sender, EventArgs e)
    {
        RamsPhoto? photo = null;
        string? currentSiteName = null;

        try
        {
            var currentSite = _locationTracking.GetCurrentSite();
            if (currentSite == null)
            {
                await DisplayAlert("Error", "You must be at a site", "OK");
                return;
            }

            currentSiteName = currentSite.Name;
            var userId = _deviceIdentity.GetUserId();

            // Show choice: Camera or Gallery
            var choice = await DisplayActionSheet(
                "RAMS Form Photo",
                "Cancel",
                null,
                "📷 Take Photo",
                "🖼️ Choose from Gallery"
            );

            if (choice == "Cancel" || choice == null)
            {
                return;
            }

            if (choice == "📷 Take Photo")
            {
                _logger.LogInformation("🔵 About to call TakePhotoAsync");
                photo = await _ramsPhotoService.TakePhotoAsync(userId, currentSite.Id, currentSite.Name);
                _logger.LogInformation("🔵 TakePhotoAsync completed, photo is {Result}", photo != null ? "not null" : "null");
            }
            else if (choice == "🖼️ Choose from Gallery")
            {
                _logger.LogInformation("🔵 About to call PickPhotoAsync");
                photo = await _ramsPhotoService.PickPhotoAsync(userId, currentSite.Id, currentSite.Name);
                _logger.LogInformation("🔵 PickPhotoAsync completed, photo is {Result}", photo != null ? "not null" : "null");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed in photo capture");
        }

        // Small delay to let camera activity fully close
        await Task.Delay(500);

        // Force back to main thread and show result
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                if (photo != null)
                {
                    _logger.LogInformation("🔵 About to upload RAMS photo...");
                    _logger.LogWarning("🔵 _ramsPhotoService null check: {IsNull}", _ramsPhotoService == null);

                    var uploaded = await _ramsPhotoService.UploadPhotoAsync(photo);

                    _logger.LogWarning("🔵 Upload completed, result: {Uploaded}", uploaded);

                    // version 1.0
                    var message = $"Photo saved locally for {currentSiteName}\n\nSize: {photo.FileSizeBytes / 1024:N0} KB\n";

                    // version 1.1
                    //var message = uploaded
                    //    ? $"Photo uploaded for {currentSiteName}!\n\nSize: {photo.FileSizeBytes / 1024:N0} KB"
                    //    : $"Photo saved for {currentSiteName}\n\nSize: {photo.FileSizeBytes / 1024:N0} KB\n\n⚠️ Will upload when online";

                    await DisplayAlert("✅ Photo Saved", message, "OK");
                }
                else
                {
                    await DisplayAlert("Cancelled", "No photo was captured", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("🔴 EXCEPTION caught in photo upload handler");
                _logger.LogWarning(ex, "🔴 EXCEPTION caught in photo upload handler: {Message}", ex.Message);
                await DisplayAlert("Error", $"Upload error: {ex.Message}", "OK");
            }
        });
    }

    //private void UpdateWorkAssignments()
    //{
    //    // TODO: Load from API/local storage
    //    WorkItemsContainer.Clear();
    //    TodaysWorkLabel.Text = "No work assignments today";
    //    TodaysWorkLabel.TextColor = Colors.Gray;
    //}

    //private void UpdateSyncStatus()
    //{
    //    // TODO: Check pending queue
    //    SyncStatusLabel.Text = "All synced ✓";
    //    SyncStatusLabel.TextColor = Colors.Green;
    //}

    //private void UpdateWeeklyStats()
    //{
    //    // TODO: Query local database
    //    SitesVisitedLabel.Text = "0";
    //    RamsSignedLabel.Text = "0";
    //}

    private async void OnSignNowClicked(object sender, EventArgs e)
    {
        // TODO: Navigate to RAMS viewer
        await DisplayAlert("Sign RAMS", "This will navigate to RAMS signing", "OK");
    }

    //private async void OnSyncNowClicked(object sender, EventArgs e)
    //{
    //    SyncButton.IsEnabled = false;
    //    SyncButton.Text = "Syncing...";

    //    // TODO: Trigger actual sync
    //    await Task.Delay(1000); // Simulate sync

    //    SyncButton.Text = "Sync Now";
    //    SyncButton.IsEnabled = true;

    //    await DisplayAlert("Sync Complete", "All data synced successfully", "OK");
    //}

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3;
        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δφ = (lat2 - lat1) * Math.PI / 180;
        var Δλ = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                Math.Cos(φ1) * Math.Cos(φ2) *
                Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}
