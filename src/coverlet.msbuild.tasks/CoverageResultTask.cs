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
        private string _filename;
        private string _format;
        private int _threshold;
        private string _thresholdTypes;

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
            get { return _thresholdTypes; }
            set { _thresholdTypes = value; }
        }

        public override bool Execute()
        {
            try
            {
                Console.WriteLine("\nCalculating coverage result...");
                var coverage = InstrumentationTask.Coverage;
                var result = coverage.GetCoverageResult();

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

                var branchTotal = 0d;
                var methodTotal = 0d;
                var lineTotal = 0d;
                var summary = new CoverageSummary();
                var table = new ConsoleTable("Module", "Line", "Branch", "Method");

                foreach (var module in result.Modules)
                {
                    var linePercent = summary.CalculateLineCoverage(module.Value).Percent * 100;
                    var branchPercent = summary.CalculateBranchCoverage(module.Value).Percent * 100;
                    var methodPercent = summary.CalculateMethodCoverage(module.Value).Percent * 100;
                    table.AddRow(Path.GetFileNameWithoutExtension(module.Key), $"{linePercent}%", $"{branchPercent}%", $"{methodPercent}%");

                    lineTotal += linePercent;
                    branchTotal += branchPercent;
                    methodTotal += methodPercent;
                }

                Console.WriteLine();
                Console.WriteLine(table.ToStringAlternative());

                if (_threshold > 0)
                {
                    var thresholdFailed = false;
                    var exceptionBuilder = new StringBuilder();
                    var lineAverage = lineTotal / result.Modules.Count;
                    var branchAverage = branchTotal / result.Modules.Count;
                    var methodAverage = methodTotal / result.Modules.Count;
                    var thresholdTypes = _thresholdTypes.Split(',').Select(t => t.ToLower());
                    foreach (var thresholdType in thresholdTypes)
                    {
                        if (thresholdType == "line" && lineAverage < _threshold)
                        {
                            thresholdFailed = true;
                            exceptionBuilder.AppendLine($"Overall average '{thresholdType}' coverage '{lineAverage}%' is lower than specified threshold '{_threshold}%'");
                        }

                        else if (thresholdType == "branch" && branchAverage < _threshold)
                        {
                            thresholdFailed = true;
                            exceptionBuilder.AppendLine($"Overall average '{thresholdType}' coverage '{branchAverage}%' is lower than specified threshold '{_threshold}%'");
                        }

                        else if (thresholdType == "method" && methodAverage < _threshold)
                        {
                            thresholdFailed = true;
                            exceptionBuilder.AppendLine($"Overall average '{thresholdType}' coverage '{methodAverage}%' is lower than specified threshold '{_threshold}%'");
                        }

                        else if (thresholdType != "line" && thresholdType != "branch" && thresholdType != "method")
                        {
                            Console.WriteLine($"Threshold type of {thresholdType} is not recognized/supported and will be ignored.");
                        }
                    }

                    if (thresholdFailed)
                    {
                        throw new Exception(exceptionBuilder.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
                    }
                }
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
