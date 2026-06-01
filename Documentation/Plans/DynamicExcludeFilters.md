# Dynamic Assembly Exclude Filters for coverlet.MTP

## Problem

`coverlet.MTP` maintains a hard-coded, growing list of `s_defaultExcludeFilters` to exclude test
infrastructure assemblies (xunit, NUnit, MSTest, ReportGenerator.Mtp, etc.). This is brittle —
every new test framework integration requires a code change.

## Solution

Instead of maintaining a static list, introspect the test process's own **loaded assemblies** at
instrumentation time, convert them to exclude-filter patterns, and use those as the dynamic
defaults. This list is only used when **no configuration file is present**, mirroring the existing
config-file-takes-precedence logic.

## Architecture

```
coverlet.core
  └── Helpers/
        └── ProcessAssemblyHelper.cs          ← NEW: discovers loaded assemblies
  └── Abstractions/
        └── IProcessAssemblyHelper.cs         ← NEW: interface for testability

coverlet.MTP
  └── Configuration/
        └── CoverageConfiguration.cs          ← MODIFIED: use dynamic list as defaults
        └── CoverletMTPConstants.cs           ← MODIFIED: keep only "[coverlet.*]*"
  └── Collector/
        └── CollectorExtension.cs             ← MODIFIED: pass helper to CoverageConfiguration

test/coverlet.core.tests
  └── Helpers/
        └── ProcessAssemblyHelperTests.cs     ← NEW: unit tests

test/coverlet.MTP.tests
  └── Configuration/
        └── CoverageConfigurationDynamicTests.cs  ← NEW: integration-style tests
```

---

## Implementation Steps

### Step 1 — Add `IProcessAssemblyHelper` to `coverlet.core`

**New file: `src/coverlet.core/Abstractions/IProcessAssemblyHelper.cs`**

```csharp
namespace Coverlet.Core.Abstractions;

/// <summary>
/// Provides access to assemblies loaded by the current process,
/// used to build dynamic exclude-filter defaults.
/// </summary>
internal interface IProcessAssemblyHelper
{
    /// <summary>
    /// Returns the simple assembly names (no version, culture, or public key token)
    /// of assemblies directly loaded into the current process's default ALC,
    /// excluding the assembly under test itself.
    /// Transitive/indirect dependencies that are not yet loaded are not returned.
    /// </summary>
    /// <param name="testAssemblyName">
    /// Simple name of the test assembly to exclude from the list.
    /// </param>
    IReadOnlyList<string> GetLoadedAssemblyNames(string testAssemblyName);
}
```

**Design notes:**

- Returns only **simple names** (e.g., `xunit.runner.utility`) — already-loaded assemblies only,
  so no transitive dependencies are returned inadvertently.
- The test assembly itself is excluded so it does not accidentally become an exclude-filter.
- `internal` visibility — not part of the public API.

---

### Step 2 — Implement `ProcessAssemblyHelper` in `coverlet.core`

**New file: `src/coverlet.core/Helpers/ProcessAssemblyHelper.cs`**

```csharp
using System.Reflection;
using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Helpers;

internal sealed class ProcessAssemblyHelper : IProcessAssemblyHelper
{
    /// <inheritdoc/>
    public IReadOnlyList<string> GetLoadedAssemblyNames(string testAssemblyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testAssemblyName);

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => a.GetName().Name)
            .Where(name => !string.IsNullOrWhiteSpace(name)
                        && !name.Equals(testAssemblyName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }
}
```

**Design notes:**

- Uses `AppDomain.CurrentDomain.GetAssemblies()` — returns **currently loaded** assemblies only
  (no transitive unloaded dependencies).
- Skips dynamic/in-memory assemblies (`a.IsDynamic`) which have no meaningful name.
- Skips assemblies without a `Location` (e.g., in-memory emitted assemblies).
- `Distinct` guards against edge cases where the same name appears under different load contexts.

---

### Step 3 — Add a conversion helper: assembly names → exclude filter patterns

Add a `static` helper method inside `ProcessAssemblyHelper`:

```csharp
/// <summary>
/// Converts a simple assembly name to a coverlet exclude-filter pattern.
/// e.g. "xunit.runner.utility" → "[xunit.runner.utility]*"
/// </summary>
internal static string ToExcludeFilter(string assemblyName)
    => $"[{assemblyName}]*";
```

---

### Step 4 — Modify `CoverageConfiguration` in `coverlet.MTP`

**File: `src/coverlet.MTP/Configuration/CoverageConfiguration.cs`**

#### Changes

1. Inject `IProcessAssemblyHelper` as an optional constructor parameter (defaults to
   `ProcessAssemblyHelper` when `null`).
2. Reduce `s_defaultExcludeFilters` to a single permanent baseline entry — `"[coverlet.*]*"`.
   All other entries are now discovered dynamically.
3. Add a private method `BuildDynamicExcludeFilters(string testAssemblyName)` that:
   - Calls `IProcessAssemblyHelper.GetLoadedAssemblyNames(testAssemblyName)`.
   - Converts each name to a filter pattern via `ProcessAssemblyHelper.ToExcludeFilter(...)`.
   - Merges with `s_defaultExcludeFilters` (the baseline) and de-duplicates.
4. Modify `GetExcludeFilters()` — Priority 3 (defaults) now calls `BuildDynamicExcludeFilters`.

