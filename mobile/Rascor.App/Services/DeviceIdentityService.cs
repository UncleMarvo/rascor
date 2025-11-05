using Microsoft.Extensions.Logging;
using Rascor.App.Core;
using System.Security.Cryptography;
using System.Text;

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
    //public string GetDeviceId()
    //{
    //    var deviceId = Preferences.Get(DeviceIdKey, null);

    //    if (string.IsNullOrEmpty(deviceId))
    //    {
    //        // Generate new unique device ID
    //        deviceId = Guid.NewGuid().ToString();
    //        Preferences.Set(DeviceIdKey, deviceId);
    //        _logger.LogInformation("📱 Generated new device ID: {DeviceId}", deviceId);
    //    }
    //    else
    //    {
    //        _logger.LogInformation("📱 Using existing device ID: {DeviceId}", deviceId);
    //    }

    //    return deviceId;
    //}

    public string GetDeviceId()
    {
        var deviceId = Preferences.Get(DeviceIdKey, null);

        if (string.IsNullOrEmpty(deviceId))
        {
            // Generate deterministic device ID from multiple device characteristics
            // This ensures same device always gets same ID (even after app reset)
            deviceId = GenerateDeterministicDeviceId();
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
    /// Generates a deterministic EVT#### formatted device ID from device characteristics
    /// Uses multiple device properties to minimize collision probability
    /// Returns format: EVT0001 to EVT9999
    /// </summary>
    private string GenerateDeterministicDeviceId()
    {
        var deviceInfo = Microsoft.Maui.Devices.DeviceInfo.Current;

        // Combine multiple device-specific identifiers to create unique fingerprint
        // Using multiple properties reduces collision chance significantly
        var deviceFingerprint = string.Join("|", new[]
        {
            deviceInfo.Platform.ToString(),      // Android, iOS, Windows, etc.
            deviceInfo.Model?.Trim() ?? "",     // Device model (e.g., "SM-G991B", "iPhone 14 Pro")
            deviceInfo.Manufacturer?.Trim() ?? "", // Manufacturer (e.g., "Samsung", "Apple")
            deviceInfo.VersionString ?? "",     // OS version (e.g., "13", "16.5")
            deviceInfo.DeviceType.ToString(),   // Physical, Virtual, etc.
            deviceInfo.Idiom.ToString()         // Phone, Tablet, Desktop, etc.
        });

        _logger.LogDebug("Device fingerprint components: {Fingerprint}", deviceFingerprint);

        // Hash the fingerprint to get deterministic numeric value
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(deviceFingerprint));

        // Convert first 4 bytes of hash to uint, then modulo to get 1-9999 range
        // Using modulo 9999 and adding 1 ensures range is 1-9999 (not 0-9998)
        var hashValue = BitConverter.ToUInt32(hashBytes, 0);
        var deviceNumber = (int)(hashValue % 9999) + 1;

        // Format as EVT#### with zero padding
        var formattedId = $"EVT{deviceNumber:D4}";

        _logger.LogDebug("Generated device number: {Number} from hash", deviceNumber);

        return formattedId;
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
