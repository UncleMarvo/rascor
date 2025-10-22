using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rascor.App.Configuration;
using Rascor.App.Core;
using Rascor.App.Pages;
using Rascor.App.Services;
using Shiny;
using System.Reflection;

namespace Rascor.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("Rascor.App.appsettings.json");

        var config = new ConfigurationBuilder()
            .AddJsonStream(stream!)
            .Build();

        builder.Configuration.AddConfiguration(config);

        // Configure Shiny for background services, geofencing, GPS, and notifications
        builder.UseShiny();
        
        // Register Shiny services
        builder.Services.AddGeofencing<Services.RascorGeofenceDelegate>();
        builder.Services.AddGps(); // Add GPS tracking
        builder.Services.AddNotifications();

        builder.Services.Configure<ApiSettings>(
            builder.Configuration.GetSection("ApiSettings"));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register services
        builder.Services.AddSingleton<DeviceIdentityService>();
        builder.Services.AddSingleton<BackendApi>();
        builder.Services.AddSingleton<EventQueueService>();
        builder.Services.AddSingleton<ConfigService>();
        builder.Services.AddSingleton<LocalNotificationService>();
        builder.Services.AddSingleton<LocationTrackingService>();

        // Register Shiny geofencing service wrapper
        builder.Services.AddSingleton<IGeofenceService, Services.ShinyGeofenceService>();

        // Register pages - new tab-based structure
        builder.Services.AddTransient<MainPage>(); // Keep for now (legacy)
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<PermissionGuidePage>();
        
        // New tab pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<MyWorkPage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}
