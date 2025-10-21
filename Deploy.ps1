# Generated deployment configuration
$ResourceGroup = "SiteAttendanceRG"
$AppServiceName = "siteattendance-api-1411956859"

# Run the deployment
.\Deploy-RascorBackend.ps1 -ResourceGroup $ResourceGroup -AppServiceName $AppServiceName
