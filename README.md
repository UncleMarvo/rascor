# Site Attendance MVP

Hands-free site attendance tracking using .NET MAUI (Android/iOS) with native OS geofencing and ASP.NET Core backend.

## Repository

**GitHub**: https://github.com/UncleMarvo/site-attendance

## Prerequisites

- **Windows**: Visual Studio 2022 (17.12+) with .NET MAUI workload
- **.NET SDK**: 9.0.100 or later
- **Android SDK**: API 35+ (via VS or Android Studio)
- **iOS Build**: macOS with Xcode 16+ (for iOS builds)

## Quick Start

### 1. Clone Repository

```bash
git clone https://github.com/UncleMarvo/site-attendance.git
cd site-attendance
```

### 2. Install Workloads

```powershell
# Check current workloads
dotnet workload list

# Install MAUI workloads
dotnet workload update
dotnet workload install maui-android maui-ios
```

### 3. Run Bootstrap (Optional)

```powershell
# Windows PowerShell
.\bootstrap.ps1

# macOS/Linux bash
chmod +x bootstrap.sh
./bootstrap.sh
```

### 4. Build Solution

```powershell
dotnet restore
dotnet build SiteAttendance.sln
```

### 5. Run Backend API

```powershell
cd backend/src/SiteAttendance.Api
dotnet run
```

API runs on `https://localhost:5001` (or check console output).

Swagger UI: `https://localhost:5001/swagger`

### 6. Run Mobile App

**Visual Studio 2022:**
1. Open `SiteAttendance.sln`
2. Set `SiteAttendance.App` as startup project
3. Select Android emulator or iOS simulator
4. Press F5 (Debug) or Ctrl+F5 (Run)

**Command Line (Android):**
```powershell
dotnet build mobile/SiteAttendance.App/SiteAttendance.App.csproj -f net9.0-android
# Deploy via adb or VS
```

## Project Structure

```
site-attendance/
├─ backend/src/
│  ├─ SiteAttendance.Api/          # ASP.NET Core Minimal API
│  ├─ SiteAttendance.Application/  # Use cases (handlers)
│  ├─ SiteAttendance.Domain/       # Entities, ports, config
│  └─ SiteAttendance.Infrastructure/ # Repos, providers
└─ mobile/
   ├─ SiteAttendance.App/          # MAUI app (net9.0-android;ios)
   └─ SiteAttendance.App.Core/     # Shared contracts (net9.0)
```

## API Endpoints

- `GET /config/mobile?userId={id}` - Remote config + assigned sites
- `POST /events/geofence` - Log geofence enter/exit event
- `GET /swagger` - API documentation

## Troubleshooting

### `NETSDK1135: SupportedOSPlatformVersion not compatible`
**Fix:** Remove any manual `<SupportedOSPlatformVersion>` or `<TargetPlatformVersion>` from `.csproj`. Let SDK infer.

### `GeofencingClient` or `GeofencingRequest` not found (Android)
**Fix:** Ensure `Xamarin.GooglePlayServices.Location` and `.Tasks` are installed:
```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net9.0-android'">
  <PackageReference Include="Xamarin.GooglePlayServices.Location" Version="121.3.0.7" />
  <PackageReference Include="Xamarin.GooglePlayServices.Tasks" Version="121.3.0.7" />
</ItemGroup>
```
Add `using Android.Gms.Location;` and `using Android.Gms.Extensions;`.

### `InitializeComponent` does not exist
**Fix:** This repo uses **pure C# pages** (no XAML compile issues). Files use partial classes with XAML.

### Metadata file 'SiteAttendance.Infrastructure.dll' not found
**Fix:** Build projects in order:
```powershell
dotnet build backend/src/SiteAttendance.Domain
dotnet build backend/src/SiteAttendance.Infrastructure
dotnet build backend/src/SiteAttendance.Application
dotnet build backend/src/SiteAttendance.Api
```

### Package restore fails for `Microsoft.Extensions.Http`
**Fix:** Ensure `SiteAttendance.Infrastructure.csproj` includes:
```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
```

### iOS: `CLLocationManager` regions not monitored
**Fix:** Check `Info.plist` has location usage descriptions (already included in repo).

### Android: Geofence not triggering
**Fix:** 
1. Ensure `ACCESS_FINE_LOCATION` and `ACCESS_BACKGROUND_LOCATION` permissions in `AndroidManifest.xml` (already included).
2. Grant location permissions in device settings.
3. Use physical device or emulator with Google Play Services.
4. Send mock location via Android Studio location tools.

## Configuration

### Backend Remote Config

Edit `backend/src/SiteAttendance.Api/appsettings.json`:
```json
{
  "RemoteConfig": {
    "DefaultRadiusMeters": 150,
    "MaxConcurrentSites": 20,
    "DebounceEnterMinutes": 5,
    "DebounceExitMinutes": 3
  }
}
```

### Mobile App Backend URL

Edit `mobile/SiteAttendance.App/Services/BackendApi.cs`:
```csharp
private const string BaseUrl = "https://your-api.azurewebsites.net";
```

For local testing:
- **Android Emulator**: `http://10.0.2.2:5001`
- **iOS Simulator**: `https://localhost:5001`
- **Physical Device**: Your machine's LAN IP (e.g., `https://192.168.1.100:5001`)

## Testing Geofences

### Android Emulator
1. Deploy app to emulator
2. Grant location permissions
3. Open Android Studio → Extended Controls (⋮) → Location
4. Set lat/long near a configured site (e.g., 37.7749, -122.4194)
5. Move location inside/outside geofence radius
6. Check app logs and backend `/events/geofence` POST

### iOS Simulator
1. Deploy app to simulator
2. Grant location permissions
3. Xcode → Debug → Simulate Location → Custom Location
4. Enter lat/long and move across geofence boundary

## Architecture Highlights

- **Clean Architecture**: Domain → Application → Infrastructure → API
- **Dependency Injection**: All providers swappable via interfaces
- **Platform Abstraction**: `IGeofenceService` with Android/iOS implementations
- **Native Geofencing**: 
  - Android: `GeofencingClient` from Google Play Services
  - iOS: `CLLocationManager` Region Monitoring
- **Remote Config**: Backend-driven radius, debounce, site limits
- **Provider Pattern**: Mock/Console implementations with stubs for OneSignal/SES

## Follow-Up Tasks (TODO)

- [ ] Replace in-memory repos with EF Core (Postgres) + migrations
- [ ] Add OneSignal/SES adapters implementing `IPushProvider`/`IEmailProvider`
- [ ] Add SQLite offline queue in app for event retries
- [ ] Implement debounce checks in `LogGeofenceEventHandler`
- [ ] Admin portal for assignments & settings
- [ ] Play Integrity API / App Attest hooks for device verification
- [ ] Background task registration for iOS (BGTaskScheduler)
- [ ] Foreground service for Android 14+ restrictions

## License

MIT

## Support

For issues, please file a GitHub issue at https://github.com/UncleMarvo/site-attendance/issues
