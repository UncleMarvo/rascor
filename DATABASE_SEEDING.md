# Database Seeding Configuration

## Overview
The RASCOR application has been updated to prevent automatic database seeding on every startup. This improves performance and prevents potential data conflicts in production environments.

## Changes Made

### 1. Added Wexford Sites (Phase 2A)
Two new sites have been added to the seed data:

- **site-004**: Wexford Home
  - Latitude: 52.5103567
  - Longitude: -6.5767854
  - Radius: 50 meters

- **site-005**: Wexford GreenTechHQ
  - Latitude: 52.4930494
  - Longitude: -6.5640803
  - Radius: 150 meters

Both sites are assigned to the `user-demo` test user.

### 2. Conditional Database Seeding
Database seeding now requires explicit opt-in via an environment variable.

## How to Enable Database Seeding

### Local Development
Set the environment variable before running the application:

**Windows (PowerShell):**
```powershell
$env:ENABLE_DATABASE_SEED = "true"
dotnet run
```

**Windows (Command Prompt):**
```cmd
set ENABLE_DATABASE_SEED=true
dotnet run
```

**macOS/Linux:**
```bash
export ENABLE_DATABASE_SEED=true
dotnet run
```

### Azure App Service
1. Navigate to your App Service in Azure Portal
2. Go to **Configuration** → **Application settings**
3. Click **+ New application setting**
4. Add:
   - **Name**: `ENABLE_DATABASE_SEED`
   - **Value**: `true`
5. Click **OK**, then **Save**
6. Restart the app service

### appsettings.json (Alternative)
Add to `appsettings.Development.json` or `appsettings.json`:

```json
{
  "ENABLE_DATABASE_SEED": "true"
}
```

## Default Behavior
- **Without the environment variable**: Database migrations run, but seeding is skipped
- **With `ENABLE_DATABASE_SEED=true`**: Database migrations run AND seeding occurs
- **With any other value**: Seeding is skipped

## Seeding Logic
The seeding process is idempotent and only adds data if it doesn't already exist:

1. **Sites**: Only seeded if no sites exist in the database
2. **RAMS Data**: Only seeded if no work types exist in the database

This means you can safely enable seeding multiple times without creating duplicates.

## Production Recommendations
1. **Initial Deployment**: Set `ENABLE_DATABASE_SEED=true` for the first deployment
2. **After First Run**: Remove or set to `false` to prevent unnecessary seeding checks
3. **Data Updates**: Only enable when you need to seed new reference data

## Logs
The application logs will indicate whether seeding occurred:

```
Database migration complete.
ENABLE_DATABASE_SEED is set to 'true'. Seeding data...
✅ Sites seeded successfully!
🌱 Seeding RAMS data...
✅ RAMS data seeded successfully!
Database initialization complete!
```

Or if skipped:

```
Database migration complete.
Database seeding skipped (ENABLE_DATABASE_SEED not set to 'true').
To enable seeding, set the ENABLE_DATABASE_SEED environment variable to 'true'.
Database initialization complete!
```

## Files Modified
- `backend/src/Rascor.Infrastructure/Data/DbInitializer.cs` - Added Wexford sites
- `backend/src/Rascor.Api/Program.cs` - Added conditional seeding logic
