# coverlet.core Performance Improvement Proposal

> **Scope:** `src/coverlet.core` – targets net8.0, net9.0, net10.0  
> **Approach:** measure first (`InstrumentationOptionsBenchmarks`, `ReportFormatBenchmarks`), then apply,
> then measure again and record results in `BenchmarkHistory.md`.

---

## Background & Methodology

BenchmarkDotNet profiling of the existing `CoverageWorkflowBenchmark` and the newly added
`InstrumentationOptionsBenchmarks` / `ReportFormatBenchmarks` suites reveals the following
hot paths and allocation hot-spots.  Every proposal below is backed by a targeted benchmark
that can be run in isolation to confirm the gain.

---

## Proposal 1 – Avoid double module-read in `Instrumenter.InstrumentModule`

### Problem
`Instrumenter` opens and fully reads the Cecil `ModuleDefinition` **twice**: once in
`CreateReachabilityHelper()` and once in `InstrumentModule()`.  For large assemblies this
means two complete IL stream reads plus two symbol-store loads.

```csharp
// Instrumenter.cs – today
private void InstrumentModule()
{
    CreateReachabilityHelper();            // ← read #1
    using Stream stream = ...OpenReadWrite
    using var module = ModuleDefinition.ReadModule(stream, ...); // ← read #2
    ...
}
```

### Proposed Fix
Refactor `CreateReachabilityHelper` to accept (or return) the `ModuleDefinition` so it can be
reused inside `InstrumentModule`.  A read-only in-memory `MemoryStream` copy of the bytes can
act as the source for the reachability pass, while the original file stream is used for the
read/write instrumentation pass.

```csharp
// After
private void InstrumentModule()
{
    using Stream stream = _fileSystem.NewFileStream(_module, FileMode.Open, FileAccess.ReadWrite);
    // Copy bytes for read-only reachability analysis
    byte[] moduleBytes = ReadAllBytes(stream);
    stream.Position = 0;

    _reachabilityHelper = CreateReachabilityHelper(moduleBytes);  // no second file I/O

    using var module = ModuleDefinition.ReadModule(stream, writerParams);
    ...
}
```

**Expected gain:** Reduces file I/O by ~50 % per module; removes one full Cecil type-system
initialisation per instrumented assembly.

**Benchmark to verify:** `InstrumentationOptionsBenchmarks.InstrumentWithOptions`

---

## Proposal 2 – Cache `IsTypeExcluded` / `IsTypeIncluded` results

### Problem
`IsTypeExcluded` and `_instrumentationHelper.IsTypeIncluded` are called for every
`TypeDefinition` in a module, and each call internally iterates over all configured filter
expressions using `Matcher` / string comparisons.  For assemblies with hundreds of types the
same filter string is re-evaluated thousands of times.

### Proposed Fix
Introduce a per-instrumentation-run `Dictionary<string, bool>` cache keyed on
`type.FullName`.  The cache is local to the `Instrumenter` instance (no concurrency concern).

```csharp
private readonly Dictionary<string, bool> _typeExcludedCache = new(StringComparer.Ordinal);
private readonly Dictionary<string, bool> _typeIncludedCache = new(StringComparer.Ordinal);

private bool IsTypeExcluded(TypeDefinition type)
{
    if (_typeExcludedCache.TryGetValue(type.FullName, out bool cached))
        return cached;
    bool result = ComputeIsTypeExcluded(type);
    _typeExcludedCache[type.FullName] = result;
    return result;
}
```

**Expected gain:** Eliminates redundant filter evaluation for nested and repeated type lookups;
measurable on assemblies with >50 types (e.g. `coverlet.testsubject`).

**Benchmark to verify:** `InstrumentationOptionsBenchmarks.InstrumentWithOptions`
(compare `SkipAutoProps=false` vs `true` to see filter call volume effect).

---

## Proposal 3 – Replace `LINQ` hot paths in `InstrumentType` with direct loops

### Problem
Several inner loops inside `InstrumentType` and `InstrumentMethod` use LINQ operators
(`Any`, `FirstOrDefault`, `Where`) on small `IList`/array collections.  In a tight loop
over thousands of instructions these allocate enumerator objects and add GC pressure.

Key sites (by line in `Instrumenter.cs`):

| Site | Current code |
|------|-------------|
| Branch exclusion check | `_branchesInCompiledGeneratedClass?.Any(b => ...)` |
| Exclude attribute check | `type.CustomAttributes.Any(attr => IsExcludeAttribute(attr) && ...)` |
| State machine interface check | `type.Interfaces.Any(i => ...)` |

### Proposed Fix
Replace with `foreach` + early `return`/`break`:

```csharp
// Before
bool excluded = type.CustomAttributes.Any(attr => IsExcludeAttribute(attr));

// After
bool excluded = false;
foreach (var attr in type.CustomAttributes)
{
    if (IsExcludeAttribute(attr)) { excluded = true; break; }
}
```

For collections that are genuinely array-backed, using `Array.Exists` (no allocation) is an
alternative for single-predicate checks.

**Expected gain:** Reduced Gen0 allocation per instrumented method; visible in the
`Allocated` column of `InstrumentationOptionsBenchmarks`.

**Benchmark to verify:** `InstrumentationOptionsBenchmarks` – compare `Allocated` column
before and after.

---

## Proposal 4 – Pool `StringBuilder` instances in reporters

