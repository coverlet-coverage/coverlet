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
                if (_format == "lcov")
                    reporter = new LcovReporter();
                else
                    reporter = new JsonReporter();

                File.WriteAllText(_filename, result.Format(reporter));

                CoverageSummary coverageSummary = new CoverageSummary(result);
                var summary = coverageSummary.CalculateSummary();

                ConsoleTable table = new ConsoleTable("Module", "Coverage");
                foreach (var item in summary)
                    table.AddRow(item.Key, $"{item.Value}%");

                Console.WriteLine();
                table.Write(Format.Alternative);
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
