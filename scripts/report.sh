#!/bin/bash
set -e

# Get the workspace root directory
WORKSPACE_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# Get the workspace root directory (parent of the script's directory)
WORKSPACE_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$WORKSPACE_ROOT"

# Arguments
#  -reports:D:\a\1\s\test\**\*.opencover.xml
#   -targetdir:D:\a\1\s\artifacts\CoverageReport
#   -reporttypes:HtmlInline_AzurePipelines_Dark;Cobertura
#   -assemblyfilters:-xunit;-coverlet.testsubject;-Coverlet.Tests.ProjectSample.*;-coverlet.core.tests.samples.netstandard;-coverletsamplelib.integration.template;-coverlet.tests.utils
#   -classfilters:
#   -verbosity:Verbose
#   --minimumCoverageThresholds:lineCoverage=90

dotnet tool restore --add-source https://api.nuget.org/v3/index.json
dotnet tool list
dotnet reportgenerator -reports:"$WORKSPACE_ROOT/test/**/*.cobertura.xml" -targetdir:"$WORKSPACE_ROOT/artifacts/CoverageReport" -reporttypes:"HtmlInline_AzurePipelines;Cobertura;Markdown" -assemblyfilters:"-xunit;-coverlet.testsubject;-Coverlet.Tests.ProjectSample.*;-coverlet.core.tests.samples.netstandard;-coverletsamplelib.integration.template;-coverlet.tests.utils;-coverletsample.integration.determisticbuild" -verbosity:Verbose
