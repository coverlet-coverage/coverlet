using System;
using System.Collections.Generic;
using System.Linq;

using Coverlet.Core.Enums;
using Coverlet.Core.Instrumentation;

namespace Coverlet.Core.ObjectModel
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

        public ThresholdTypeFlags GetThresholdTypesBelowThreshold(CoverageSummary summary, double threshold, ThresholdTypeFlags thresholdTypes, ThresholdStatistic thresholdStat)
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
                        double line = summary.CalculateLineCoverage(Modules).AverageModulePercent;
                        double branch = summary.CalculateBranchCoverage(Modules).AverageModulePercent;
                        double method = summary.CalculateMethodCoverage(Modules).AverageModulePercent;

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
                case ThresholdStatistic.Total:
                    {
                        double line = summary.CalculateLineCoverage(Modules).Percent;
                        double branch = summary.CalculateBranchCoverage(Modules).Percent;
                        double method = summary.CalculateMethodCoverage(Modules).Percent;

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

    public class CoverageSummary
    {
        public CoverageDetails CalculateLineCoverage(Lines lines)
        {
            var details = new CoverageDetails();
            details.Covered = lines.Where(l => l.Value > 0).Count();
            details.Total = lines.Count;
            return details;
        }

        public CoverageDetails CalculateLineCoverage(Methods methods)
        {
            var details = new CoverageDetails();
            foreach (var method in methods)
            {
                var methodCoverage = CalculateLineCoverage(method.Value.Lines);
                details.Covered += methodCoverage.Covered;
                details.Total += methodCoverage.Total;
            }
            return details;
        }

        public CoverageDetails CalculateLineCoverage(Classes classes)
        {
            var details = new CoverageDetails();
            foreach (var @class in classes)
            {
                var classCoverage = CalculateLineCoverage(@class.Value);
                details.Covered += classCoverage.Covered;
                details.Total += classCoverage.Total;
            }
            return details;
        }

        public CoverageDetails CalculateLineCoverage(Documents documents)
        {
            var details = new CoverageDetails();
            foreach (var document in documents)
            {
                var documentCoverage = CalculateLineCoverage(document.Value);
                details.Covered += documentCoverage.Covered;
                details.Total += documentCoverage.Total;
            }
            return details;
        }

        public CoverageDetails CalculateLineCoverage(Modules modules)
        {
            var details = new CoverageDetails();
            var accumPercent = 0.0D;
            foreach (var module in modules)
            {
                var moduleCoverage = CalculateLineCoverage(module.Value);
                details.Covered += moduleCoverage.Covered;
                details.Total += moduleCoverage.Total;
                accumPercent += moduleCoverage.Percent;
            }
            details.AverageModulePercent = accumPercent / modules.Count;
            return details;
        }

        public CoverageDetails CalculateBranchCoverage(IList<BranchInfo> branches)
        {
            var details = new CoverageDetails();
            details.Covered = branches.Count(bi => bi.Hits > 0);
            details.Total = branches.Count;
            return details;
        }

        public int CalculateCyclomaticComplexity(IList<BranchInfo> branches)
        {
            return Math.Max(1, branches.Count);
        }

        public int CalculateCyclomaticComplexity(Methods methods)
        {
            return methods.Values.Select(m => CalculateCyclomaticComplexity(m.Branches)).Sum();
        }

        public int CalculateMaxCyclomaticComplexity(Methods methods)
        {
            return methods.Values.Select(m => CalculateCyclomaticComplexity(m.Branches)).DefaultIfEmpty(1).Max();
        }

        public int CalculateMinCyclomaticComplexity(Methods methods)
        {
            return methods.Values.Select(m => CalculateCyclomaticComplexity(m.Branches)).DefaultIfEmpty(1).Min();
        }

        public int CalculateCyclomaticComplexity(Modules modules)
        {
            return modules.Values.Select(CalculateCyclomaticComplexity).Sum();
        }

        public int CalculateMaxCyclomaticComplexity(Modules modules)
        {
            return modules.Values.Select(CalculateCyclomaticComplexity).DefaultIfEmpty(1).Max();
        }

        public int CalculateMinCyclomaticComplexity(Modules modules)
        {
            return modules.Values.Select(CalculateCyclomaticComplexity).DefaultIfEmpty(1).Min();
        }

        public int CalculateCyclomaticComplexity(Documents documents)
        {
            return documents.Values.SelectMany(c => c.Values.Select(CalculateCyclomaticComplexity)).Sum();
        }

        public CoverageDetails CalculateBranchCoverage(Methods methods)
        {
            var details = new CoverageDetails();
            foreach (var method in methods)
            {
                var methodCoverage = CalculateBranchCoverage(method.Value.Branches);
                details.Covered += methodCoverage.Covered;
                details.Total += methodCoverage.Total;
            }
            return details;
        }

        public CoverageDetails CalculateBranchCoverage(Classes classes)
        {
            var details = new CoverageDetails();
            foreach (var @class in classes)
            {
                var classCoverage = CalculateBranchCoverage(@class.Value);
                details.Covered += classCoverage.Covered;
                details.Total += classCoverage.Total;
            }
            return details;
        }

        public CoverageDetails CalculateBranchCoverage(Documents documents)
        {
            var details = new CoverageDetails();
            foreach (var document in documents)
            {
                var documentCoverage = CalculateBranchCoverage(document.Value);
                details.Covered += documentCoverage.Covered;
                details.Total += documentCoverage.Total;
            }
            return details;
        }

        public CoverageDetails CalculateBranchCoverage(Modules modules)
        {
            var details = new CoverageDetails();
            var accumPercent = 0.0D;
            foreach (var module in modules)
            {
                var moduleCoverage = CalculateBranchCoverage(module.Value);
                details.Covered += moduleCoverage.Covered;
                details.Total += moduleCoverage.Total;
                accumPercent += moduleCoverage.Percent;
            }
            details.AverageModulePercent = accumPercent / modules.Count;
            return details;
        }

        public CoverageDetails CalculateMethodCoverage(Lines lines)
        {
            var details = new CoverageDetails();
            details.Covered = lines.Any(l => l.Value > 0) ? 1 : 0;
            details.Total = 1;
            return details;
        }

        public CoverageDetails CalculateMethodCoverage(Methods methods)
        {
            var details = new CoverageDetails();
            var methodsWithLines = methods.Where(m => m.Value.Lines.Count > 0);
            foreach (var method in methodsWithLines)
            {
                var methodCoverage = CalculateMethodCoverage(method.Value.Lines);
                details.Covered += methodCoverage.Covered;
            }
            details.Total = methodsWithLines.Count();
            return details;
        }

        public CoverageDetails CalculateMethodCoverage(Classes classes)
        {
            var details = new CoverageDetails();
            foreach (var @class in classes)
            {
                var classCoverage = CalculateMethodCoverage(@class.Value);
                details.Covered += classCoverage.Covered;
                details.Total += classCoverage.Total;
            }
            return details;
        }

        public CoverageDetails CalculateMethodCoverage(Documents documents)
        {
            var details = new CoverageDetails();
            foreach (var document in documents)
            {
                var documentCoverage = CalculateMethodCoverage(document.Value);
                details.Covered += documentCoverage.Covered;
                details.Total += documentCoverage.Total;
            }
            return details;
        }

        public CoverageDetails CalculateMethodCoverage(Modules modules)
        {
            var details = new CoverageDetails();
            var accumPercent = 0.0D;
            foreach (var module in modules)
            {
                var moduleCoverage = CalculateMethodCoverage(module.Value);
                details.Covered += moduleCoverage.Covered;
                details.Total += moduleCoverage.Total;
                accumPercent += moduleCoverage.Percent;
            }
            details.AverageModulePercent = accumPercent / modules.Count;
            return details;
        }
    }

    public class CoverageDetails
    {
        private double _averageModulePercent;
        public double Covered { get; internal set; }
        public int Total { get; internal set; }
        public double AverageModulePercent
        {
            get { return Math.Floor(_averageModulePercent * 100) / 100; }
            internal set { _averageModulePercent = value; }
        }

        public double Percent => Total == 0 ? 100D : Math.Floor((Covered / Total) * 10000) / 100;
    }
}