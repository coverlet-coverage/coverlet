using System.IO;

using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Reporters;

namespace Coverlet.MSbuild.Tasks
{
    internal class ReportWriter
    {
        private readonly string _coverletMultiTargetFrameworksCurrentTFM;
        private readonly string _directory;
        private readonly string _output;
        private readonly IReporter _reporter;
        private readonly IFileSystem _fileSystem;
        private readonly IConsole _console;
        private readonly CoverageResult _result;

        public ReportWriter(string coverletMultiTargetFrameworksCurrentTFM, string directory, string output, IReporter reporter, IFileSystem fileSystem, IConsole console, CoverageResult result)
            => (_coverletMultiTargetFrameworksCurrentTFM, _directory, _output, _reporter, _fileSystem, _console, _result) =
                (coverletMultiTargetFrameworksCurrentTFM, directory, output, reporter, fileSystem, console, result);

        public string WriteReport()
        {
            string filename = Path.GetFileName(_output);

            string separatorPoint = string.IsNullOrEmpty(_coverletMultiTargetFrameworksCurrentTFM) ? "" : ".";

            if (filename == string.Empty)
            {
                // empty filename for instance only directory is passed to CoverletOutput c:\reportpath
                // c:\reportpath\coverage.reportedextension
                filename = $"coverage.{_coverletMultiTargetFrameworksCurrentTFM}{separatorPoint}{_reporter.Extension}";
            }
            else if (Path.HasExtension(filename))
            {
                // filename with extension for instance c:\reportpath\file.ext
                // we keep user specified name
                filename = $"{Path.GetFileNameWithoutExtension(filename)}{separatorPoint}{_coverletMultiTargetFrameworksCurrentTFM}{Path.GetExtension(filename)}";
            }
            else
            {
                // filename without extension for instance c:\reportpath\file
                // c:\reportpath\file.reportedextension
                filename = $"{filename}{separatorPoint}{_coverletMultiTargetFrameworksCurrentTFM}.{_reporter.Extension}";
            }

            string report = Path.Combine(_directory, filename);
            _console.WriteLine($"  Generating report '{report}'");
            _fileSystem.WriteAllText(report, _reporter.Report(_result));
            return report;
        }
    }
}
