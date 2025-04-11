# How to benchmark coverlet.core

Coverlet.core.benchmark uses [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) which has some runtime requirements

- Build the project in `Release` mode
- Make sure you have the latest version of the .NET SDK installed
- Make sure you have the latest version of the BenchmarkDotNet package installed

Use a terminal and navigate to the `artifacts\bin\coverlet.core.benchmark.tests\release` directory. Then run the following command:

```bash
coverlet.core.benchmark.tests.exe
```
> [!TIP]
> If error occurred missing `TestAssets\System.Private.CoreLib.dll` or `TestAssets\System.Private.CoreLib.pdb`.
> Just copy the files from `artifacts\bin\coverlet.msbuild.tasks.tests\debug\TestAssets`.

The benchmark will automatically create reports in folder `BenchmarkDotNet.Artifacts` eg. find these files:

```
BenchmarkRun-20250411-083105.log
results\BenchmarkRun-joined-2025-04-11-08-38-13-report-github.md
results\BenchmarkRun-joined-2025-04-11-08-38-13-report.csv
results\BenchmarkRun-joined-2025-04-11-08-38-13-report.html
results\BenchmarkRun-joined-2025-04-11-08-55-34-report-github.md
results\BenchmarkRun-joined-2025-04-11-08-55-34-report.csv
results\BenchmarkRun-joined-2025-04-11-08-55-34-report.html
```

A typical report looks like this:

<blockquote>

```txt
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26120.3671)
AMD Ryzen 7 Microsoft Surface Edition, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.408
  [Host] : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2

Job=ShortRun  Toolchain=InProcessNoEmitToolchain  IterationCount=3
LaunchCount=1  WarmupCount=3
```

| Type                   | Method                | Mean                | Error               | StdDev            | Gen0        | Gen1       | Gen2      | Allocated    |
|----------------------- |---------------------- |--------------------:|--------------------:|------------------:|------------:|-----------:|----------:|-------------:|
| CoverageBenchmarks     | GetCoverageBenchmark  |            45.61 ns |            19.55 ns |          1.072 ns |      0.0612 |          - |         - |        128 B |
| InstrumenterBenchmarks | InstrumenterBenchmark | 3,845,759,400.00 ns | 1,029,418,568.21 ns | 56,425,905.569 ns | 777000.0000 | 94000.0000 | 2000.0000 | 2798567144 B |
</blockquote>

> [!NOTE]
> This should be done for every coverlet release to avoid performance degradations.
> ToDo: Create a table with the results of the last 5 releases.

