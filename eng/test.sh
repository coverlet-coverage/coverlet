#!/bin/bash
set -e

# Get the workspace root directory (parent of the script's directory)
WORKSPACE_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$WORKSPACE_ROOT"
echo "Starting tests... (root folder: ${PWD##*/})"

# Kill existing test processes if they exist
pkill -f "coverlet.core.tests.dll" 2>/dev/null || true
pkill -f "coverlet.core.coverage.tests.dll" 2>/dev/null || true
pkill -f "coverlet.msbuild.tasks.tests.dll" 2>/dev/null || true
pkill -f "coverlet.integration.tests.dll" 2>/dev/null || true

# coverlet.core.tests
dotnet build-server shutdown
dotnet test test/coverlet.core.tests/coverlet.core.tests.csproj -c Debug --no-build -bl:test.core.binlog /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" -- --results-directory "$WORKSPACE_ROOT/artifacts/reports" --report-xunit-trx --report-xunit-trx-filename "coverlet.core.tests.trx" --diagnostic --diagnostic-output-directory "$WORKSPACE_ROOT/artifacts/log/Debug" --diagnostic-output-fileprefix "coverlet.core.tests"

# coverlet.core.coverage.tests !!!! does not work on Linux (Dev Container) VS debugger assemblies not available !!!!
# dotnet build-server shutdown
# dotnet test test/coverlet.core.coverage.tests/coverlet.core.coverage.tests.csproj -c Debug --no-build -bl:test.core.coverage.binlog /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" -- --results-directory "$WORKSPACE_ROOT/artifacts/reports" --report-xunit-trx --report-xunit-trx-filename "coverlet.core.coverage.tests.trx" --diagnostic --diagnostic-output-directory "$WORKSPACE_ROOT/artifacts/log/Debug" --diagnostic-output-fileprefix "coverlet.core.coverage.tests"

# coverlet.msbuild.tasks.tests
dotnet build-server shutdown
dotnet test test/coverlet.msbuild.tasks.tests/coverlet.msbuild.tasks.tests.csproj -c Debug --no-build -bl:test.msbuild.binlog --results-directory:"$WORKSPACE_ROOT/artifacts/reports" /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.xunit.extensions]*%2c[coverlet.tests.projectsample]*%2c[testgen_]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" --diag:"$WORKSPACE_ROOT/artifacts/log/Debug/coverlet.msbuild.test.diag.log;tracelevel=verbose"

# coverlet.collector.tests
dotnet build-server shutdown
dotnet test test/coverlet.collector.tests/coverlet.collector.tests.csproj -c Debug --no-build -bl:test.collector.binlog /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" --diag:"$WORKSPACE_ROOT/artifacts/log/Debug/coverlet.collector.test.diag.log;tracelevel=verbose"

# coverlet.integration.tests (default net8.0)
dotnet build-server shutdown
dotnet test test/coverlet.integration.tests/coverlet.integration.tests.csproj -f net8.0 -c Debug --no-build -bl:test.integration.binlog -- --results-directory "$WORKSPACE_ROOT/artifacts/reports" --report-xunit-trx --report-xunit-trx-filename "coverlet.integration.tests.trx" --diagnostic --diagnostic-output-directory "$WORKSPACE_ROOT/artifacts/log/Debug" --diagnostic-output-fileprefix "coverlet.integration.tests"

dotnet build-server shutdown

# Get the SDK version from global.json
SDK_VERSION=$(grep -oP '"version"\s*:\s*"\K[^"]+' global.json)
SDK_MAJOR_VERSION=$(echo "$SDK_VERSION" | cut -d'.' -f1)

# Check if the SDK version is 9.0.* or higher (9.0.*, 10.0.*, etc.)
if [[ "$SDK_MAJOR_VERSION" -ge 9 ]]; then
    # Check if the net9.0 test dll exists
    if [ -f "$WORKSPACE_ROOT/artifacts/bin/coverlet.integration.tests/debug_net9.0/coverlet.integration.tests.dll" ]; then
        echo "Executing command for SDK version $SDK_VERSION (9.0+ detected)..."
        dotnet test test/coverlet.integration.tests/coverlet.integration.tests.csproj -f net9.0 -c Debug --no-build -bl:test.integration.binlog -- --results-directory "$WORKSPACE_ROOT/artifacts/reports" --report-xunit-trx --report-xunit-trx-filename "coverlet.integration.tests.trx" --diagnostic --diagnostic-output-directory "$WORKSPACE_ROOT/artifacts/log/Debug" --diagnostic-output-fileprefix "coverlet.integration.tests"
        dotnet build-server shutdown
    else
        echo "Skipping command execution. Required file does not exist."
    fi
fi
