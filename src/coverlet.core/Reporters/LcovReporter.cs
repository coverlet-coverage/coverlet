// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Reporters
{
  internal class LcovReporter : IReporter
  {
    public ReporterOutputType OutputType => ReporterOutputType.File;

    public string Format => "lcov";

    public string Extension => "info";

    public string Report(CoverageResult result, ISourceRootTranslator sourceRootTranslator)
    {
      if (result.Parameters.DeterministicReport)
      {
        throw new NotSupportedException("Deterministic report not supported by lcov reporter");
      }

      CoverageSummary summary = new();
      List<string> lcov = [];

      foreach (KeyValuePair<string, Documents> module in result.Modules)
      {
        foreach (KeyValuePair<string, Classes> doc in module.Value)
        {
          CoverageDetails docLineCoverage = summary.CalculateLineCoverage(doc.Value);
          CoverageDetails docBranchCoverage = summary.CalculateBranchCoverage(doc.Value);
          CoverageDetails docMethodCoverage = summary.CalculateMethodCoverage(doc.Value);

          lcov.Add("SF:" + doc.Key);
          foreach (KeyValuePair<string, Methods> @class in doc.Value)
          {
            foreach (KeyValuePair<string, Method> method in @class.Value)
            {
              // Skip all methods with no lines
              if (method.Value.Lines.Count == 0)
                continue;

              lcov.Add($"FN:{method.Value.Lines.First().Key - 1},{method.Key}");
              lcov.Add($"FNDA:{method.Value.Lines.First().Value},{method.Key}");

              foreach (KeyValuePair<int, int> line in method.Value.Lines)
                lcov.Add($"DA:{line.Key},{line.Value}");

              foreach (BranchInfo branch in method.Value.Branches)
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
