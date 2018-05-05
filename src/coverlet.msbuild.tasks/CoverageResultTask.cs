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

                var reporters = new List<IReporter>(new ReporterFactory(_format).CreateReporters());
                if (reporters.Count != _format.Split(',').Length)
                    throw new Exception($"Specified output format '{_format}' is not supported");

                foreach(var reporter in reporters)
                {
                    var reportFilename = _filename + "." + reporter.Extension;
                    Console.WriteLine($"  Generating report '{reportFilename}'");
                    File.WriteAllText(reportFilename, reporter.Report(result));
                }

                double total = 0;
                CoverageSummary summary = new CoverageSummary();
                ConsoleTable table = new ConsoleTable("Module", "Coverage");

                foreach (var module in result.Modules)
                {
                    double percent = summary.CalculateLineCoverage(module.Value) * 100;
                    table.AddRow(System.IO.Path.GetFileNameWithoutExtension(module.Key), $"{percent}%");
                    total += percent;
                }

                Console.WriteLine();
                Console.WriteLine(table.ToMarkDownString());

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
