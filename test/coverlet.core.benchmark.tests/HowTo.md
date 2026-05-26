# How to benchmark coverlet.core

Coverlet.core.benchmark uses [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) which has some runtime requirements:

- Build the project in `Release` mode
- Make sure you have the latest version of the .NET SDK installed
- Make sure you have the latest version of the BenchmarkDotNet package installed

## Running the benchmarks

Use a terminal and run the following commands:

```bash
dotnet build test/coverlet.core.benchmark.tests -c release
cd artifacts/bin/coverlet.core.benchmark.tests/release_net10.0
./coverlet.core.benchmark.tests.exe
```

> [!TIP]
> If an error occurs about a missing `TestAssets\System.Private.CoreLib.dll` or
> `TestAssets\System.Private.CoreLib.pdb`, copy the files from
> `artifacts\bin\coverlet.core.tests\debug\TestAssets`.

The benchmark run automatically creates reports in the `BenchmarkDotNet.Artifacts` folder:

```text
BenchmarkRun-20250411-083105.log
results\BenchmarkRun-joined-2025-04-11-08-38-13-report-github.md
results\BenchmarkRun-joined-2025-04-11-08-38-13-report.csv
results\BenchmarkRun-joined-2025-04-11-08-38-13-report.html
```

> [!NOTE]
> Run the benchmarks for every coverlet release to detect performance regressions early.
> Update *Documentation/BenchmarkHistory.md* from the repo root using `pwsh scripts/Update-BenchmarkHistory.ps1`

---

## Available benchmark classes

### `CoverageBenchmarks`

**File:** `CoverageBenchmarks.cs`

Measures the cost of `Coverage.GetCoverageResult()` in isolation.  The assembly is
instrumented once in `[GlobalSetup]` so only the result-collection phase is timed.

| Benchmark | Description |
|-----------|-------------|
| `GetCoverageBenchmark` | Calls `GetCoverageResult()` against the benchmark assembly itself. |

---

### `InstrumenterBenchmarks`

**File:** `InstrumenterBenchmarks.cs`

Measures end-to-end instrumentation of the `coverlet.benchmark.subject` assembly including
full service-provider construction on every iteration.  Useful as a coarse regression
canary across releases.

| Benchmark | Description |
|-----------|-------------|
| `InstrumenterBigClassBenchmark` | Full `PrepareModules` run including DI setup. |

---

### `CoverageWorkflowBenchmark`

**File:** `Simulator.cs`

Simulates the complete three-phase coverage workflow against a freshly built copy of
`coverlet.benchmark.subject`:

| Phase | Method | Description |
|-------|--------|-------------|
| 1 | `Phase1_InstrumentAssemblies` | Instruments the SUT assembly via `Coverage.PrepareModules`. |
| 2 | `Phase2_GenerateHits` | Executes the instrumented assembly to produce hit data. |
| 3 | `Phase3_ProcessResults` | Calls `GetCoverageResult` and generates a TeamCity report. |

| Benchmark | Description |
|-----------|-------------|
| `SimulateWorkflow` | Runs all three phases end-to-end. |

---

### `InstrumentationOptionsBenchmarks`

**File:** `InstrumentationOptionsBenchmarks.cs`

Parametrised benchmarks that measure `PrepareModules` duration and memory allocation for
every combination of the three most impactful `CoverageParameters` flags.  Runs all
2 × 2 × 2 = **8 combinations** in a single pass.

| Parameter | Values | Effect |
|-----------|--------|--------|
| `SingleHit` | `false`, `true` | Replaces `Interlocked.Increment` per line with a single boolean write when `true`. |
| `SkipAutoProps` | `false`, `true` | Skips instrumentation of compiler-generated auto-property accessors when `true`. |
| `IncludeTestAssembly` | `false`, `true` | Also instruments the test host assembly when `true`, roughly doubling work. |

| Benchmark | Description |
|-----------|-------------|
| `Instrumentation - PrepareModules` | `Coverage.PrepareModules` under each parameter combination. |

---

### `ReportFormatBenchmarks`

**File:** `InstrumentationOptionsBenchmarks.cs`

