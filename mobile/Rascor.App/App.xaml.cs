using Microsoft.Extensions.Logging;
using Rascor.App.Services;

namespace Rascor.App;

public partial class App : Application
{
    private readonly ILogger<App> _logger;
    private readonly ConfigService _configService;

    public App(ILogger<App> logger, ConfigService configService)
    {
        InitializeComponent();
        _logger = logger;
        _logger.LogInformation("Rascor App started");
        _configService = configService;

        // Auto-initialize geofencing on app startup
        _ = InitializeGeofencingAsync();
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
                });
            }
        };
        
        return window;
    }
}
