using Microsoft.Extensions.Logging;
using Rascor.App.Services;

namespace Rascor.App.Pages;

public partial class OnboardingPage : ContentPage
{
    private readonly ILogger<OnboardingPage> _logger;

    public OnboardingPage(ILogger<OnboardingPage> logger)
    {
        InitializeComponent();
        _logger = logger;
    }

    private async void OnGetStartedClicked(object sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("User completed onboarding");
            
            // Mark onboarding as complete
            Preferences.Set("OnboardingCompleted", true);
            
            // Navigate to main page
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete onboarding");
            await DisplayAlert("Error", "Failed to proceed. Please try again.", "OK");
        }
    }
}