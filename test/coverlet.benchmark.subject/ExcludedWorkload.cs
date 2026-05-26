// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ExcludedWorkload.cs
// Purpose: exercises the IsTypeExcluded / IsMethodExcluded fast-path caches (P2)
// by providing types and methods with [ExcludeFromCoverage] / [ExcludeFromCodeCoverage]
// at every granularity: assembly, class, method, and property.
// Neighbouring non-excluded types ensure the caches see real miss/hit alternation.

using System;
using System.Diagnostics.CodeAnalysis;

// ── assembly-level exclusion attribute (applied to one class via attribute target) ──────
// (assembly-level [ExcludeFromCodeCoverage] is not used here because it would exclude the
//  entire subject; instead individual types carry the attribute.)

namespace coverlet.benchmark.subject
{
    // ── fully excluded class ─────────────────────────────────────────────────

    /// <summary>Entire class is excluded from coverage; exercises IsTypeExcluded cache.</summary>
    [ExcludeFromCodeCoverage(Justification = "Infrastructure helper, not under test")]
    public class FullyExcludedClass
    {
        public int Compute(int a, int b) => a + b;
        public string Format(int v) => v.ToString();
        public bool IsValid(int v) => v > 0;

        public int ComplexMethod(int x)
        {
            if (x < 0) return -1;
            int result = 0;
            for (int i = 0; i < x; i++)
            {
                result += i % 2 == 0 ? i : -i;
            }
            return result;
        }
    }

    // ── partially excluded class: some methods excluded, others not ──────────

    /// <summary>
    /// Only some methods carry [ExcludeFromCodeCoverage];
    /// exercises the method-level exclusion cache within a covered type.
    /// </summary>
    public class PartiallyExcludedClass
    {
        // covered
        public int Add(int a, int b) => a + b;

        // excluded at method level
        [ExcludeFromCodeCoverage(Justification = "Auto-generated hash")]
        public override int GetHashCode() => HashCode.Combine(42);

        // covered
        public int Multiply(int a, int b)
        {
            if (a == 0 || b == 0) return 0;
            return a * b;
        }

        // excluded at method level
        [ExcludeFromCodeCoverage(Justification = "Debug-only diagnostic")]
        public string Dump() => $"PartiallyExcludedClass({Add(1, 2)})";

        // covered
        public bool IsPositive(int v) => v > 0;

        // excluded
        [ExcludeFromCodeCoverage]
        public string DiagnosticInfo() => "diagnostic";

        // covered
        public int Subtract(int a, int b) => a - b;
    }

    // ── excluded property accessors ──────────────────────────────────────────

    public class PropertyExclusionClass
    {
        public int Value
        {
            // getter covered
            get;

            // setter excluded
            [ExcludeFromCodeCoverage(Justification = "Setter not reachable in tests")]
            set;
        }

        // fully excluded auto-property (attribute on property → covers both accessors)
        [ExcludeFromCodeCoverage]
        public string? Tag { get; set; }

        public int Compute() => Value * 2;
    }

    // ── fully excluded nested class ───────────────────────────────────────────

    public class OuterClass
    {
        public int CoveredMethod(int x) => x + 1;

        [ExcludeFromCodeCoverage]
        public class ExcludedNestedClass
        {
            public void Run() { }
            public int Calc(int v) => v;
        }

        public class CoveredNestedClass
        {
            public void Run() { }
            public int Calc(int v) => v * 2;
        }
    }

    // ── excluded generic class ────────────────────────────────────────────────

    [ExcludeFromCodeCoverage(Justification = "Excluded generic type")]
    public class ExcludedGeneric<T>
    {
        private T _value;
        public ExcludedGeneric(T value) { _value = value; }
        public T Get() => _value;
        public void Set(T v) { _value = v; }
    }

    // ── covered class alongside excluded sibling (cache alternation) ─────────

    public class CoveredSiblingA
    {
        public int Method1(int x) => x + 10;
        public int Method2(int x) => x * 10;
        public bool Check(int x) => x > 0;
    }

    [ExcludeFromCodeCoverage]
    public class ExcludedSiblingB
    {
        public int Method1(int x) => x + 10;
        public int Method2(int x) => x * 10;
    }

    public class CoveredSiblingC
    {
        public int Method1(int x) => x - 10;
        public bool Check(int x) => x < 100;
    }

    [ExcludeFromCodeCoverage]
    public class ExcludedSiblingD
    {
        public void Run() { }
    }

    public class CoveredSiblingE
    {
        public string Label(int x) => $"E:{x}";
    }

    // ── exclusion via custom attribute name (exercises attribute-name matching) ─

    // A custom attribute that carries the conventional coverage-exclusion name.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ExcludeFromCoverageAttribute : Attribute
    {
        public string? Reason { get; }
        public ExcludeFromCoverageAttribute(string? reason = null) { Reason = reason; }
    }

    [ExcludeFromCoverage(reason: "Uses Coverlet custom attribute")]
    public class CustomAttributeExcludedClass
    {
        public int Value => 42;
        public string Name => "custom-excluded";
    }

    public class MethodLevelCustomExclusion
    {
        public int Covered(int x) => x + 1;

        [ExcludeFromCoverage(reason: "Not testable in unit context")]
        public void ExcludedMethod() { }

        public bool AlsoCovered(int x) => x > 0;
    }

    // ── excluded abstract / interface implementors ───────────────────────────

    public interface IWorkItem
    {
        void Execute();
        int Priority { get; }
    }

    [ExcludeFromCodeCoverage]
    public class ExcludedWorkItem : IWorkItem
    {
        public void Execute() { }
        public int Priority => 0;
    }

    public class CoveredWorkItem : IWorkItem
    {
        public CoveredWorkItem(int priority) { Priority = priority; }
        public void Execute() { }
        public int Priority { get; }
    }

    // ── excluded static class ────────────────────────────────────────────────

    [ExcludeFromCodeCoverage]
    public static class ExcludedHelpers
    {
        public static int Square(int x) => x * x;
        public static bool IsEven(int x) => x % 2 == 0;
    }
}
