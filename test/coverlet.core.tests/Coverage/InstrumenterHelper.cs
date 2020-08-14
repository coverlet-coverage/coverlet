using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Reporters;
using Coverlet.Core.Symbols;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Palmmedia.ReportGenerator.Core;
using Tmds.Utils;
using Xunit;

namespace Coverlet.Core.Tests
{
    static class TestInstrumentationHelper
    {
        private static IServiceProvider _processWideContainer;

        /// <summary>
        /// caller sample:  TestInstrumentationHelper.GenerateHtmlReport(result, sourceFileFilter: @"+**\Samples\Instrumentation.cs");
        ///                 TestInstrumentationHelper.GenerateHtmlReport(result);
        /// </summary>
        public static void GenerateHtmlReport(CoverageResult coverageResult, IReporter reporter = null, string sourceFileFilter = "", [CallerMemberName] string directory = "")
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
            Assert.True(new Generator().GenerateReport(new ReportConfiguration(
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
            null)));
        }

        public static CoverageResult GetCoverageResult(string filePath)
        {
            SetTestContainer();
            using var result = new FileStream(filePath, FileMode.Open);
            var logger = new Mock<ILogger>();
            logger.Setup(l => l.LogVerbose(It.IsAny<string>())).Callback((string message) =>
            {
                Assert.DoesNotContain("not found for module: ", message);
            });
            _processWideContainer.GetRequiredService<IInstrumentationHelper>().SetLogger(logger.Object);
            CoveragePrepareResult coveragePrepareResultLoaded = CoveragePrepareResult.Deserialize(result);
            Coverage coverage = new Coverage(coveragePrepareResultLoaded, logger.Object, _processWideContainer.GetService<IInstrumentationHelper>(), new FileSystem(), new SourceRootTranslator(new Mock<ILogger>().Object, new FileSystem()));
            return coverage.GetCoverageResult();
        }

        async public static Task<CoveragePrepareResult> Run<T>(Func<dynamic, Task> callMethod,
                                                               Func<string, string[]> includeFilter = null,
                                                               Func<string, string[]> excludeFilter = null,
                                                               string persistPrepareResultToFile = null,
                                                               bool disableRestoreModules = false,
                                                               bool skipAutoProps = false)
        {
            if (persistPrepareResultToFile is null)
            {
                throw new ArgumentNullException(nameof(persistPrepareResultToFile));
            }

            // Rename test file to avoid locks
            string location = typeof(T).Assembly.Location;
            string fileName = Path.ChangeExtension($"testgen_{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}", ".dll");
            string logFile = Path.ChangeExtension(fileName, ".log");
            string newPath = Path.Combine(Path.GetDirectoryName(location), fileName);

            File.Copy(location, newPath);
            File.Copy(Path.ChangeExtension(location, ".pdb"), Path.ChangeExtension(newPath, ".pdb"));

            SetTestContainer(newPath, disableRestoreModules);

            static string[] defaultFilters(string _) => Array.Empty<string>();

            CoverageParameters parameters = new CoverageParameters
            {
                IncludeFilters = (includeFilter is null ? defaultFilters(fileName) : includeFilter(fileName)).Concat(
                new string[]
                {
                    $"[{Path.GetFileNameWithoutExtension(fileName)}*]{typeof(T).FullName}*"
                }).ToArray(),
                IncludeDirectories = Array.Empty<string>(),
                ExcludeFilters = (excludeFilter is null ? defaultFilters(fileName) : excludeFilter(fileName)).Concat(new string[]
                {
                    "[xunit.*]*",
                    "[coverlet.*]*"
                }).ToArray(),
                ExcludedSourceFiles = Array.Empty<string>(),
                ExcludeAttributes = Array.Empty<string>(),
                IncludeTestAssembly = true,
                SingleHit = false,
                MergeWith = string.Empty,
                UseSourceLink = false,
                SkipAutoProps = skipAutoProps
            };

            // Instrument module
            Coverage coverage = new Coverage(newPath, parameters, new Logger(logFile),
            _processWideContainer.GetService<IInstrumentationHelper>(), _processWideContainer.GetService<IFileSystem>(), _processWideContainer.GetService<ISourceRootTranslator>(), _processWideContainer.GetService<ICecilSymbolHelper>());
            CoveragePrepareResult prepareResult = coverage.PrepareModules();

            Assert.Single(prepareResult.Results);

            // Load new assembly
            Assembly asm = Assembly.LoadFile(newPath);

            // Instance type and call method
            await callMethod(Activator.CreateInstance(asm.GetType(typeof(T).FullName)));

            // Flush tracker
            Type tracker = asm.GetTypes().Single(n => n.FullName.Contains("Coverlet.Core.Instrumentation.Tracker"));

            // For debugging purpouse
            // int[] hitsArray = (int[])tracker.GetField("HitsArray").GetValue(null);
            // string hitsFilePath = (string)tracker.GetField("HitsFilePath").GetValue(null);

            // Void UnloadModule(System.Object, System.EventArgs)
            tracker.GetTypeInfo().GetMethod("UnloadModule").Invoke(null, new object[2] { null, null });

            // Persist CoveragePrepareResult
            using (FileStream fs = new FileStream(persistPrepareResultToFile, FileMode.Open))
            {
                await CoveragePrepareResult.Serialize(prepareResult).CopyToAsync(fs);
            }

            return prepareResult;
        }

