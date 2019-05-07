using System;
using System.IO;
using System.Linq;
using System.Text;

using ConsoleTables;
using Coverlet.Core;
using Coverlet.Core.Enums;
using Coverlet.Core.Reporters;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class CoverageResultTask : Task
    {
        private MSBuildLogger _logger;

        [Required]
        public ITaskItem InstrumenterState { get; set; }

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

        public CoverageResultTask()
        {
            _logger = new MSBuildLogger(Log);
        }

        public override bool Execute()
        {
            try
            {
                Console.WriteLine("\nCalculating coverage result...");

                if (InstrumenterState is null || !File.Exists(InstrumenterState.ItemSpec))
                {
                    _logger.LogError("Instrumenter result file not found");
                    return false;
                }

                // Deserialize and cleanup
                IInstrumentStateSerializer serializer = new JsonInstrumentStateSerializer();
                InstrumenterState instrumenterState = serializer.Deserialize(new FileStream(InstrumenterState.ItemSpec, FileMode.Open));
                File.Delete(InstrumenterState.ItemSpec);

                ICoverageCalculator coverageResult = new CoverageCalculator(instrumenterState, _logger);
                CoverageResult result = coverageResult.GetCoverageResult();

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
                        // Output to file
                        var filename = Path.GetFileName(Output);
                        filename = (filename == string.Empty) ? $"coverage.{reporter.Extension}" : filename;
                        filename = Path.HasExtension(filename) ? filename : $"{filename}.{reporter.Extension}";

                        var report = Path.Combine(directory, filename);
                        Console.WriteLine($"  Generating report '{report}'");
                        File.WriteAllText(report, reporter.Report(result));
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

                var totalLinePercent = summary.CalculateLineCoverage(result.Modules).Percent;
                var totalBranchPercent = summary.CalculateBranchCoverage(result.Modules).Percent;
                var totalMethodPercent = summary.CalculateMethodCoverage(result.Modules).Percent;

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
                coverageTable.AddRow("Average", $"{totalLinePercent / numModules}%", $"{totalBranchPercent / numModules}%", $"{totalMethodPercent / numModules}%");

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
