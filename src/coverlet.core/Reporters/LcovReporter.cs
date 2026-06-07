// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Reporters
{
  internal class LcovReporter : IReporter
  {
    // P4: Pool a StringBuilder per thread to avoid repeated large heap allocations.
    private static readonly ThreadLocal<StringBuilder> s_sb =
        new(() => new StringBuilder(capacity: 64 * 1024));

    public ReporterOutputType OutputType => ReporterOutputType.File;

    public string Format => "lcov";

    public string Extension => "info";

    public string Report(CoverageResult result, ISourceRootTranslator sourceRootTranslator)
    {
      if (result.Parameters.DeterministicReport)
      {
        throw new NotSupportedException("Deterministic report not supported by lcov reporter");
      }

      StringBuilder sb = s_sb.Value!;
      sb.Clear();

      foreach (KeyValuePair<string, Documents> module in result.Modules)
      {
        foreach (KeyValuePair<string, Classes> doc in module.Value)
        {
          CoverageDetails docLineCoverage = CoverageSummary.CalculateLineCoverage(doc.Value);
          CoverageDetails docBranchCoverage = CoverageSummary.CalculateBranchCoverage(doc.Value);
          CoverageDetails docMethodCoverage = CoverageSummary.CalculateMethodCoverage(doc.Value);

          sb.Append("SF:").AppendLine(doc.Key);
          foreach (KeyValuePair<string, Methods> @class in doc.Value)
          {
            foreach (KeyValuePair<string, Method> method in @class.Value)
            {
              // Skip all methods with no lines
              if (method.Value.Lines.Count == 0)
                continue;

              KeyValuePair<int, int> firstLine = method.Value.Lines.First();
              sb.Append("FN:").Append(firstLine.Key - 1).Append(',').AppendLine(method.Key);
              sb.Append("FNDA:").Append(firstLine.Value).Append(',').AppendLine(method.Key);

              foreach (KeyValuePair<int, int> line in method.Value.Lines)
                sb.Append("DA:").Append(line.Key).Append(',').Append(line.Value).AppendLine();

              foreach (BranchInfo branch in method.Value.Branches)
              {
                sb.Append("BRDA:").Append(branch.Line).Append(',')
                  .Append(branch.Offset).Append(',')
                  .Append(branch.Path).Append(',')
                  .Append(branch.Hits).AppendLine();
              }
            }
          }

          sb.Append("LF:").Append(docLineCoverage.Total).AppendLine();
          sb.Append("LH:").Append(docLineCoverage.Covered.ToString(CultureInfo.InvariantCulture)).AppendLine();

          sb.Append("BRF:").Append(docBranchCoverage.Total).AppendLine();
          sb.Append("BRH:").Append(docBranchCoverage.Covered.ToString(CultureInfo.InvariantCulture)).AppendLine();

          sb.Append("FNF:").Append(docMethodCoverage.Total).AppendLine();
          sb.Append("FNH:").Append(docMethodCoverage.Covered.ToString(CultureInfo.InvariantCulture)).AppendLine();

          sb.AppendLine("end_of_record");
        }
      }

      // Trim the trailing newline added by the last AppendLine to match the original
      // string.Join(Environment.NewLine, ...) output which has no trailing newline.
      if (sb.Length >= Environment.NewLine.Length)
        sb.Length -= Environment.NewLine.Length;

      return sb.ToString();
    }
  }
}