### Problem
`CoberturaReporter`, `OpenCoverReporter`, and `LcovReporter` each create a fresh
`StringBuilder` (or `XmlWriter` backed by a `MemoryStream`) per `Report()` call.  When
coverage is collected during a test run these reporters may be called frequently.

### Proposed Fix
Use `Microsoft.Extensions.ObjectPool` (already an indirect dependency via ASP.NET) or a
simple `ThreadLocal<StringBuilder>` with a `Clear()` call:

```csharp
private static readonly ThreadLocal<StringBuilder> s_sb =
    new(() => new StringBuilder(capacity: 64 * 1024));

public string Report(CoverageResult result, ISourceRootTranslator translator)
{
    var sb = s_sb.Value!;
    sb.Clear();
    // build report into sb ...
    return sb.ToString();
}
```

**Expected gain:** Fewer large `StringBuilder` / `MemoryStream` heap allocations per report
generation; improves `ReportFormatBenchmarks` `Allocated` metric.

**Benchmark to verify:** `ReportFormatBenchmarks.GetCoverageAndReport`

---

## Proposal 5 – Parallelise multi-module instrumentation in `Coverage.PrepareModules`

### Problem
`PrepareModules` instruments each module in a sequential `foreach` loop.  Projects with many
assemblies (e.g. integration test suites) wait for each module to complete before starting
the next.

```csharp
foreach (string module in validModules)   // sequential today
{
    var instrumenter = new Instrumenter(...);
    if (instrumenter.CanInstrument()) { ... }
}
```

### Proposed Fix
Replace the `foreach` with `Parallel.ForEach` (with an `IInstrumentationHelper` instance per
thread to avoid Cecil reader sharing) or use `Task.WhenAll` over a pool of workers:

```csharp
var results = new System.Collections.Concurrent.ConcurrentBag<InstrumenterResult>();
Parallel.ForEach(validModules, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
    module =>
    {
        var localHelper = CreateThreadLocalHelper();
        var instrumenter = new Instrumenter(module, Identifier, _parameters, _logger,
                                            localHelper, _fileSystem, _sourceRootTranslator, _cecilSymbolHelper);
        if (instrumenter.CanInstrument())
        {
            _instrumentationHelper.BackupOriginalModule(module, Identifier, ...);
            var r = instrumenter.Instrument();
            if (!instrumenter.SkipModule) results.Add(r);
        }
    });
_results.AddRange(results);
```

> ⚠️ Cecil `ModuleDefinition` instances are **not** thread-safe.  Each parallel worker must
> use its own `AssemblyResolver` and `ModuleDefinition`; shared `IInstrumentationHelper` state
> (filter lists, regex caches) must be made read-only or synchronised.

**Expected gain:** Near-linear speed-up proportional to module count on multi-core machines.
Best measured with a solution containing ≥4 assemblies.

**Benchmark to verify:** Extend `CoverageWorkflowBenchmark` with a multi-module variant that
points at an `IncludeDirectories` containing several copies of `coverlet.testsubject.dll`.

---

## Proposal 6 – Compile filter expressions once (Regex / Glob)

### Problem
`InstrumentationHelper.IsTypeExcluded` / `IsTypeIncluded` rebuild `Matcher` (glob) instances
or call `Regex.IsMatch` with pattern strings on every call.  Patterns do not change during a
single coverage run.

### Proposed Fix
Compile all include/exclude globs to `Matcher` instances once during
`InstrumentationHelper` construction (or first use) and cache them:

```csharp
// Cache per filter-string list identity
private readonly Lazy<Matcher> _excludeMatcher;

public InstrumentationHelper(...)
{
    _excludeMatcher = new Lazy<Matcher>(() =>
    {
        var m = new Matcher(StringComparison.OrdinalIgnoreCase);
        foreach (var f in _excludeFilters ?? []) m.AddInclude(f);
        return m;
    });
}
```

**Expected gain:** Eliminates repeated `Matcher` object construction on the hot path;
reduces allocations visible in the Gen0/Gen1 columns of `InstrumentationOptionsBenchmarks`.

**Benchmark to verify:** `InstrumentationOptionsBenchmarks.InstrumentWithOptions`

---

## Implementation Priority

| # | Proposal | Effort | Expected Impact | Risk |
|---|----------|--------|----------------|------|
| 2 | Cache type-exclusion results | Low | High | Low |
| 6 | Pre-compile filter expressions | Low | High | Low |
| 3 | Replace LINQ hot paths with loops | Low | Medium | Low |
| 4 | Pool `StringBuilder` in reporters | Low | Medium | Low |
| 1 | Avoid double module read | Medium | High | Medium |
| 5 | Parallelise `PrepareModules` | High | Very High | High |

Start with proposals **2, 6, 3** (low-effort / low-risk) and confirm with benchmarks before
proceeding to proposals **1 and 4**.  Proposal **5** requires a design review to ensure Cecil
thread-safety constraints are met.

---

## Tracking Results

Run `scripts/Update-BenchmarkHistory.ps1` after each benchmark run to append the new
measurements to `BenchmarkHistory.md`.  Compare the **Mean (ms)** and **Allocated (MB)**
columns for `InstrumentWithOptions` and `GetCoverageAndReport` across releases.


## Implementation plan

- ✅ P2,P6,P3 => PR #1934
- ✅ P1,P4 => PR #1934
- ⏭️ P5 => separate PR required (High Risk)
