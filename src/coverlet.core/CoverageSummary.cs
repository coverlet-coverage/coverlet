// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Coverlet.Core
{
  internal class CoverageSummary
  {
    public static CoverageDetails CalculateLineCoverage(Lines lines)
    {
      var details = new CoverageDetails
      {
        Covered = lines.Count(l => l.Value > 0),
        Total = lines.Count
      };
      return details;
    }

    public static CoverageDetails CalculateLineCoverage(Methods methods)
    {
      var details = new CoverageDetails();
      foreach (KeyValuePair<string, Method> method in methods)
      {
        CoverageDetails methodCoverage = CalculateLineCoverage(method.Value.Lines);
        details.Covered += methodCoverage.Covered;
        details.Total += methodCoverage.Total;
      }
      return details;
    }

    public static CoverageDetails CalculateLineCoverage(Classes classes)
    {
      var details = new CoverageDetails();
      foreach (KeyValuePair<string, Methods> @class in classes)
      {
        CoverageDetails classCoverage = CalculateLineCoverage(@class.Value);
        details.Covered += classCoverage.Covered;
        details.Total += classCoverage.Total;
      }
      return details;
    }

    public static CoverageDetails CalculateLineCoverage(Documents documents)
    {
      var details = new CoverageDetails();
      foreach (KeyValuePair<string, Classes> document in documents)
      {
        CoverageDetails documentCoverage = CalculateLineCoverage(document.Value);
        details.Covered += documentCoverage.Covered;
        details.Total += documentCoverage.Total;
      }
      return details;
    }

    public static CoverageDetails CalculateLineCoverage(Modules modules)
    {
      var details = new CoverageDetails { Modules = modules };
      double accumPercent = 0.0D;

      if (modules.Count == 0)
        return details;

      foreach (KeyValuePair<string, Documents> module in modules)
      {
        CoverageDetails moduleCoverage = CalculateLineCoverage(module.Value);
        details.Covered += moduleCoverage.Covered;
        details.Total += moduleCoverage.Total;
        accumPercent += moduleCoverage.Percent;
      }
      details.AverageModulePercent = accumPercent / modules.Count;
      return details;
    }

    public static CoverageDetails CalculateBranchCoverage(IList<BranchInfo> branches)
    {
      var details = new CoverageDetails
      {
        Covered = branches.Count(bi => bi.Hits > 0),
        Total = branches.Count
      };
      return details;
    }

    public static int CalculateNpathComplexity(IList<BranchInfo> branches)
    {
      // Adapted from OpenCover see https://github.com/OpenCover/opencover/blob/master/main/OpenCover.Framework/Persistance/BasePersistance.cs#L419
      if (!branches.Any())
      {
        return 0;
      }

      var paths = new Dictionary<int, int>();
      foreach (BranchInfo branch in branches)
      {
        if (!paths.TryGetValue(branch.Offset, out int count))
        {
          count = 0;
        }
        paths[branch.Offset] = ++count;
      }

      int npath = 1;
      foreach (int branchPoints in paths.Values)
      {
        try
        {
          npath = checked(npath * branchPoints);
        }
        catch (OverflowException)
        {
          npath = int.MaxValue;
          break;
        }
      }
      return npath;
    }

    public static int CalculateCyclomaticComplexity(IList<BranchInfo> branches)
    {
      return Math.Max(1, branches.Count);
    }

    public static int CalculateCyclomaticComplexity(Methods methods)
    {
      return methods.Values.Select(m => CalculateCyclomaticComplexity(m.Branches)).Sum();
    }

    public static int CalculateMaxCyclomaticComplexity(Methods methods)
    {
      return methods.Values.Select(m => CalculateCyclomaticComplexity(m.Branches)).DefaultIfEmpty(1).Max();
    }

    public static int CalculateMinCyclomaticComplexity(Methods methods)
    {
      return methods.Values.Select(m => CalculateCyclomaticComplexity(m.Branches)).DefaultIfEmpty(1).Min();
    }

    public static int CalculateCyclomaticComplexity(Modules modules)
    {
      return modules.Values.Select(CalculateCyclomaticComplexity).Sum();
    }

    public static int CalculateMaxCyclomaticComplexity(Modules modules)
    {
      return modules.Values.Select(CalculateCyclomaticComplexity).DefaultIfEmpty(1).Max();
    }

    public static int CalculateMinCyclomaticComplexity(Modules modules)
    {
      return modules.Values.Select(CalculateCyclomaticComplexity).DefaultIfEmpty(1).Min();
    }

    public static int CalculateCyclomaticComplexity(Documents documents)
    {
      return documents.Values.SelectMany(c => c.Values.Select(CalculateCyclomaticComplexity)).Sum();
    }

    public static CoverageDetails CalculateBranchCoverage(Methods methods)
    {
      var details = new CoverageDetails();
      foreach (KeyValuePair<string, Method> method in methods)
      {
        CoverageDetails methodCoverage = CalculateBranchCoverage(method.Value.Branches);
        details.Covered += methodCoverage.Covered;
        details.Total += methodCoverage.Total;
      }
      return details;
    }

    public static CoverageDetails CalculateBranchCoverage(Classes classes)
    {
      var details = new CoverageDetails();
      foreach (KeyValuePair<string, Methods> @class in classes)
      {
        CoverageDetails classCoverage = CalculateBranchCoverage(@class.Value);
        details.Covered += classCoverage.Covered;
        details.Total += classCoverage.Total;
      }
      return details;
    }

    public static CoverageDetails CalculateBranchCoverage(Documents documents)
    {
      var details = new CoverageDetails();
      foreach (KeyValuePair<string, Classes> document in documents)
      {
        CoverageDetails documentCoverage = CalculateBranchCoverage(document.Value);
        details.Covered += documentCoverage.Covered;
        details.Total += documentCoverage.Total;
      }
      return details;
    }

    public static CoverageDetails CalculateBranchCoverage(Modules modules)
    {
      var details = new CoverageDetails { Modules = modules };
      double accumPercent = 0.0D;

      if (modules.Count == 0)
        return details;

      foreach (KeyValuePair<string, Documents> module in modules)
      {
        CoverageDetails moduleCoverage = CalculateBranchCoverage(module.Value);
        details.Covered += moduleCoverage.Covered;
        details.Total += moduleCoverage.Total;
        accumPercent += moduleCoverage.Percent;
      }
      details.AverageModulePercent = modules.Count == 0 ? 0 : accumPercent / modules.Count;
      return details;
    }

    public static CoverageDetails CalculateMethodCoverage(Lines lines)
    {
      var details = new CoverageDetails
      {
        Covered = lines.Any(l => l.Value > 0) ? 1 : 0,
        Total = 1
      };
      return details;
    }

    public static CoverageDetails CalculateMethodCoverage(Methods methods)
    {
      var details = new CoverageDetails();
      IEnumerable<KeyValuePair<string, Method>> methodsWithLines = methods.Where(m => m.Value.Lines.Count > 0);
      foreach (KeyValuePair<string, Method> method in methodsWithLines)
      {
        CoverageDetails methodCoverage = CalculateMethodCoverage(method.Value.Lines);
        details.Covered += methodCoverage.Covered;
      }
      details.Total = methodsWithLines.Count();
      return details;
    }

    public static CoverageDetails CalculateMethodCoverage(Classes classes)
    {
      var details = new CoverageDetails();
      foreach (KeyValuePair<string, Methods> @class in classes)
      {
        CoverageDetails classCoverage = CalculateMethodCoverage(@class.Value);
        details.Covered += classCoverage.Covered;
        details.Total += classCoverage.Total;
      }
      return details;
    }

    public static CoverageDetails CalculateMethodCoverage(Documents documents)
    {
      var details = new CoverageDetails();
      foreach (KeyValuePair<string, Classes> document in documents)
      {
        CoverageDetails documentCoverage = CalculateMethodCoverage(document.Value);
        details.Covered += documentCoverage.Covered;
        details.Total += documentCoverage.Total;
      }
      return details;
    }

    public static CoverageDetails CalculateMethodCoverage(Modules modules)
    {
      var details = new CoverageDetails { Modules = modules };
      double accumPercent = 0.0D;

      if (modules.Count == 0)
        return details;

      foreach (KeyValuePair<string, Documents> module in modules)
      {
        CoverageDetails moduleCoverage = CalculateMethodCoverage(module.Value);
        details.Covered += moduleCoverage.Covered;
        details.Total += moduleCoverage.Total;
        accumPercent += moduleCoverage.Percent;
      }
      details.AverageModulePercent = modules.Count == 0 ? 0 : accumPercent / modules.Count;
      return details;
    }
  }
}
