using System;
using System.Collections.Generic;
using System.IO;
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

        public override bool Execute()
        {
            try
            {
                Console.WriteLine("\nCalculating coverage result...");
                var coverage = InstrumentationTask.Coverage;
                CoverageResult result = coverage.GetCoverageResult();

                var directory = Path.GetDirectoryName(_filename);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

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

                double total = 0;
                CoverageSummary summary = new CoverageSummary();
                ConsoleTable table = new ConsoleTable("Module", "Line", "Branch", "Method");

                foreach (var module in result.Modules)
                {
                    double linePercent = summary.CalculateLineCoverage(module.Value).Percent * 100;
                    double branchPercent = summary.CalculateBranchCoverage(module.Value).Percent * 100;
                    double methodPercent = summary.CalculateMethodCoverage(module.Value).Percent * 100;
                    table.AddRow(Path.GetFileNameWithoutExtension(module.Key), $"{linePercent}%", $"{branchPercent}%", $"{methodPercent}%");
                    total += linePercent;
                }

                Console.WriteLine();
                Console.WriteLine(table.ToStringAlternative());

                double average = total / result.Modules.Count;
                if (average < _threshold)
                    throw new Exception($"Overall average coverage '{average}%' is lower than specified threshold '{_threshold}%'");
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
