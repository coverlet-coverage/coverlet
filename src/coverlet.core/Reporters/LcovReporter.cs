using System.Linq;
using System.IO;

namespace Coverlet.Core.Reporters
{
    public class LcovReporter : IReporter
    {
        public string Format => "lcov";

        public string Extension => "info";

        public void Report(CoverageResult result, StreamWriter streamWriter)
        {
            CoverageSummary summary = new CoverageSummary();

            foreach (var module in result.Modules)
            {
                foreach (var doc in module.Value)
                {
                    var docLineCoverage = summary.CalculateLineCoverage(doc.Value);
                    var docBranchCoverage = summary.CalculateBranchCoverage(doc.Value);
                    var docMethodCoverage = summary.CalculateMethodCoverage(doc.Value);

                    streamWriter.WriteLine("SF:" + doc.Key);
                    foreach (var @class in doc.Value)
                    {
                        foreach (var method in @class.Value)
                        {
                            // Skip all methods with no lines
                            if (method.Value.Lines.Count == 0)
                                continue;

                            streamWriter.WriteLine($"FN:{method.Value.Lines.First().Key - 1},{method.Key}");
                            streamWriter.WriteLine($"FNDA:{method.Value.Lines.First().Value},{method.Key}");

                            foreach (var line in method.Value.Lines)
                                streamWriter.WriteLine($"DA:{line.Key},{line.Value}");

                            foreach (var branch in method.Value.Branches)
                            {
                                streamWriter.WriteLine($"BRDA:{branch.Line},{branch.Offset},{branch.Path},{branch.Hits}");
                            }
                        }
                    }

                    streamWriter.WriteLine($"LF:{docLineCoverage.Total}");
                    streamWriter.WriteLine($"LH:{docLineCoverage.Covered}");

                    streamWriter.WriteLine($"BRF:{docBranchCoverage.Total}");
                    streamWriter.WriteLine($"BRH:{docBranchCoverage.Covered}");

                    streamWriter.WriteLine($"FNF:{docMethodCoverage.Total}");
                    streamWriter.WriteLine($"FNH:{docMethodCoverage.Covered}");

                    streamWriter.WriteLine("end_of_record");
                }
            }
        }
    }
}