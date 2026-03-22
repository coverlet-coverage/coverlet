#!/usr/bin/env pwsh
# report.ps1 - Helper script to generate coverage reports for the Coverlet project.
#
# Usage:
#   ./report.ps1
#
# Arguments passed to reportgenerator:
#   -reports: test/**/*.cobertura.xml
#   -targetdir: artifacts/CoverageReport
#   -reporttypes: HtmlInline_AzurePipelines;Cobertura;Markdown
#   -assemblyfilters: -xunit;-coverlet.testsubject;-Coverlet.Tests.ProjectSample.*;...
#   -verbosity: Verbose

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Get the workspace root directory (parent of the script's directory)
$WorkspaceRoot = Split-Path -Parent $PSScriptRoot
Set-Location $WorkspaceRoot

dotnet tool restore --add-source https://api.nuget.org/v3/index.json
dotnet tool list
dotnet reportgenerator "-reports:$WorkspaceRoot/test/**/*.cobertura.xml" "-targetdir:$WorkspaceRoot/artifacts/CoverageReport" '-reporttypes:HtmlInline_AzurePipelines;Cobertura;Markdown' '-assemblyfilters:-xunit;-coverlet.testsubject;-Coverlet.Tests.ProjectSample.*;-coverlet.core.tests.samples.netstandard;-coverletsamplelib.integration.template;-coverlet.tests.utils;-coverletsample.integration.determisticbuild' -verbosity:Verbose
