# ================================================================
# RENAME PROJECT FROM SITE-ATTENDANCE TO RASCOR
# ================================================================
# Run this from the root of your repository
# Usage: .\rename-to-rascor.ps1

$ErrorActionPreference = "Stop"

Write-Host "🔄 Starting project rename to RASCOR..." -ForegroundColor Cyan

# ----------------------------------------------------------------
# 1. Rename project folders
# ----------------------------------------------------------------
Write-Host "`n📁 Renaming project folders..." -ForegroundColor Yellow

if (Test-Path "mobile\SiteAttendance.App") {
    Rename-Item "mobile\SiteAttendance.App" "Rascor.App"
    Write-Host "  ✅ Renamed SiteAttendance.App → Rascor.App"
}

if (Test-Path "mobile\SiteAttendance.App.Core") {
    Rename-Item "mobile\SiteAttendance.App.Core" "Rascor.App.Core"
    Write-Host "  ✅ Renamed SiteAttendance.App.Core → Rascor.App.Core"
}

# ----------------------------------------------------------------
# 2. Rename solution file
# ----------------------------------------------------------------
Write-Host "`n📄 Renaming solution file..." -ForegroundColor Yellow

if (Test-Path "mobile\SiteAttendance.sln") {
    Rename-Item "mobile\SiteAttendance.sln" "Rascor.sln"
    Write-Host "  ✅ Renamed SiteAttendance.sln → Rascor.sln"
}

# ----------------------------------------------------------------
# 3. Update content in all files
# ----------------------------------------------------------------
Write-Host "`n✏️  Updating file contents..." -ForegroundColor Yellow

$replacements = @(
    @{Old = "SiteAttendance.App.Core"; New = "Rascor.App.Core"},
    @{Old = "SiteAttendance.App"; New = "Rascor.App"},
    @{Old = "SiteAttendance"; New = "Rascor"},
    @{Old = "Site Attendance"; New = "RASCOR"},
    @{Old = "site-attendance"; New = "rascor"},
    @{Old = "com.yourorg.siteattendance"; New = "com.yourorg.rascor"}
)

$fileTypes = @("*.cs", "*.csproj", "*.sln", "*.xaml", "*.json", "*.xml", "*.targets", "*.props")

$fileCount = 0
Get-ChildItem -Path "mobile" -Recurse -Include $fileTypes -File | ForEach-Object {
    $file = $_
    
    # Skip files in obj/bin folders to avoid build artifacts
    if ($file.FullName -match '\\obj\\' -or $file.FullName -match '\\bin\\') {
        return
    }
    
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        $modified = $false
        
        foreach ($replacement in $replacements) {
            if ($content -match [regex]::Escape($replacement.Old)) {
                $content = $content -replace [regex]::Escape($replacement.Old), $replacement.New
                $modified = $true
            }
        }
        
        if ($modified) {
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
            $fileCount++
            Write-Host "  ✅ Updated: $($file.Name)" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "  ⚠️  Skipped: $($file.Name) - $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host "`n  📊 Updated $fileCount files" -ForegroundColor Cyan

# ----------------------------------------------------------------
# 4. Update Android package name in AndroidManifest.xml
# ----------------------------------------------------------------
Write-Host "`n📱 Updating Android package identifier..." -ForegroundColor Yellow

$manifestPath = "mobile\Rascor.App\Platforms\Android\AndroidManifest.xml"
if (Test-Path $manifestPath) {
    $manifest = Get-Content $manifestPath -Raw
    $manifest = $manifest -replace 'package="com\.yourorg\.siteattendance"', 'package="com.yourorg.rascor"'
    Set-Content -Path $manifestPath -Value $manifest -NoNewline
    Write-Host "  ✅ Updated AndroidManifest.xml"
}

# ----------------------------------------------------------------
# 5. Clean obj/bin folders to remove old artifacts
# ----------------------------------------------------------------
Write-Host "`n🧹 Cleaning build artifacts..." -ForegroundColor Yellow

Get-ChildItem -Path "mobile" -Include "obj","bin" -Recurse -Directory | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force
    Write-Host "  ✅ Cleaned: $($_.FullName)"
}

# ----------------------------------------------------------------
# 6. Stage changes for commit
# ----------------------------------------------------------------
Write-Host "`n📦 Staging changes..." -ForegroundColor Yellow

git add -A

Write-Host "`n✨ Project rename complete!" -ForegroundColor Green
Write-Host "`n📝 Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review changes: git status"
Write-Host "  2. Commit: git commit -m 'Rename project to RASCOR'"
Write-Host "  3. Push: git push"
Write-Host "`n  Then rebuild in Visual Studio!"