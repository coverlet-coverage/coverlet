#!/usr/bin/env pwsh
# test.ps1 - Helper script to run tests for the Coverlet project.
#
# Usage:
#   ./test.ps1
#
# Note: Ensure that the .NET SDK is installed and available in the system PATH.
# For building the project, use the separate build.ps1 script.
# Note: .NET 10+ does not support 'dotnet test' with vstest; all test projects
#       are executed via 'dotnet exec' on the pre-built DLL directly.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Get the workspace root directory (parent of the script's directory)
$WorkspaceRoot = Split-Path -Parent $PSScriptRoot
Set-Location $WorkspaceRoot
Write-Host "Starting tests... (root folder: $(Split-Path -Leaf $WorkspaceRoot))"

# Kill existing test processes if they exist
$testProcesses = @(
    'coverlet.core.tests',
    'coverlet.core.coverage.tests',
    'coverlet.msbuild.tasks.tests',
    'coverlet.collector.tests',
    'coverlet.integration.tests',
    'coverlet.MTP.tests',
    'coverlet.MTP.validation.tests'
)
foreach ($proc in $testProcesses) {
    Get-Process -Name $proc -ErrorAction SilentlyContinue | Stop-Process -Force
}

$BuildConfiguration = 'Debug'
$BuildConfigDir = $BuildConfiguration.ToLower()

# Get the SDK version from global.json and build the list of frameworks to test
$globalJson = Get-Content -Path 'global.json' -Raw | ConvertFrom-Json
$sdkVersion = $globalJson.sdk.version
$sdkMajorVersion = [int]($sdkVersion -split '\.')[0]

$frameworks = [System.Collections.Generic.List[string]]@('net8.0')
if ($sdkMajorVersion -ge 9) { $frameworks.Add('net9.0') }
if ($sdkMajorVersion -ge 10) { $frameworks.Add('net10.0') }

Write-Host "Detected SDK version $sdkVersion. Testing frameworks: $($frameworks -join ', ')"

foreach ($fw in $frameworks) {
    $fwDir = "${BuildConfigDir}_${fw}"
    Write-Host "=========================================="
    Write-Host "Testing $fw"
    Write-Host "=========================================="

    # coverlet.MTP.tests
    dotnet build-server shutdown
    dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.MTP.tests/$fwDir/coverlet.MTP.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.MTP.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.MTP.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress

    # coverlet.core.tests
    dotnet build-server shutdown
    dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.core.tests/$fwDir/coverlet.core.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.core.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.core.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress

    # coverlet.core.coverage.tests !!!! does not work on Linux (Dev Container) VS debugger assemblies not available !!!!
    if ($IsWindows) {
        dotnet build-server shutdown
        dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.core.coverage.tests/$fwDir/coverlet.core.coverage.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.core.coverage.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.core.coverage.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress
    }

    # coverlet.msbuild.tasks.tests
    dotnet build-server shutdown
    dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.msbuild.tasks.tests/$fwDir/coverlet.msbuild.tasks.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.msbuild.tasks.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.msbuild.tasks.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress

    # coverlet.collector.tests -> fails 'dotnet test' is not supported anymore. This requires an additional stage for < .NET 10 to build the test project with 'dotnet build' and then execute the tests with 'dotnet exec' on the pre-built DLL directly. For .NET 10+ this is not required as 'dotnet test' is supported again.
    # dotnet build-server shutdown
    # dotnet test test/coverlet.collector.tests/coverlet.collector.tests.csproj -c Debug -f ${fwDir} -bl:test.collector.binlog /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" --diag:"$WORKSPACE_ROOT/artifacts/log/Debug/coverlet.collector.test.diag.${fwDir}.log;tracelevel=verbose"

    # coverlet.integration.tests
    dotnet build-server shutdown
    dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.integration.tests/$fwDir/coverlet.integration.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.integration.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.integration.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress

    # coverlet.MTP.validation.tests
    dotnet build-server shutdown
    dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.MTP.validation.tests/$fwDir/coverlet.MTP.validation.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.MTP.validation.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.MTP.validation.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress
}