```csharp
// Priority 3: Dynamic defaults from process-loaded assemblies
string[] dynamicDefaults = BuildDynamicExcludeFilters(
    Path.GetFileNameWithoutExtension(_testAssemblyName));
LogOptionValue(CoverletOptionNames.Exclude, dynamicDefaults, isExplicit: false);
return dynamicDefaults;
```

#### Constructor signature change

```csharp
public CoverageConfiguration(
    ICommandLineOptions commandLineOptions,
    CoverletMTPSettings? configFileSettings,
    string? testAssemblyName,
    ILogger? logger = null,
    IProcessAssemblyHelper? processAssemblyHelper = null)
```

The `testAssemblyName` parameter is required so the test assembly itself is not added as an
exclude-filter. `CollectorExtension` already holds `_testModulePath` when it constructs
`CoverageConfiguration` — this is the correct handoff point.

---

### Step 5 — Update `CollectorExtension` call site

**File: `src/coverlet.MTP/Collector/CollectorExtension.cs`**

In `BeforeTestHostProcessStartAsync`, update the `CoverageConfiguration` construction:

```csharp
// Before
var config = new CoverageConfiguration(
    _commandLineOptions,
    configFileSettings,
    _loggerFactory.CreateLogger(nameof(CollectorExtension)));

// After
var config = new CoverageConfiguration(
    _commandLineOptions,
    configFileSettings,
    testAssemblyName: Path.GetFileNameWithoutExtension(_testModulePath),
    logger: _loggerFactory.CreateLogger(nameof(CollectorExtension)));
    // processAssemblyHelper: null → uses ProcessAssemblyHelper by default
```

---

### Step 6 — Configuration-file bypass (confirm existing behavior)

The existing priority logic in `GetExcludeFilters()` is preserved unchanged:

| Priority | Condition | Source | Dynamic list applied? |
|---|---|---|---|
| 1 | `--exclude` CLI flag set | CLI values merged with `[coverlet.*]*` baseline | No |
| 2 | Config file present (`IsFromConfigFile == true`) | Config file only | **No** |
| 3 | Neither | Dynamic loaded-assembly list | **Yes** |

This satisfies the requirement: _"If a configuration file is used, this list will not be
considered."_

---

### Step 7 — Update `CoverletMTPConstants.cs`

Reduce to a single permanent baseline constant; remove the long static list (which is now
discovered dynamically):

```csharp
// The coverlet assemblies themselves must always be excluded.
// All other test framework assemblies are dynamically discovered at runtime
// from the process's loaded assemblies (see ProcessAssemblyHelper).
public const string DefaultExcludeFilter = "[coverlet.*]*";
```

---

### Step 8 — Tests

#### `coverlet.core.tests` — `ProcessAssemblyHelperTests.cs`

| Test name | What it verifies |
|---|---|
| `WhenGetLoadedAssemblyNamesThenReturnsNonEmptyList` | Happy path: real loaded assemblies are returned |
| `WhenGetLoadedAssemblyNamesThenExcludesTestAssembly` | The test assembly simple name is not in the result |
| `WhenGetLoadedAssemblyNamesThenExcludesDynamicAssemblies` | No dynamic/in-memory assemblies included |
| `WhenGetLoadedAssemblyNamesThenNoDuplicates` | Result contains no duplicate names |
| `WhenTestAssemblyNameIsEmptyThenThrowsArgumentException` | Guard clause for empty string |
| `WhenTestAssemblyNameIsWhitespaceThenThrowsArgumentException` | Guard clause for whitespace string |
| `WhenToExcludeFilterCalledThenReturnsCorrectPattern` | `"xunit.core"` → `"[xunit.core]*"` |

#### `coverlet.MTP.tests` — `CoverageConfigurationDynamicTests.cs`

Uses `Mock<IProcessAssemblyHelper>` injected via the new constructor parameter to avoid real
`AppDomain` calls in unit tests.

| Test name | What it verifies |
|---|---|
| `WhenNoConfigFileThenDynamicFiltersApplied` | Priority 3: dynamic defaults returned |
| `WhenNoConfigFileThenBaselineFilterAlwaysPresent` | `[coverlet.*]*` is always included |
| `WhenConfigFilePresentThenDynamicFiltersNotApplied` | Priority 2 bypasses `IProcessAssemblyHelper` |
| `WhenCliExcludeFlagSetThenBaselineMergedNotDynamic` | Priority 1 merges CLI values with baseline only |
| `WhenDynamicHelperReturnsEmptyThenBaselineFallbackUsed` | Empty dynamic list still returns `[coverlet.*]*` |
| `WhenDynamicHelperReturnsAssemblyNamesThenFiltersHaveCorrectFormat` | Pattern format `[name]*` is correct |

---

## Summary of Benefits

| | Before | After |
|---|---|---|
| New framework support | Manual entry in `s_defaultExcludeFilters` | Automatic — if the assembly is loaded at test time |
| `ReportGenerator.Mtp*` issue | Requires manual filter addition | Resolved automatically |
| Config file users | Unaffected (Priority 2) | Unchanged |
| CLI `--exclude` users | Merged with static defaults | Merged with `[coverlet.*]*` baseline only |
| Testability | Static array — hard to unit test | `IProcessAssemblyHelper` is mockable |
| Maintenance | Every new framework = code change | Zero maintenance for common cases |
