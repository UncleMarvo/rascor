#!/usr/bin/env pwsh
# bootstrap.ps1 - Turn-key .NET 9 MAUI + ASP.NET Core scaffold
# Generates site-attendance solution with geofencing support

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "🚀 Site Attendance Scaffold - .NET 9 MAUI + Backend" -ForegroundColor Cyan
Write-Host ""

# 1. Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK $dotnetVersion" -ForegroundColor Green
    
    if (-not $dotnetVersion.StartsWith("9.")) {
        Write-Host "⚠️  .NET 9.x required, found $dotnetVersion" -ForegroundColor Red
        Write-Host "   Install .NET 9 SDK from https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "❌ .NET SDK not found. Install from https://dot.net" -ForegroundColor Red
    exit 1
}

# 2. Check MAUI workloads
Write-Host "Checking MAUI workloads..." -ForegroundColor Yellow
$workloads = dotnet workload list | Out-String
$hasAndroid = $workloads -match "maui-android"
$hasIOS = $workloads -match "maui-ios"

if (-not $hasAndroid -or -not $hasIOS) {
    Write-Host "⚠️  MAUI workloads missing. Installing..." -ForegroundColor Yellow
    dotnet workload update
    dotnet workload install maui-android maui-ios
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Workload installation failed" -ForegroundColor Red
        exit 1
    }
}
Write-Host "✓ MAUI workloads installed" -ForegroundColor Green

# 3. Repository already initialized via git clone
Write-Host "✓ Repository ready" -ForegroundColor Green

Write-Host ""
Write-Host "✅ Bootstrap complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. dotnet restore" -ForegroundColor White
Write-Host "  2. dotnet build SiteAttendance.sln" -ForegroundColor White
Write-Host "  3. cd backend/src/SiteAttendance.Api && dotnet run" -ForegroundColor White
Write-Host "  4. Open solution in Visual Studio 2022 and deploy mobile app" -ForegroundColor White
Write-Host ""
