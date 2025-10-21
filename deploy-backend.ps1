# Deploy-RascorBackend.ps1
# Run from the rascor root folder
# This script pulls latest code, builds backend, and deploys to Azure

param(
    [string]$ResourceGroup = "rascor-rg",  # Change to your resource group name
    [string]$AppServiceName = "siteattendance-api-1411956859"  # Your existing App Service
)

# Color output functions
function Write-Info { param($msg) Write-Host "ℹ️  $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "✅ $msg" -ForegroundColor Green }
function Write-Error { param($msg) Write-Host "❌ $msg" -ForegroundColor Red }
function Write-Warning { param($msg) Write-Host "⚠️  $msg" -ForegroundColor Yellow }

# Store current location
$originalLocation = Get-Location

try {
    Write-Host "`n🚀 RASCOR Backend Deployment Script`n" -ForegroundColor Magenta

    # Step 0: Verify we're in the right repo
    Write-Info "Verifying repository..."
    $gitRemote = git remote get-url origin
    if ($gitRemote -notlike "*rascor*") {
        Write-Error "Wrong repository! Expected 'rascor' but found: $gitRemote"
        Write-Info "Navigate to the rascor folder first"
        exit 1
    }
    Write-Success "In correct repository: rascor"

    # Step 1: Git Pull
    Write-Info "Pulling latest changes from GitHub..."
    git pull origin main
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Git pull failed!"
        exit 1
    }
    Write-Success "Git pull completed"

    # Step 2: Build Backend
    Write-Info "Building backend..."
    Set-Location "backend/src/Rascor.Api"
    
    # Get absolute path for publish folder
    $publishPath = Join-Path (Get-Location) "publish"
    $zipPath = Join-Path (Get-Location) "deploy.zip"
    
    # Clean previous build and old zip
    Write-Info "Cleaning previous build..."
    if (Test-Path $publishPath) { Remove-Item $publishPath -Recurse -Force }
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    dotnet clean --configuration Release --verbosity quiet
    
    # Restore packages
    Write-Info "Restoring NuGet packages..."
    dotnet restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "NuGet restore failed!"
        Set-Location $originalLocation
        exit 1
    }
    
    # Build
    Write-Info "Compiling backend (Release configuration)..."
    dotnet build --configuration Release --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed! Aborting deployment."
        Set-Location $originalLocation
        exit 1
    }
    Write-Success "Build successful!"

    # Step 3: Check Azure CLI
    Write-Info "Checking Azure CLI..."
    $azVersion = az version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Azure CLI not found! Please install: https://aka.ms/installazurecliwindows"
        Set-Location $originalLocation
        exit 1
    }
    Write-Success "Azure CLI found"

    # Step 4: Check Azure Login
    Write-Info "Checking Azure login status..."
    $account = az account show 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Not logged into Azure. Logging in..."
        az login
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Azure login failed!"
            Set-Location $originalLocation
            exit 1
        }
    }
    Write-Success "Azure authentication verified"

    # Step 5: Verify App Service exists
    Write-Info "Verifying App Service '$AppServiceName' exists..."
    $appExists = az webapp show --name $AppServiceName --resource-group $ResourceGroup 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "App Service '$AppServiceName' not found in resource group '$ResourceGroup'!"
        Write-Warning "Available App Services:"
        az webapp list --query "[].{Name:name, ResourceGroup:resourceGroup}" --output table
        Set-Location $originalLocation
        exit 1
    }
    Write-Success "App Service found"

    # Step 6: Stop the App Service (ensures clean deployment)
    Write-Info "Stopping App Service to ensure clean deployment..."
    az webapp stop --name $AppServiceName --resource-group $ResourceGroup 2>$null
    Start-Sleep -Seconds 3
    Write-Success "App Service stopped"

    # Step 7: Publish and Package
    Write-Host "`n📦 Preparing deployment package..." -ForegroundColor Yellow
    Write-Info "Target: $AppServiceName"
    Write-Info "Resource Group: $ResourceGroup"
    
    # Publish application
    Write-Info "Publishing application..."
    dotnet publish --configuration Release --output $publishPath --no-build --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Publish failed!"
        Set-Location $originalLocation
        exit 1
    }
    Write-Success "Publish completed"

    # Verify critical files exist
    Write-Info "Verifying deployment package..."
    $criticalFiles = @("Rascor.Api.dll", "appsettings.json", "web.config")
    foreach ($file in $criticalFiles) {
        if (-not (Test-Path (Join-Path $publishPath $file))) {
            Write-Error "Missing critical file: $file"
            Set-Location $originalLocation
            exit 1
        }
    }
    Write-Success "All critical files present"

    # Create zip file
    Write-Info "Creating deployment package..."
    Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force
    if (-not (Test-Path $zipPath)) {
        Write-Error "Failed to create zip file!"
        Set-Location $originalLocation
        exit 1
    }
    $zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
    Write-Success "Package created: $zipSize MB"

    # Step 8: Deploy to Azure using Kudu ZipDeploy (most reliable)
    Write-Info "Uploading to Azure using ZipDeploy API (this may take a few minutes)..."
    Write-Info "Package: $zipPath"
    
    # Use az webapp deployment for the most reliable deployment
    az webapp deployment source config-zip --resource-group $ResourceGroup --name $AppServiceName --src $zipPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Deployment failed!"
        
        # Try to restart app anyway
        Write-Info "Attempting to start app service..."
        az webapp start --name $AppServiceName --resource-group $ResourceGroup 2>$null
        
        Set-Location $originalLocation
        exit 1
    }

    Write-Success "Deployment uploaded successfully!"

    # Step 9: Start the App Service
    Write-Info "Starting App Service..."
    az webapp start --name $AppServiceName --resource-group $ResourceGroup 2>$null
    Write-Success "App Service started"

    # Step 10: Wait for app to fully start
    Write-Info "Waiting for app to fully start (45 seconds)..."
    Start-Sleep -Seconds 45

    # Step 11: Verify deployment with retries
    Write-Info "Verifying deployment..."
    
    $apiUrl = "https://$AppServiceName.azurewebsites.net"
    Write-Info "Testing endpoint: $apiUrl"
    
    $maxRetries = 5
    $retryCount = 0
    $success = $false
    
    while ($retryCount -lt $maxRetries -and -not $success) {
        try {
            $response = Invoke-WebRequest -Uri $apiUrl -TimeoutSec 30 -UseBasicParsing -ErrorAction Stop
            $content = $response.Content | ConvertFrom-Json
            
            # Check if it's the new version with RAMS
            if ($content.Version -like "*RAMS*") {
                Write-Success "✅ API is responding with NEW VERSION! (Version: $($content.Version))"
                $success = $true
            }
            else {
                Write-Warning "API responding but appears to be old version. Retrying..."
                $retryCount++
                if ($retryCount -lt $maxRetries) {
                    Start-Sleep -Seconds 10
                }
            }
        }
        catch {
            $retryCount++
            if ($retryCount -lt $maxRetries) {
                Write-Warning "Attempt $retryCount failed, retrying in 10 seconds..."
                Start-Sleep -Seconds 10
            }
            else {
                Write-Warning "Could not verify API response after $maxRetries attempts"
            }
        }
    }

    # Step 12: Test RAMS endpoint specifically
    Write-Info "Testing RAMS work-types endpoint..."
    $workTypesUrl = "$apiUrl/api/work-types"
    try {
        $response = Invoke-RestMethod -Uri $workTypesUrl -TimeoutSec 30
        $workTypeCount = $response.Count
        Write-Success "✅ RAMS work-types endpoint working! Found $workTypeCount work types"
    }
    catch {
        Write-Warning "⚠️ RAMS work-types endpoint not responding yet"
        Write-Info "URL: $workTypesUrl"
    }

    # Step 13: Test bootstrap endpoint
    Write-Info "Testing enhanced bootstrap endpoint..."
    $testUrl = "$apiUrl/config/mobile?userId=user-demo"
    try {
        $response = Invoke-RestMethod -Uri $testUrl -TimeoutSec 30
        $siteCount = $response.sites.Count
        $hasWorkAssignments = $null -ne $response.workAssignments
        
        if ($hasWorkAssignments) {
            $assignmentCount = $response.workAssignments.Count
            Write-Success "✅ Bootstrap endpoint working! Sites: $siteCount, Work Assignments: $assignmentCount"
        }
        else {
            Write-Warning "⚠️ Bootstrap responding but missing RAMS data (workAssignments field)"
            Write-Info "This might mean the old code is still cached"
        }
    }
    catch {
        Write-Warning "⚠️ Could not test bootstrap endpoint"
        Write-Info "Try manually: $testUrl"
    }

    # Step 14: Clean up
    Write-Info "Cleaning up temporary files..."
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    # Success summary
    Write-Host "`n" -NoNewline
    Write-Host "═══════════════════════════════════════" -ForegroundColor Green
    Write-Host "✅ DEPLOYMENT COMPLETED!" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════" -ForegroundColor Green
    Write-Host "`n📋 Deployment Details:" -ForegroundColor Cyan
    Write-Host "   URL: $apiUrl" -ForegroundColor White
    Write-Host "   Swagger: $apiUrl/swagger" -ForegroundColor White
    Write-Host "   Work Types: $workTypesUrl" -ForegroundColor White
    Write-Host "   Bootstrap: $testUrl" -ForegroundColor White
    Write-Host "`n⏰ Important:" -ForegroundColor Yellow
    Write-Host "   If RAMS endpoints show old data, wait 2-3 minutes" -ForegroundColor White
    Write-Host "   Azure may be caching the old version" -ForegroundColor White
    Write-Host "   Then refresh Swagger and test again" -ForegroundColor White
    Write-Host "`n🎯 Next Steps:" -ForegroundColor Cyan
    Write-Host "   1. Wait 2-3 minutes for full startup" -ForegroundColor White
    Write-Host "   2. Open Swagger: $apiUrl/swagger" -ForegroundColor White
    Write-Host "   3. Verify you see RAMS endpoints (work-types, assignments, etc.)" -ForegroundColor White
    Write-Host "   4. Test bootstrap endpoint for workAssignments field" -ForegroundColor White
    Write-Host "   5. Test mobile app 'Start Monitoring' button" -ForegroundColor White
    Write-Host "`n"

}
catch {
    Write-Error "Unexpected error: $_"
    Write-Error $_.ScriptStackTrace
    
    # Try to restart app service if it was stopped
    Write-Info "Attempting to restart app service..."
    Set-Location $originalLocation
    Set-Location "backend/src/Rascor.Api"
    az webapp start --name $AppServiceName --resource-group $ResourceGroup 2>$null
    
    Set-Location $originalLocation
    exit 1
}
finally {
    # Always return to original location
    Set-Location $originalLocation
}