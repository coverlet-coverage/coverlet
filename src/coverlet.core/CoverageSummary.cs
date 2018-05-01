using System;
using System.Collections.Generic;
using System.Linq;

namespace Coverlet.Core
{
    public class CoverageDetails
    {
        public double Covered { get; set; }
        public int Total { get; set; }
        public double Percent { get; set; }
    }
    public class CoverageSummary
    {
        public CoverageDetails CalculateLineCoverage(Lines lines)
        {
            var details = new CoverageDetails();
            details.Covered = lines.Where(l => l.Value.Hits > 0).Count();
            details.Total = lines.Count;
            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
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

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
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

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
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

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
            return details;
        }

        public CoverageDetails CalculateLineCoverage(Modules modules)
        {
            var details = new CoverageDetails();
            foreach (var module in modules)
            {
                var moduleCoverage = CalculateLineCoverage(module.Value);
                details.Covered += moduleCoverage.Covered;
                details.Total += moduleCoverage.Total;
            }

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
            return details;
        }

        public CoverageDetails CalculateBranchCoverage(List<BranchInfo> branchInfo)
        {
            var details = new CoverageDetails();
            details.Covered = branchInfo.Count(bi => bi.Hits > 0);
            details.Total = branchInfo.Count;
            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
            return details;
        }

        public CoverageDetails CalculateBranchCoverage(Branches branches)
        {
            var details = new CoverageDetails();
            details.Covered = branches.Sum(b => b.Value.Where(bi => bi.Hits > 0).Count());
            details.Total = branches.Sum(b => b.Value.Count());
            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
            return details;
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

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
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

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
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

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
            return details;
        }

        public CoverageDetails CalculateBranchCoverage(Modules modules)
        {
            var details = new CoverageDetails();
            foreach (var module in modules)
            {
                var moduleCoverage = CalculateBranchCoverage(module.Value);
                details.Covered += moduleCoverage.Covered;
                details.Total += moduleCoverage.Total;
            }

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
            return details;
        }

        public CoverageDetails CalculateMethodCoverage(Lines lines)
        {
            var details = new CoverageDetails();
            details.Covered = lines.Any(l => l.Value.Hits > 0) ? 1 : 0;
            details.Total = 1;
            details.Percent = details.Covered;
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

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
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

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
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

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
            return details;
        }

        public CoverageDetails CalculateMethodCoverage(Modules modules)
        {
            var details = new CoverageDetails();
            foreach (var module in modules)
            {
                var moduleCoverage = CalculateMethodCoverage(module.Value);
                details.Covered += moduleCoverage.Covered;
                details.Total += moduleCoverage.Total;
            }

            double coverage = details.Total == 0 ? details.Total : details.Covered / details.Total;
            details.Percent = Math.Round(coverage, 3);
            return details;
        }
    }
}