Measures the full `GetCoverageResult()` + report serialisation cost for each supported
output format.  The assembly is instrumented once in `[GlobalSetup]`; only the
report-generation phase is timed per iteration.

| Parameter | Values |
|-----------|--------|
| `ReportFormat` | `json`, `lcov`, `opencover`, `cobertura`, `teamcity` |

| Benchmark | Description |
|-----------|-------------|
| `GetCoverageResult + Report` | Result collection and serialisation for the selected format. |

---

## Recording results in BenchmarkHistory.md

After each benchmark run, append the results to [`BenchmarkHistory.md`](../../BenchmarkHistory.md)
using the automation script at `scripts/Update-BenchmarkHistory.ps1`.

### Script parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `-ArtifactsRoot` | No | Current directory | Folder that contains the `BenchmarkDotNet.Artifacts` sub-folder. |
| `-HistoryFile` | No | `Documentation/BenchmarkHistory.md` | Path to the accumulated results Markdown file. |
| `-CoverletVersion` | No | Read from `coverlet.core.csproj` | Version string to record, e.g. `"6.0.5"`. |
| `-BenchmarkFilter` | No | *(all rows)* | Case-insensitive substring filter on the Method column. |

### Usage examples

```powershell
# Record all benchmark results for a specific version
pwsh scripts/Update-BenchmarkHistory.ps1 `
    -ArtifactsRoot "artifacts/bin/coverlet.core.benchmark.tests/release_net10.0" `
    -CoverletVersion "6.0.5"

# Record only InstrumentationOptionsBenchmarks rows
pwsh scripts/Update-BenchmarkHistory.ps1 `
    -ArtifactsRoot "artifacts/bin/coverlet.core.benchmark.tests/release_net10.0" `
    -CoverletVersion "6.0.5" `
    -BenchmarkFilter "InstrumentationOptions"

# Override the output file location
pwsh scripts/Update-BenchmarkHistory.ps1 `
    -ArtifactsRoot "artifacts/bin/coverlet.core.benchmark.tests/release_net10.0" `
    -HistoryFile   "docs/BenchmarkHistory.md" `
    -CoverletVersion "6.0.5"
```

The script:
- Finds the newest `*-report-github.md` file in `BenchmarkDotNet.Artifacts/results`
- Normalises all BenchmarkDotNet duration units (ns / μs / ms / s) to **milliseconds**
- Normalises allocated bytes to **MB**
- Auto-detects any `[Params]` columns and records them in the **Options** column
- Creates `BenchmarkHistory.md` with the correct table header if the file does not yet exist
- Appends one row per benchmark entry: `Date | Version | Runtime | BenchmarkClass | Method | Options | Mean (ms) | Max (ms) | Allocated (MB)`

---

## Performance improvement proposals

See [`PerformanceImprovementProposal.md`](PerformanceImprovementProposal.md) for a
prioritised list of concrete optimisations with code sketches and the benchmark that can
verify each gain.

---

## Additional information

