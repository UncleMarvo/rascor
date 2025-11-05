using Microsoft.Extensions.Logging;
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
    private readonly DeviceIdentityService _deviceIdentity;
    private readonly GeofenceStateService _stateService;
    private readonly IServiceProvider _serviceProvider;

    public HomePage(
        ILogger<HomePage> logger,
        ConfigService configService,
        LocationTrackingService locationTracking,
        BackendApi backendApi,
        DeviceIdentityService deviceIdentity,
        GeofenceStateService stateService,
        IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
        _locationTracking = locationTracking;
        _backendApi = backendApi;
        _deviceIdentity = deviceIdentity;
        _stateService = stateService;
        _serviceProvider = serviceProvider;

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
