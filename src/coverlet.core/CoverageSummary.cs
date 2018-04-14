using System;
using System.Collections.Generic;
using System.Linq;

namespace Coverlet.Core
{
    public class CoverageSummary
    {
        private CoverageResult _result;

        public CoverageSummary(CoverageResult result)
        {
            _result = result;
        }

        public CoverageSummaryResult CalculateSummary()
        {
            CoverageSummaryResult result = new CoverageSummaryResult();

            foreach (var mod in _result.Modules)
            {
                int totalLines = 0, linesCovered = 0;
                foreach (var doc in mod.Value)
                {
                    foreach (var @class in doc.Value)
                    {
                        foreach (var method in @class.Value)
                        {
                            foreach (var line in method.Value)
                            {
                                totalLines++;
                                if (line.Value.Hits > 0)
                                    linesCovered++;
                            }
                        }
                    }
                }

                result.Add(System.IO.Path.GetFileNameWithoutExtension(mod.Key), totalLines == 0 ? totalLines : (linesCovered * 100) / totalLines);
            }

            return result;
        }

        public double CalculateLineCoverage(KeyValuePair<string, Lines> method)
        {
            double coverage = 0, totalLines = 0, linesCovered = 0;
            foreach (var line in method.Value)
            {
                totalLines++;
                if (line.Value.Hits > 0)
                    linesCovered++;
            }

            coverage = totalLines == 0 ? totalLines : linesCovered / totalLines;
            return Math.Round(coverage, 3);
        }

        public double CalculateLineCoverage(KeyValuePair<string, Methods> @class)
        {
            double total = 0, average = 0;
            foreach (var method in @class.Value)
                total += CalculateLineCoverage(method);

            average = total / @class.Value.Count;
            return Math.Round(average, 3);
        }

        public double CalculateLineCoverage(KeyValuePair<string, Classes> document)
        {
            double total = 0, average = 0;
            foreach (var @class in document.Value)
                total += CalculateLineCoverage(@class);

            average = total / document.Value.Count;
            return Math.Round(average, 3);
        }

        public double CalculateLineCoverage(KeyValuePair<string, Documents> module)
        {
            double total = 0, average = 0;
            foreach (var document in module.Value)
                total += CalculateLineCoverage(document);

            average = total / module.Value.Count;
            return Math.Round(average, 3);
        }

        public double CalculateBranchCoverage(KeyValuePair<string, Lines> method)
        {
            double coverage = 0, totalLines = 0, linesCovered = 0;
            foreach (var line in method.Value)
            {
                totalLines++;
                if (line.Value.Hits > 0 && line.Value.IsBranchPoint)
                    linesCovered++;
            }

            coverage = totalLines == 0 ? totalLines : linesCovered / totalLines;
            return Math.Round(coverage, 3);
        }

        public double CalculateBranchCoverage(KeyValuePair<string, Methods> @class)
        {
            double total = 0, average = 0;
            foreach (var method in @class.Value)
                total += CalculateBranchCoverage(method);

            average = total / @class.Value.Count;
            return Math.Round(average, 3);
        }

        public double CalculateBranchCoverage(KeyValuePair<string, Classes> document)
        {
            double total = 0, average = 0;
            foreach (var @class in document.Value)
                total += CalculateBranchCoverage(@class);

            average = total / document.Value.Count;
            return Math.Round(average, 3);
        }

        public double CalculateBranchCoverage(KeyValuePair<string, Documents> module)
        {
            double total = 0, average = 0;
            foreach (var document in module.Value)
                total += CalculateBranchCoverage(document);

            average = total / module.Value.Count;
            return Math.Round(average, 3);
        }

        public double CalculateMethodCoverage(KeyValuePair<string, Lines> method)
        {
            if (method.Value.Any(l => l.Value.Hits > 0))
                return 1;

            return 0;
        }

        public double CalculateMethodCoverage(KeyValuePair<string, Methods> @class)
        {
            double total = 0, average = 0;
            foreach (var method in @class.Value)
                total += CalculateMethodCoverage(method);

            average = total / @class.Value.Count;
            return Math.Round(average, 3);
        }

        public double CalculateMethodCoverage(KeyValuePair<string, Classes> document)
        {
            double total = 0, average = 0;
            foreach (var @class in document.Value)
                total += CalculateMethodCoverage(@class);

            average = total / document.Value.Count;
            return Math.Round(average, 3);
        }

        public double CalculateMethodCoverage(KeyValuePair<string, Documents> module)
        {
            double total = 0, average = 0;
            foreach (var document in module.Value)
                total += CalculateMethodCoverage(document);

            average = total / module.Value.Count;
            return Math.Round(average, 3);
        }
    }
}