- [BenchmarkDotNet](https://benchmarkdotnet.org)
- [Analyze BenchmarkDotNet data in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/profiling/profiling-with-benchmark-dotnet)
- [.NET benchmarking and profiling for beginners](https://medium.com/ingeniouslysimple/net-benchmarking-and-profiling-for-beginners-62462e1e9a19)

---

## Historical results

<blockquote>

## Coverlet 6.0.0

```text
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26120.3671)
AMD Ryzen 7 Microsoft Surface Edition, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.203
  [Host] : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2

Job=ShortRun  Toolchain=InProcessNoEmitToolchain  IterationCount=3
LaunchCount=1  WarmupCount=3

```

| Type                   | Method                | Mean                | Error              | StdDev            | Gen0        | Gen1        | Gen2      | Allocated    |
|----------------------- |---------------------- |--------------------:|-------------------:|------------------:|------------:|------------:|----------:|-------------:|
| CoverageBenchmarks     | GetCoverageBenchmark  |            46.42 ns |           1.670 ns |          0.092 ns |      0.0612 |           - |         - |        128 B |
| InstrumenterBenchmarks | InstrumenterBenchmark | 4,938,713,766.67 ns | 767,760,955.034 ns | 42,083,568.809 ns | 857000.0000 | 109000.0000 | 2000.0000 | 2879633880 B |

## Coverlet 6.0.1

```text
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26120.3671)
AMD Ryzen 7 Microsoft Surface Edition, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.408
  [Host] : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2

Job=ShortRun  Toolchain=InProcessNoEmitToolchain  IterationCount=3
LaunchCount=1  WarmupCount=3

```

| Type                   | Method                | Mean                | Error              | StdDev            | Gen0        | Gen1       | Gen2      | Allocated    |
|----------------------- |---------------------- |--------------------:|-------------------:|------------------:|------------:|-----------:|----------:|-------------:|
| CoverageBenchmarks     | GetCoverageBenchmark  |            48.14 ns |           8.681 ns |          0.476 ns |      0.0612 |          - |         - |        128 B |
| InstrumenterBenchmarks | InstrumenterBenchmark | 3,675,771,933.33 ns | 874,256,026.013 ns | 47,920,923.025 ns | 789000.0000 | 97000.0000 | 2000.0000 | 2864466608 B |

## Coverlet 6.0.2

```text
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26120.3671)
AMD Ryzen 7 Microsoft Surface Edition, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.408
  [Host] : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2

Job=ShortRun  Toolchain=InProcessNoEmitToolchain  IterationCount=3
LaunchCount=1  WarmupCount=3

```

| Type                   | Method                | Mean                 | Error               | StdDev             | Gen0        | Gen1        | Gen2      | Allocated    |
|----------------------- |---------------------- |---------------------:|--------------------:|-------------------:|------------:|------------:|----------:|-------------:|
| CoverageBenchmarks     | GetCoverageBenchmark  |             46.20 ns |            12.15 ns |           0.666 ns |      0.0612 |           - |         - |        128 B |
| InstrumenterBenchmarks | InstrumenterBenchmark | 19,105,224,033.33 ns | 4,450,103,671.99 ns | 243,925,199.451 ns | 867000.0000 | 130000.0000 | 2000.0000 | 3170097400 B |

## Coverlet 6.0.3

```text
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26120.3671)
AMD Ryzen 7 Microsoft Surface Edition, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.408
  [Host] : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2

Job=ShortRun  Toolchain=InProcessNoEmitToolchain  IterationCount=3
LaunchCount=1  WarmupCount=3

```

| Type                   | Method                | Mean                | Error              | StdDev            | Gen0        | Gen1       | Gen2      | Allocated    |
|----------------------- |---------------------- |--------------------:|-------------------:|------------------:|------------:|-----------:|----------:|-------------:|
| CoverageBenchmarks     | GetCoverageBenchmark  |            47.32 ns |           4.246 ns |          0.233 ns |      0.0612 |          - |         - |        128 B |
| InstrumenterBenchmarks | InstrumenterBenchmark | 3,620,665,600.00 ns | 580,611,812.738 ns | 31,825,292.772 ns | 775000.0000 | 91000.0000 | 2000.0000 | 2798558288 B |

## Coverlet 6.0.4

```text
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26120.3671)
AMD Ryzen 7 Microsoft Surface Edition, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.408
  [Host] : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2

Job=ShortRun  Toolchain=InProcessNoEmitToolchain  IterationCount=3
LaunchCount=1  WarmupCount=3

```

| Type                   | Method                | Mean                | Error              | StdDev            | Gen0        | Gen1       | Gen2      | Allocated    |
|----------------------- |---------------------- |--------------------:|-------------------:|------------------:|------------:|-----------:|----------:|-------------:|
| CoverageBenchmarks     | GetCoverageBenchmark  |            43.59 ns |           9.890 ns |          0.542 ns |      0.0612 |          - |         - |        128 B |
| InstrumenterBenchmarks | InstrumenterBenchmark | 3,594,263,533.33 ns | 193,126,202.387 ns | 10,585,898.871 ns | 776000.0000 | 97000.0000 | 2000.0000 | 2798557560 B |

</blockquote>
