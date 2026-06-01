// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// AutoPropsWorkload.cs
// Purpose: exercises SkipInlineAssignedAutoProperty and SkipGeneratedBackingFieldAssignment
// during instrumentation. Contains types that trigger the backing-field enumeration path:
//  - Classes with many inline-initialised auto-properties (high field-scan cost per instruction)
//  - Records without primary constructors (issue-1633 scenario)
//  - Abstract record hierarchies
//  - Records with explicit property bodies that produce real branch points
// These types are intentionally NOT exercised at runtime in the benchmark; the benchmark
// measures only the instrumentation (PrepareModules) phase.

using System;
using System.Collections.Generic;

namespace coverlet.benchmark.subject
{
    // ── Classes with many inline-initialised auto-properties ─────────────────
    // Each property contributes one k__BackingField. The constructor body gains
    // one ldarg/stfld pair per property, so SkipGeneratedBackingFieldAssignment
    // is invoked O(props × ctor-instructions) times.

    /// <summary>Ten auto-properties initialised inline — baseline density.</summary>
    public class AutoProps10
    {
        public string Prop01 { get; set; } = "v01";
        public string Prop02 { get; set; } = "v02";
        public string Prop03 { get; set; } = "v03";
        public string Prop04 { get; set; } = "v04";
        public string Prop05 { get; set; } = "v05";
        public string Prop06 { get; set; } = "v06";
        public string Prop07 { get; set; } = "v07";
        public string Prop08 { get; set; } = "v08";
        public string Prop09 { get; set; } = "v09";
        public string Prop10 { get; set; } = "v10";
    }

    /// <summary>Twenty-five auto-properties — amplifies the O(N×M) scan.</summary>
    public class AutoProps25
    {
        public string P01 { get; set; } = "a";
        public string P02 { get; set; } = "b";
        public string P03 { get; set; } = "c";
        public string P04 { get; set; } = "d";
        public string P05 { get; set; } = "e";
        public string P06 { get; set; } = "f";
        public string P07 { get; set; } = "g";
        public string P08 { get; set; } = "h";
        public string P09 { get; set; } = "i";
        public string P10 { get; set; } = "j";
        public string P11 { get; set; } = "k";
        public string P12 { get; set; } = "l";
        public string P13 { get; set; } = "m";
        public string P14 { get; set; } = "n";
        public string P15 { get; set; } = "o";
        public string P16 { get; set; } = "p";
        public string P17 { get; set; } = "q";
        public string P18 { get; set; } = "r";
        public string P19 { get; set; } = "s";
        public string P20 { get; set; } = "t";
        public string P21 { get; set; } = "u";
        public string P22 { get; set; } = "v";
        public string P23 { get; set; } = "w";
        public string P24 { get; set; } = "x";
        public string P25 { get; set; } = "y";
    }

    // ── Records without primary constructors (issue-1633) ────────────────────
    // These are the exact patterns that the PR fixes. The compiler emits a
    // parameterless .ctor() that contains only ldarg.0 / call System.Object::.ctor() / ret.
    // The removed SkipDefaultInitializationSystemObject used to short-circuit this pattern;
    // now each ldarg instruction falls through to SkipGeneratedBackingFieldAssignment.

    /// <summary>Record without primary-constructor parens — the issue-1633 trigger.</summary>
    public record RecordNoCtor
    {
        public string Name { get; init; } = string.Empty;
        public int Age { get; init; }
        public bool IsActive { get; init; } = true;

        public string Display() => $"{Name}/{Age}/{IsActive}";
    }

    /// <summary>Record with empty primary constructor — the user workaround.</summary>
    public record RecordEmptyCtor()
    {
        public string Name { get; init; } = string.Empty;
        public int Age { get; init; }
        public bool IsActive { get; init; } = true;

        public string Display() => $"{Name}/{Age}/{IsActive}";
    }

    /// <summary>Record with primary constructor parameters and additional properties.</summary>
    public record RecordWithPrimaryCtor(string Name, int Age)
    {
        public bool IsActive { get; init; } = true;
        public string Category { get; init; } = "default";

        public string Display() => $"{Name}/{Age}/{IsActive}/{Category}";
    }

    // ── Abstract record hierarchies ──────────────────────────────────────────

    public abstract record AbstractShapeRecord
    {
        public string Color { get; init; } = "white";
        public abstract double Area();
    }

    public abstract record AbstractShapeWithCtor()
    {
        public string Color { get; init; } = "white";
        public abstract double Area();
    }

    public record CircleRecord : AbstractShapeRecord
    {
        public double Radius { get; init; }
        public override double Area() => Math.PI * Radius * Radius;
    }

    public record RectangleRecord : AbstractShapeWithCtor
    {
        public double Width { get; init; }
        public double Height { get; init; }
        public override double Area() => Width * Height;
    }

    // ── Records with branches (triggers branch-point instrumentation path) ───
    // These records produce real branch-points in their methods so that the
    // benchmark covers both SkipAutoProps logic AND the branch-instrumentation path.

    public record OrderRecord(string Id, decimal Amount, string Status)
    {
        public bool IsValid => !string.IsNullOrWhiteSpace(Id) && Amount > 0;

        public string Classify() => Amount switch
        {
            <= 0 => "invalid",
            < 100 => "small",
            < 1_000 => "medium",
            < 10_000 => "large",
            _ => "enterprise",
        };

        public string Describe()
        {
            if (!IsValid)
                return "invalid order";

            string tier = Classify();
            return Status switch
            {
                "pending" => $"{tier}: awaiting payment",
                "paid" => $"{tier}: processing",
                "shipped" => $"{tier}: in transit",
                "delivered" => $"{tier}: complete",
                _ => $"{tier}: unknown status",
            };
        }
    }

    public record ProductRecord(string Sku, string Name, decimal Price, int Stock)
    {
        public bool InStock => Stock > 0;
        public bool IsAffordable => Price < 50m;

        public string StockStatus()
        {
            if (Stock <= 0)
                return "out-of-stock";
            if (Stock < 5)
                return "low-stock";
            if (Stock < 20)
                return "limited";
            return "available";
        }

        public string PriceTier() => Price switch
        {
            < 10m => "budget",
            < 50m => "value",
            < 200m => "standard",
            < 500m => "premium",
            _ => "luxury",
        };
    }

    // ── Mixed class/record scenario ──────────────────────────────────────────
    // A class that owns record-typed auto-properties; used to verify that
    // SkipGeneratedBackingFieldAssignment correctly handles field references
    // to record types and doesn't misclassify them.

    public class RecordAggregator
    {
        public RecordNoCtor? NoCtor { get; set; }
        public RecordEmptyCtor? EmptyCtor { get; set; }
        public RecordWithPrimaryCtor? WithCtor { get; set; }

        public string Summary()
        {
            if (NoCtor is null && EmptyCtor is null && WithCtor is null)
                return "empty";

            var parts = new List<string>(3);
            if (NoCtor is not null)
                parts.Add(NoCtor.Display());
            if (EmptyCtor is not null)
                parts.Add(EmptyCtor.Display());
            if (WithCtor is not null)
                parts.Add(WithCtor.Display());

            return string.Join(", ", parts);
        }
    }
}
