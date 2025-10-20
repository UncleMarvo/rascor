using Rascor.App.Pages;

namespace Rascor.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes for navigation
        Routing.RegisterRoute("OnboardingPage", typeof(OnboardingPage));
        Routing.RegisterRoute("PermissionGuidePage", typeof(PermissionGuidePage));
        Routing.RegisterRoute("MainPage", typeof(MainPage));
    }
}
