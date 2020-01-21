using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Coverlet.Core.Abstracts;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Reporters;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Palmmedia.ReportGenerator.Core;
using Xunit.Sdk;

namespace Coverlet.Core.Tests
{
    [Flags]
    public enum BuildConfiguration
    {
        Debug = 1,
        Release = 2
    }

    static class TestInstrumentationAssert
    {
        public static Document Document(this CoverageResult coverageResult, string docName)
        {
            if (docName is null)
            {
                throw new ArgumentNullException(nameof(docName));
            }

            foreach (InstrumenterResult instrumenterResult in coverageResult.InstrumentedResults)
            {
                foreach (KeyValuePair<string, Document> document in instrumenterResult.Documents)
                {
                    if (Path.GetFileName(document.Key) == docName)
                    {
                        return document.Value;
                    }
                }
            }

            throw new XunitException($"Document not found '{docName}'");
        }

        public static Document AssertBranchesCovered(this Document document, params (int line, int ordinal, int hits)[] lines)
        {
            return AssertBranchesCovered(document, BuildConfiguration.Debug | BuildConfiguration.Release, lines);
        }

        public static Document ExpectedTotalNumberOfBranches(this Document document, BuildConfiguration configuration, int totalExpectedBranch)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            BuildConfiguration buildConfiguration = GetAssemblyBuildConfiguration();

            if ((buildConfiguration & configuration) != buildConfiguration)
            {
                return document;
            }

            int totalBranch = document.Branches.GroupBy(g => g.Key.Line).Count();

            if (totalBranch != totalExpectedBranch)
            {
                throw new XunitException($"Expected total branch is '{totalExpectedBranch}', actual '{totalBranch}'");
            }

            return document;
        }

