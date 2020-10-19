using Coverlet.Core.Abstractions;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Coverlet.Core.Reporters
{
    internal class LcovReporter : IReporter
    {
        private IFilePathHelper _filePathHelper;

        public ReporterOutputType OutputType => ReporterOutputType.File;

        public string Format => "lcov";

        public string Extension => "info";

        public LcovReporter(IFilePathHelper filePathHelper)
        {
            _filePathHelper = filePathHelper;
        }

        public string Report(CoverageResult result)
        {
            CoverageSummary summary = new CoverageSummary();
            List<string> lcov = new List<string>();

            var absolutePaths = _filePathHelper.GetBasePaths(result.Modules.Values.SelectMany(k => k.Keys), result.UseSourceLink).ToList();

            foreach (var module in result.Modules)
            {
                foreach (var doc in module.Value)
                {
                    var docLineCoverage = summary.CalculateLineCoverage(doc.Value);
                    var docBranchCoverage = summary.CalculateBranchCoverage(doc.Value);
                    var docMethodCoverage = summary.CalculateMethodCoverage(doc.Value);

                    lcov.Add("SF:" + _filePathHelper.GetRelativePathFromBase(absolutePaths, doc.Key, result.UseSourceLink));
                    foreach (var @class in doc.Value)
                    {
                        foreach (var method in @class.Value)
                        {
                            // Skip all methods with no lines
                            if (method.Value.Lines.Count == 0)
                                continue;

                            lcov.Add($"FN:{method.Value.Lines.First().Key - 1},{method.Key}");
                            lcov.Add($"FNDA:{method.Value.Lines.First().Value},{method.Key}");

                            foreach (var line in method.Value.Lines)
                                lcov.Add($"DA:{line.Key},{line.Value}");

                            foreach (var branch in method.Value.Branches)
                            {
                                lcov.Add($"BRDA:{branch.Line},{branch.Offset},{branch.Path},{branch.Hits}");
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
    }
}