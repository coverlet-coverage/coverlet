# Understanding Branch Coverage in Coverlet

This document explains how branch coverage works in coverlet, why certain patterns report more branches than expected, and how to interpret coverage results accurately.

## How Coverlet Measures Branch Coverage

Coverlet is an **IL-based** (Intermediate Language) code coverage tool. This means it analyzes the compiled bytecode rather than the original C# source code. This distinction is important because the C# compiler transforms source code into IL, and the resulting IL often contains more branches than are visible in the source.

### Key Concept: IL vs Source

When you write C# code, the compiler converts it to IL instructions. The IL representation often differs from the source:

| Source Code | IL Behavior |
| ------------- | ------------- |
| `if (condition)` | Generates a conditional branch instruction (`brfalse`/`brtrue`) |
| `&&` operator | Generates multiple conditional jumps for short-circuit evaluation |
| `\|\|` operator | Generates multiple conditional jumps for short-circuit evaluation |
| `??` operator | Generates null-check branches |
| `?.` operator | Generates null-check branches |

## Common "Unexpected" Branch Patterns

### 1. `if` Statement Without `else` (Issue #1786)

**What users expect:** An `if` without `else` has only one path.

**What coverlet reports:** Two branches.

**Example:**

```csharp
public void Example(bool condition)
{
    if (condition)  // Reports 2 branches
    {
        DoSomething();
    }
    // No else block
}
```

**Why this happens:**

At the IL level, the code compiles to:

```text
ldarg.1              // Load 'condition'
brfalse.s IL_000a    // Branch 1: If false, jump to continuation
call DoSomething     // Branch 0: Execute if true
IL_000a:             // Continuation point
ret
```

The `brfalse.s` instruction has two possible outcomes:

- **Path 0:** Condition is true → execute the `then` block
- **Path 1:** Condition is false → skip to continuation

**How to read the branch ordinals:**

- Branch ordinals are assigned from the compiled IL flow, not from the original C# syntax.
- For this `brfalse.s` pattern, **ordinal 0** is the fall-through path, so it maps to `condition == true`.
- For this same pattern, **ordinal 1** is the jump target, so it maps to `condition == false`.
- Ordinals should be treated as per-branch identifiers. Do not assume `0 == false` and `1 == true` in general; the mapping depends on the emitted IL instruction.
- Having 2 branches for an `if` without `else` is expected at the IL level. Issue #1786 was about **incorrect hit reporting** where both ordinals were incorrectly marked as covered even when only one execution path was taken.

**Fix (implemented):** Coverlet now uses a *trampoline* instrumentation strategy for taken-branch (jump) paths. When `brfalse.s` jumps to the continuation, execution passes through a dedicated trampoline block appended at the end of the method body, which records the hit exclusively for that jump path. Fall-through arrivals at the same continuation instruction no longer trigger the jump-path counter, eliminating the false-positive.

Both paths represent real execution flows that can be tested independently:

```csharp
[Fact]
public void Test_BothBranches()
{
    Example(true);   // Covers path 0 (fall-through into then block)
    Example(false);  // Covers path 1 (jump to continuation — skip block)
}
```

---

### 2. Short-Circuiting Operators (Issue #1751)

**What users expect:** `if (a && b)` has two branches (true/false).

**What coverlet reports:** Four or more branches.

**Example:**

```csharp
public void Example(bool a, bool b)
{
    if (a && b)  // Reports 4 branches
    {
        DoSomething();
    }
}
```

**Why this happens:**

The `&&` operator uses short-circuit evaluation. If `a` is false, `b` is never evaluated. The compiler generates IL like this:

```text
ldarg.1              // Load 'a'
brfalse.s IL_skip    // Branch 1: If a is false, skip to else/continuation
ldarg.2              // Load 'b'
brfalse.s IL_skip    // Branch 2: If b is false, skip to else/continuation
call DoSomething     // Execute if both true
IL_skip:
ret
```

This creates **four branch paths**:

| Path | `a` | `b` | Result |
| ------ | ----- | ----- | -------- |
| 0 | true | true | Execute `then` block |
| 1 | true | false | Skip (short-circuit on `b`) |
| 2 | false | (not evaluated) | Skip (short-circuit on `a`) |
| 3 | (implicit continuation) | | Continue after block |

**To achieve 100% branch coverage:**

```csharp
[Fact]
public void Test_AllBranches()
{
    Example(true, true);   // a=true, b=true → executes block
    Example(true, false);  // a=true, b=false → skips block
    Example(false, true);  // a=false → skips block (b not evaluated)
    Example(false, false); // a=false → skips block (b not evaluated)
}
```

> **Note:** The last two test cases hit the same branch for `a`, but you need at least three distinct test cases to cover all branches.

---

### 3. Compound Conditions

The branch count increases with each logical operator:

| Expression | Approximate Branch Count |
| ------------ | ------------------------- |
| `if (a)` | 2 |
| `if (a && b)` | 4 |
| `if (a && b && c)` | 6 |
| `if (a \|\| b)` | 4 |
| `if (a \|\| b \|\| c)` | 6 |
| `if ((a && b) \|\| c)` | 6 |

