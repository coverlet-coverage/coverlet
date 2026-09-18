# Codebelt.Coverlet.MTP

[![CI](https://github.com/codebeltnet/coverlet/actions/workflows/ci-pipeline.yml/badge.svg)](https://github.com/codebeltnet/coverlet/actions/workflows/ci-pipeline.yml) [![NuGet](https://img.shields.io/nuget/v/Codebelt.Coverlet.MTP.svg)](https://www.nuget.org/packages/Codebelt.Coverlet.MTP/) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Cross-platform code coverage for .NET with explicit TFM support and framework-aligned dependencies.

`Codebelt.Coverlet.MTP` is a deliberately narrow Codebelt-maintained fork of [Coverlet](https://github.com/coverlet-coverage/coverlet) for Microsoft Testing Platform integration. Coverlet remains the upstream project; this repository keeps the Coverlet engine and MTP integration recognizable while focusing package identity, target frameworks, dependency groups, and CI/CD on the Codebelt-maintained MTP package.

## Scope

This repository produces one public NuGet package: `Codebelt.Coverlet.MTP`. It preserves the implementation assembly name and namespace used by the MTP extension (`coverlet.MTP.dll`, `Coverlet.MTP`, and `Coverlet.MTP.TestingPlatformBuilderHook`) so existing Microsoft Testing Platform discovery remains compatible.

Retained production projects:

| Project | Purpose |
| --- | --- |
| `src/coverlet.MTP` | Microsoft Testing Platform extension and package entry point. |
| `src/coverlet.core` | Coverlet instrumentation engine, reporters, and coverage model. |
| `src/coverlet.template` | `netstandard2.0` tracker template embedded by `coverlet.core` for instrumentation compatibility. |

The package supports:

```text
netstandard2.0
net9.0
net10.0
```

`netstandard2.0` is a package compatibility target. Tests execute on `net9.0` and `net10.0`.

## Installation

Add the package to an MTP-enabled test project:

```bash
dotnet add package Codebelt.Coverlet.MTP
```

Example test project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3.mtp-v2" Version="4.0.1" />
    <PackageReference Include="Microsoft.Testing.Platform" Version="2.4.1" />
    <PackageReference Include="Codebelt.Coverlet.MTP" Version="*" />
  </ItemGroup>
</Project>
```

## Usage

Run the MTP test project with the normal Coverlet MTP argument:

```bash
dotnet test --coverlet
```

or execute the built test assembly directly:

```bash
dotnet exec <test-assembly.dll> --coverlet
```

Coverage reports are written to the test results/output location. By default the MTP integration emits `json` and `cobertura` reports.

Common options:

| Option | Description |
| --- | --- |
| `--coverlet` | Enables coverage collection. |
| `--coverlet-output-format <format>` | Adds report formats such as `json`, `cobertura`, `lcov`, `opencover`, or `teamcity`. |
| `--coverlet-include <filter>` | Includes assemblies/types matching a Coverlet filter. |
| `--coverlet-exclude <filter>` | Excludes assemblies/types matching a Coverlet filter. |
| `--coverlet-exclude-by-file <pattern>` | Excludes source files matching a glob pattern. |
| `--coverlet-exclude-by-attribute <attribute>` | Excludes code marked with the specified attribute. |
| `--coverlet-include-test-assembly` | Attempts to include the test assembly where the MTP execution model allows it. |

See [Coverlet.MTP.Integration.md](Documentation/Coverlet.MTP.Integration.md) for the full MTP option reference and configuration-file behavior.

## Upstream relationship

This fork is based on [coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet). The intent is not to replace Coverlet or rewrite its instrumentation internals; the fork exists to provide explicit target-framework support and framework-aligned NuGet dependency groups for the Microsoft Testing Platform package.

For Coverlet drivers outside this repository's scope, such as the console tool, VSTest collector, or MSBuild integration, use the upstream Coverlet packages and documentation.

## License and notices

Coverlet is licensed under the MIT license. This fork preserves the upstream [MIT license](LICENSE) and [third-party notices](THIRD-PARTY-NOTICES.txt).
