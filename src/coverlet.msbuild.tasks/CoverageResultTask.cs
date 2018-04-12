using System;
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

                Console.WriteLine($"  Generating report '{_filename}'");

                IReporter reporter = default(IReporter);
                switch (_format)
                {
                    case "lcov":
                        reporter = new LcovReporter();
                        break;
                    case "opencover":
                        reporter = new OpenCoverReporter();
                        break;
                    case "cobertura":
                        reporter = new CoberturaReporter();
                        break;
                    default:
                        reporter = new JsonReporter();
                        break;
                }

                File.WriteAllText(_filename, result.Format(reporter));

                int total = 0;
                CoverageSummary coverageSummary = new CoverageSummary(result);
                var summary = coverageSummary.CalculateSummary();

                ConsoleTable table = new ConsoleTable("Module", "Coverage");
                foreach (var item in summary)
                {
                    table.AddRow(item.Key, $"{item.Value}%");
                    total += item.Value;
                }

                Console.WriteLine();
                table.Write(Format.Alternative);

                int average = total / summary.Count;
                if (average < _threshold)
                    return false;
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
