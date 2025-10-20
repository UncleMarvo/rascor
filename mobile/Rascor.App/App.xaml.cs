using Microsoft.Extensions.Logging;

namespace Rascor.App;

public partial class App : Application
{
    private readonly ILogger<App> _logger;

    public App(ILogger<App> logger)
    {
        InitializeComponent();
        _logger = logger;
        _logger.LogInformation("Rascor App started");
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
