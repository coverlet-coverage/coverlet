using System;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleTables;
using Coverlet.Core;
using Coverlet.Core.Reporters;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class CoverageResultTask : Task
    {
        private string _output;
        private string _format;
        private int _threshold;
        private string _thresholdType;

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
        public int Threshold
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

        public override bool Execute()
        {
            try
            {
                Console.WriteLine("\nCalculating coverage result...");

                var coverage = InstrumentationTask.Coverage;
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
                        throw new Exception($"Specified output format '{format}' is not supported");

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

                var thresholdFailed = false;
                var thresholdTypes = _thresholdType.Split(',').Select(t => t.Trim());
                var summary = new CoverageSummary();
                var exceptionBuilder = new StringBuilder();
                var coverageTable = new ConsoleTable("Module", "Line", "Branch", "Method");
                var overallLineCoverage = summary.CalculateLineCoverage(result.Modules);
                var overallBranchCoverage = summary.CalculateBranchCoverage(result.Modules);
                var overallMethodCoverage = summary.CalculateMethodCoverage(result.Modules);

                if (_threshold > 0)
                {
                    var overallLinePercent = overallLineCoverage.Percent * 100;
                    var overallBranchPercent = overallBranchCoverage.Percent * 100;
                    var overallMethodPercent = overallMethodCoverage.Percent * 100;

                    if (overallLinePercent < _threshold && thresholdTypes.Contains("total-line", StringComparer.OrdinalIgnoreCase))
                    {
                        exceptionBuilder.AppendLine($"Project has a overall line coverage '{overallLinePercent}%' below specified threshold '{_threshold}%'");
                        thresholdFailed = true;
                    }

                    if (overallBranchPercent < _threshold && thresholdTypes.Contains("total-branch", StringComparer.OrdinalIgnoreCase))
                    {
                        exceptionBuilder.AppendLine($"Project has a overall branch coverage '{overallBranchPercent}%' below specified threshold '{_threshold}%'");
                        thresholdFailed = true;
                    }

                    if (overallMethodPercent < _threshold && thresholdTypes.Contains("total-method", StringComparer.OrdinalIgnoreCase))
                    {
                        exceptionBuilder.AppendLine($"Project has a overall method coverage '{overallMethodPercent}%' below specified threshold '{_threshold}%'");
                        thresholdFailed = true;
                    }
                }
                
                foreach (var module in result.Modules)
                {
                    var linePercent = summary.CalculateLineCoverage(module.Value).Percent * 100;
                    var branchPercent = summary.CalculateBranchCoverage(module.Value).Percent * 100;
                    var methodPercent = summary.CalculateMethodCoverage(module.Value).Percent * 100;

                    coverageTable.AddRow(Path.GetFileNameWithoutExtension(module.Key), $"{linePercent}%", $"{branchPercent}%", $"{methodPercent}%");

                    if (_threshold > 0)
                    {
                        if (linePercent < _threshold && thresholdTypes.Contains("line", StringComparer.OrdinalIgnoreCase))
                        {
                            exceptionBuilder.AppendLine($"'{Path.GetFileNameWithoutExtension(module.Key)}' has a line coverage '{linePercent}%' below specified threshold '{_threshold}%'");
                            thresholdFailed = true;
                        }

                        if (branchPercent < _threshold && thresholdTypes.Contains("branch", StringComparer.OrdinalIgnoreCase))
                        {
                            exceptionBuilder.AppendLine($"'{Path.GetFileNameWithoutExtension(module.Key)}' has a branch coverage '{branchPercent}%' below specified threshold '{_threshold}%'");
                            thresholdFailed = true;
                        }

                        if (methodPercent < _threshold && thresholdTypes.Contains("method", StringComparer.OrdinalIgnoreCase))
                        {
                            exceptionBuilder.AppendLine($"'{Path.GetFileNameWithoutExtension(module.Key)}' has a method coverage '{methodPercent}%' below specified threshold '{_threshold}%'");
                            thresholdFailed = true;
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine(coverageTable.ToStringAlternative());
                Console.WriteLine($"Total Line: {overallLineCoverage.Percent * 100}%");
                Console.WriteLine($"Total Branch: {overallBranchCoverage.Percent * 100}%");
                Console.WriteLine($"Total Method: {overallMethodCoverage.Percent * 100}%");

                if (thresholdFailed)
                    throw new Exception(exceptionBuilder.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }

            return true;
        }
    }
}
