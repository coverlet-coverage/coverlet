#!/usr/bin/env pwsh
# build.ps1 - Helper script to build and package the Coverlet project.
#
# This script performs the following tasks:
# 1. Cleans up temporary files and build artifacts
# 2. Builds individual project targets (required for Linux compatibility)
# 3. Packages the project in both debug and release configurations
#
# Usage:
#   ./build.ps1
#
# Note: Ensure that the .NET SDK is installed and available in the system PATH.
# For running tests, use the separate test.ps1 script.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Get the workspace root directory (parent of the script's directory)
$WorkspaceRoot = Split-Path -Parent $PSScriptRoot
Set-Location $WorkspaceRoot
Write-Host "Starting build... (root folder: $(Split-Path -Leaf $WorkspaceRoot))"

Write-Host "Please cleanup '/tmp' folder if needed!"

# Shutdown build server and kill any running test processes
dotnet build-server shutdown
Get-Process -Name "coverlet.core.tests" -ErrorAction SilentlyContinue | Stop-Process -Force

# Delete coverage files
Write-Host "Cleaning up coverage files and build artifacts..."
$coveragePatterns = @(
    'coverage.cobertura.xml',
    'coverage.json',
    'coverage.net8.0.json',
    'coverage.net9.0.json',
    'coverage.net10.0.json',
    'coverage.opencover.xml',
    'coverage.net8.0.opencover.xml',
    'coverage.net9.0.opencover.xml',
    'coverage.net10.0.opencover.xml'
)
foreach ($pattern in $coveragePatterns) {
    Get-ChildItem -Path . -Filter $pattern -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force
}

# Delete binlog files
Get-ChildItem -Path . -Filter '*.binlog' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force

# Remove artifacts directory
if (Test-Path 'artifacts') {
    Remove-Item -Path 'artifacts' -Recurse -Force
}

# Clean up local NuGet packages
$nugetPackagesRoot = Join-Path $HOME '.nuget' 'packages'
$nugetCleanPaths = @(
    (Join-Path $nugetPackagesRoot 'coverlet.msbuild' 'V1.0.0'),
    (Join-Path $nugetPackagesRoot 'coverlet.collector' 'V1.0.0')
)
foreach ($path in $nugetCleanPaths) {
    if (Test-Path $path) {
        Remove-Item -Path $path -Recurse -Force
    }
}

# Remove preview packages from NuGet cache
$previewPatterns = @('coverlet.msbuild', 'coverlet.collector', 'coverlet.console', 'coverlet.MTP')
foreach ($pkg in $previewPatterns) {
    $pkgPath = Join-Path $nugetPackagesRoot $pkg
    if (Test-Path $pkgPath) {
        Get-ChildItem -Path $pkgPath -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match 'preview' } |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Remove TestResults, bin, and obj directories
Get-ChildItem -Path . -Include 'TestResults', 'bin', 'obj' -Directory -Recurse -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Cleanup complete. Starting build..."

# Pack initial packages (Debug)
dotnet pack -c Debug src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj
dotnet pack -c Debug src/coverlet.collector/coverlet.collector.csproj

# Restore packages using solution file to ensure all dependencies are resolved correctly
dotnet restore

# Build individual projects with binlog
dotnet build src/coverlet.core/coverlet.core.csproj -bl:build.core.binlog
dotnet build src/coverlet.collector/coverlet.collector.csproj -bl:build.collector.binlog
dotnet build src/coverlet.console/coverlet.console.csproj -bl:build.console.binlog
dotnet build src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj -bl:build.msbuild.tasks.binlog
dotnet build src/coverlet.MTP/coverlet.MTP.csproj -bl:build.MTP.binlog

# Create NuGet packages (Debug)
dotnet pack -c Debug src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj
dotnet pack -c Debug src/coverlet.collector/coverlet.collector.csproj
dotnet pack -c Debug src/coverlet.console/coverlet.console.csproj
dotnet pack -c Debug src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj
dotnet pack -c Debug src/coverlet.MTP/coverlet.MTP.csproj

# Create NuGet packages (Release)
dotnet pack src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj
dotnet pack src/coverlet.collector/coverlet.collector.csproj
dotnet pack src/coverlet.console/coverlet.console.csproj
dotnet pack src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj
dotnet pack src/coverlet.MTP/coverlet.MTP.csproj

# Build test projects with binlog
dotnet build test/coverlet.tests.utils/coverlet.tests.utils.csproj
dotnet build test/coverlet.collector.tests/coverlet.collector.tests.csproj -bl:build.collector.tests.binlog
dotnet build test/coverlet.core.tests/coverlet.core.tests.csproj -bl:build.coverlet.core.tests.binlog
# coverlet.core.coverage.tests !!!! does not build on Linux (Dev Container) VS debugger assemblies not available !!!!
if ($IsWindows) {
    dotnet build test/coverlet.core.coverage.tests/coverlet.core.coverage.tests.csproj -bl:build.core.coverage.tests.binlog
}
dotnet build test/coverlet.msbuild.tasks.tests/coverlet.msbuild.tasks.tests.csproj -bl:build.coverlet.msbuild.tasks.tests.binlog
dotnet build test/coverlet.MTP.tests/coverlet.MTP.tests.csproj -bl:build.MTP.tests.binlog
dotnet build test/coverlet.MTP.validation.tests/coverlet.MTP.validation.tests.csproj -bl:build.MTP.validation.tests.binlog

# Get the SDK version from global.json
$globalJson = Get-Content -Path 'global.json' -Raw | ConvertFrom-Json
$sdkVersion = $globalJson.sdk.version
$sdkMajorVersion = [int]($sdkVersion -split '\.')[0]

dotnet build test/coverlet.integration.tests/coverlet.integration.tests.csproj -f net8.0 -bl:build.coverlet.integration.tests.8.0.binlog /p:ContinuousIntegrationBuild=true
# Check if the SDK version is 10.0.* or higher (10.0.*, 11.0.*, etc.)
if ($sdkMajorVersion -ge 10) {
    Write-Host "Executing command for SDK version $sdkVersion (10.0+ detected)..."
    # dotnet build test/coverlet.integration.tests/coverlet.integration.tests.csproj -f net9.0 -bl:build.coverlet.integration.tests.9.0.binlog /p:ContinuousIntegrationBuild=true
    dotnet build test/coverlet.integration.tests/coverlet.integration.tests.csproj -f net10.0 -bl:build.coverlet.integration.tests.10.0.binlog /p:ContinuousIntegrationBuild=true
}

dotnet build-server shutdown

Write-Host "Build complete!"
