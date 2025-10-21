# Setup-DeploymentConfig.ps1
# Helper script to find your Azure resources and create deployment config

function Write-Info { param($msg) Write-Host "ℹ️  $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "✅ $msg" -ForegroundColor Green }
function Write-Error { param($msg) Write-Host "❌ $msg" -ForegroundColor Red }

Write-Host "`n🔍 Azure Resource Discovery`n" -ForegroundColor Magenta

# Check Azure CLI
$azVersion = az version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Azure CLI not installed!"
    Write-Host "Download from: https://aka.ms/installazurecliwindows"
    exit 1
}

# Check login
Write-Info "Checking Azure login..."
$account = az account show 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Info "Logging into Azure..."
    az login
}

# Get subscription
$subscription = az account show --query "name" -o tsv
Write-Success "Logged in to: $subscription"

# List resource groups
Write-Host "`n📦 Your Resource Groups:" -ForegroundColor Yellow
az group list --query "[].{Name:name, Location:location}" --output table

Write-Host "`n"
$rgName = Read-Host "Enter your Resource Group name (e.g., rascor-rg)"

# List App Services in that resource group
Write-Host "`n🌐 App Services in '$rgName':" -ForegroundColor Yellow
az webapp list --resource-group $rgName --query "[].{Name:name, DefaultHostName:defaultHostName, State:state}" --output table

Write-Host "`n"
$appName = Read-Host "Enter your App Service name (e.g., siteattendance-api-1411956859)"

# Verify it exists
$appExists = az webapp show --name $appName --resource-group $rgName 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error "App Service not found!"
    exit 1
}

Write-Success "App Service verified!"

# Create deployment script with correct parameters
$scriptContent = @"
# Generated deployment configuration
`$ResourceGroup = "$rgName"
`$AppServiceName = "$appName"

# Run the deployment
.\Deploy-RascorBackend.ps1 -ResourceGroup `$ResourceGroup -AppServiceName `$AppServiceName
"@

$scriptContent | Out-File "Deploy.ps1" -Encoding UTF8

Write-Host "`n"
Write-Host "═══════════════════════════════════════" -ForegroundColor Green
Write-Success "Configuration saved to Deploy.ps1"
Write-Host "═══════════════════════════════════════" -ForegroundColor Green
Write-Host "`n📝 Your settings:" -ForegroundColor Cyan
Write-Host "   Resource Group: $rgName" -ForegroundColor White
Write-Host "   App Service: $appName" -ForegroundColor White
Write-Host "`n🚀 To deploy, run:" -ForegroundColor Cyan
Write-Host "   .\Deploy.ps1" -ForegroundColor Yellow
Write-Host "`n"