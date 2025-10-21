using Rascor.App.Pages;

namespace Rascor.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes for modal/push navigation
        Routing.RegisterRoute(nameof(OnboardingPage), typeof(OnboardingPage));
        Routing.RegisterRoute(nameof(PermissionGuidePage), typeof(PermissionGuidePage));
    }
}
