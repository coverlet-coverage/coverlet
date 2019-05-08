using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Enums;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Symbols;

namespace Coverlet.Core
{
    public class BranchInfo
    {
        public int Line { get; set; }
        public int Offset { get; set; }
        public int EndOffset { get; set; }
        public int Path { get; set; }
        public uint Ordinal { get; set; }
        public int Hits { get; set; }
    }

    public class Lines : SortedDictionary<int, int> { }

    public class Branches : List<BranchInfo> { }

    public class Method
    {
        internal Method()
        {
            Lines = new Lines();
            Branches = new Branches();
        }
        public Lines Lines;
        public Branches Branches;
    }
    public class Methods : Dictionary<string, Method> { }
    public class Classes : Dictionary<string, Methods> { }
    public class Documents : Dictionary<string, Classes> { }
    public class Modules : Dictionary<string, Documents> { }

    public class CoverageResult
    {
        public string Identifier;
        public Modules Modules;
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

        public ThresholdTypeFlags GetThresholdTypesBelowThreshold(CoverageSummary summary, double threshold, ThresholdTypeFlags thresholdTypes, ThresholdStatistic thresholdStat)
        {
            var thresholdTypeFlags = ThresholdTypeFlags.None;
            switch (thresholdStat)
            {
                case ThresholdStatistic.Minimum:
                    {
                        foreach (var module in Modules)
                        {
                            var line = summary.CalculateLineCoverage(module.Value).Percent * 100;
                            var branch = summary.CalculateBranchCoverage(module.Value).Percent * 100;
                            var method = summary.CalculateMethodCoverage(module.Value).Percent * 100;

                            if ((thresholdTypes & ThresholdTypeFlags.Line) != ThresholdTypeFlags.None)
                            {
                                if (line < threshold)
                                    thresholdTypeFlags |= ThresholdTypeFlags.Line;
                            }

                            if ((thresholdTypes & ThresholdTypeFlags.Branch) != ThresholdTypeFlags.None)
                            {
                                if (branch < threshold)
                                    thresholdTypeFlags |= ThresholdTypeFlags.Branch;
                            }

                            if ((thresholdTypes & ThresholdTypeFlags.Method) != ThresholdTypeFlags.None)
                            {
                                if (method < threshold)
                                    thresholdTypeFlags |= ThresholdTypeFlags.Method;
                            }
                        }
                    }
                    break;
                case ThresholdStatistic.Average:
                    {
                        double line = 0;
                        double branch = 0;
                        double method = 0;
                        int numModules = Modules.Count;

                        foreach (var module in Modules)
                        {
                            line += summary.CalculateLineCoverage(module.Value).Percent * 100;
                            branch += summary.CalculateBranchCoverage(module.Value).Percent * 100;
                            method += summary.CalculateMethodCoverage(module.Value).Percent * 100;
                        }

                        if ((thresholdTypes & ThresholdTypeFlags.Line) != ThresholdTypeFlags.None)
                        {
                            if ((line / numModules) < threshold)
                                thresholdTypeFlags |= ThresholdTypeFlags.Line;
                        }

                        if ((thresholdTypes & ThresholdTypeFlags.Branch) != ThresholdTypeFlags.None)
                        {
                            if ((branch / numModules) < threshold)
                                thresholdTypeFlags |= ThresholdTypeFlags.Branch;
                        }

                        if ((thresholdTypes & ThresholdTypeFlags.Method) != ThresholdTypeFlags.None)
                        {
                            if ((method / numModules) < threshold)
                                thresholdTypeFlags |= ThresholdTypeFlags.Method;
                        }
                    }
                    break;
                case ThresholdStatistic.Total:
                    {
                        var line = summary.CalculateLineCoverage(Modules).Percent * 100;
                        var branch = summary.CalculateBranchCoverage(Modules).Percent * 100;
                        var method = summary.CalculateMethodCoverage(Modules).Percent * 100;

                        if ((thresholdTypes & ThresholdTypeFlags.Line) != ThresholdTypeFlags.None)
                        {
                            if (line < threshold)
                                thresholdTypeFlags |= ThresholdTypeFlags.Line;
                        }

                        if ((thresholdTypes & ThresholdTypeFlags.Branch) != ThresholdTypeFlags.None)
                        {
                            if (branch < threshold)
                                thresholdTypeFlags |= ThresholdTypeFlags.Branch;
                        }

                        if ((thresholdTypes & ThresholdTypeFlags.Method) != ThresholdTypeFlags.None)
                        {
                            if (method < threshold)
                                thresholdTypeFlags |= ThresholdTypeFlags.Method;
                        }
                    }
                    break;
            }

            return thresholdTypeFlags;
        }
    }
}