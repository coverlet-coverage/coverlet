#!/bin/bash
set -e

# build.sh - Helper script to build and package the Coverlet project.
#
# This script performs the following tasks:
# 1. Cleans up temporary files and build artifacts
# 2. Builds individual project targets (required for Linux compatibility)
# 3. Packages the project in both debug and release configurations
#
# Usage:
#   ./build.sh
#
# Note: Ensure that the .NET SDK is installed and available in the system PATH.
# For running tests, use the separate test.sh script.

# Get the workspace root directory
# Get the workspace root directory (parent of the script's directory)
WORKSPACE_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$WORKSPACE_ROOT"
echo "Starting build... (root folder: ${PWD##*/})"

echo "Please cleanup '/tmp' folder if needed!"

# Shutdown build server and kill any running test processes
dotnet build-server shutdown
pkill -f "coverlet.core.tests.exe" 2>/dev/null || true

# Delete coverage files
echo "Cleaning up coverage files and build artifacts..."
find . -name "coverage.cobertura.xml" -delete 2>/dev/null || true
find . -name "coverage.json" -delete 2>/dev/null || true
find . -name "coverage.net8.0.json" -delete 2>/dev/null || true
find . -name "coverage.opencover.xml" -delete 2>/dev/null || true
find . -name "coverage.net8.0.opencover.xml" -delete 2>/dev/null || true

# Delete binlog files in integration tests
rm -f test/coverlet.integration.determisticbuild/*.binlog 2>/dev/null || true

# Remove artifacts directory
rm -rf artifacts

# Clean up local NuGet packages
rm -rf "$HOME/.nuget/packages/coverlet.msbuild/V1.0.0" 2>/dev/null || true
rm -rf "$HOME/.nuget/packages/coverlet.collector/V1.0.0" 2>/dev/null || true

# Remove TestResults, bin, and obj directories
find . -type d \( -name "TestResults" -o -name "bin" -o -name "obj" \) -exec rm -rf {} + 2>/dev/null || true

# Remove preview packages from NuGet cache
find "$HOME/.nuget/packages" -type d \( -path "*/coverlet.msbuild/8.0.0-preview*" -o -path "*/coverlet.collector/8.0.0-preview*" -o -path "*/coverlet.console/8.0.0-preview*" \) -exec rm -rf {} + 2>/dev/null || true

echo "Cleanup complete. Starting build..."

# Pack initial packages (Debug)
dotnet pack -c Debug src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj /p:ContinuousIntegrationBuild=true
dotnet pack -c Debug src/coverlet.collector/coverlet.collector.csproj /p:ContinuousIntegrationBuild=true

# Build individual projects with binlog
dotnet build src/coverlet.core/coverlet.core.csproj -bl:build.core.binlog /p:ContinuousIntegrationBuild=true
dotnet build src/coverlet.collector/coverlet.collector.csproj -bl:build.collector.binlog /p:ContinuousIntegrationBuild=true
dotnet build src/coverlet.console/coverlet.console.csproj -bl:build.console.binlog /p:ContinuousIntegrationBuild=true
dotnet build src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj -bl:build.msbuild.tasks.binlog /p:ContinuousIntegrationBuild=true

# Build test projects with binlog
dotnet build test/coverlet.collector.tests/coverlet.collector.tests.csproj -bl:build.collector.tests.binlog /p:ContinuousIntegrationBuild=true
dotnet build test/coverlet.core.coverage.tests/coverlet.core.coverage.tests.csproj -bl:build.core.coverage.tests.binlog /p:ContinuousIntegrationBuild=true
dotnet build test/coverlet.core.tests/coverlet.core.tests.csproj -bl:build.coverlet.core.tests.binlog /p:ContinuousIntegrationBuild=true
dotnet build test/coverlet.msbuild.tasks.tests/coverlet.msbuild.tasks.tests.csproj -bl:build.coverlet.msbuild.tasks.tests.binlog /p:ContinuousIntegrationBuild=true
dotnet build test/coverlet.integration.tests/coverlet.integration.tests.csproj -f net8.0 -bl:build.coverlet.core.tests.8.0.binlog /p:ContinuousIntegrationBuild=true

# Get the SDK version from global.json
SDK_VERSION=$(grep -oP '"version"\s*:\s*"\K[^"]+' global.json)
SDK_MAJOR_VERSION=$(echo "$SDK_VERSION" | cut -d'.' -f1)

# Check if the SDK version is 9.0.* or higher (9.0.*, 10.0.*, etc.)
if [[ "$SDK_MAJOR_VERSION" -ge 9 ]]; then
    echo "Executing command for SDK version $SDK_VERSION (9.0+ detected)..."
    dotnet build test/coverlet.integration.tests/coverlet.integration.tests.csproj -f net9.0 -bl:build.coverlet.core.tests.9.9.binlog /p:ContinuousIntegrationBuild=true
fi

# Create NuGet packages (Debug)
dotnet pack -c Debug src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj /p:ContinuousIntegrationBuild=true
dotnet pack -c Debug src/coverlet.collector/coverlet.collector.csproj /p:ContinuousIntegrationBuild=true
dotnet pack -c Debug src/coverlet.console/coverlet.console.csproj /p:ContinuousIntegrationBuild=true
dotnet pack -c Debug src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj /p:ContinuousIntegrationBuild=true

# Create NuGet packages (Release)
dotnet pack src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj /p:ContinuousIntegrationBuild=true
dotnet pack src/coverlet.collector/coverlet.collector.csproj /p:ContinuousIntegrationBuild=true
dotnet pack src/coverlet.console/coverlet.console.csproj /p:ContinuousIntegrationBuild=true
dotnet pack src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj /p:ContinuousIntegrationBuild=true

dotnet build-server shutdown

echo "Build complete!"