        public static string ToStringBranches(this Document document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<BranchKey, Branch> branch in document.Branches)
            {
                builder.AppendLine($"({branch.Value.Number}, {branch.Value.Ordinal}, {branch.Value.Hits}),");
            }
            return builder.ToString();
        }

        public static Document AssertBranchesCovered(this Document document, BuildConfiguration configuration, params (int line, int ordinal, int hits)[] lines)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            BuildConfiguration buildConfiguration = GetAssemblyBuildConfiguration();

            if ((buildConfiguration & configuration) != buildConfiguration)
            {
                return document;
            }

            List<string> branchesToCover = new List<string>(lines.Select(b => $"[line {b.line} ordinal {b.ordinal}]"));
            foreach (KeyValuePair<BranchKey, Branch> branch in document.Branches)
            {
                foreach ((int lineToCheck, int ordinalToCheck, int expectedHits) in lines)
                {
                    if (branch.Value.Number == lineToCheck)
                    {
                        if (branch.Value.Ordinal == ordinalToCheck)
                        {
                            branchesToCover.Remove($"[line {branch.Value.Number} ordinal {branch.Value.Ordinal}]");

                            if (branch.Value.Hits != expectedHits)
                            {
                                throw new XunitException($"Unexpected hits expected line: {lineToCheck} ordinal {ordinalToCheck} hits: {expectedHits} actual hits: {branch.Value.Hits}");
                            }
                        }
                    }
                }
            }

            if (branchesToCover.Count != 0)
            {
                throw new XunitException($"Not all requested branch found, {branchesToCover.Select(l => l.ToString()).Aggregate((a, b) => $"{a}, {b}")}");
            }

            return document;
        }

        public static Document AssertLinesCovered(this Document document, params (int line, int hits)[] lines)
        {
            return AssertLinesCovered(document, BuildConfiguration.Debug | BuildConfiguration.Release, lines);
        }

        public static Document AssertLinesCoveredAllBut(this Document document, BuildConfiguration configuration, params int[] linesNumber)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            BuildConfiguration buildConfiguration = GetAssemblyBuildConfiguration();

            if ((buildConfiguration & configuration) != buildConfiguration)
            {
                return document;
            }

            foreach (KeyValuePair<int, Line> line in document.Lines)
            {
                bool skip = false;
                foreach (int number in linesNumber)
                {
                    if (line.Value.Number == number)
                    {
                        skip = true;
                        if (line.Value.Hits > 0)
                        {
                            throw new XunitException($"Hits not expected for line {line.Value.Number}");
                        }
                    }
                }

                if (skip)
                    continue;

                if (line.Value.Hits == 0)
                {
                    throw new XunitException($"Hits expected for line: {line.Value.Number}");
                }
            }

            return document;
        }

        public static Document AssertLinesCovered(this Document document, BuildConfiguration configuration, params (int line, int hits)[] lines)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            BuildConfiguration buildConfiguration = GetAssemblyBuildConfiguration();

            if ((buildConfiguration & configuration) != buildConfiguration)
            {
                return document;
            }

            List<int> linesToCover = new List<int>(lines.Select(l => l.line));
            foreach (KeyValuePair<int, Line> line in document.Lines)
            {
                foreach ((int lineToCheck, int expectedHits) in lines)
                {
                    if (line.Value.Number == lineToCheck)
                    {
                        linesToCover.Remove(line.Value.Number);
                        if (line.Value.Hits != expectedHits)
                        {
                            throw new XunitException($"Unexpected hits expected line: {lineToCheck} hits: {expectedHits} actual hits: {line.Value.Hits}");
                        }
                    }
                }
            }

            if (linesToCover.Count != 0)
            {
                throw new XunitException($"Not all requested line found, {linesToCover.Select(l => l.ToString()).Aggregate((a, b) => $"{a}, {b}")}");
            }

            return document;
        }

        public static Document AssertNonInstrumentedLines(this Document document, BuildConfiguration configuration, int from, int to)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            BuildConfiguration buildConfiguration = GetAssemblyBuildConfiguration();

            if ((buildConfiguration & configuration) != buildConfiguration)
            {
                return document;
            }

            int[] lineRange = Enumerable.Range(from, to - from + 1).ToArray();

            if (document.Lines.Select(l => l.Value.Number).Intersect(lineRange).Count() > 0)
            {
                throw new XunitException($"Unexpected instrumented lines, '{string.Join(',', lineRange)}'");
            }

            return document;
        }

        private static BuildConfiguration GetAssemblyBuildConfiguration()
        {
            var configurationAttribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>();
            if (configurationAttribute.Configuration.Equals("Debug", StringComparison.InvariantCultureIgnoreCase))
            {
                return Tests.BuildConfiguration.Debug;
            }
            else if (configurationAttribute.Configuration.Equals("Release", StringComparison.InvariantCultureIgnoreCase))
            {
                return Tests.BuildConfiguration.Release;
            }
            else
            {
                throw new NotSupportedException($"Build configuration '{configurationAttribute.Configuration}' not supported");
            }
        }
    }

    static class TestInstrumentationHelper
    {
        /// <summary>
        /// caller sample:  TestInstrumentationHelper.GenerateHtmlReport(result, sourceFileFilter: @"+**\Samples\Instrumentation.cs");
        ///                 TestInstrumentationHelper.GenerateHtmlReport(result);
        /// </summary>
        public static void GenerateHtmlReport(CoverageResult coverageResult, IReporter reporter = null, string sourceFileFilter = "", [CallerMemberName]string directory = "")
        {
            JsonReporter defaultReporter = new JsonReporter();
            reporter ??= new CoberturaReporter();
            DirectoryInfo dir = Directory.CreateDirectory(directory);
            dir.Delete(true);
            dir.Create();
            string reportFile = Path.Combine(dir.FullName, Path.ChangeExtension("report", defaultReporter.Extension));
            File.WriteAllText(reportFile, defaultReporter.Report(coverageResult));
            reportFile = Path.Combine(dir.FullName, Path.ChangeExtension("report", reporter.Extension));
            File.WriteAllText(reportFile, reporter.Report(coverageResult));
            // i.e. reportgenerator -reports:"C:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\Condition_If\report.cobertura.xml" -targetdir:"C:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\Condition_If" -filefilters:+**\Samples\Instrumentation.cs
            new Generator().GenerateReport(new ReportConfiguration(
            new[] { reportFile },
            dir.FullName,
            new string[0],
            null,
            new string[0],
            new string[0],
            new string[0],
            new string[0],
            string.IsNullOrEmpty(sourceFileFilter) ? new string[0] : new[] { sourceFileFilter },
            null,
            null));
        }

        public static CoverageResult GetCoverageResult(string filePath)
        {
            using (var result = new FileStream(filePath, FileMode.Open))
            {
                CoveragePrepareResult coveragePrepareResultLoaded = CoveragePrepareResult.Deserialize(result);
                Coverage coverage = new Coverage(coveragePrepareResultLoaded, new Mock<ILogger>().Object, DependencyInjection.Current.GetService<IInstrumentationHelper>(), new FileSystem());
                return coverage.GetCoverageResult();
            }
        }

        async public static Task<CoveragePrepareResult> Run<T>(Func<dynamic, Task> callMethod, string persistPrepareResultToFile, bool disableRestoreModules = false)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IRetryHelper, CustomRetryHelper>();
            serviceCollection.AddTransient<IProcessExitHandler, CustomProcessExitHandler>();
            serviceCollection.AddTransient<IFileSystem, FileSystem>();
            if (disableRestoreModules)
            {
                serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelperForDebugging>();
            }
            else
            {
                serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
            }

            // Setup correct retry helper to avoid exception in InstrumentationHelper.RestoreOriginalModules on remote process exit
            DependencyInjection.Set(serviceCollection.BuildServiceProvider());


            // Rename test file to avoid locks
            string location = typeof(T).Assembly.Location;
            string fileName = Path.ChangeExtension($"testgen_{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}", ".dll");
            string logFile = Path.ChangeExtension(fileName, ".log");
            string newPath = Path.Combine(Path.GetDirectoryName(location), fileName);

            File.Copy(location, newPath);
            File.Copy(Path.ChangeExtension(location, ".pdb"), Path.ChangeExtension(newPath, ".pdb"));

            // Instrument module
            Coverage coverage = new Coverage(newPath,
            new string[]
            {
                $"[{Path.GetFileNameWithoutExtension(fileName)}*]{typeof(T).FullName}*"
            },
            Array.Empty<string>(),
            new string[]
            {
                "[xunit.*]*",
                "[coverlet.*]*"
            }, Array.Empty<string>(), Array.Empty<string>(), true, false, "", false, new Logger(logFile), DependencyInjection.Current.GetService<IInstrumentationHelper>(), DependencyInjection.Current.GetService<IFileSystem>());
            CoveragePrepareResult prepareResult = coverage.PrepareModules();

            // Load new assembly
            Assembly asm = Assembly.LoadFile(newPath);

            // Instance type and call method
            await callMethod(Activator.CreateInstance(asm.GetType(typeof(T).FullName)));

            // Flush tracker
            Type tracker = asm.GetTypes().Single(n => n.FullName.Contains("Coverlet.Core.Instrumentation.Tracker"));

            // Void UnloadModule(System.Object, System.EventArgs)
            tracker.GetTypeInfo().GetMethod("UnloadModule").Invoke(null, new object[2] { null, null });

            // Persist CoveragePrepareResult
            using (FileStream fs = new FileStream(persistPrepareResultToFile, FileMode.Open))
            {
                CoveragePrepareResult.Serialize(prepareResult).CopyTo(fs);
            }

            return prepareResult;
        }
    }

    class CustomProcessExitHandler : IProcessExitHandler
    {
        public void Add(EventHandler handler)
        {
            // We don't subscribe to process exit, we let parent restore module.
            // On msbuild/console/collector code run inside same app domain so statics list of 
            // files to restore are shared, but on test we run instrumentation on child process
            // so there is a race between parent/child on files restore.
            // In normal condition Process.Exit try to restore files only in case of
            // exception and if in InstrumentationHelper._backupList there are files remained.
        }
    }

    class CustomRetryHelper : IRetryHelper
    {
        public T Do<T>(Func<T> action, Func<TimeSpan> backoffStrategy, int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(backoffStrategy());
                    }
                    return action();
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("RestoreOriginalModules"))
                    {
                        // If we're restoring modules mean that process are closing and we cannot override copied test file because is locked so we hide error
                        // to have a correct process exit value
                        return default;
                    }
                    else
                    {
                        exceptions.Add(ex);
                    }
                }
            }
            throw new AggregateException(exceptions);
        }

        public void Retry(Action action, Func<TimeSpan> backoffStrategy, int maxAttemptCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, backoffStrategy, maxAttemptCount);
        }
    }

    // We log to files for debugging pourpose, we can check if instrumentation is ok
    class Logger : ILogger
    {
        string _logFile;

        public Logger(string logFile) => _logFile = logFile;

        public void LogError(string message)
        {
            File.AppendAllText(_logFile, message + Environment.NewLine);
        }

        public void LogError(Exception exception)
        {
            File.AppendAllText(_logFile, exception.ToString() + Environment.NewLine);
        }

        public void LogInformation(string message, bool important = false)
        {
            File.AppendAllText(_logFile, message + Environment.NewLine);
        }

        public void LogVerbose(string message)
        {
            File.AppendAllText(_logFile, message + Environment.NewLine);
        }

        public void LogWarning(string message)
        {
            File.AppendAllText(_logFile, message + Environment.NewLine);
        }
    }

    class InstrumentationHelperForDebugging : InstrumentationHelper
    {
        public InstrumentationHelperForDebugging(IProcessExitHandler processExitHandler, IRetryHelper retryHelper, IFileSystem fileSystem, ILogger logger)
            : base(processExitHandler, retryHelper, fileSystem, logger)
        {

        }

        public override void RestoreOriginalModule(string module, string identifier)
        {
            // DO NOT RESTORE
        }

        public override void RestoreOriginalModules()
        {
            // DO NOT RESTORE
        }
    }
}
