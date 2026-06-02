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
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | CoverageBenchmarks | GetCoverageBenchmark |  | 0.000 | 0.000 | 0.0001 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | CoverageWorkflowBenchmark | 'Simulate Workflow' |  | 301.211 | 303.043 | 32.7153 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumenterBenchmarks | InstrumenterBenchmark |  | 163.011 | 164.322 | 31.4229 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=False, SkipAutoProps=False | 165.156 | 166.427 | 35.0818 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=False, SkipAutoProps=False | 163.051 | 164.120 | 35.0816 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=False, SkipAutoProps=True | 161.334 | 162.224 | 31.4235 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=False, SkipAutoProps=True | 159.776 | 161.314 | 31.4235 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=True, SkipAutoProps=False | 162.572 | 164.107 | 35.0816 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=True, SkipAutoProps=False | 161.109 | 162.177 | 35.0816 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=True, SkipAutoProps=True | 157.373 | 158.005 | 31.4235 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=True, SkipAutoProps=True | 160.595 | 161.934 | 31.4235 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=cobertura | 0.004 | 0.004 | 0.0182 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=json | 0.002 | 0.002 | 0.0006 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=lcov | 0.000 | 0.000 | 0.0004 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=opencover | 0.004 | 0.004 | 0.0219 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=teamcity | 0.001 | 0.001 | 0.0030 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | AutoPropsBenchmarks | 'AutoProps + Records - PrepareModules' | FocusRecordTypes=False, SkipAutoProps=False | 64.666 | 65.966 | 35.0809 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | AutoPropsBenchmarks | 'AutoProps + Records - PrepareModules' | FocusRecordTypes=True, SkipAutoProps=False | 52.223 | 53.244 | 11.9528 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | AutoPropsBenchmarks | 'AutoProps + Records - PrepareModules' | FocusRecordTypes=False, SkipAutoProps=True | 64.261 | 65.072 | 31.4231 |
| 2026-06-01 | 10.0.2-p0 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | AutoPropsBenchmarks | 'AutoProps + Records - PrepareModules' | FocusRecordTypes=True, SkipAutoProps=True | 46.282 | 47.781 | 8.5417 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | CoverageBenchmarks | GetCoverageBenchmark |  | 0.000 | 0.000 | 0.0001 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | CoverageWorkflowBenchmark | 'Simulate Workflow' |  | 295.523 | 296.789 | 32.4536 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumenterBenchmarks | InstrumenterBenchmark |  | 158.681 | 159.447 | 31.1532 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=False, SkipAutoProps=False | 163.594 | 164.888 | 35.0962 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=False, SkipAutoProps=False | 166.969 | 168.505 | 35.0960 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=False, SkipAutoProps=True | 162.495 | 163.318 | 31.1680 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=False, SkipAutoProps=True | 160.894 | 161.814 | 31.1538 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=True, SkipAutoProps=False | 167.232 | 168.968 | 35.0818 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=True, SkipAutoProps=False | 165.804 | 167.145 | 35.0960 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=False, SingleHit=True, SkipAutoProps=True | 166.735 | 168.488 | 31.1680 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | InstrumentationOptionsBenchmarks | 'Instrumentation - PrepareModules' | IncludeTestAssembly=True, SingleHit=True, SkipAutoProps=True | 165.862 | 167.492 | 31.1538 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=cobertura | 0.004 | 0.004 | 0.0182 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=json | 0.002 | 0.002 | 0.0006 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=lcov | 0.000 | 0.000 | 0.0004 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=opencover | 0.006 | 0.006 | 0.0219 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | ReportFormatBenchmarks | 'GetCoverageResult + Report' | ReportFormat=teamcity | 0.001 | 0.001 | 0.0030 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | AutoPropsBenchmarks | 'AutoProps + Records - PrepareModules' | FocusRecordTypes=False, SkipAutoProps=False | 77.191 | 79.786 | 35.0810 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | AutoPropsBenchmarks | 'AutoProps + Records - PrepareModules' | FocusRecordTypes=True, SkipAutoProps=False | 56.200 | 57.403 | 11.9530 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | AutoPropsBenchmarks | 'AutoProps + Records - PrepareModules' | FocusRecordTypes=False, SkipAutoProps=True | 66.007 | 67.148 | 31.1534 |
| 2026-06-01 | 10.0.2-i1633 | .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3 | AutoPropsBenchmarks | 'AutoProps + Records - PrepareModules' | FocusRecordTypes=True, SkipAutoProps=True | 53.622 | 56.676 | 8.2825 |
