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
#Requires -PSEdition Core
#Requires -Version 7

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
    'coverlet.integration.legacy.tests',
    'coverlet.MTP.tests',
    'coverlet.MTP.validation.tests'
)
foreach ($proc in $testProcesses) {
    Get-Process -Name $proc -ErrorAction SilentlyContinue | Stop-Process -Force
}

# Get the SDK version from global.json (WorkspaceRoot/global.json) and build the list of frameworks to test
$globalJson = Get-Content -Path 'global.json' -Raw | ConvertFrom-Json
$sdkVersion = $globalJson.sdk.version
$sdkMajorVersion = [int]($sdkVersion -split '\.')[0]

$BuildConfiguration = 'Debug'
$BuildConfigDir = $BuildConfiguration.ToLower()

# Get the SDK version from global.json and build the list of frameworks to test
$globalJson = Get-Content -Path 'global.json' -Raw | ConvertFrom-Json
$sdkVersion = $globalJson.sdk.version
$sdkMajorVersion = [int]($sdkVersion -split '\.')[0]

$frameworks = [System.Collections.Generic.List[string]]@('net8.0')
# if ($sdkMajorVersion -ge 9) { $frameworks.Add('net9.0') }
if ($sdkMajorVersion -ge 10) { $frameworks.Add('net10.0') }

Write-Host "Detected SDK version $sdkVersion. Testing frameworks: $($frameworks -join ', ')"

foreach ($fw in $frameworks) {
    $fwDir = "${BuildConfigDir}_${fw}"
    Write-Host "=========================================="
    Write-Host ".NET 10 Testing $fw"
    Write-Host "=========================================="

    # coverlet.MTP.tests
    dotnet build-server shutdown
    dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.MTP.tests/$fwDir/coverlet.MTP.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.MTP.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.MTP.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress

    # coverlet.core.tests
    dotnet build-server shutdown
    dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.core.tests/$fwDir/coverlet.core.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.core.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.core.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress

    # coverlet.core.coverage.tests - waits for keyboard input (strange behavior)
    # dotnet build-server shutdown
    # dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.core.coverage.tests/$fwDir/coverlet.core.coverage.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.core.coverage.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.core.coverage.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress

    # coverlet.msbuild.tasks.tests
    dotnet build-server shutdown
    dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.msbuild.tasks.tests/$fwDir/coverlet.msbuild.tasks.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.msbuild.tasks.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.msbuild.tasks.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress

    # coverlet.integration.MTP.tests
    dotnet build-server shutdown
    dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.integration.MTP.tests/$fwDir/coverlet.integration.MTP.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.integration.MTP.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.integration.MTP.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress

    # coverlet.MTP.validation.tests
    dotnet build-server shutdown
    dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.MTP.validation.tests/$fwDir/coverlet.MTP.validation.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.MTP.validation.tests.${fwDir}.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.MTP.validation.tests.${fwDir}" --results-directory "$WorkspaceRoot/artifacts/reports/" --no-progress
}

