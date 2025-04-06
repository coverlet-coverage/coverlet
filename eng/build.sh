#!/bin/bash

# build.sh - Helper script to build, package, and test the Coverlet project.
#
# This script performs the following tasks:
# 1. Builds the project in debug configuration and generates a binary log.
# 2. Packages the project in both debug and release configurations.
# 3. Shuts down any running .NET build servers.
# 4. Runs unit tests for various Coverlet components with code coverage enabled,
#    generating binary logs and diagnostic outputs.
# 5. Outputs test results in xUnit TRX format and stores them in the specified directories.
#
# Usage:
#   ./build.sh
#
# Note: Ensure that the .NET SDK is installed and available in the system PATH.

# Build the project
dotnet build -c debug -bl:build.binlog
dotnet pack -c debug
dotnet pack -c release
dotnet build-server shutdown

# Run tests with code coverage
dotnet test test/coverlet.core.tests/coverlet.core.tests.csproj -c debug --no-build -bl:test.core.binlog /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" -- --results-directory "artifacts/reports" --report-xunit-trx --report-xunit-trx-filename "coverlet.core.tests.trx" --diagnostic --diagnostic-output-directory "artifacts/log/debug" --diagnostic-output-fileprefix "coverlet.core.tests"
dotnet build-server shutdown
dotnet test test/coverlet.core.coverage.tests/coverlet.core.coverage.tests.csproj -c debug --no-build -bl:test.core.coverage.binlog /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" -- --results-directory "artifacts/reports" --report-xunit-trx --report-xunit-trx-filename "coverlet.core.coverage.tests.trx" --diagnostic --diagnostic-output-directory "artifacts/log/debug" --diagnostic-output-fileprefix "coverlet.core.coverage.tests"
dotnet build-server shutdown
dotnet test test/coverlet.msbuild.tasks.tests/coverlet.msbuild.tasks.tests.csproj -c debug --no-build -bl:test.msbuild.binlog /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" -- --results-directory:"artifacts/reports" --report-xunit-trx --report-xunit-trx-filename "coverlet.msbuild.tasks.tests.trx" --diagnostic --diagnostic-output-directory "artifacts/log/debug" --diagnostic-output-fileprefix "coverlet.msbuild.tasks.tests"
dotnet build-server shutdown
dotnet test test/coverlet.collector.tests/coverlet.collector.tests.csproj -c debug --no-build -bl:test.collector.binlog /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*%2c[coverlet.tests.projectsample]*" /p:ExcludeByAttribute="GeneratedCodeAttribute" --diag:"artifacts/log/debug/coverlet.collector.test.diag.log;tracelevel=verbose"
dotnet build-server shutdown
dotnet test test/coverlet.integration.tests/coverlet.integration.tests.csproj -c debug --no-build -bl:test.integration.binlog -- --results-directory "artifacts/reports" --report-xunit-trx --report-xunit-trx-filename "coverlet.integration.tests.trx" --diagnostic --diagnostic-output-directory "artifacts/log/debug" --diagnostic-output-fileprefix "coverlet.integration.tests"
