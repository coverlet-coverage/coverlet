// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// DeepNestingWorkload.cs
// Purpose: exercises ComputeIsTypeExcluded's ancestor walk and the type-name reconstruction
// loop in InstrumentationHelper by producing types with 5+ nesting levels.  A mix of
// excluded outer / non-excluded inner types and vice-versa ensures the ancestor walk
// cannot short-circuit on the first level.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace coverlet.benchmark.subject
{
    // ── 5-level deep nesting (all covered) ───────────────────────────────────

    /// <summary>Root of a 5-level nesting hierarchy; all levels are covered.</summary>
    public class Level1
    {
        public int Compute(int x) => x + 1;

        public class Level2
        {
            public int Compute(int x) => x + 2;

            public class Level3
            {
                public int Compute(int x) => x + 3;

                public class Level4
                {
                    public int Compute(int x) => x + 4;

                    public class Level5
                    {
                        public int Compute(int x) => x + 5;
                        public bool IsPositive(int x) => x > 0;
                        public string Label(int x) => $"L5:{x}";
                    }

                    public Level5 Inner { get; } = new Level5();
                    public bool IsNonNegative(int x) => x >= 0;
                }

                public Level4 Inner { get; } = new Level4();
                public string Label(int x) => $"L3:{x}";
            }

            public Level3 Inner { get; } = new Level3();
            public string Label(int x) => $"L2:{x}";
        }

        public Level2 Inner { get; } = new Level2();
        public string Label(int x) => $"L1:{x}";
    }

    // ── 6-level deep nesting with excluded outer ─────────────────────────────

    /// <summary>
    /// Outer class is excluded; inner classes are NOT – the ancestor walk must traverse
    /// the excluded parent and still decide correctly for each inner type.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Outer shell is infrastructure")]
    public class ExcludedOuter
    {
        public int ShellMethod(int x) => x;

        public class Inner1
        {
            public int Method(int x) => x + 10;

            public class Inner2
            {
                public int Method(int x) => x + 20;

                public class Inner3
                {
                    public int Method(int x) => x + 30;

                    public class Inner4
                    {
                        public int Method(int x) => x + 40;
                    }
                }
            }
        }
    }

    // ── excluded inner inside covered outer ──────────────────────────────────

    public class CoveredOuter
    {
        public int TopMethod(int x) => x * 2;

        public class CoveredInner1
        {
            public int Method(int x) => x + 1;

            [ExcludeFromCodeCoverage(Justification = "Generated code")]
            public class ExcludedInner2
            {
                public int Method(int x) => x + 2;

                public class ExcludedInner3   // inherits exclusion through ancestor
                {
                    public int Method(int x) => x + 3;

                    public class ExcludedInner4
                    {
                        public int Method(int x) => x + 4;
                    }
                }
            }

            public class CoveredSiblingInner2
            {
                public int Method(int x) => x - 1;
                public bool Check(int x) => x > 0;
            }
        }

        public class CoveredInner1B
        {
            public int Method(int x) => x + 100;
            public string Label(int x) => $"1B:{x}";
        }
    }

    // ── generic nested types ─────────────────────────────────────────────────

    public class GenericOuter<TOuter>
    {
        private readonly TOuter _value;
        public GenericOuter(TOuter value) { _value = value; }

        public TOuter Get() => _value;

        public class GenericInner<TInner>
        {
            private readonly TInner _inner;
            public GenericInner(TInner inner) { _inner = inner; }
            public TInner Get() => _inner;

            public class DeepestGeneric<TDeep>
            {
                private readonly TDeep _deep;
                public DeepestGeneric(TDeep deep) { _deep = deep; }
                public TDeep Get() => _deep;
                public string Label() => $"{typeof(TDeep).Name}:{_deep}";
            }
        }
    }

    // ── static nested types ──────────────────────────────────────────────────

    public class ContainerWithStaticNested
    {
        public int InstanceMethod(int x) => x + 1;

        public static class Config
        {
            public static int DefaultValue => 42;
            public static string DefaultName => "default";

            public static class Advanced
            {
                public static int MaxRetries => 3;
                public static TimeSpan Timeout => TimeSpan.FromSeconds(30);

                public static class Debug
                {
                    public static bool Enabled => false;
                    public static string LogLevel => "Warning";
                }
            }
        }

        public static int GetDefault() => Config.DefaultValue;
        public static int GetMaxRetries() => Config.Advanced.MaxRetries;
    }

    // ── interfaces declared inside nested classes ────────────────────────────

    public class ModuleHost
    {
        public interface IPlugin
        {
            string Name { get; }
            void Initialize();
        }

        public class DefaultPlugin : IPlugin
        {
            public string Name => "Default";
            public void Initialize() { }
        }

        public class AdvancedPlugin : IPlugin
        {
            public AdvancedPlugin(string name) { Name = name; }
            public string Name { get; }
            public void Initialize() { }

            public class SubComponent
            {
                public bool IsReady => true;
                public string Status => "ok";
            }
        }

        private readonly List<IPlugin> _plugins = [];
        public void Register(IPlugin plugin) => _plugins.Add(plugin);
        public IReadOnlyList<IPlugin> Plugins => _plugins;
    }

    // ── 7-level nesting (max depth test) ────────────────────────────────────

    public class Deep7
    {
        public class L2 { public class L3 { public class L4 { public class L5 { public class L6 { public class L7
        {
            public int Value => 7;
            public bool IsMax(int x) => x == 7;
            public string Label => "deepest";
        } } } } } }

        public int Depth => 7;
    }
}
