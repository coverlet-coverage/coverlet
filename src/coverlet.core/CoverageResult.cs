// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Coverlet.Core.Enums;
using Coverlet.Core.Instrumentation;

namespace Coverlet.Core
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
    [JsonConstructor]
    public Method()
    {
      Lines = [];
      Branches = [];
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
    public string Identifier { get; set; }
    public Modules Modules { get; set; }
    public List<InstrumenterResult> InstrumentedResults { get; set; }
    public CoverageParameters Parameters { get; set; }

    public CoverageResult() { }

    public void Merge(Modules modules)
    {
      foreach (KeyValuePair<string, Documents> module in modules)
      {
        if (!Modules.Keys.Contains(module.Key))
        {
          Modules.Add(module.Key, module.Value);
        }
        else
        {
          foreach (KeyValuePair<string, Classes> document in module.Value)
          {
            if (!Modules[module.Key].ContainsKey(document.Key))
            {
              Modules[module.Key].Add(document.Key, document.Value);
            }
            else
            {
              foreach (KeyValuePair<string, Methods> @class in document.Value)
              {
                if (!Modules[module.Key][document.Key].ContainsKey(@class.Key))
                {
                  Modules[module.Key][document.Key].Add(@class.Key, @class.Value);
                }
                else
                {
                  foreach (KeyValuePair<string, Method> method in @class.Value)
                  {
                    if (!Modules[module.Key][document.Key][@class.Key].ContainsKey(method.Key))
                    {
                      Modules[module.Key][document.Key][@class.Key].Add(method.Key, method.Value);
                    }
                    else
                    {
                      foreach (KeyValuePair<int, int> line in method.Value.Lines)
                      {
                        if (!Modules[module.Key][document.Key][@class.Key][method.Key].Lines.ContainsKey(line.Key))
                        {
                          Modules[module.Key][document.Key][@class.Key][method.Key].Lines.Add(line.Key, line.Value);
                        }
                        else
                        {
                          Modules[module.Key][document.Key][@class.Key][method.Key].Lines[line.Key] += line.Value;
                        }
                      }

                      foreach (BranchInfo branch in method.Value.Branches)
                      {
                        Branches branches = Modules[module.Key][document.Key][@class.Key][method.Key].Branches;
                        BranchInfo branchInfo = branches.FirstOrDefault(b => b.EndOffset == branch.EndOffset && b.Line == branch.Line && b.Offset == branch.Offset && b.Ordinal == branch.Ordinal && b.Path == branch.Path);
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

    public ThresholdTypeFlags GetThresholdTypesBelowThreshold(CoverageSummary summary, Dictionary<ThresholdTypeFlags, double> thresholdTypeFlagValues, ThresholdStatistic thresholdStat)
    {
      ThresholdTypeFlags thresholdTypeFlags = ThresholdTypeFlags.None;
      switch (thresholdStat)
      {
        case ThresholdStatistic.Minimum:
          {
            if (!Modules.Any())
              thresholdTypeFlags = CompareThresholdValues(thresholdTypeFlagValues, thresholdTypeFlags, 0, 0, 0);

            foreach (KeyValuePair<string, Documents> module in Modules)
            {
              double line = summary.CalculateLineCoverage(module.Value).Percent;
              double branch = summary.CalculateBranchCoverage(module.Value).Percent;
              double method = summary.CalculateMethodCoverage(module.Value).Percent;

              thresholdTypeFlags = CompareThresholdValues(thresholdTypeFlagValues, thresholdTypeFlags, line, branch, method);
            }
          }
          break;
        case ThresholdStatistic.Average:
          {
            double line = summary.CalculateLineCoverage(Modules).AverageModulePercent;
            double branch = summary.CalculateBranchCoverage(Modules).AverageModulePercent;
            double method = summary.CalculateMethodCoverage(Modules).AverageModulePercent;

            thresholdTypeFlags = CompareThresholdValues(thresholdTypeFlagValues, thresholdTypeFlags, line, branch, method);
          }
          break;
        case ThresholdStatistic.Total:
          {
            double line = summary.CalculateLineCoverage(Modules).Percent;
            double branch = summary.CalculateBranchCoverage(Modules).Percent;
            double method = summary.CalculateMethodCoverage(Modules).Percent;

            thresholdTypeFlags = CompareThresholdValues(thresholdTypeFlagValues, thresholdTypeFlags, line, branch, method);
          }
          break;
      }

      return thresholdTypeFlags;
    }

    private static ThresholdTypeFlags CompareThresholdValues(
        Dictionary<ThresholdTypeFlags, double> thresholdTypeFlagValues, ThresholdTypeFlags thresholdTypeFlags,
        double line, double branch, double method)
    {
      if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Line, out double lineThresholdValue) &&
          lineThresholdValue > line)
      {
        thresholdTypeFlags |= ThresholdTypeFlags.Line;
      }

      if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Branch, out double branchThresholdValue) &&
          branchThresholdValue > branch)
      {
        thresholdTypeFlags |= ThresholdTypeFlags.Branch;
      }

      if (thresholdTypeFlagValues.TryGetValue(ThresholdTypeFlags.Method, out double methodThresholdValue) &&
          methodThresholdValue > method)
      {
        thresholdTypeFlags |= ThresholdTypeFlags.Method;
      }

      return thresholdTypeFlags;
    }
  }
}
