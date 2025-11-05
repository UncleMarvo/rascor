using Microsoft.Extensions.Logging;
using Rascor.App.Services;
using Rascor.App.Helpers;

namespace Rascor.App;

public partial class App : Application
{
    private readonly ILogger<App> _logger;
    private readonly ConfigService _configService;
    private readonly RegistrationEmailService _emailService;
    private readonly DeviceIdentityService _deviceIdentityService;

    public App(
        ILogger<App> logger,
        ConfigService configService,
        RegistrationEmailService emailService,
        DeviceIdentityService deviceIdentityService)
    {
        InitializeComponent();

        _logger = logger;
        _logger.LogInformation("Rascor App started");

        _configService = configService;
        _emailService = emailService;

        // Auto-initialize geofencing on app startup
        _ = InitializeGeofencingAsync();
        _deviceIdentityService = deviceIdentityService;
    }

    private async Task InitializeGeofencingAsync()
    {
        try
        {
            _logger.LogInformation("Auto-initializing geofencing on app startup");
            await _configService.InitializeAsync();
            _logger.LogWarning("Geofencing initialized - monitoring {Count} sites", _configService.Sites.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-initialization failed - user will need to use Settings to initialize");
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // Check if this is first launch and navigate after window is ready
        window.Created += (s, e) =>
        {
            var onboardingCompleted = Preferences.Get("OnboardingCompleted", false);

            if (!onboardingCompleted)
            {
                _logger.LogInformation("First launch - showing onboarding");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100); // Brief delay to ensure Shell is ready
                    await Shell.Current.GoToAsync("OnboardingPage");

                    // send email
                    await CheckRegistrationEmailAsync();
                });
            }
            else
            {
                // Onboarding already done - check registration email
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(1000); // Wait for app to fully load
                });
            }
        };

        return window;
    }

    private async Task CheckRegistrationEmailAsync()
    {
        try
        {
            // Check if registration email already sent
            if (!PreferencesHelper.HasSentRegistrationEmail)
            {
                _logger.LogInformation("First run after onboarding - prompting for device registration");

                // Get both IDs
                var deviceUserId = GetDeviceUserId();
                var deviceIdentifier = GetDeviceIdentifier();

                // Prompt user
                bool shouldSend = await Shell.Current.DisplayAlert(
                    "Device Registration Required",
                    $"This device needs to be registered with your supervisor.\n\n" +
                    $"Device: {deviceIdentifier}\n" +
                    $"User ID: {deviceUserId}\n\n" +
                    $"An email will open - please send it to complete registration.",
                    "Open Email",
                    "Remind Me Later"
                );

                if (shouldSend)
                {
                    bool success = await _emailService.SendRegistrationEmailAsync(deviceUserId, deviceIdentifier);

                    if (!success)
                    {
                        _logger.LogWarning("Failed to open email app for registration");
                        await Shell.Current.DisplayAlert(
                            "Email Not Available",
                            "Could not open email app. Please use the 'Register Device' button in Settings.",
                            "OK"
                        );
                    }
                    else
                    {
                        _logger.LogInformation("Registration email opened successfully");
                    }
                }
                else
                {
                    _logger.LogInformation("User chose to register later");
                    await Shell.Current.DisplayAlert(
                        "Registration Reminder",
                        "You can register your device anytime from the Settings screen.",
                        "OK"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking registration email status");
        }
    }

    private string GetDeviceUserId()
    {
        return _deviceIdentityService.GetDeviceId();
    }

    private string GetDeviceIdentifier()
    {
        var deviceInfo = DeviceInfo.Current;

        // Format: Manufacturer Model (Platform Version)
        // Example: "Samsung SM-G991B (Android 13)" or "Apple iPhone 14 Pro (iOS 16.5)"
        string identifier = $"{deviceInfo.Manufacturer} {deviceInfo.Model} ({deviceInfo.Platform} {deviceInfo.VersionString})";

        return identifier;
    }
}