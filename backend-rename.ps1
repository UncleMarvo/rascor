cd "C:\WORK\Contracts - RASCORE\Applications\rascor\backend"

# ----------------------------------------------------------------
# 1. Rename backend project folders
# ----------------------------------------------------------------
Write-Host "📁 Renaming backend project folders..." -ForegroundColor Yellow

if (Test-Path "SiteAttendance.Api") {
    Rename-Item "SiteAttendance.Api" "Rascor.Api"
    Write-Host "  ✅ Renamed SiteAttendance.Api → Rascor.Api"
}

if (Test-Path "SiteAttendance.Application") {
    Rename-Item "SiteAttendance.Application" "Rascor.Application"
    Write-Host "  ✅ Renamed SiteAttendance.Application → Rascor.Application"
}

if (Test-Path "SiteAttendance.Domain") {
    Rename-Item "SiteAttendance.Domain" "Rascor.Domain"
    Write-Host "  ✅ Renamed SiteAttendance.Domain → Rascor.Domain"
}

if (Test-Path "SiteAttendance.Infrastructure") {
    Rename-Item "SiteAttendance.Infrastructure" "Rascor.Infrastructure"
    Write-Host "  ✅ Renamed SiteAttendance.Infrastructure → Rascor.Infrastructure"
}

# ----------------------------------------------------------------
# 2. Rename .csproj files inside each folder
# ----------------------------------------------------------------
Write-Host "`n📄 Renaming .csproj files..." -ForegroundColor Yellow

Rename-Item "Rascor.Api\SiteAttendance.Api.csproj" "Rascor.Api.csproj" -ErrorAction SilentlyContinue
Rename-Item "Rascor.Application\SiteAttendance.Application.csproj" "Rascor.Application.csproj" -ErrorAction SilentlyContinue
Rename-Item "Rascor.Domain\SiteAttendance.Domain.csproj" "Rascor.Domain.csproj" -ErrorAction SilentlyContinue
Rename-Item "Rascor.Infrastructure\SiteAttendance.Infrastructure.csproj" "Rascor.Infrastructure.csproj" -ErrorAction SilentlyContinue

Write-Host "  ✅ Renamed all .csproj files"

# ----------------------------------------------------------------
# 3. Rename solution file if exists
# ----------------------------------------------------------------
Write-Host "`n📄 Renaming solution file..." -ForegroundColor Yellow

if (Test-Path "SiteAttendance.sln") {
    Rename-Item "SiteAttendance.sln" "Rascor.sln"
    Write-Host "  ✅ Renamed SiteAttendance.sln → Rascor.sln"
}

# ----------------------------------------------------------------
# 4. Update all file contents
# ----------------------------------------------------------------
Write-Host "`n✏️  Updating file contents..." -ForegroundColor Yellow

$replacements = @(
    @{Old = "SiteAttendance.Infrastructure"; New = "Rascor.Infrastructure"},
    @{Old = "SiteAttendance.Application"; New = "Rascor.Application"},
    @{Old = "SiteAttendance.Domain"; New = "Rascor.Domain"},
    @{Old = "SiteAttendance.Api"; New = "Rascor.Api"},
    @{Old = "SiteAttendance"; New = "Rascor"},
    @{Old = "Site Attendance"; New = "RASCOR"},
    @{Old = "site-attendance"; New = "rascor"}
)

$fileTypes = @("*.cs", "*.csproj", "*.sln", "*.json", "*.xml", "*.config")
$fileCount = 0

Get-ChildItem -Path "." -Recurse -Include $fileTypes -File | ForEach-Object {
    $file = $_
    
    # Skip obj/bin folders
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
        Write-Host "  ⚠️  Skipped: $($file.Name)" -ForegroundColor Yellow
    }
}

Write-Host "`n  📊 Updated $fileCount files" -ForegroundColor Cyan

# ----------------------------------------------------------------
# 5. Clean build artifacts
# ----------------------------------------------------------------
Write-Host "`n🧹 Cleaning build artifacts..." -ForegroundColor Yellow

Get-ChildItem -Path "." -Include "obj","bin" -Recurse -Directory | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force
    Write-Host "  ✅ Cleaned: $($_.FullName)"
}

Write-Host "`n✨ Backend rename complete!" -ForegroundColor Green