**Example with three conditions:**

```csharp
public void Example(bool a, bool b, bool c)
{
    if (a && b && c)  // Reports 6 branches
    {
        DoSomething();
    }
}
```

**Test cases needed for full coverage:**

```csharp
Example(true, true, true);    // All true → execute block
Example(true, true, false);   // c is false → skip
Example(true, false, true);   // b is false → skip (c not evaluated)
Example(false, true, true);   // a is false → skip (b, c not evaluated)
```

---

### 4. Null-Conditional Operators

**Example:**

```csharp
public int? Example(string? input)
{
    return input?.Length;  // Reports 2 branches
}
```

**Why this happens:**

The `?.` operator compiles to a null check:

```text
ldarg.1
brfalse.s IL_null    // If null, return null
call get_Length
box
ret
IL_null:
ldnull
ret
```

**Test cases for full coverage:**

```csharp
Example("hello");  // Non-null path
Example(null);     // Null path
```

---

### 5. Null-Coalescing Operator

**Example:**

```csharp
public string Example(string? input)
{
    return input ?? "default";  // Reports 2 branches
}
```

**Test cases for full coverage:**

```csharp
Example("value");  // input is not null
Example(null);     // input is null, returns "default"
```

---

### 6. Ternary Operator

**Example:**

```csharp
public string Example(bool condition)
{
    return condition ? "yes" : "no";  // Reports 2 branches
}
```

**Test cases for full coverage:**

```csharp
Example(true);   // Returns "yes"
Example(false);  // Returns "no"
```

---

### 7. Pattern Matching

**Example:**

```csharp

public string Example(object? obj)
{
    return obj switch  // Multiple branches based on patterns
    {
        int i => $"Integer: {i}",
        string s => $"String: {s}",
        null => "Null",
        _ => "Unknown"
    };
}
```

Each pattern generates branches. The exact count depends on the patterns and compiler optimizations.

---

## Compiler-Generated Branches

Coverlet automatically filters out many compiler-generated branches that users cannot directly test. These include:

| Pattern | Filtered By |
| --------- | ------------ |
| Async state machine branches | `SkipGeneratedBranchesForAsyncIterator` |
| `await foreach` disposal | `SkipGeneratedBranchesForAwaitForeach` |
| `await using` disposal | `SkipGeneratedBranchesForAwaitUsing` |
| Lambda caching | `SkipLambdaCachedField` |
| Exception filters | `SkipBranchGeneratedExceptionFilter` |
| Finally blocks | `SkipBranchGeneratedFinallyBlock` |

If you see unexpected branches in async code or LINQ expressions, they may be compiler-generated patterns that coverlet hasn't yet learned to filter. Please [report an issue](https://github.com/coverlet-coverage/coverlet/issues) with a minimal reproduction.

---

## FAQ

### Q: Why does my simple method show so many branches?

**A:** Count the logical operators and null-checks:

- Each `&&` adds ~2 branches
- Each `||` adds ~2 branches
- Each `?.` adds ~2 branches
- Each `??` adds ~2 branches
- Each `if` adds 2 branches

### Q: Is this a bug in coverlet?

**A:** For the patterns described above, no. This is how IL-based coverage tools work. The branches exist in the compiled code and represent real execution paths.

### Q: How can I achieve 100% branch coverage?

**A:** You need to write tests that exercise every possible path through your conditional logic:

1. For `if (a && b)`: Test with `(T,T)`, `(T,F)`, and `(F,*)`
2. For `if (a || b)`: Test with `(F,F)`, `(F,T)`, and `(T,*)`
3. For null checks: Test with both null and non-null values

### Q: Should I aim for 100% branch coverage?

**A:** It depends on your project requirements. Consider:

- **High-risk code** (financial, security, safety): Aim for high branch coverage
- **Business logic**: Focus on meaningful test scenarios
- **UI/Infrastructure code**: Line coverage may be sufficient

### Q: Can I exclude certain branches from coverage?

**A:** You can exclude entire methods or classes using attributes:

```csharp
[ExcludeFromCodeCoverage]
public void NotCovered() { }
```

For more granular control, see the [coverlet documentation](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/ExcludeFromCoverage.md).

---

## Related Issues

- [#1786 - False positive branch coverage for `if` without `else`](https://github.com/coverlet-coverage/coverlet/issues/1786)
- [#1751 - False positive due to short-circuiting operators](https://github.com/coverlet-coverage/coverlet/issues/1751)

---

## Summary

| Behavior | Explanation |
| ---------- | ------------- |
| `if` without `else` shows 2 branches | The IL has both "then" and "skip" paths |
| `&&` creates multiple branches | Short-circuit evaluation generates separate jumps |
| `\|\|` creates multiple branches | Short-circuit evaluation generates separate jumps |
| `?.` shows 2 branches | Null check creates a branch |
| `??` shows 2 branches | Null coalescing creates a branch |

**Key takeaway:** Coverlet reports IL-level branches, which reflect the actual execution paths in compiled code. This provides accurate coverage of all possible code paths, even those implicit in the source code.
