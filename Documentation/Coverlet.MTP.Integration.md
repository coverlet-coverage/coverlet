# Coverlet Microsoft Testing Platform Integration

[Microsoft.Testing.Platform and Microsoft Test Framework](https://github.com/microsoft/testfx) is a lightweight alternative for VSTest.

More information is available here:

- [Microsoft.Testing.Platform overview](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro?tabs=dotnetcli)
- [Microsoft.Testing.Platform extensibility](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-architecture-extensions)

coverlet.MTP implements coverlet.collector functionality for Microsoft.Testing.Platform.

## Supported Runtime Versions

Since version `8.0.0`:

- .NET Core >= 8.0

## Quick Start

### Installation

Add the `coverlet.MTP` package to your test project:

```bash
dotnet add package coverlet.MTP
```

ToDo: Usage details

A sample project file looks like:

```xml
<Project Sdk="Microsoft.NET.Sdk">
   <PropertyGroup>
      <TargetFramework>net8.0</TargetFramework>
      <IsPackable>false</IsPackable>
      <IsTestProject>true</IsTestProject>
      <OutputType>Exe</OutputType>
      <!-- Enable Microsoft Testing Platform - not required for .NET 10 and later -->
      <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
      <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
   </PropertyGroup>
      <ItemGroup>
      <!-- Use xunit.v3.mtp-v2 for MTP v2.x compatibility -->
      <PackageReference Include="xunit.v3.mtp-v2" Version="3.2.1" />
      <PackageReference Include="Microsoft.Testing.Platform" Version="2.0.2" />
      <PackageReference Include="coverlet.MTP" Version="8.0.0" />
   </ItemGroup>
</Project>
```

### Basic Usage

To collect code coverage, run your test executable with the `--coverlet` flag:

```bash
dotnet exec <test-assembly.dll> --coverlet
```

Or using `dotnet test` with MTP enabled projects:

```bash
dotnet test --coverlet
```

After the test run, a `coverage.json` file containing the results will be generated in the current directory.

## Command Line Options

The `coverlet.MTP` extension provides the following command line options. To see all available options, run:

```bash
dotnet exec <test-assembly.dll> --help
```

### Coverage Options

| Option | Description |
|:-------|:------------|
| `--coverlet` | Enable code coverage data collection. |
| `--coverlet-output-format <format>` | Output format(s) for coverage report. Supported formats: `json`, `lcov`, `opencover`, `cobertura`, `teamcity`. Can be specified multiple times. |
| `--coverlet-output <path>` | Output path for coverage files. |
| `--coverlet-include <filter>` | Include assemblies matching filters (e.g., `[Assembly]Type`). Can be specified multiple times. |
| `--coverlet-include-directory <path>` | Include additional directories for instrumentation. Can be specified multiple times. |
| `--coverlet-exclude <filter>` | Exclude assemblies matching filters (e.g., `[Assembly]Type`). Can be specified multiple times. |
| `--coverlet-exclude-by-file <pattern>` | Exclude source files matching glob patterns. Can be specified multiple times. |
| `--coverlet-exclude-by-attribute <attribute>` | Exclude methods/classes decorated with attributes. Can be specified multiple times. |
| `--coverlet-include-test-assembly` | Include test assembly in coverage. |
| `--coverlet-single-hit` | Limit the number of hits to one for each location. |
| `--coverlet-skip-auto-props` | Skip auto-implemented properties. |
| `--coverlet-does-not-return-attribute <attribute>` | Attributes that mark methods as not returning. Can be specified multiple times. |
| `--coverlet-exclude-assemblies-without-sources <value>` | Exclude assemblies without source code. Values: `MissingAll`, `MissingAny`, `None`. |
| `--coverlet-source-mapping-file <path>` | Path to a SourceRootsMappings file. |

### Examples

**Generate coverage in JSON format (default):**

```bash
dotnet exec TestProject.dll --coverlet
```

**Generate coverage in Cobertura format:**

```bash
dotnet exec TestProject.dll --coverlet --coverlet-output-format cobertura
```

**Generate coverage in multiple formats:**

```bash
dotnet run TestProject.dll --coverlet --coverlet-output-format json --coverlet-output-format cobertura --coverlet-output-format lcov
```

**Specify output directory:**

```bash
dotnet exec TestProject.dll --coverlet --coverlet-output ./coverage/
```

**Include only specific assemblies:**

```bash
dotnet exec TestProject.dll --coverlet --coverlet-include "[MyApp.]"
```

**Exclude test assemblies and specific namespaces:**

```bash
dotnet exec TestProject.dll --coverlet --coverlet-exclude "[.Tests]" --coverlet-exclude "[]MyApp.Generated."
```

**Exclude by attribute:**

```bash
dotnet exec TestProject.dll --coverlet --coverlet-exclude-by-attribute "Obsolete" --coverlet-exclude-by-attribute "GeneratedCode"
```

## Coverage Output

Coverlet can generate coverage results in multiple formats:

- `json` (default) - Coverlet's native JSON format
- `lcov` - LCOV format
- `opencover` - OpenCover XML format
- `cobertura` - Cobertura XML format
- `teamcity` - TeamCity service messages

By default, coverage files are written to the current working directory. Use `--coverlet-output` to specify a different location.

## Filter Expressions

Filter expressions allow fine-grained control over what gets included or excluded from coverage.

**Syntax:** `[Assembly-Filter]Type-Filter`

**Wildcards:**

- `*` matches zero or more characters
- `?` makes the prefixed character optional

**Examples:**

- `[*]*` - All types in all assemblies
- `[coverlet.*]Coverlet.Core.Coverage` - Specific class in matching assemblies
- `[*]Coverlet.Core.Instrumentation.*` - All types in a namespace
- `[coverlet.*.tests?]*` - Assemblies ending with `.test` or `.tests`

Both `--coverlet-include` and `--coverlet-exclude` can be used together, but `--coverlet-exclude` takes precedence.

## Excluding From Coverage

### Using Attributes

Apply the `ExcludeFromCodeCoverage` attribute from `System.Diagnostics.CodeAnalysis` to exclude methods or classes:

```csharp
[ExcludeFromCodeCoverage] public class NotCovered { // This class will be excluded from coverage }
```

Additional attributes can be specified using `--coverlet-exclude-by-attribute`.

### Using Source File Patterns

Exclude source files using glob patterns with `--coverlet-exclude-by-file`:

```bash
dotnet exec TestProject.dll --coverlet --coverlet-exclude-by-file "**/Generated/*.cs"
```

## How It Works

The `coverlet.MTP` extension integrates with the Microsoft Testing Platform using the extensibility model:

1. **Test Host Controller Extension**: Implements `ITestHostProcessLifetimeHandler` to instrument assemblies before tests run and collect coverage after tests complete.

2. **Before Tests Run**:
   - Locates the test assembly and referenced assemblies with PDBs
   - Instruments assemblies by inserting code to record sequence point hits

3. **After Tests Run**:
   - Restores original non-instrumented assemblies
   - Reads recorded hit information
   - Generates coverage report in the specified format(s)

## Comparison with coverlet.collector (VSTest)

| Feature | coverlet.MTP | coverlet.collector |
|:--------|:-------------|:-------------------|
| Test Platform | Microsoft Testing Platform | VSTest |
| Configuration | Command line options | runsettings file |
| Output Location | Configurable via `--coverlet-output` | TestResults folder |
| Default Format | JSON | Cobertura |

## Known Limitations

- Threshold validation is not yet supported (planned for future releases)
- Report merging is not yet supported (use external tools like `dotnet-coverage` or `reportgenerator`)

> [!TIP]
> **Merging coverage files from multiple test runs:**
>
> Use `dotnet-coverage` tool:
> ```bash
> dotnet-coverage merge coverage/**/coverage.cobertura.xml -f cobertura -o coverage/merged.xml
> ```
>
> Or use `reportgenerator`:
> ```bash
> reportgenerator -reports:"**/*.cobertura.xml" -targetdir:"coverage/report" -reporttypes:"HtmlInline_AzurePipelines_Dark;Cobertura"
> ```

## Troubleshooting

### Enable Diagnostic Output

Use the MTP diagnostic options to get detailed logs:

```bash
dotnet exec TestProject.dll --coverlet --diagnostic --diagnostic-verbosity trace --diagnostic-output-directory ./logs
```

### Debug Coverlet Extension

Set the environment variable to attach a debugger:

```bash
set COVERLET_MTP_DEBUG=1 dotnet exec TestProject.dll --coverlet
```

## Requirements

- .NET 8.0 SDK or newer
- Microsoft.Testing.Platform 2.0.0 or newer
- Test framework with MTP support (e.g., xUnit v3 (xunit.v3.mtp-v2), MSTest v3, NUnit with MTP adapter)

## Related Documentation

- [VSTest Integration](VSTestIntegration.md) - For VSTest-based projects using `coverlet.collector`
- [MSBuild Integration](MSBuildIntegration.md) - For MSBuild-based coverage collection
- [Global Tool](GlobalTool.md) - For standalone coverage collection
- [Known Issues](KnownIssues.md) - Common issues and workarounds

