using System;
using System.Collections.Generic;
using System.Linq;

namespace Coverlet.Core
{
    public class CoverageSummary
    {
        public double CalculateLineCoverage(Lines lines)
        {
            double linesCovered = lines.Where(l => l.Value.Hits > 0).Count();
            double coverage = lines.Count == 0 ? lines.Count : linesCovered / lines.Count;
            return Math.Round(coverage, 3);
        }

        public double CalculateLineCoverage(Methods methods)
        {
            double total = 0, average = 0;
            foreach (var method in methods)
                total += CalculateLineCoverage(method.Value.Lines);

            average = total / methods.Count;
            return Math.Round(average, 3);
        }

        public double CalculateLineCoverage(Classes classes)
        {
            double total = 0, average = 0;
            foreach (var @class in classes)
                total += CalculateLineCoverage(@class.Value);

            average = total / classes.Count;
            return Math.Round(average, 3);
        }

        public double CalculateLineCoverage(Documents documents)
        {
            double total = 0, average = 0;
            foreach (var document in documents)
                total += CalculateLineCoverage(document.Value);

            average = total / documents.Count;
            return Math.Round(average, 3);
        }

        public double CalculateLineCoverage(Modules modules)
        {
            double total = 0, average = 0;
            foreach (var module in modules)
                total += CalculateLineCoverage(module.Value);

            average = total / modules.Count;
            return Math.Round(average, 3);
        }

        public double CalculateBranchCoverage(Branches branches)
        {
            double pointsCovered = branches.Sum(b => b.Value.Where(bi => bi.Hits > 0).Count());
            double totalPoints = branches.Sum(b => b.Value.Count());
            double coverage = totalPoints == 0 ? totalPoints : pointsCovered / totalPoints;
            return Math.Round(coverage, 3);
        }

        public double CalculateBranchCoverage(Methods methods)
        {
            double total = 0, average = 0;
            foreach (var method in methods)
                total += CalculateBranchCoverage(method.Value.Branches);

            average = total / methods.Count;
            return Math.Round(average, 3);
        }

        public double CalculateBranchCoverage(Classes classes)
        {
            double total = 0, average = 0;
            foreach (var @class in classes)
                total += CalculateBranchCoverage(@class.Value);

            average = total / classes.Count;
            return Math.Round(average, 3);
        }

        public double CalculateBranchCoverage(Documents documents)
        {
            double total = 0, average = 0;
            foreach (var document in documents)
                total += CalculateBranchCoverage(document.Value);

            average = total / documents.Count;
            return Math.Round(average, 3);
        }

        public double CalculateBranchCoverage(Modules modules)
        {
            double total = 0, average = 0;
            foreach (var module in modules)
                total += CalculateBranchCoverage(module.Value);

            average = total / modules.Count;
            return Math.Round(average, 3);
        }

        public double CalculateMethodCoverage(Lines lines)
        {
            if (lines.Any(l => l.Value.Hits > 0))
                return 1;

            return 0;
        }

        public double CalculateMethodCoverage(Methods methods)
        {
            double total = 0, average = 0;
            foreach (var method in methods)
                total += CalculateMethodCoverage(method.Value.Lines);

            average = total / methods.Count;
            return Math.Round(average, 3);
        }

        public double CalculateMethodCoverage(Classes classes)
        {
            double total = 0, average = 0;
            foreach (var @class in classes)
                total += CalculateMethodCoverage(@class.Value);

            average = total / classes.Count;
            return Math.Round(average, 3);
        }

        public double CalculateMethodCoverage(Documents documents)
        {
            double total = 0, average = 0;
            foreach (var document in documents)
                total += CalculateMethodCoverage(document.Value);

            average = total / documents.Count;
            return Math.Round(average, 3);
        }

        public double CalculateMethodCoverage(Modules modules)
        {
            double total = 0, average = 0;
            foreach (var module in modules)
                total += CalculateMethodCoverage(module.Value);

            average = total / modules.Count;
            return Math.Round(average, 3);
        }
    }
}