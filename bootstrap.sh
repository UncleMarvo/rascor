#!/usr/bin/env bash
# bootstrap.sh - Turn-key .NET 9 MAUI + ASP.NET Core scaffold
# Generates site-attendance solution with geofencing support

set -euo pipefail

echo "🚀 Site Attendance Scaffold - .NET 9 MAUI + Backend"
echo ""

# 1. Check .NET SDK
echo "Checking .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Install from https://dot.net"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "✓ .NET SDK $DOTNET_VERSION"

if [[ ! $DOTNET_VERSION =~ ^9\. ]]; then
    echo "⚠️  .NET 9.x required, found $DOTNET_VERSION"
    echo "   Install .NET 9 SDK from https://dotnet.microsoft.com/download/dotnet/9.0"
    exit 1
fi

# 2. Check MAUI workloads
echo "Checking MAUI workloads..."
WORKLOADS=$(dotnet workload list)
if ! echo "$WORKLOADS" | grep -q "maui-android" || ! echo "$WORKLOADS" | grep -q "maui-ios"; then
    echo "⚠️  MAUI workloads missing. Installing..."
    dotnet workload update
    dotnet workload install maui-android maui-ios
fi
echo "✓ MAUI workloads installed"

# 3. Repository already initialized via git clone
echo "✓ Repository ready"

echo ""
echo "✅ Bootstrap complete!"
echo ""
echo "Next steps:"
echo "  1. dotnet restore"
echo "  2. dotnet build SiteAttendance.sln"
echo "  3. cd backend/src/SiteAttendance.Api && dotnet run"
echo "  4. Open solution in Visual Studio 2022 and deploy mobile app"
echo ""
