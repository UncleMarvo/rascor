# 📱 Site Attendance Mobile App

## Azure Backend Connection

The mobile app is now configured to connect to the **Azure production backend**:

**Backend URL:** `https://siteattendance-api-1411956859.azurewebsites.net`

### What Works

✅ **Bootstrap API** - Fetches assigned sites for a user  
✅ **Geofence Events** - Logs Enter/Exit events to Azure  
✅ **Background Monitoring** - Uses Shiny.Locations for cross-platform geofencing  
✅ **Permission Handling** - Android and iOS location permissions configured  

---

## Building & Running

### Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or VS Code with C# Dev Kit
- For iOS: Mac with Xcode
- For Android: Android SDK

### Quick Start

```bash
cd mobile/SiteAttendance.App

# Run on Android
dotnet build -t:Run -f net9.0-android

# Run on iOS (Mac only)
dotnet build -t:Run -f net9.0-ios
```

### Testing with Demo User

The app uses `user-demo` as the hardcoded user ID for MVP testing.

This user has 3 assigned sites:
- **Dublin Office** - 53.3498, -6.2603 (100m radius)
- **Dublin Warehouse** - 53.352, -6.257 (150m radius)
- **Cork Office** - 51.8985, -8.4756 (120m radius)

---

## How It Works

1. **Start Tracking** - Taps "Start Monitoring" button
2. **Fetch Config** - Calls `/config/mobile?userId=user-demo`
3. **Setup Geofences** - Configures 3 site geofences using Shiny
4. **Background Monitoring** - Monitors location even when app is closed
5. **Log Events** - Posts to `/events/geofence` on Enter/Exit

---

## Architecture

```
SiteAttendance.App (MAUI)
├── Services/
│   ├── BackendApi.cs              # Azure API client
│   ├── ShinyGeofenceService.cs    # Cross-platform geofencing
│   ├── SiteAttendanceGeofenceDelegate.cs  # Event handler
│   └── ConfigService.cs           # Local config management
├── MainPage.xaml                  # UI
└── Platforms/
    ├── Android/
    │   ├── AndroidManifest.xml    # Permissions
    │   └── AndroidGeofenceService.cs
    └── iOS/
        ├── Info.plist             # Permissions
        └── IOSGeofenceService.cs
```

---

## Configuration

### Changing Backend URL

Edit `Services/BackendApi.cs`:

```csharp
private const string BaseUrl = "https://siteattendance-api-1411956859.azurewebsites.net";
```

### Changing User ID

Edit `MainPage.xaml.cs`:

```csharp
private const string UserId = "user-demo";
```

---

## Permissions

### Android (`Platforms/Android/AndroidManifest.xml`)
- ✅ `ACCESS_FINE_LOCATION` - GPS tracking
- ✅ `ACCESS_BACKGROUND_LOCATION` - Background monitoring
- ✅ `FOREGROUND_SERVICE` - Background service
- ✅ `POST_NOTIFICATIONS` - Local notifications
- ✅ `INTERNET` - API calls

### iOS (`Platforms/iOS/Info.plist`)
- ✅ `NSLocationAlwaysAndWhenInUseUsageDescription`
- ✅ `NSLocationWhenInUseUsageDescription`
- ✅ `UIBackgroundModes` - Location updates

---

## Next Steps

- [ ] Add user authentication
- [ ] Implement offline queue for failed events
- [ ] Add local notifications on geofence events
- [ ] Add site list view with distance calculations
- [ ] Add event history view

---

## Troubleshooting

### "Connection refused" errors
- Ensure you're connected to the internet
- Check that Azure backend is running: https://siteattendance-api-1411956859.azurewebsites.net

### Location not updating
- Grant location permissions (Always Allow on iOS)
- Enable location services in device settings
- Check app logs for permission errors

### Geofences not triggering
- Ensure you're within 100-150m of a test site
- Wait 5-10 seconds for geofence to register
- Check Azure logs to see if events are being posted

---

## Support

For issues, check:
1. **Backend Logs:** Azure Portal → App Service → Log Stream
2. **App Logs:** Visual Studio Output window or device logs
3. **API Health:** https://siteattendance-api-1411956859.azurewebsites.net/swagger
