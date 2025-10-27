using Rascor.App.Pages;

namespace Rascor.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        this.Navigated += (s, e) =>
        {
            PageTitleLabel.Text = CurrentPage?.Title ?? "";
        };

        // Register routes for modal/push navigation
        Routing.RegisterRoute(nameof(OnboardingPage), typeof(OnboardingPage));
        Routing.RegisterRoute(nameof(PermissionGuidePage), typeof(PermissionGuidePage));
    }
}
