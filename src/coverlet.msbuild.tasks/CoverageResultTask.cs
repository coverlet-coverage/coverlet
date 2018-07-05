using System;
using System.Diagnostics;
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
        private string _filename;
        private string _format;
        private int _threshold;
        private string _thresholdType;

        [Required]
        public string Output
        {
            get { return _filename; }
            set { _filename = value; }
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
                var duration = new Stopwatch();
                duration.Start();
                var coverage = InstrumentationTask.Coverage;
                var result = coverage.GetCoverageResult();
                duration.Stop();
                Console.WriteLine($"Results calculated in {duration.Elapsed.TotalSeconds} seconds");

                var directory = Path.GetDirectoryName(_filename);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var formats = _format.Split(',');
                foreach (var format in formats)
                {
                    var reporter = new ReporterFactory(format).CreateReporter();
                    if (reporter == null)
                        throw new Exception($"Specified output format '{format}' is not supported");

                    var report = _filename + "." + reporter.Extension;
                    Console.WriteLine($"  Generating report '{report}'");
                    File.WriteAllText(report, reporter.Report(result));
                }

                var thresholdFailed = false;
                var thresholdTypes = _thresholdType.Split(',').Select(t => t.Trim());
                var summary = new CoverageSummary();
                var exceptionBuilder = new StringBuilder();
                var coverageTable = new ConsoleTable("Module", "Line", "Branch", "Method");
                var averageTable = new ConsoleTable("", "Line", "Branch", "Method");
                var lineAverage = 0d;
                var branchAverage = 0d;
                var methodAverage = 0d;

                foreach (var module in result.Modules)
                {
                    var linePercent = summary.CalculateLineCoverage(module.Value).Percent * 100;
                    var branchPercent = summary.CalculateBranchCoverage(module.Value).Percent * 100;
                    var methodPercent = summary.CalculateMethodCoverage(module.Value).Percent * 100;

                    lineAverage += linePercent;
                    branchAverage += branchPercent;
                    methodAverage += methodPercent;

                    coverageTable.AddRow(Path.GetFileNameWithoutExtension(module.Key), $"{linePercent}%", $"{branchPercent}%", $"{methodPercent}%");

                    if (_threshold > 0)
                    {
                        if (linePercent < _threshold && thresholdTypes.Contains("line"))
                        {
                            exceptionBuilder.AppendLine($"'{Path.GetFileNameWithoutExtension(module.Key)}' has a line coverage '{linePercent}%' below specified threshold '{_threshold}%'");
                            thresholdFailed = true;
                        }

                        if (branchPercent < _threshold && thresholdTypes.Contains("branch"))
                        {
                            exceptionBuilder.AppendLine($"'{Path.GetFileNameWithoutExtension(module.Key)}' has a branch coverage '{branchPercent}%' below specified threshold '{_threshold}%'");
                            thresholdFailed = true;
                        }

                        if (methodPercent < _threshold && thresholdTypes.Contains("method"))
                        {
                            exceptionBuilder.AppendLine($"'{Path.GetFileNameWithoutExtension(module.Key)}' has a method coverage '{methodPercent}%' below specified threshold '{_threshold}%'");
                            thresholdFailed = true;
                        }
                    }
                }

                lineAverage = lineAverage / result.Modules.Count;
                branchAverage = branchAverage / result.Modules.Count;
                methodAverage = methodAverage / result.Modules.Count;
                averageTable.AddRow("Average", $"{lineAverage}%", $"{branchAverage}%", $"{methodAverage}%");

                Console.WriteLine();
                Console.WriteLine(coverageTable.ToStringAlternative());
                Console.WriteLine(averageTable.ToStringAlternative());

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
