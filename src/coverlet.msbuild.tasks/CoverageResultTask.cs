using System;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleTables;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Enums;
using Coverlet.Core.Reporters;
using Microsoft.Build.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace Coverlet.MSbuild.Tasks
{
    public class CoverageResultTask : BaseTask
    {
        private MSBuildLogger _logger;

        [Required]
        public string Output { get; set; }

        [Required]
        public string OutputFormat { get; set; }

        [Required]
        public double Threshold { get; set; }

        [Required]
        public string ThresholdType { get; set; }

        [Required]
        public string ThresholdStat { get; set; }

        [Required]
        public ITaskItem InstrumenterState { get; set; }

        public string CoverletMultiTargetFrameworksCurrentTFM { get; set; }

        public CoverageResultTask()
        {
            _logger = new MSBuildLogger(Log);
        }

        public override bool Execute()
        {
            try
            {
                Console.WriteLine("\nCalculating coverage result...");

                IFileSystem fileSystem = ServiceProvider.GetService<IFileSystem>();
                if (InstrumenterState is null || !fileSystem.Exists(InstrumenterState.ItemSpec))
                {
                    _logger.LogError("Result of instrumentation task not found");
                    return false;
                }

                Coverage coverage = null;
                using (Stream instrumenterStateStream = fileSystem.NewFileStream(InstrumenterState.ItemSpec, FileMode.Open))
                {
                    var instrumentationHelper = ServiceProvider.GetService<IInstrumentationHelper>();
                    // Task.Log is teared down after a task and thus the new MSBuildLogger must be passed to the InstrumentationHelper
                    // https://github.com/microsoft/msbuild/issues/5153
                    instrumentationHelper.SetLogger(_logger);
                    coverage = new Coverage(CoveragePrepareResult.Deserialize(instrumenterStateStream), this._logger, ServiceProvider.GetService<IInstrumentationHelper>(), fileSystem, ServiceProvider.GetService<ISourceRootTranslator>());
                }

                try
                {
                    fileSystem.Delete(InstrumenterState.ItemSpec);
                }
                catch (Exception ex)
                {
                    // We don't want to block coverage for I/O errors
                    _logger.LogWarning($"Exception during instrument state deletion, file name '{InstrumenterState.ItemSpec}' exception message '{ex.Message}'");
                }

                CoverageResult result = coverage.GetCoverageResult();

                var directory = Path.GetDirectoryName(Output);
                if (directory == string.Empty)
                {
                    directory = Directory.GetCurrentDirectory();
                }
                else if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var formats = OutputFormat.Split(',');
                foreach (var format in formats)
                {
                    var reporter = new ReporterFactory(format).CreateReporter();
                    if (reporter == null)
                    {
                        throw new Exception($"Specified output format '{format}' is not supported");
                    }

                    if (reporter.OutputType == ReporterOutputType.Console)
                    {
                        // Output to console
                        Console.WriteLine("  Outputting results to console");
                        Console.WriteLine(reporter.Report(result));
                    }
                    else
                    {
                        ReportWriter writer = new ReportWriter(CoverletMultiTargetFrameworksCurrentTFM,
                                                                directory,
                                                                Output,
                                                                reporter,
                                                                fileSystem,
                                                                ServiceProvider.GetService<IConsole>(),
                                                                result);
                        writer.WriteReport();
                    }
                }

                var thresholdTypeFlags = ThresholdTypeFlags.None;
                var thresholdStat = ThresholdStatistic.Minimum;

                foreach (var thresholdType in ThresholdType.Split(',').Select(t => t.Trim()))
                {
                    if (thresholdType.Equals("line", StringComparison.OrdinalIgnoreCase))
                    {
                        thresholdTypeFlags |= ThresholdTypeFlags.Line;
                    }
                    else if (thresholdType.Equals("branch", StringComparison.OrdinalIgnoreCase))
                    {
                        thresholdTypeFlags |= ThresholdTypeFlags.Branch;
                    }
                    else if (thresholdType.Equals("method", StringComparison.OrdinalIgnoreCase))
                    {
                        thresholdTypeFlags |= ThresholdTypeFlags.Method;
                    }
                }

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
                int numModules = result.Modules.Count;

                var linePercentCalculation = summary.CalculateLineCoverage(result.Modules);
                var branchPercentCalculation = summary.CalculateBranchCoverage(result.Modules);
                var methodPercentCalculation = summary.CalculateMethodCoverage(result.Modules);

                var totalLinePercent = linePercentCalculation.Percent;
                var totalBranchPercent = branchPercentCalculation.Percent;
                var totalMethodPercent = methodPercentCalculation.Percent;

                var averageLinePercent = linePercentCalculation.AverageModulePercent;
                var averageBranchPercent = branchPercentCalculation.AverageModulePercent;
                var averageMethodPercent = methodPercentCalculation.AverageModulePercent;

                foreach (var module in result.Modules)
                {
                    var linePercent = summary.CalculateLineCoverage(module.Value).Percent;
                    var branchPercent = summary.CalculateBranchCoverage(module.Value).Percent;
                    var methodPercent = summary.CalculateMethodCoverage(module.Value).Percent;

                    coverageTable.AddRow(Path.GetFileNameWithoutExtension(module.Key), $"{linePercent}%", $"{branchPercent}%", $"{methodPercent}%");
                }

                Console.WriteLine();
                Console.WriteLine(coverageTable.ToStringAlternative());

                coverageTable.Columns.Clear();
                coverageTable.Rows.Clear();

                coverageTable.AddColumn(new[] { "", "Line", "Branch", "Method" });
                coverageTable.AddRow("Total", $"{totalLinePercent}%", $"{totalBranchPercent}%", $"{totalMethodPercent}%");
                coverageTable.AddRow("Average", $"{averageLinePercent}%", $"{averageBranchPercent}%", $"{averageMethodPercent}%");

                Console.WriteLine(coverageTable.ToStringAlternative());

                thresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, Threshold, thresholdTypeFlags, thresholdStat);
                if (thresholdTypeFlags != ThresholdTypeFlags.None)
                {
                    var exceptionMessageBuilder = new StringBuilder();
                    if ((thresholdTypeFlags & ThresholdTypeFlags.Line) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} line coverage is below the specified {Threshold}");
                    }

                    if ((thresholdTypeFlags & ThresholdTypeFlags.Branch) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} branch coverage is below the specified {Threshold}");
                    }

                    if ((thresholdTypeFlags & ThresholdTypeFlags.Method) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} method coverage is below the specified {Threshold}");
                    }

                    throw new Exception(exceptionMessageBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return false;
            }

            return true;
        }
    }
}
