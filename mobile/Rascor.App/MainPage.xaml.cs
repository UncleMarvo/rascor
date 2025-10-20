using Microsoft.Extensions.Logging;
using Rascor.App.Services;

namespace Rascor.App;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;
    private readonly ConfigService _configService;
    private readonly BackendApi _backendApi;
    private readonly DeviceIdentityService _deviceIdentity;
    private readonly EventQueueService _eventQueue;
    private readonly LocalNotificationService _notificationService;
    private bool _isInitialized = false;

    public MainPage(
        ILogger<MainPage> logger, 
        ConfigService configService, 
        BackendApi backendApi, 
        DeviceIdentityService deviceIdentity,
        EventQueueService eventQueue,
        LocalNotificationService notificationService)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
        _backendApi = backendApi;
        _deviceIdentity = deviceIdentity;
        _eventQueue = eventQueue;
        _notificationService = notificationService;
        
        System.Diagnostics.Debug.WriteLine("🎯🎯🎯 MainPage CONSTRUCTOR called!");
        _logger.LogWarning("🎯🎯🎯 MainPage constructor - logger working!");
        
        // Auto-initialize on startup
        _ = AutoInitializeAsync();
    }

    private async Task AutoInitializeAsync()
    {
        try
        {
            StatusLabel.Text = "🔄 Initializing...";
            
            // Request notification permissions
            var notifGranted = await _notificationService.RequestPermissionAsync();
            _logger.LogWarning("🔔 Notification permission granted: {Granted}", notifGranted);
            
            // Initialize geofence monitoring
            var userId = _deviceIdentity.GetUserId();
            _logger.LogInformation("🚀 Auto-initializing for user {UserId}", userId);
            
            await _configService.InitializeAsync();
            _isInitialized = true;
            
            var pendingCount = await _eventQueue.GetPendingCountAsync();
            var queueInfo = pendingCount > 0 ? $"\n📦 {pendingCount} events queued" : "";
            
            StatusLabel.Text = $"✅ Monitoring {_configService.Sites.Count} sites!\nDevice: {userId.Substring(0, 8)}...{queueInfo}";
            _logger.LogWarning("✅ Auto-initialization successful - monitoring {Count} sites", _configService.Sites.Count);
            
            // Show welcome notification
            if (notifGranted)
            {
                await _notificationService.ShowNotificationAsync(
                    "RASCOR Active",
                    $"Monitoring {_configService.Sites.Count} sites"
                );
            }
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            var deviceInfo = _deviceIdentity.GetDeviceInfo();
            StatusLabel.Text = $"❌ Init failed: {ex.Message}\nDevice: {deviceInfo.DeviceId.Substring(0, 8)}...";
            _logger.LogError(ex, "Auto-initialization failed");
            System.Diagnostics.Debug.WriteLine($"❌ Auto-init ERROR: {ex}");
        }
    }

    private async Task UpdateStatusAsync()
    {
        var deviceInfo = _deviceIdentity.GetDeviceInfo();
        var pendingCount = await _eventQueue.GetPendingCountAsync();
        
        if (_isInitialized)
        {
            var queueInfo = pendingCount > 0 ? $"\n📦 {pendingCount} events queued" : "";
            StatusLabel.Text = $"✅ Monitoring {_configService.Sites.Count} sites!\nDevice: {deviceInfo.DeviceId.Substring(0, 8)}...{queueInfo}";
        }
        else
        {
            if (pendingCount > 0)
            {
                StatusLabel.Text = $"Device: {deviceInfo.DeviceId.Substring(0, 8)}...\n📦 {pendingCount} events queued\nReady to monitor!";
            }
            else
            {
                StatusLabel.Text = $"Device: {deviceInfo.DeviceId.Substring(0, 8)}...\nReady to monitor!";
            }
        }
    }

    private async void OnTestNotificationClicked(object sender, EventArgs e)
    {
        try
        {
            _logger.LogWarning("🔔 Test Notification button clicked!");
            StatusLabel.Text = "🔔 Sending test notification...";
            
            await _notificationService.ShowNotificationAsync(
                "Test Notification",
                $"This is a test notification at {DateTime.Now:HH:mm:ss}"
            );
            
            StatusLabel.Text = "✅ Test notification sent!\nCheck notification shade";
            _logger.LogWarning("🔔 Test notification sent successfully");
            
            // Restore status after 3 seconds
            await Task.Delay(3000);
            await UpdateStatusAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Notification error: {ex.Message}";
            _logger.LogError(ex, "Failed to send test notification");
        }
    }

    private async void OnCopyDeviceIdClicked(object sender, EventArgs e)
    {
        try
        {
            var deviceInfo = _deviceIdentity.GetDeviceInfo();
            await Clipboard.SetTextAsync(deviceInfo.DeviceId);
            
            StatusLabel.Text = $"✅ Device ID copied!\n{deviceInfo.DeviceId}";
            _logger.LogInformation("Device ID copied to clipboard: {DeviceId}", deviceInfo.DeviceId);
            
            // Show full ID for 5 seconds, then restore status
            await Task.Delay(5000);
            await UpdateStatusAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Copy failed: {ex.Message}";
            _logger.LogError(ex, "Failed to copy device ID");
        }
    }

    private async void OnInitializeClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🚨🚨🚨 OnInitializeClicked CALLED (manual override)!");
        StatusLabel.Text = "🚨 Re-initializing...";
        
        try
        {
            var userId = _deviceIdentity.GetUserId();
            StatusLabel.Text = $"Re-initializing for user {userId.Substring(0, 8)}...";
            
            await _configService.InitializeAsync();
            _isInitialized = true;
            
            var pendingCount = await _eventQueue.GetPendingCountAsync();
            var queueInfo = pendingCount > 0 ? $"\n📦 {pendingCount} events queued" : "";
            
            StatusLabel.Text = $"✅ Monitoring {_configService.Sites.Count} sites!\nUser: {userId.Substring(0, 8)}...{queueInfo}";
            _logger.LogWarning("✅ Manual re-initialization successful - {Count} sites", _configService.Sites.Count);
            System.Diagnostics.Debug.WriteLine($"✅ SUCCESS! Monitoring {_configService.Sites.Count} sites");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Error: {ex.Message}";
            _logger.LogError(ex, "Manual initialization failed");
            System.Diagnostics.Debug.WriteLine($"❌ ERROR: {ex}");
        }
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔄 OnSyncClicked CALLED!");
        
        try
        {
            var pendingCount = await _eventQueue.GetPendingCountAsync();
            
            if (pendingCount == 0)
            {
                StatusLabel.Text = "✅ No events to sync!";
                await Task.Delay(2000);
                await UpdateStatusAsync();
                return;
            }
            
            StatusLabel.Text = $"🔄 Syncing {pendingCount} events...";
            _logger.LogInformation("Starting manual sync of {Count} events", pendingCount);
            
            var (synced, failed) = await _eventQueue.SyncQueuedEventsAsync();
            
            // Cleanup old synced events
            await _eventQueue.CleanupOldEventsAsync();
            
            var remaining = await _eventQueue.GetPendingCountAsync();
            
            if (failed == 0)
            {
                StatusLabel.Text = $"✅ Synced {synced} events!\n{remaining} events remaining";
            }
            else
            {
                StatusLabel.Text = $"⚠️ Synced {synced}, failed {failed}\n{remaining} events remaining";
            }
            
            _logger.LogInformation("Sync complete: {Synced} synced, {Failed} failed, {Remaining} remaining", 
                synced, failed, remaining);
            
            // Restore status after 3 seconds
            await Task.Delay(3000);
            await UpdateStatusAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Sync error: {ex.Message}";
            _logger.LogError(ex, "Sync failed");
        }
    }

    private async void OnSimulateEnterClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🚨🚨🚨 OnSimulateEnterClicked CALLED!");
        
        if (_configService.Sites.Count == 0)
        {
            StatusLabel.Text = "Not initialized! Use 'Start Monitoring' first";
            return;
        }

        try
        {
            var firstSite = _configService.Sites[0];
            StatusLabel.Text = $"Simulating ENTER for {firstSite.Name}...";
            System.Diagnostics.Debug.WriteLine($"Simulating ENTER for {firstSite.Name}");

            var success = await _configService.SimulateGeofenceEventAsync(
                firstSite.Id,
                "Enter",
                firstSite.Latitude,
                firstSite.Longitude
            );

            if (success)
            {
                StatusLabel.Text = $"✅ ENTER event sent for {firstSite.Name}";
                _logger.LogWarning("🔔 About to send Enter notification...");
                await _notificationService.ShowNotificationAsync(
                    "Site Enter",
                    $"You entered {firstSite.Name}"
                );
                _logger.LogWarning("🔔 Enter notification sent");
            }
            else
            {
                var pendingCount = await _eventQueue.GetPendingCountAsync();
                StatusLabel.Text = $"📦 ENTER queued offline for {firstSite.Name}\n{pendingCount} events pending";
                _logger.LogWarning("🔔 About to send offline Enter notification...");
                await _notificationService.ShowNotificationAsync(
                    "Site Enter (Offline)",
                    $"You entered {firstSite.Name} - will sync when online"
                );
                _logger.LogWarning("🔔 Offline Enter notification sent");
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ ENTER event processed");
            
            // Restore status after 3 seconds
            await Task.Delay(3000);
            await UpdateStatusAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Error: {ex.Message}";
            _logger.LogError(ex, "Simulate enter failed");
            System.Diagnostics.Debug.WriteLine($"❌ Simulate enter error: {ex}");
        }
    }

    private async void OnSimulateExitClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🚨🚨🚨 OnSimulateExitClicked CALLED!");
        
        if (_configService.Sites.Count == 0)
        {
            StatusLabel.Text = "Not initialized! Use 'Start Monitoring' first";
            return;
        }

        try
        {
            var firstSite = _configService.Sites[0];
            StatusLabel.Text = $"Simulating EXIT for {firstSite.Name}...";

            var success = await _configService.SimulateGeofenceEventAsync(
                firstSite.Id,
                "Exit",
                firstSite.Latitude,
                firstSite.Longitude
            );

            if (success)
            {
                StatusLabel.Text = $"✅ EXIT event sent for {firstSite.Name}";
                _logger.LogWarning("🔔 About to send Exit notification...");
                await _notificationService.ShowNotificationAsync(
                    "Site Exit",
                    $"You exited {firstSite.Name}"
                );
                _logger.LogWarning("🔔 Exit notification sent");
            }
            else
            {
                var pendingCount = await _eventQueue.GetPendingCountAsync();
                StatusLabel.Text = $"📦 EXIT queued offline for {firstSite.Name}\n{pendingCount} events pending";
                _logger.LogWarning("🔔 About to send offline Exit notification...");
                await _notificationService.ShowNotificationAsync(
                    "Site Exit (Offline)",
                    $"You exited {firstSite.Name} - will sync when online"
                );
                _logger.LogWarning("🔔 Offline Exit notification sent");
            }
            
            // Restore status after 3 seconds
            await Task.Delay(3000);
            await UpdateStatusAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Error: {ex.Message}";
            _logger.LogError(ex, "Simulate exit failed");
        }
    }
}
