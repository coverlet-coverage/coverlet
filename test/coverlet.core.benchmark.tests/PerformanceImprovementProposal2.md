# coverlet.core Performance Improvement Proposal 2

> **Scope:** `src/coverlet.core` – targets net8.0, net9.0, net10.0
> **Builds on:** `PerformanceImprovementProposal.md` (P1–P6 all merged in PR #1934)
> **Approach:** measure first, then apply, then record results in `BenchmarkHistory.md`.

---

## Review of Proposal 1 (P1–P6) outcomes

After P1–P6 were merged the benchmarks showed less improvement than predicted for two reasons:

1. **LINQ replacement (P3) produced no measurable gain** – on .NET 9/10 the JIT inlines most
   `Any`/`FirstOrDefault` calls on small `IList<T>` and the BCL's `foreach`-based enumerator is
   already allocation-free for list-backed collections.  The change is harmless but the
   numbers confirmed what the user noted: replacing LINQ with `foreach` on modern runtimes is
   not a reliable speedup.  **Revert P3 loop-replacements only where readability suffers.**

2. **`coverlet.testsubject` is a poor benchmark subject** – `BigClass.cs` is 6 200 lines of
   identical `if (y > n) { new SubClassN().Do(y); }` blocks.  Every subclass follows the
   same shape, so:
   - Branch diversity is near-zero (one branch per method, always the same pattern).
   - No async state machines → `IAsyncStateMachine` code paths are never exercised.
   - No iterators, no complex switch, no generics, no lambda captures.
   - The `insideExceptionHandlerOffsets` fast-path is always empty.
   - The Cecil type-filter caches (P2, P6) warm up in the first few types and then hit 100 %
     for the remaining ~200 identical sub-classes, making the proposals look better than they
     are against real code.

---

## Benchmark Subject Improvements

### BS-1 – Add a richer synthetic assembly alongside `coverlet.testsubject`

Create **`test/coverlet.benchmark.subject`** (new class library, `net8.0`) that contains the
following patterns, each in a separate file so the benchmark can target individual groups:

| File | Purpose |
|------|---------|
| `AsyncWorkload.cs` | 10–20 `async Task` methods with `await`, error handling, nested async calls → exercises `IAsyncStateMachine` type detection and the `IsCompilerGeneratedStateMachineType` fast-path |
| `IteratorWorkload.cs` | `IEnumerable<T>` iterator methods (`yield return`) → exercises `IEnumerator` interface detection |
| `SwitchWorkload.cs` | Methods with large `switch` expressions (10–20 arms) and nested `if`/`switch` combinations → increases `branchPoints` count per method dramatically |
| `LambdaWorkload.cs` | LINQ query bodies, `Func<>` captures, local functions with closures → exercises the `<>c` compiled-generated-class branch-fixup path in `Coverage.GetCoverageResult` |
| `GenericWorkload.cs` | Open generics, constrained type parameters, generic methods → stresses filter regex matching against generic type names |
| `ExcludedWorkload.cs` | Types and methods with `[ExcludeFromCoverage]` / `[ExcludeFromCodeCoverage]` at various levels → makes P2 caches do real work |
| `DeepNestingWorkload.cs` | Deeply nested classes (5+ levels) → exercises `ComputeIsTypeExcluded`'s ancestor walk |

**Benefit:** Makes every existing proposal measurable against realistic code; prevents
regressions in future PRs from hiding behind the uniform BigClass benchmark.

### BS-2 – Multi-module benchmark variant

Add a `MultiModuleBenchmarks` class that points `IncludeDirectories` at a directory
containing **4–6 copies** of the new `coverlet.benchmark.subject.dll` (renamed to simulate
distinct assemblies).  This is the only meaningful way to benchmark `PrepareModules` wall
time and any future P5 parallelisation work.

---

## New Performance Proposals

---

### P7 – Pre-group branch-by-line lookup in `CoberturaReporter`

#### Problem
Inside `Report()`, for every `(method, line)` pair the reporter calls:

```csharp
// CoberturaReporter.cs – inner line loop
bool isBranchPoint = meth.Value.Branches.Any(b => b.Line == ln.Key);   // O(branches)
// ...
var branches = meth.Value.Branches.Where(b => b.Line == ln.Key).ToList(); // O(branches) again
```

For a method with *L* lines and *B* branches this is **O(L × B)** per method, repeated for
every method in every class in every module.  A 200-method module with ~20 branches per
method and ~30 lines per method → ≈ 120 000 linear scans.

#### Proposed Fix
Build a `Dictionary<int, List<BranchInfo>>` keyed on line number once per method before the
line loop:

```csharp
// Build once per method
var branchesByLine = new Dictionary<int, List<BranchInfo>>(meth.Value.Branches.Count);
foreach (BranchInfo b in meth.Value.Branches)
{
    if (!branchesByLine.TryGetValue(b.Line, out var list))
        branchesByLine[b.Line] = list = [];
    list.Add(b);
}

// Then inside the line loop: O(1) per line
bool isBranchPoint = branchesByLine.ContainsKey(ln.Key);
if (isBranchPoint)
{
    var branches = branchesByLine[ln.Key];
    ...
}
```

The same pattern applies to `OpenCoverReporter` (check for duplication before fixing).

**Expected gain:** Reduces `Report()` allocation (no repeated `.ToList()` per line) and CPU
time from O(L×B) to O(L+B) per method.  Visible in `ReportFormatBenchmarks` `Allocated` and
`Mean` for `cobertura` and `opencover` formats on the richer benchmark subject.

**Benchmark to verify:** `ReportFormatBenchmarks.GetCoverageAndReport` with
`ReportFormat = "cobertura"` and `"opencover"`.

---

### P8 – Eliminate double-enumeration in `CoverageSummary.CalculateMethodCoverage`

#### Problem
`CalculateMethodCoverage(Methods methods)` filters with `Where` and then calls `.Count()` on
the filtered sequence a second time:

```csharp
// CoverageSummary.cs
IEnumerable<KeyValuePair<string, Method>> methodsWithLines =
    methods.Where(m => m.Value.Lines.Count > 0);
foreach (var method in methodsWithLines)
{
    ...
    details.Covered += methodCoverage.Covered;
}
details.Total = methodsWithLines.Count();   // ← re-enumerates the IEnumerable
```

`.Count()` on a re-enumerated `Where` query re-runs the predicate for every element.

Also in `CalculateLineCoverage(Lines lines)`:
```csharp
Covered = lines.Count(l => l.Value > 0),   // LINQ on every call
Total   = lines.Count                       // property – fine
```

#### Proposed Fix
Use a single `foreach` loop accumulating both `Covered` and `Total`:

```csharp
public static CoverageDetails CalculateMethodCoverage(Methods methods)
{
    var details = new CoverageDetails();
    foreach (KeyValuePair<string, Method> method in methods)
    {
        if (method.Value.Lines.Count == 0)
            continue;
        details.Total++;
        details.Covered += method.Value.Lines.Any(l => l.Value > 0) ? 1 : 0;
    }
    return details;
}
```

For `CalculateLineCoverage(Lines lines)` replace the LINQ `Count()` with a `foreach`:

```csharp
public static CoverageDetails CalculateLineCoverage(Lines lines)
{
    int covered = 0;
    foreach (KeyValuePair<int, int> l in lines)
    {
        if (l.Value > 0) covered++;
    }
    return new CoverageDetails { Covered = covered, Total = lines.Count };
}
```

**Expected gain:** Removes one full re-scan of method lists in the report phase.
Most visible in `ReportFormatBenchmarks` `Allocated` column (fewer LINQ state machine
allocations in tight loops).

**Benchmark to verify:** `ReportFormatBenchmarks.GetCoverageAndReport` – `Allocated` metric.

---

### P9 – Pre-group `HitCandidates` by `docIndex` in `CalculateCoverage`

#### Problem
In `Coverage.CalculateCoverage`, the nested-range detection loop is O(n²):

```csharp
// Coverage.cs – CalculateCoverage
foreach (HitCandidate hitCandidate in result.HitCandidates)
{
    if (hitCandidate.isBranch || hitCandidate.end == hitCandidate.start) continue;

    foreach (HitCandidate hitCandidateToCompare in
        result.HitCandidates.Where(x => x.docIndex.Equals(hitCandidate.docIndex)))  // O(n) per outer
    { ... }
}
```

For an assembly with *N* non-branch, multi-line candidates spread over *D* documents, the
inner `Where` re-scans all `N` candidates for every outer candidate: **O(N²)** total.

#### Proposed Fix
Pre-group hit candidates by `docIndex` once before the outer loop using `ToLookup`:

```csharp
// Build once
ILookup<int, HitCandidate> byDocIndex = result.HitCandidates.ToLookup(x => x.docIndex);

foreach (HitCandidate hitCandidate in result.HitCandidates)
{
    if (hitCandidate.isBranch || hitCandidate.end == hitCandidate.start) continue;

    foreach (HitCandidate hitCandidateToCompare in byDocIndex[hitCandidate.docIndex])
    { ... }
}
```

`ILookup<K,V>` uses a pre-built hashtable so each inner group lookup is O(1) + O(group size),
reducing total complexity to **O(N + D × G²)** where G is the average group size (usually
small per document).

**Expected gain:** For instrumented assemblies with many sequence points spanning multiple
lines (async state machines, iterator blocks), `CalculateCoverage` becomes measurably faster.

**Benchmark to verify:** A dedicated `CalculateCoverageBenchmarks` targeting the richer
benchmark subject (BS-1) to expose the O(n²) pattern.

---

### P10 – Cache the validation `Regex` in `IsValidFilterExpression`

#### Problem
`InstrumentationHelper.IsValidFilterExpression` constructs a new `Regex` object on every
invocation:

```csharp
// InstrumentationHelper.cs
if (new Regex(@"[^\w*]", s_regexOptions, TimeSpan.FromSeconds(10))
        .IsMatch(filter.Replace(...)))
    return false;
```

This method is called for every filter string at the start of `PrepareModules` and in
validation paths inside `SelectModules`.  Although the absolute call count is small (bounded
by the number of configured filters), creating compiled `Regex` instances is expensive and
wasteful when the pattern never changes.

Additionally, `filter.Count(f => f == '[')` and `filter.Count(f => f == ']')` iterate the
string twice each.  A single-pass scan is cheaper.

#### Proposed Fix
Promote the validation regex to a static readonly field alongside the existing `s_regexOptions`:

```csharp
private static readonly Regex s_invalidFilterCharsRegex =
    new(@"[^\w*]", RegexOptions.Compiled, TimeSpan.FromSeconds(10));
```

Replace the two `.Count()` scans with a single-pass helper that counts `[` and `]`
simultaneously and short-circuits as soon as a count exceeds 1.

**Expected gain:** Negligible on a single run, but relevant for tools that call
`IsValidFilterExpression` in a tight loop (e.g. the MSBuild task that validates all filter
properties).

**Benchmark to verify:** Micro-benchmark in `InstrumentationOptionsBenchmarks` – add a
`ValidateFilters` benchmark that calls `IsValidFilterExpression` for 50 typical filter strings.

---

### P11 – Pre-build a `HashSet` for `BranchInCompilerGeneratedClass` lookup

#### Problem
`Coverage.BranchInCompilerGeneratedClass(string methodName)` is called in a tight nested
loop for every branch of every method of every class-with-`<>c`-prefix:

```csharp
// Coverage.cs
private bool BranchInCompilerGeneratedClass(string methodName)
{
    foreach (InstrumenterResult instrumentedResult in _results)   // O(modules)
    {
        if (instrumentedResult.BranchesInCompiledGeneratedClass.Contains(methodName))
            return true;                                           // O(entries per module)
    }
    return false;
}
```

`BranchesInCompiledGeneratedClass` is a `string[]` (immutable array), so `.Contains` is a
linear scan.  The entire lookup is called once per anonymous-delegate branch, which for a
large solution with many LINQ closures adds up.

#### Proposed Fix
Build a `HashSet<string>` once before the outer loop in `GetCoverageResult`:

```csharp
// In GetCoverageResult, before the outer module loop
var branchesInGeneratedClassSet = new HashSet<string>(StringComparer.Ordinal);
foreach (InstrumenterResult r in _results)
{
    foreach (string m in r.BranchesInCompiledGeneratedClass)
        branchesInGeneratedClassSet.Add(m);
}
```

Replace the `BranchInCompilerGeneratedClass` call with a `branchesInGeneratedClassSet.Contains(...)` O(1) lookup.

**Expected gain:** O(1) vs O(modules × array-size) per branch lookup.  Most visible on
projects with many LINQ queries (lambda workloads) – well covered by BS-1's `LambdaWorkload`.

**Benchmark to verify:** `ReportFormatBenchmarks.GetCoverageAndReport` (the `GetCoverageResult`
phase) using the richer benchmark subject.

---

### P12 – Pre-sort `unreachableRanges` and replace linear search with binary search in `InstrumentIL`

#### Problem
`InstrumentIL` advances a `currentUnreachableRangeIx` cursor through the
`ImmutableArray<UnreachableRange>` to determine reachability per instruction:

```csharp
while (currentUnreachableRangeIx < unreachableRanges.Length
       && instrOffset > unreachableRanges[currentUnreachableRangeIx].EndOffset)
{
    currentUnreachableRangeIx++;
}
```

This is already O(n) amortised across the instruction loop because the cursor never resets.
**However**, this relies on `unreachableRanges` being sorted by `EndOffset`.  If
`ReachabilityHelper.FindUnreachableIL` ever returns ranges out of order (e.g. after a future
refactor), the cursor will skip ranges silently, producing wrong coverage data.  Add an
assertion (debug builds) and document the contract.

Additionally, the `insideExceptionHandlerOffsets` `HashSet<int>` is built by an O(instructions
× exception_handlers) double loop.  For methods with many exception handlers (e.g. async state
machines generated by Roslyn can have 10+ handlers), this is the dominant cost before any IL
is instrumented.

#### Proposed Fix
Replace the double loop with a single pass over exception handlers only, storing `[start,
end)` intervals, and check membership with an interval list instead of a per-offset set:

```csharp
// Build compact interval list – O(handlers) instead of O(instructions × handlers)
List<(int start, int end)> exceptionHandlerRanges = [];
foreach (ExceptionHandler eh in method.Body.ExceptionHandlers)
{
    if (eh.TryStart is not null)
        exceptionHandlerRanges.Add((eh.TryStart.Offset, eh.TryEnd?.Offset ?? int.MaxValue));
    if (eh.HandlerStart is not null)
        exceptionHandlerRanges.Add((eh.HandlerStart.Offset, eh.HandlerEnd?.Offset ?? int.MaxValue));
    if (eh.FilterStart is not null)
        exceptionHandlerRanges.Add((eh.FilterStart.Offset, eh.HandlerStart?.Offset ?? int.MaxValue));
}

// Membership check: O(handlers) per instruction – better than O(1) HashSet when handlers < ~8
bool IsInExceptionHandler(int offset)
{
    foreach ((int s, int e) in exceptionHandlerRanges)
        if (offset >= s && offset < e) return true;
    return false;
}
```

For most methods (0–2 handlers) an interval list is faster than a `HashSet` due to
lower allocation and better cache locality.  For async methods with many handlers (10+)
sort the intervals and binary-search.

**Expected gain:** Reduces `InstrumentIL` setup time for async-heavy assemblies (the new
BS-1 `AsyncWorkload` will expose this).

**Benchmark to verify:** `InstrumenterBenchmarks.InstrumenterBigClassBenchmark` with the new
`AsyncWorkload` test subject.

---

### P13 – Avoid `module.GetTypes()` full-walk when all types are filtered

#### Problem
`InstrumentModule` calls `module.GetTypes()` unconditionally and then checks
`IsTypeExcluded` / `IsTypeIncludedCached` per type.  For assemblies where the include filter
does not match any type (common for transitive dependencies pulled in via
`IncludeDirectories`), every type is loaded, fully resolved by Cecil, and then discarded.

#### Proposed Fix
Before calling `module.GetTypes()`, check whether the module name itself matches any include
filter:

```csharp
string moduleName = Path.GetFileNameWithoutExtension(_module);
if (!_instrumentationHelper.IsTypeIncluded(_module, "*", _parameters.IncludeFilters))
{
    // Module-level short-circuit: the module filter portion matches nothing.
    // No type in this assembly can pass the include filter.
    SkipModule = true;
    return;
}
```

This is a module-level fast-exit that avoids Cecil's full type enumeration for excluded modules.

**Note:** The `IncludeFilters` format is `[ModuleGlob]TypeGlob`.  Pass `"*"` as the type
portion to test only the module portion of the filter.  The existing `IsTypeFilterMatch`
already handles wildcard type patterns.

**Expected gain:** Significant wall-time reduction in multi-module projects where a broad
`IncludeDirectories` picks up unrelated assemblies.

**Benchmark to verify:** BS-2 (multi-module benchmark) with a mixed set of matching and
non-matching module names.

---

## Implementation Priority

| # | Proposal | Effort | Expected Impact | Risk | TFM | Benchmark |
|---|----------|--------|----------------|------|-----|-----------|
| BS-1 | Richer benchmark subject | Medium | Prerequisite | None | net8.0 | All |
| BS-2 | Multi-module benchmark variant | Low | Prerequisite | None | net10.0 | P5/P13 |
| P7 | Pre-group branches by line in reporters | Low | High | Low | all | ReportFormatBenchmarks |
| P8 | Eliminate double-enumeration in CoverageSummary | Low | Medium | Low | all | ReportFormatBenchmarks |
| P9 | Pre-group HitCandidates by docIndex | Low | Medium-High | Low | all | CalculateCoverageBenchmarks |
| P11 | HashSet for BranchInCompilerGeneratedClass | Low | Medium | Low | all | ReportFormatBenchmarks |
| P10 | Cache validation Regex | Low | Low | Low | all | InstrumentationOptionsBenchmarks |
| P12 | Interval list for exception handler ranges | Medium | Medium | Low | all | InstrumenterBenchmarks |
| P13 | Module-level short-circuit in InstrumentModule | Low | High (multi-module) | Medium | all | BS-2 |
| P5 | Parallelise PrepareModules | High | Very High | High | net8.0+ | BS-2 |

**Recommended sequence:**
1. Implement **BS-1** first (richer subject), run all existing benchmarks to establish a
   revised baseline.
2. Apply **P7, P8, P9, P11** together (all low-risk, all reporter/coverage-result phase);
   confirm with benchmarks before merging.
3. Apply **P10** as a micro-cleanup with P7–P11.
4. Apply **P12** after BS-1 shows async-workload overhead.
5. Apply **P13** after BS-2 is available.
6. **P5** in a separate PR after design review.

---

## Notes on LINQ vs. `foreach` (P3 revisited)

On .NET 9/10 the JIT inlines `Enumerable.Any`, `Enumerable.Count`, and `Enumerable.Where`
on small concrete `List<T>` / `T[]` collections aggressively.  The allocation cost comes from
**iterator state machine heap objects** created for `IEnumerable<T>` chains that cross method
boundaries and are not directly on a `List<T>`.

The **right heuristic** is:
- Replace LINQ only when the source is `IList<T>` or `IEnumerable<T>` (interface type,
  devirtualisation not guaranteed) **and** the call site is inside a tight inner loop.
- Keep LINQ when the source is a concrete `List<T>` or array — the JIT already handles it.
- Always replace `.Count()` after a `.Where()` that isn't materialised (double-enumeration).

---

## Tracking Results

Run `scripts/Update-BenchmarkHistory.ps1` after each benchmark run to append the new
measurements to `BenchmarkHistory.md`.

## Implementation Plan

- ⏳ BS-1 – new richer benchmark subject
- ⏳ BS-2 – multi-module benchmark variant
- ⏳ P7, P8, P9, P10, P11 => single PR (reporter + coverage-result phase)
- ⏳ P12 => separate PR (InstrumentIL change)
- ⏳ P13 => separate PR (module-level short-circuit)
- ⏭️ P5 => separate PR, requires design review (Cecil thread-safety)
