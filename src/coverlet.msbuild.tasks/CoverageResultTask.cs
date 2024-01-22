// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleTables;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Enums;
using Coverlet.Core.Reporters;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Coverlet.MSbuild.Tasks
{
  public class CoverageResultTask : BaseTask
  {
    private readonly MSBuildLogger _logger;

    [Required]
    public string Output { get; set; }

    [Required]
    public string OutputFormat { get; set; }

    [Required]
    public string Threshold { get; set; }

    [Required]
    public string ThresholdType { get; set; }

    [Required]
    public string ThresholdStat { get; set; }

    [Required]
    public ITaskItem InstrumenterState { get; set; }

    public string CoverletMultiTargetFrameworksCurrentTFM { get; set; }

    [Output]
    public ITaskItem[] ReportItems { get; set; }

    public CoverageResultTask()
    {
      _logger = new MSBuildLogger(Log);
    }

    public override bool Execute()
    {
      try
      {
        _logger.LogInformation("\nCalculating coverage result...", true);

        IFileSystem fileSystem = ServiceProvider.GetService<IFileSystem>();
        if (InstrumenterState is null || !fileSystem.Exists(InstrumenterState.ItemSpec))
        {
          _logger.LogError("Result of instrumentation task not found");
          return false;
        }

        Coverage coverage = null;
        using (Stream instrumenterStateStream = fileSystem.NewFileStream(InstrumenterState.ItemSpec, FileMode.Open))
        {
          IInstrumentationHelper instrumentationHelper = ServiceProvider.GetService<IInstrumentationHelper>();
          // Task.Log is teared down after a task and thus the new MSBuildLogger must be passed to the InstrumentationHelper
          // https://github.com/microsoft/msbuild/issues/5153
          instrumentationHelper.SetLogger(_logger);
          coverage = new Coverage(CoveragePrepareResult.Deserialize(instrumenterStateStream), _logger, ServiceProvider.GetService<IInstrumentationHelper>(), fileSystem, ServiceProvider.GetService<ISourceRootTranslator>());
        }

        try
        {
          fileSystem.Delete(InstrumenterState.ItemSpec);
        }
        catch (Exception ex)
        {
          // We don't want to block coverage for I/O errors
          _logger.LogInformation($"Exception during instrument state deletion, file name '{InstrumenterState.ItemSpec}'");
          _logger.LogWarning(ex);
        }

        CoverageResult result = coverage.GetCoverageResult();

        string directory = Path.GetDirectoryName(Output);
        if (directory == string.Empty)
        {
          directory = Directory.GetCurrentDirectory();
        }
        else if (!Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        string[] formats = OutputFormat.Split(',');
        var coverageReportPaths = new List<ITaskItem>(formats.Length);
        ISourceRootTranslator sourceRootTranslator = ServiceProvider.GetService<ISourceRootTranslator>();
        foreach (string format in formats)
        {
          IReporter reporter = new ReporterFactory(format).CreateReporter();
          if (reporter == null)
          {
            throw new ArgumentException($"Specified output format '{format}' is not supported");
          }

          if (reporter.OutputType == ReporterOutputType.Console)
          {
            // Output to TaskLoggingHelper
            Log.LogMessage(MessageImportance.High, "  Outputting results to console");
            Log.LogMessage(MessageImportance.High, reporter.Report(result, sourceRootTranslator));
          }
          else
          {
            ReportWriter writer = new(CoverletMultiTargetFrameworksCurrentTFM,
                                                    directory,
                                                    Output,
                                                    reporter,
                                                    fileSystem,
                                                    result,
                                                    sourceRootTranslator);
            string path = writer.WriteReport();
            Log.LogMessage(MessageImportance.High, $" Generating report '{path}'");
            var metadata = new Dictionary<string, string> { ["Format"] = format };
            coverageReportPaths.Add(new TaskItem(path, metadata));
          }
        }

        ReportItems = coverageReportPaths.ToArray();

        var thresholdTypeFlagQueue = new Queue<ThresholdTypeFlags>();

        foreach (string thresholdType in ThresholdType.Split(',').Select(t => t.Trim()))
        {
          if (thresholdType.Equals("line", StringComparison.OrdinalIgnoreCase))
          {
            thresholdTypeFlagQueue.Enqueue(ThresholdTypeFlags.Line);
          }
          else if (thresholdType.Equals("branch", StringComparison.OrdinalIgnoreCase))
          {
            thresholdTypeFlagQueue.Enqueue(ThresholdTypeFlags.Branch);
          }
          else if (thresholdType.Equals("method", StringComparison.OrdinalIgnoreCase))
          {
            thresholdTypeFlagQueue.Enqueue(ThresholdTypeFlags.Method);
          }
        }

        var thresholdTypeFlagValues = new Dictionary<ThresholdTypeFlags, double>();
        if (Threshold.Contains(','))
        {
          IEnumerable<string> thresholdValues = Threshold.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());
          if (thresholdValues.Count() != thresholdTypeFlagQueue.Count)
          {
            throw new ArgumentException($"Threshold type flag count ({thresholdTypeFlagQueue.Count}) and values count ({thresholdValues.Count()}) doesn't match");
          }

          foreach (string threshold in thresholdValues)
          {
            if (double.TryParse(threshold, out double value))
            {
              thresholdTypeFlagValues[thresholdTypeFlagQueue.Dequeue()] = value;
            }
            else
            {
              throw new ArgumentException($"Invalid threshold value must be numeric");
            }
          }
        }
        else
        {
          double thresholdValue = double.Parse(Threshold);

          while (thresholdTypeFlagQueue.Any())
          {
            thresholdTypeFlagValues[thresholdTypeFlagQueue.Dequeue()] = thresholdValue;
          }
        }

        ThresholdStatistic thresholdStat = ThresholdStatistic.Minimum;
        if (ThresholdStat.Equals("average", StringComparison.OrdinalIgnoreCase))
        {
          thresholdStat = ThresholdStatistic.Average;
        }
        else if (ThresholdStat.Equals("total", StringComparison.OrdinalIgnoreCase))
        {
          thresholdStat = ThresholdStatistic.Total;
        }

        var coverageTable = new ConsoleTable("Module", "Line", "Branch", "Method");
        var summary = new CoverageSummary();

        CoverageDetails linePercentCalculation = summary.CalculateLineCoverage(result.Modules);
        CoverageDetails branchPercentCalculation = summary.CalculateBranchCoverage(result.Modules);
        CoverageDetails methodPercentCalculation = summary.CalculateMethodCoverage(result.Modules);

        double totalLinePercent = linePercentCalculation.Percent;
        double totalBranchPercent = branchPercentCalculation.Percent;
        double totalMethodPercent = methodPercentCalculation.Percent;

        double averageLinePercent = linePercentCalculation.AverageModulePercent;
        double averageBranchPercent = branchPercentCalculation.AverageModulePercent;
        double averageMethodPercent = methodPercentCalculation.AverageModulePercent;

        foreach (KeyValuePair<string, Documents> module in result.Modules)
        {
          double linePercent = summary.CalculateLineCoverage(module.Value).Percent;
          double branchPercent = summary.CalculateBranchCoverage(module.Value).Percent;
          double methodPercent = summary.CalculateMethodCoverage(module.Value).Percent;

          coverageTable.AddRow(Path.GetFileNameWithoutExtension(module.Key), $"{InvariantFormat(linePercent)}%", $"{InvariantFormat(branchPercent)}%", $"{InvariantFormat(methodPercent)}%");
        }

        Console.WriteLine();
        Console.WriteLine(coverageTable.ToStringAlternative());

        coverageTable.Columns.Clear();
        coverageTable.Rows.Clear();

        coverageTable.AddColumn(new[] { "", "Line", "Branch", "Method" });
        coverageTable.AddRow("Total", $"{InvariantFormat(totalLinePercent)}%", $"{InvariantFormat(totalBranchPercent)}%", $"{InvariantFormat(totalMethodPercent)}%");
        coverageTable.AddRow("Average", $"{InvariantFormat(averageLinePercent)}%", $"{InvariantFormat(averageBranchPercent)}%", $"{InvariantFormat(averageMethodPercent)}%");

        Console.WriteLine(coverageTable.ToStringAlternative());

        ThresholdTypeFlags thresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, thresholdTypeFlagValues, thresholdStat);
        if (thresholdTypeFlags != ThresholdTypeFlags.None)
        {
          var exceptionMessageBuilder = new StringBuilder();
          if ((thresholdTypeFlags & ThresholdTypeFlags.Line) != ThresholdTypeFlags.None)
          {
            exceptionMessageBuilder.AppendLine(
                $"The {thresholdStat.ToString().ToLower()} line coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Line]}");
          }

          if ((thresholdTypeFlags & ThresholdTypeFlags.Branch) != ThresholdTypeFlags.None)
          {
            exceptionMessageBuilder.AppendLine(
                $"The {thresholdStat.ToString().ToLower()} branch coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Branch]}");
          }

          if ((thresholdTypeFlags & ThresholdTypeFlags.Method) != ThresholdTypeFlags.None)
          {
            exceptionMessageBuilder.AppendLine(
                $"The {thresholdStat.ToString().ToLower()} method coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Method]}");
          }

          throw new InvalidOperationException(exceptionMessageBuilder.ToString());
        }
      }
      catch (Exception ex)
      {
        Log.LogErrorFromException(ex, true);
        return false;
      }

      return true;
    }

    private static string InvariantFormat(double value) => value.ToString(CultureInfo.InvariantCulture);
  }
}