        private static void SetTestContainer(string testModule = null, bool disableRestoreModules = false)
        {
            LazyInitializer.EnsureInitialized(ref _processWideContainer, () =>
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddTransient<IRetryHelper, CustomRetryHelper>();
                serviceCollection.AddTransient<IProcessExitHandler, CustomProcessExitHandler>();
                serviceCollection.AddTransient<IFileSystem, FileSystem>();
                serviceCollection.AddTransient(_ => new Mock<ILogger>().Object);

                // We need to keep singleton/static semantics
                if (disableRestoreModules)
                {
                    serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelperForDebugging>();
                }
                else
                {
                    serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
                }
                serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(serviceProvider =>
                string.IsNullOrEmpty(testModule) ?
                new SourceRootTranslator(serviceProvider.GetRequiredService<ILogger>(), serviceProvider.GetRequiredService<IFileSystem>()) :
                new SourceRootTranslator(testModule, serviceProvider.GetRequiredService<ILogger>(), serviceProvider.GetRequiredService<IFileSystem>()));

                serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();

                return serviceCollection.BuildServiceProvider();
            });
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
                    if (ex.ToString().Contains("RestoreOriginalModules") || ex.ToString().Contains("RestoreOriginalModule"))
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
        public InstrumentationHelperForDebugging(IProcessExitHandler processExitHandler, IRetryHelper retryHelper, IFileSystem fileSystem, ILogger logger, ISourceRootTranslator sourceTranslator)
            : base(processExitHandler, retryHelper, fileSystem, logger, sourceTranslator)
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

    public abstract class ExternalProcessExecutionTest
    {
        protected FunctionExecutor FunctionExecutor = new FunctionExecutor(
        o =>
        {
            o.StartInfo.RedirectStandardError = true;
            o.OnExit = p =>
            {
                if (p.ExitCode != 0)
                {
                    string message = $"Function exit code failed with exit code: {p.ExitCode}" + Environment.NewLine +
                                      p.StandardError.ReadToEnd();
                    throw new Xunit.Sdk.XunitException(message);
                }
            };
        });
    }

    public static class FunctionExecutorExtensions
    {
        public static void RunInProcess(this FunctionExecutor executor, Func<string[], Task<int>> func, string[] args)
        {
            Assert.Equal(0, func(args).Result);
        }

        public static void RunInProcess(this FunctionExecutor executor, Func<Task<int>> func)
        {
            Assert.Equal(0, func().Result);
        }
    }
}
