# Coverlet Benchmark History

This file is maintained automatically by `scripts/Update-BenchmarkHistory.ps1`.
Do not edit the table rows by hand – re-run the script after each benchmark run.

See [`test/coverlet.core.benchmark.tests/HowTo.md`](../test/coverlet.core.benchmark.tests/HowTo.md)
for instructions on how to run the benchmarks.

See [`test/coverlet.core.benchmark.tests/PerformanceImprovementProposal.md`](../test/coverlet.core.benchmark.tests/PerformanceImprovementProposal.md)
for the performance improvement roadmap.

---

## How to update this file

```powershell
# After building and running benchmarks:
pwsh scripts/Update-BenchmarkHistory.ps1 `
    -ArtifactsRoot "artifacts/bin/coverlet.core.benchmark.tests/release_net10.0" `
    -CoverletVersion "10.0.2"
```

---

## Results

> **Options column** shows only user-configured `[Params]` values (e.g. `SingleHit=True`).
> BDN infrastructure columns (Job, Toolchain, WarmupCount, Lock Contentions, Exceptions, etc.) are excluded by the script.
> Rows where BDN could not measure (out-of-process jobs, benchmark exceptions) are automatically skipped.

| Date | Version | Runtime | BenchmarkClass | Method | Options | Mean (ms) | Max (ms) | Allocated (MB) |
| ------ | --------- | --------- | ---------------- | -------- | --------- | ----------: | ---------: | ---------------: |
| 2025-01-01 | 6.0.0 | .NET 6.0.36, X64 RyuJIT AVX2 | CoverageBenchmarks | GetCoverageBenchmark | | 0.000 | 0.000 | 0.0001 |
| 2025-01-01 | 6.0.0 | .NET 6.0.36, X64 RyuJIT AVX2 | InstrumenterBenchmarks | InstrumenterBigClassBenchmark | | 4938.714 | 5706.474 | 2745.291 |
| 2025-01-01 | 6.0.1 | .NET 8.0.15, X64 RyuJIT AVX2 | CoverageBenchmarks | GetCoverageBenchmark | | 0.000 | 0.000 | 0.0001 |
| 2025-01-01 | 6.0.1 | .NET 8.0.15, X64 RyuJIT AVX2 | InstrumenterBenchmarks | InstrumenterBigClassBenchmark | | 3675.772 | 4550.028 | 2732.512 |
| 2025-01-01 | 6.0.2 | .NET 8.0.15, X64 RyuJIT AVX2 | CoverageBenchmarks | GetCoverageBenchmark | | 0.000 | 0.000 | 0.0001 |
| 2025-01-01 | 6.0.2 | .NET 8.0.15, X64 RyuJIT AVX2 | InstrumenterBenchmarks | InstrumenterBigClassBenchmark | | 19105.224 | 23555.328 | 3023.829 |
| 2025-01-01 | 6.0.3 | .NET 8.0.15, X64 RyuJIT AVX2 | CoverageBenchmarks | GetCoverageBenchmark | | 0.000 | 0.000 | 0.0001 |
| 2025-01-01 | 6.0.3 | .NET 8.0.15, X64 RyuJIT AVX2 | InstrumenterBenchmarks | InstrumenterBigClassBenchmark | | 3620.666 | 4201.277 | 2668.645 |
| 2025-01-01 | 6.0.4 | .NET 8.0.15, X64 RyuJIT AVX2 | CoverageBenchmarks | GetCoverageBenchmark | | 0.000 | 0.000 | 0.0001 |
| 2025-01-01 | 6.0.4 | .NET 8.0.15, X64 RyuJIT AVX2 | InstrumenterBenchmarks | InstrumenterBigClassBenchmark | | 3594.264 | 3787.390 | 2668.644 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | CoverageBenchmarks | GetCoverageBenchmark |  | 0.000 | 0.000 | 0.0001 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | CoverageWorkflowBenchmark | 'Simulate Workflow' |  | 476.285 | 478.140 | 47.5729 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumenterBenchmarks | InstrumenterBigClassBenchmark |  | 168.756 | 169.609 | 44.6814 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=False, SkipAutoProps=False | 168.749 | 169.379 | 44.6826 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=False, SkipAutoProps=False | 167.956 | 168.744 | 44.6821 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=False, SkipAutoProps=True | 168.890 | 169.583 | 44.6824 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=False, SkipAutoProps=True | 168.642 | 169.347 | 44.6824 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=True, SkipAutoProps=False | 167.901 | 168.607 | 44.6824 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=True, SkipAutoProps=False | 169.350 | 170.012 | 44.6824 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=True, SkipAutoProps=True | 169.348 | 170.052 | 44.6812 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=True, SkipAutoProps=True | 169.326 | 170.140 | 44.6824 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=cobertura | 0.003 | 0.003 | 0.0182 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=json | 0.002 | 0.002 | 0.0006 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=lcov | 0.000 | 0.000 | 0.0004 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=opencover | 0.004 | 0.004 | 0.0219 |
| 2026-05-25 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=teamcity | 0.001 | 0.001 | 0.0030 |