# --- Legacy tests (net8.0, net9.0) using the legacy global.json (SDK 9.0.x) ---
$legacyGlobalJson = "$WorkspaceRoot/src/legacy/global.json"
if (Test-Path $legacyGlobalJson) {
    $legacyJson = Get-Content -Path $legacyGlobalJson -Raw | ConvertFrom-Json
    $legacySdkVersion = $legacyJson.sdk.version
    $legacySdkMajor = [int]($legacySdkVersion -split '\.')[0]

    if ($legacySdkMajor -eq 9) {
        Write-Host ""
        Write-Host "=========================================="
        Write-Host "Legacy tests (SDK $legacySdkVersion)"
        Write-Host "=========================================="

        # Swap global.json to use legacy SDK
        $originalGlobalJson = "$WorkspaceRoot/global.json"
        $backupGlobalJson = "$WorkspaceRoot/global.json.bak"
        Copy-Item -Path $originalGlobalJson -Destination $backupGlobalJson -Force
        Copy-Item -Path $legacyGlobalJson -Destination $originalGlobalJson -Force
        Write-Host "Switched global.json to legacy SDK $legacySdkVersion"

        try {
            $legacyFrameworks = @('net8.0', 'net9.0')
            Write-Host "Testing legacy frameworks: $($legacyFrameworks -join ', ')"

            foreach ($fw in $legacyFrameworks) {
                Write-Host "=========================================="
                Write-Host "Legacy Testing $fw"
                Write-Host "=========================================="

                dotnet build-server shutdown

                # coverlet.MTP.tests
                dotnet test test/coverlet.MTP.tests/coverlet.MTP.tests.csproj -f $fw -c $BuildConfiguration --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:ExcludeByAttribute="GeneratedCodeAttribute" --results-directory "$WorkspaceRoot/artifacts/reports/" --logger "trx;LogFileName=coverlet.MTP.tests.$fw.trx" --diag:"$WorkspaceRoot/artifacts/log/coverlet.MTP.tests.$fw.diag;tracelevel=verbose"

                # coverlet.core.tests
                dotnet test test/coverlet.core.tests/coverlet.core.tests.csproj -f $fw -c $BuildConfiguration --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" --results-directory "$WorkspaceRoot/artifacts/reports/" --logger "trx;LogFileName=coverlet.core.tests.$fw.trx" --diag:"$WorkspaceRoot/artifacts/log/coverlet.core.tests.$fw.diag;tracelevel=verbose"

                # coverlet.core.coverage.tests
                dotnet test test/coverlet.core.coverage.tests/coverlet.core.coverage.tests.csproj -f $fw -c $BuildConfiguration --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" --results-directory "$WorkspaceRoot/artifacts/reports/" --logger "trx;LogFileName=coverlet.core.coverage.tests.$fw.trx" --diag:"$WorkspaceRoot/artifacts/log/coverlet.core.coverage.tests.$fw.diag;tracelevel=verbose"

                # coverlet.msbuild.tasks.tests
                dotnet test test/coverlet.msbuild.tasks.tests/coverlet.msbuild.tasks.tests.csproj -f $fw -c $BuildConfiguration --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" --results-directory "$WorkspaceRoot/artifacts/reports/" --logger "trx;LogFileName=coverlet.msbuild.tasks.tests.$fw.trx" --diag:"$WorkspaceRoot/artifacts/log/coverlet.msbuild.tasks.tests.$fw.diag;tracelevel=verbose"

                # coverlet.collector.tests
                dotnet test test/coverlet.collector.tests/coverlet.collector.tests.csproj -f $fw -c $BuildConfiguration --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" --results-directory "$WorkspaceRoot/artifacts/reports/" --logger "trx;LogFileName=coverlet.collector.tests.$fw.trx" --diag:"$WorkspaceRoot/artifacts/log/coverlet.collector.tests.$fw.diag;tracelevel=verbose"

                # coverlet.integration.legacy.tests
                dotnet test test/coverlet.integration.legacy.tests/coverlet.integration.legacy.tests.csproj -f $fw -c $BuildConfiguration --no-build --results-directory "$WorkspaceRoot/artifacts/reports/" --logger "trx;LogFileName=coverlet.integration.legacy.tests.$fw.trx" --diag:"$WorkspaceRoot/artifacts/log/coverlet.integration.legacy.tests.$fw.diag;tracelevel=verbose"

                # coverlet.MTP.validation.tests
                dotnet exec "$WorkspaceRoot/artifacts/bin/coverlet.MTP.validation.tests/${BuildConfiguration}_$fw/coverlet.MTP.validation.tests.dll" --diagnostic --diagnostic-verbosity trace --report-xunit-trx --report-xunit-trx-filename "coverlet.MTP.validation.tests.$fw.trx" --diagnostic-output-directory "$WorkspaceRoot/artifacts/log/" --diagnostic-file-prefix "coverlet.MTP.validation.tests_$fw" --results-directory "$WorkspaceRoot/artifacts/reports/"
            }

            dotnet build-server shutdown
        }
        finally {
            # Restore original global.json
            Copy-Item -Path $backupGlobalJson -Destination $originalGlobalJson -Force
            Remove-Item -Path $backupGlobalJson -Force
            Write-Host "Restored original global.json"
        }
    }
    else {
        Write-Host "Legacy global.json SDK version $legacySdkVersion is not SDK 9.x, skipping legacy tests."
    }
}
else {
    Write-Host "No legacy global.json found at $legacyGlobalJson, skipping legacy tests."
}
