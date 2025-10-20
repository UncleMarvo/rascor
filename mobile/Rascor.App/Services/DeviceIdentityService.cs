using Microsoft.Extensions.Logging;
using Rascor.App.Core;

namespace Rascor.App.Services;

/// <summary>
/// Manages device identity and user information
/// </summary>
public class DeviceIdentityService
{
    private readonly ILogger<DeviceIdentityService> _logger;
    private const string DeviceIdKey = "DeviceId";
    private const string UserIdKey = "UserId";

    public DeviceIdentityService(ILogger<DeviceIdentityService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a unique device identifier
    /// </summary>
    public string GetDeviceId()
    {
        var deviceId = Preferences.Get(DeviceIdKey, null);
        
        if (string.IsNullOrEmpty(deviceId))
        {
            // Generate new unique device ID
            deviceId = Guid.NewGuid().ToString();
            Preferences.Set(DeviceIdKey, deviceId);
            _logger.LogInformation("📱 Generated new device ID: {DeviceId}", deviceId);
        }
        else
        {
            _logger.LogInformation("📱 Using existing device ID: {DeviceId}", deviceId);
        }

        return deviceId;
    }

    /// <summary>
    /// Gets the user ID (for now, same as device ID, but could be set via login)
    /// </summary>
    public string GetUserId()
    {
        var userId = Preferences.Get(UserIdKey, null);
        
        if (string.IsNullOrEmpty(userId))
        {
            // For MVP, use device ID as user ID
            // In production, this would be set after login/registration
            userId = GetDeviceId();
            Preferences.Set(UserIdKey, userId);
            _logger.LogInformation("👤 User ID set to device ID: {UserId}", userId);
        }

        return userId;
    }

    /// <summary>
    /// Sets a custom user ID (for future login functionality)
    /// </summary>
    public void SetUserId(string userId)
    {
        Preferences.Set(UserIdKey, userId);
        _logger.LogInformation("👤 User ID updated: {UserId}", userId);
    }

    /// <summary>
    /// Gets device information for display/debugging
    /// </summary>
    public AppDeviceInfo GetDeviceInfo()
    {
        return new AppDeviceInfo
        {
            DeviceId = GetDeviceId(),
            UserId = GetUserId(),
            Platform = Microsoft.Maui.Devices.DeviceInfo.Current.Platform.ToString(),
            Model = Microsoft.Maui.Devices.DeviceInfo.Current.Model,
            Manufacturer = Microsoft.Maui.Devices.DeviceInfo.Current.Manufacturer,
            Version = Microsoft.Maui.Devices.DeviceInfo.Current.VersionString
        };
    }

    /// <summary>
    /// Resets device identity (for testing/debugging)
    /// </summary>
    public void ResetIdentity()
    {
        Preferences.Remove(DeviceIdKey);
        Preferences.Remove(UserIdKey);
        _logger.LogWarning("⚠️ Device identity reset - will generate new ID on next request");
    }
}

public class AppDeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}
