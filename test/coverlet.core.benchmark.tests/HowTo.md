# How to benchmark coverlet.core

Coverlet.core.benchmark uses [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) which has some runtime requirements

- Build the project in `Release` mode
- Make sure you have the latest version of the .NET SDK installed
- Make sure you have the latest version of the BenchmarkDotNet package installed

Use a terminal and run the following commands:

```bash
dotnet build test/coverlet.core.benchmark.tests -c release
cd artifacts/bin/coverlet.core.benchmark.tests/release
./coverlet.core.benchmark.tests.exe
```

> [!TIP]
> If error occurred missing `TestAssets\System.Private.CoreLib.dll` or `TestAssets\System.Private.CoreLib.pdb`.
> Just copy the files from `artifacts\bin\coverlet.core.tests\debug\TestAssets`.

The benchmark will automatically create reports in folder `BenchmarkDotNet.Artifacts` eg. find these files:

```text
BenchmarkRun-20250411-083105.log
results\BenchmarkRun-joined-2025-04-11-08-38-13-report-github.md
results\BenchmarkRun-joined-2025-04-11-08-38-13-report.csv
results\BenchmarkRun-joined-2025-04-11-08-38-13-report.html
results\BenchmarkRun-joined-2025-04-11-08-55-34-report-github.md
results\BenchmarkRun-joined-2025-04-11-08-55-34-report.csv
results\BenchmarkRun-joined-2025-04-11-08-55-34-report.html
```

> [!NOTE]
> This should be done for every coverlet release to avoid performance degradations.

## Additional information

- [BenchmarkDotNet](https://benchmarkdotnet.org)
- [Analyze BenchmarkDotNet data in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/profiling/profiling-with-benchmark-dotnet)
- [.NET benchmarking and profiling for beginners](https://medium.com/ingeniouslysimple/net-benchmarking-and-profiling-for-beginners-62462e1e9a19)

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
