# Shiny.NET Geofencing Implementation

This project uses **Shiny.NET** for cross-platform background geofencing on Android and iOS.

## Architecture

### Components

1. **ShinyGeofenceService** (`Services/ShinyGeofenceService.cs`)
   - Implements `IGeofenceService` interface
   - Wraps Shiny's `IGeofenceManager`
   - Handles permission requests and geofence registration

2. **SiteAttendanceGeofenceDelegate** (`Services/SiteAttendanceGeofenceDelegate.cs`)
   - Implements `IGeofenceDelegate`
   - Receives geofence enter/exit events from Shiny
   - Posts events to backend API
   - Shows local notifications

3. **MauiProgram.cs**
   - Configures Shiny with `.UseShiny()`
   - Registers geofencing services
   - Wires up dependency injection

## Required Permissions

### Android (`Platforms/Android/AndroidManifest.xml`)
```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_LOCATION" />
```

### iOS (`Platforms/iOS/Info.plist`)
```xml
<key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
<string>We monitor site geofences for attendance tracking...</string>
<key>UIBackgroundModes</key>
<array>
    <string>location</string>
</array>
```

## How It Works

1. **Initialization**: User taps "Initialize Geofences" button
   - App requests location permissions via Shiny
   - ConfigService fetches site list from backend
   - ShinyGeofenceService registers each site as a geofence region

2. **Background Monitoring**: Shiny monitors geofences continuously
   - Works even when app is closed/backgrounded
   - Battery-efficient using OS native geofencing APIs

3. **Event Handling**: When user enters/exits a geofence
   - Shiny wakes up the app
   - SiteAttendanceGeofenceDelegate receives event
   - Event is posted to backend API
   - Local notification is shown to user

## Platform Limits

- **iOS**: Maximum 20 geofence regions
- **Android**: Maximum 100 geofence regions (but battery impact increases)

## Testing

### Using the App
1. Build and deploy to physical device (geofencing doesn't work in simulators)
2. Tap "Initialize Geofences" - grant location permissions
3. Move to a site location (within configured radius)
4. App should detect entry and show notification

### Simulate Events
- Use "Simulate Enter Event" button to manually trigger an event without moving

## Packages Used

- **Shiny.Hosting.Maui** (3.3.4) - Core Shiny integration for MAUI
- **Shiny.Locations** (3.3.4) - Geofencing and GPS services

## References

- [Shiny.NET Documentation](https://shinylib.net)
- [Shiny Geofencing Guide](https://shinylib.net/client/locations/geofencing/)
