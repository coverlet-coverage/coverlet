# Contributing

Contributions are welcome. This repository is a deliberately narrow Codebelt-maintained Microsoft Testing Platform variant of Coverlet, so changes should stay close to upstream Coverlet internals unless the fork objective requires a different package, framework, dependency, or release shape.

## Requirements

Use the .NET SDK pinned by `global.json`. The retained production matrix is `netstandard2.0`, `net9.0`, and `net10.0`; tests execute on `net9.0` and `net10.0`.

## Building, testing, and packing

Clone this repository:

```bash
git clone https://github.com/codebeltnet/coverlet.git
cd coverlet
```

Use the standard .NET commands:

```bash
dotnet restore
dotnet build -c Release --no-restore
dotnet pack src/coverlet.MTP/coverlet.MTP.csproj -c Release --no-build
dotnet test -c Release --no-build
```

The MTP package-validation tests consume the locally packed `Codebelt.Coverlet.MTP` package from `artifacts/package/release`, so pack before running `test/coverlet.MTP.validation.tests` directly.

## Focused test runs

For this repository's xUnit v3 Microsoft Testing Platform test apps, use xUnit-specific filters such as `--filter-method`, `--filter-class`, or `--filter-query`; VSTest-style `--filter` is unsupported.

Examples:

```bash
dotnet test test/coverlet.MTP.tests/coverlet.MTP.tests.csproj -c Release -f net10.0 -- --filter-class Coverlet.MTP.Collector.Tests.CollectorExtensionTests
dotnet test test/coverlet.core.tests/coverlet.core.tests.csproj -c Release -f net9.0 -- --filter-method PatternMatchingOr_Should_Report_100_Percent_Branch_Coverage
```

## Debugging isolated coverage tests

`test/coverlet.core.coverage.tests` uses an isolated child-process path for instrumentation tests that require full process separation. Set `COVERLET_DEBUG_CHILD_PROCESS=1` before running a focused test if you need the child process to wait for debugging.

```pwsh
$env:COVERLET_DEBUG_CHILD_PROCESS = "1"
dotnet test test/coverlet.core.coverage.tests/coverlet.core.coverage.tests.csproj -c Release -f net10.0 -- --filter-query "/[method]=Coverage_SkippedInstrumentedMethod"
$env:COVERLET_DEBUG_CHILD_PROCESS = $null
```
