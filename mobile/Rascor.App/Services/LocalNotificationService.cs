using Microsoft.Extensions.Logging;
using Shiny.Notifications;

namespace Rascor.App.Services;

/// <summary>
/// Service for showing local push notifications
/// </summary>
public class LocalNotificationService
{
    private readonly ILogger<LocalNotificationService> _logger;
    private readonly INotificationManager _notificationManager;
    private int _notificationId = 1000; // Start at 1000 to avoid conflicts
    private bool _channelCreated = false;

    public LocalNotificationService(
        ILogger<LocalNotificationService> logger,
        INotificationManager notificationManager)
    {
        _logger = logger;
        _notificationManager = notificationManager;
    }

    /// <summary>
    /// Create notification channel (Android requirement)
    /// </summary>
    private void EnsureChannelCreated()
    {
        if (_channelCreated)
            return;

        try
        {
            var channel = new Channel
            {
                Identifier = "geofence_events",
                Description = "Site geofence enter/exit notifications",
                Importance = ChannelImportance.High, // HIGH importance for heads-up notifications
                Actions = new List<ChannelAction>()
            };

            _notificationManager.AddChannel(channel);
            _channelCreated = true;
            _logger.LogInformation("✅ Notification channel created: geofence_events (HIGH importance)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification channel");
        }
    }

    /// <summary>
    /// Request notification permissions (call once on app startup)
    /// </summary>
    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            // Create channel first
            EnsureChannelCreated();

            var access = await _notificationManager.RequestAccess();
            
            if (access == Shiny.AccessState.Available)
            {
                _logger.LogInformation("✅ Notification permissions granted");
                return true;
            }
            
            _logger.LogWarning("⚠️ Notification permissions denied or restricted: {Status}", access);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request notification permissions");
            return false;
        }
    }

    /// <summary>
    /// Show a local notification
    /// </summary>
    public async Task ShowNotificationAsync(string title, string message)
    {
        try
        {
            // Ensure channel exists
            EnsureChannelCreated();

            var notification = new Notification
            {
                Id = _notificationId++,
                Title = title,
                Message = message,
                Channel = "geofence_events" // Android notification channel
            };

            await _notificationManager.Send(notification);
            _logger.LogInformation("📬 Notification sent: {Title} - Check notification shade by swiping down", title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification: {Title}", title);
        }
    }

    /// <summary>
    /// Synchronous version for backward compatibility
    /// </summary>
    public void ShowNotification(string title, string message)
    {
        // Fire and forget - don't block caller
        _ = ShowNotificationAsync(title, message);
    }
}
