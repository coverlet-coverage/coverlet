using System;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleTables;
using Coverlet.Core;
using Coverlet.Core.Reporters;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Palmmedia.ReportGenerator.Core;

namespace Coverlet.MSbuild.Tasks
{
    public class CoverageResultTask : Task
    {
        private string _filename;
        private string _format;
        private string _reportTypes;
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

        public string ReportTypes
        {
            get { return _reportTypes; }
            set { _reportTypes = value; }
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

                // Use Report Generator to generate HTML reports
                if (!string.IsNullOrWhiteSpace(_reportTypes))
                {
                    var reportFile = $"{_filename}.xml";

                    // Check to see if we need to create a report in the correct format for ReportGenerator
                    var createNewReport = !formats.Contains("opencover") && !formats.Contains("cobertura");
                    if (createNewReport)
                    {
                        IReporter tmpReporter = new ReporterFactory("opencover").CreateReporter();
                        reportFile = Path.Combine(Path.GetTempPath(), $"reportGenerator.{tmpReporter.Extension}");
                        File.WriteAllText(reportFile, tmpReporter.Report(result));
                    }

                    var reportTypes = _reportTypes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    new Generator().GenerateReport(new ReportConfiguration(
                        reportFilePatterns: new string[] { reportFile },
                        targetDirectory: Path.Combine(Path.GetDirectoryName(_filename), "report"),
                        historyDirectory: null,
                        reportTypes: reportTypes,
                        assemblyFilters: new string[] { },
                        classFilters: new string[] { },
                        fileFilters: new string[] { },
                        verbosityLevel: null,
                        tag: null
                    ));

                    // If we created a new report, delete it.
                    if (createNewReport)
                    {
                        File.Delete(reportFile);
                    }
                }

                var thresholdFailed = false;
                var thresholdTypes = _thresholdType.Split(',').Select(t => t.Trim());

                var summary = new CoverageSummary();
                var exceptionBuilder = new StringBuilder();
                var table = new ConsoleTable("Module", "Line", "Branch", "Method");

                foreach (var module in result.Modules)
                {
                    var linePercent = summary.CalculateLineCoverage(module.Value).Percent * 100;
                    var branchPercent = summary.CalculateBranchCoverage(module.Value).Percent * 100;
                    var methodPercent = summary.CalculateMethodCoverage(module.Value).Percent * 100;
                    table.AddRow(Path.GetFileNameWithoutExtension(module.Key), $"{linePercent}%", $"{branchPercent}%", $"{methodPercent}%");

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

                Console.WriteLine();
                Console.WriteLine(table.ToStringAlternative());
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
