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
        private string _output;
        private string _format;
        private double _threshold;
        private string _thresholdType;
        private string _thresholdStat;
        private ITaskItem _instrumenterState;
        private MSBuildLogger _logger;

        [Required]
        public string Output
        {
            get { return _output; }
            set { _output = value; }
        }

        [Required]
        public string OutputFormat
        {
            get { return _format; }
            set { _format = value; }
        }

        [Required]
        public double Threshold
        {
            get { return _threshold; }
            set { _threshold = value; }
        }

        [Required]
        public string ThresholdType
        {
            get { return _thresholdType; }
            set { _thresholdType = value; }
        }

        [Required]
        public string ThresholdStat
        {
            get { return _thresholdStat; }
            set { _thresholdStat = value; }
        }

        [Required]
        public ITaskItem InstrumenterState
        {
            get { return _instrumenterState; }
            set { _instrumenterState = value; }
        }

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
                    _logger.LogError("Result of instrumentation task not found");
                    return false;
                }

                var coverage = new Coverage(CoveragePrepareResult.Deserialize(new FileStream(InstrumenterState.ItemSpec, FileMode.Open)), this._logger);
                var result = coverage.GetCoverageResult();

                var directory = Path.GetDirectoryName(_output);
                if (directory == string.Empty)
                {
                    directory = Directory.GetCurrentDirectory();
                }
                else if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var formats = _format.Split(',');
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
                        var filename = Path.GetFileName(_output);
                        filename = (filename == string.Empty) ? $"coverage.{reporter.Extension}" : filename;
                        filename = Path.HasExtension(filename) ? filename : $"{filename}.{reporter.Extension}";

                        var report = Path.Combine(directory, filename);
                        Console.WriteLine($"  Generating report '{report}'");
                        File.WriteAllText(report, reporter.Report(result));
                    }
                }

                var thresholdTypeFlags = ThresholdTypeFlags.None;
                var thresholdStat = ThresholdStatistic.Minimum;

                foreach (var thresholdType in _thresholdType.Split(',').Select(t => t.Trim()))
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

                if (_thresholdStat.Equals("average", StringComparison.OrdinalIgnoreCase))
                {
                    thresholdStat = ThresholdStatistic.Average;
                }
                else if (_thresholdStat.Equals("total", StringComparison.OrdinalIgnoreCase))
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

                thresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, _threshold, thresholdTypeFlags, thresholdStat);
                if (thresholdTypeFlags != ThresholdTypeFlags.None)
                {
                    var exceptionMessageBuilder = new StringBuilder();
                    if ((thresholdTypeFlags & ThresholdTypeFlags.Line) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} line coverage is below the specified {_threshold}");
                    }

                    if ((thresholdTypeFlags & ThresholdTypeFlags.Branch) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} branch coverage is below the specified {_threshold}");
                    }

                    if ((thresholdTypeFlags & ThresholdTypeFlags.Method) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} method coverage is below the specified {_threshold}");
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
