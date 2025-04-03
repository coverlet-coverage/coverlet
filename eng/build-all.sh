#!/bin/bash

# Build Script for Coverlet
# ------------------------
# This script provides a complete build environment setup and cleanup for Coverlet development.
# It's designed to support fast branch switching by:
# - Cleaning all temporary build artifacts
# - Removing cached NuGet packages
# - Rebuilding all projects and tests
# - Generating new binlogs for debugging
#
# Key features:
# - Cleans TestResults, bin, obj folders for clean builds
# - Removes cached NuGet packages to prevent version conflicts
# - Builds and packs all Coverlet projects
# - Generates binlogs for build diagnostics
# - Supports CI builds with ContinuousIntegrationBuild=true
#
# Usage: ./scripts/build-all.sh
#
# Note: Run this script after switching branches to ensure a clean development environment

# Cleanup temp folders and files
echo "Please cleanup temp folder!"
dotnet build-server shutdown
rm -f coverage.cobertura.xml
rm -f coverage.json
rm -f coverage.net8.0.json
rm -f test/coverlet.integration.determisticbuild/*.binlog
rm -rf artifacts

# Clean nuget packages
rm -rf ~/.nuget/packages/coverlet.msbuild/V1.0.0
rm -rf ~/.nuget/packages/coverlet.collector/V1.0.0
find . -type d \( -name "TestResults" -o -name "bin" -o -name "obj" \) -exec rm -rf {} +
find ~/.nuget/packages -type d \( -name "coverlet.msbuild" -o -name "coverlet.collector" -o -name "coverlet.console" \) -name "8.0.0-preview*" -exec rm -rf {} +

# Build and pack projects
dotnet pack -c Debug src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj /p:ContinuousIntegrationBuild=true
dotnet pack -c Debug src/coverlet.collector/coverlet.collector.csproj /p:ContinuousIntegrationBuild=true
dotnet build -bl:build.binlog /p:ContinuousIntegrationBuild=true

# Create binlog for tests
dotnet build test/coverlet.collector.tests/coverlet.collector.tests.csproj -bl:build.collector.tests.binlog /p:ContinuousIntegrationBuild=true
dotnet build test/coverlet.core.coverage.tests/coverlet.core.coverage.tests.csproj -bl:build.core.coverage.tests.binlog /p:ContinuousIntegrationBuild=true
dotnet build test/coverlet.core.tests/coverlet.core.tests.csproj -bl:build.coverlet.core.tests.binlog /p:ContinuousIntegrationBuild=true

# Create nuget packages
dotnet pack -c Debug src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj /p:ContinuousIntegrationBuild=true
dotnet pack -c Debug src/coverlet.collector/coverlet.collector.csproj /p:ContinuousIntegrationBuild=true
dotnet pack -c Debug src/coverlet.console/coverlet.console.csproj /p:ContinuousIntegrationBuild=true
dotnet pack -c Debug src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj /p:ContinuousIntegrationBuild=true
dotnet pack src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj /p:ContinuousIntegrationBuild=true
dotnet pack src/coverlet.collector/coverlet.collector.csproj /p:ContinuousIntegrationBuild=true
dotnet pack src/coverlet.console/coverlet.console.csproj /p:ContinuousIntegrationBuild=true
dotnet pack src/coverlet.msbuild.tasks/coverlet.msbuild.tasks.csproj /p:ContinuousIntegrationBuild=true

dotnet build-server shutdown

# Commented out sections converted to shell script comments
: <<'END_COMMENT'
dotnet publish tool/Microsoft.DotNet.RemoteExecutor/tests/Microsoft.DotNet.RemoteExecutor.Tests.csproj

dotnet test test/coverlet.collector.tests /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*" --results-directory:"./artifacts/Reports"
dotnet test test/coverlet.core.tests /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*" --results-directory:"./artifacts/Reports"
dotnet test test/coverlet.integration.tests /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*" --results-directory:"./artifacts/Reports"
dotnet test test/coverlet.msbuild.tasks.tests /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*" --results-directory:"./artifacts/Reports"

dotnet test test/coverlet.core.tests/coverlet.core.tests.csproj /p:CollectCoverage=true -c Debug --no-build -bl:test.core.binlog --diag:"artifacts/log/debug/coverlet.core.test.diag.log;tracelevel=verbose"
dotnet test test/coverlet.core.coverage.tests/coverlet.core.coverage.tests.csproj /p:CollectCoverage=true -c Debug --no-build -bl:test.core.coverage.binlog --diag:"artifacts/log/debug/coverlet.core.coverage.test.diag.log;tracelevel=verbose"
dotnet test test/coverlet.msbuild.tasks.tests/coverlet.msbuild.tasks.tests.csproj /p:CollectCoverage=true -c Debug --no-build -bl:test.msbuild.binlog --diag:"artifacts/log/debug/coverlet.msbuild.test.diag.log;tracelevel=verbose"

dotnet test --no-build -bl:test.binlog /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*,\[coverlet.tests.xunit.extensions]*,\[coverlet.tests.projectsample]*,\[testgen_]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" --diag:"artifacts/log/Debug/coverlet.test.diag.log;tracelevel=verbose"

dotnet test -c Debug --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
reportgenerator -reports:"**/*.opencover.xml" -targetdir:"artifacts/reports" -reporttypes:"HtmlInline_AzurePipelines_Dark;Cobertura" -assemblyfilters:"-xunit;-coverlet.testsubject;-Coverlet.Tests.ProjectSample.*;-coverlet.core.tests.samples.netstandard;-coverlet.tests.utils;-coverlet.tests.xunit.extensions;-coverletsamplelib.integration.template;-testgen_*"

dotnet test test/coverlet.core.remote.tests/coverlet.core.remote.tests.csproj -c Debug /p:CollectCoverage=true /p:CoverletOutputFormat=opencover --diag:"artifacts/log/debug/coverlet.core.remote.test.diag.log;tracelevel=verbose"
END_COMMENT