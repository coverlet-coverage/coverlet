using System;
using System.IO;

using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
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
        private readonly ISourceRootTranslator _sourceRootTranslator;
        private readonly CoverageResult _result;
        private int _delayCounter;

        public ReportWriter(string coverletMultiTargetFrameworksCurrentTFM, string directory, string output,
                            IReporter reporter, IFileSystem fileSystem, IConsole console, CoverageResult result, ISourceRootTranslator sourceRootTranslator)
            => (_coverletMultiTargetFrameworksCurrentTFM, _directory, _output, _reporter, _fileSystem, _console, _result, _sourceRootTranslator) =
                (coverletMultiTargetFrameworksCurrentTFM, directory, output, reporter, fileSystem, console, result, sourceRootTranslator);

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

            IRetryHelper retryer = new RetryHelper();
            _console.WriteLine($"  Generating report '{report}'");
            retryer.Retry(() => _fileSystem.WriteAllText(report, _reporter.Report(_result, _sourceRootTranslator)), GetDelay, maxAttemptCount: 5);
            return report;
        }

        private TimeSpan GetDelay() => _delayCounter++ switch
        {
            0 => TimeSpan.FromSeconds(1),
            1 => TimeSpan.FromSeconds(1),
            2 => TimeSpan.FromSeconds(2),
            3 => TimeSpan.FromSeconds(3),
            4 => TimeSpan.FromSeconds(5),
            5 => TimeSpan.FromSeconds(8),
            _ => TimeSpan.FromSeconds(13)
        };
    }
}
