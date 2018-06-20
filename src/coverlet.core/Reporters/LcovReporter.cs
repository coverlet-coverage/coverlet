using System;
using System.Linq;
using System.Collections.Generic;

namespace Coverlet.Core.Reporters
{
    public class LcovReporter : IReporter
    {
        public string Format => "lcov";

        public string Extension => "info";

        public string Report(CoverageResult result)
        {
            CoverageSummary summary = new CoverageSummary();
            List<string> lcov = new List<string>();

            foreach (var module in result.Modules)
            {
                foreach (var doc in module.Value)
                {
                    var docLineCoverage = summary.CalculateLineCoverage(doc.Value);
                    var docBranchCoverage = summary.CalculateBranchCoverage(doc.Value);
                    var docMethodCoverage = summary.CalculateMethodCoverage(doc.Value);

                    lcov.Add("SF:" + doc.Key);
                    foreach (var @class in doc.Value)
                    {
                        foreach (var method in @class.Value)
                        {
                            // Skip all methods with no lines
                            if (method.Value.Lines.Count == 0)
                                continue;

                            lcov.Add($"FN:{method.Value.Lines.First().Key - 1},{method.Key}");
                            lcov.Add($"FNDA:{method.Value.Lines.First().Value.Hits},{method.Key}");

                            foreach (var line in method.Value.Lines)
                                lcov.Add($"DA:{line.Key},{line.Value.Hits}");

                            foreach (var branch in method.Value.Branches)
                            {
                                lcov.Add($"BRDA:{branch.Key},{branch.Key.Offset},{branch.Key.Path},{branch.Value.Hits}");
                            }
                        }
                    }

                    lcov.Add($"LF:{docLineCoverage.Total}");
                    lcov.Add($"LH:{docLineCoverage.Covered}");

                    lcov.Add($"BRF:{docBranchCoverage.Total}");
                    lcov.Add($"BRH:{docBranchCoverage.Covered}");

                    lcov.Add($"FNF:{docMethodCoverage.Total}");
                    lcov.Add($"FNH:{docMethodCoverage.Covered}");

                    lcov.Add("end_of_record");
                }
            }

            return string.Join(Environment.NewLine, lcov);
        }

        public CoverageResult Read(string data)
        {
            throw new NotSupportedException("Not supported by this reporter.");
        }
    }
}