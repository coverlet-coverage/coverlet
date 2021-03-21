using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Enums;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Symbols;

namespace Coverlet.Core
{
    internal class BranchInfo
    {
        public int Line { get; set; }
        public int Offset { get; set; }
        public int EndOffset { get; set; }
        public int Path { get; set; }
        public uint Ordinal { get; set; }
        public int Hits { get; set; }
    }

    internal class Lines : SortedDictionary<int, int> { }

    internal class Branches : List<BranchInfo> { }

    internal class Method
    {
        internal Method()
        {
            Lines = new Lines();
            Branches = new Branches();
        }
        public Lines Lines;
        public Branches Branches;
    }
    internal class Methods : Dictionary<string, Method> { }
    internal class Classes : Dictionary<string, Methods> { }
    internal class Documents : Dictionary<string, Classes> { }
    internal class Modules : Dictionary<string, Documents> { }

    internal class CoverageResult
    {
        public string Identifier;
        public Modules Modules;
        public bool UseSourceLink;
        internal List<InstrumenterResult> InstrumentedResults;

        internal CoverageResult() { }

        internal void Merge(Modules modules)
        {
            foreach (var module in modules)
            {
                if (!this.Modules.Keys.Contains(module.Key))
                {
                    this.Modules.Add(module.Key, module.Value);
                }
                else
                {
                    foreach (var document in module.Value)
                    {
                        if (!this.Modules[module.Key].ContainsKey(document.Key))
                        {
                            this.Modules[module.Key].Add(document.Key, document.Value);
                        }
                        else
                        {
                            foreach (var @class in document.Value)
                            {
                                if (!this.Modules[module.Key][document.Key].ContainsKey(@class.Key))
                                {
                                    this.Modules[module.Key][document.Key].Add(@class.Key, @class.Value);
                                }
                                else
                                {
                                    foreach (var method in @class.Value)
                                    {
                                        if (!this.Modules[module.Key][document.Key][@class.Key].ContainsKey(method.Key))
                                        {
                                            this.Modules[module.Key][document.Key][@class.Key].Add(method.Key, method.Value);
                                        }
                                        else
                                        {
                                            foreach (var line in method.Value.Lines)
                                            {
                                                if (!this.Modules[module.Key][document.Key][@class.Key][method.Key].Lines.ContainsKey(line.Key))
                                                {
                                                    this.Modules[module.Key][document.Key][@class.Key][method.Key].Lines.Add(line.Key, line.Value);
                                                }
                                                else
                                                {
                                                    this.Modules[module.Key][document.Key][@class.Key][method.Key].Lines[line.Key] += line.Value;
                                                }
                                            }

                                            foreach (var branch in method.Value.Branches)
                                            {
                                                var branches = this.Modules[module.Key][document.Key][@class.Key][method.Key].Branches;
                                                var branchInfo = branches.FirstOrDefault(b => b.EndOffset == branch.EndOffset && b.Line == branch.Line && b.Offset == branch.Offset && b.Ordinal == branch.Ordinal && b.Path == branch.Path);
                                                if (branchInfo == null)
                                                    branches.Add(branch);
                                                else
                                                    branchInfo.Hits += branch.Hits;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public ThresholdTypeFlags GetThresholdTypesBelowThreshold(CoverageSummary summary, Dictionary<ThresholdTypeFlags, double> thresholdTypeFlagValues, ThresholdStatistic thresholdStat)
        {
            var thresholdTypeFlags = ThresholdTypeFlags.None;
            switch (thresholdStat)
            {
                case ThresholdStatistic.Minimum:
                    {
                        foreach (var module in Modules)
                        {
                            double line = summary.CalculateLineCoverage(module.Value).Percent;
                            double branch = summary.CalculateBranchCoverage(module.Value).Percent;
                            double method = summary.CalculateMethodCoverage(module.Value).Percent;
                            
                            if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Line, out var lineThresholdValue) && lineThresholdValue > line)
                            {
                                thresholdTypeFlags |= ThresholdTypeFlags.Line;
                            }

                            if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Branch, out var branchThresholdValue) && branchThresholdValue > branch)
                            {
                                thresholdTypeFlags |= ThresholdTypeFlags.Branch;
                            }

                            if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Method, out var methodThresholdValue) && methodThresholdValue > method)
                            {
                                thresholdTypeFlags |= ThresholdTypeFlags.Method;
                            }
                        }
                    }
                    break;
                case ThresholdStatistic.Average:
                    {
                        double line = summary.CalculateLineCoverage(Modules).AverageModulePercent;
                        double branch = summary.CalculateBranchCoverage(Modules).AverageModulePercent;
                        double method = summary.CalculateMethodCoverage(Modules).AverageModulePercent;
                        
                        if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Line, out var lineThresholdValue) && lineThresholdValue > line)
                        {
                            thresholdTypeFlags |= ThresholdTypeFlags.Line;
                        }

                        if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Branch, out var branchThresholdValue) && branchThresholdValue > branch)
                        {
                            thresholdTypeFlags |= ThresholdTypeFlags.Branch;
                        }

                        if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Method, out var methodThresholdValue) && methodThresholdValue > method)
                        {
                            thresholdTypeFlags |= ThresholdTypeFlags.Method;
                        }
                    }
                    break;
                case ThresholdStatistic.Total:
                    {
                        double line = summary.CalculateLineCoverage(Modules).Percent;
                        double branch = summary.CalculateBranchCoverage(Modules).Percent;
                        double method = summary.CalculateMethodCoverage(Modules).Percent;

                        if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Line, out var lineThresholdValue) && lineThresholdValue > line)
                        {
                            thresholdTypeFlags |= ThresholdTypeFlags.Line;
                        }

                        if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Branch, out var branchThresholdValue) && branchThresholdValue > branch)
                        {
                            thresholdTypeFlags |= ThresholdTypeFlags.Branch;
                        }

                        if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Method, out var methodThresholdValue) && methodThresholdValue > method)
                        {
                            thresholdTypeFlags |= ThresholdTypeFlags.Method;
                        }
                    }
                    break;
            }

            return thresholdTypeFlags;
        }
    }
}