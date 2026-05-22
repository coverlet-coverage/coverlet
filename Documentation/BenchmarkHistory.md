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
    -CoverletVersion "6.0.5"
```

---

## Results

| Date | Version | Runtime | BenchmarkClass | Method | Options | Mean (ms) | Max (ms) | Allocated (MB) |
|------|---------|---------|----------------|--------|---------|----------:|---------:|---------------:|
| 2025-01-01 | 6.0.0 | .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2 | CoverageBenchmarks | GetCoverageBenchmark |  | 0.000 | 0.000 | 0.0001 |
| 2025-01-01 | 6.0.0 | .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2 | InstrumenterBenchmarks | InstrumenterBenchmark |  | 4938.714 | 5706.474 | 2745.291 |
| 2025-01-01 | 6.0.1 | .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2 | CoverageBenchmarks | GetCoverageBenchmark |  | 0.000 | 0.000 | 0.0001 |
| 2025-01-01 | 6.0.1 | .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2 | InstrumenterBenchmarks | InstrumenterBenchmark |  | 3675.772 | 4550.028 | 2732.512 |
| 2025-01-01 | 6.0.2 | .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2 | CoverageBenchmarks | GetCoverageBenchmark |  | 0.000 | 0.000 | 0.0001 |
| 2025-01-01 | 6.0.2 | .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2 | InstrumenterBenchmarks | InstrumenterBenchmark |  | 19105.224 | 23555.328 | 3023.829 |
| 2025-01-01 | 6.0.3 | .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2 | CoverageBenchmarks | GetCoverageBenchmark |  | 0.000 | 0.000 | 0.0001 |
| 2025-01-01 | 6.0.3 | .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2 | InstrumenterBenchmarks | InstrumenterBenchmark |  | 3620.666 | 4201.277 | 2668.645 |
| 2025-01-01 | 6.0.4 | .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2 | CoverageBenchmarks | GetCoverageBenchmark |  | 0.000 | 0.000 | 0.0001 |
| 2025-01-01 | 6.0.4 | .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2 | InstrumenterBenchmarks | InstrumenterBenchmark |  | 3594.264 | 3787.390 | 2668.644 |
