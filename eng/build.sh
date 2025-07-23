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
dotnet test test/coverlet.collector.tests /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*" --results-directory:"./artifacts/reports" --diag:"artifacts/log/debug/coverlet.collector.test.log;tracelevel=verbose"
dotnet build-server shutdown
dotnet test test/coverlet.core.tests /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*" --results-directory:"./artifacts/reports" --verbosity detailed --diag ./artifacts/log/debug/coverlet.core.tests.log
dotnet build-server shutdown
dotnet test test/coverlet.core.coverage.tests /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*" -- --results-directory "$(pwd)/artifacts/reports" --report-xunit-trx --report-xunit-trx-filename "coverlet.core.coverage.tests.trx" --diagnostic-verbosity debug  --diagnostic --diagnostic-output-directory "$(pwd)/artifacts/log/debug"
dotnet build-server shutdown
dotnet test test/coverlet.msbuild.tasks.tests /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[coverlet.core.tests.samples.netstandard]*" --results-directory:"./artifacts/reports" --verbosity detailed --diag ./artifacts/log/debug/coverlet.msbuild.tasks.tests.log
dotnet build-server shutdown
dotnet test test/coverlet.integration.tests -f net8.0 --results-directory:"./artifacts/reports" --verbosity detailed --diag ./artifacts/log/debug/coverlet.integration.tests.net8.log
dotnet test test/coverlet.integration.tests -f net9.0 --results-directory:"./artifacts/reports" --verbosity detailed --diag ./artifacts/log/debug/coverlet.integration.tests.net9.log
