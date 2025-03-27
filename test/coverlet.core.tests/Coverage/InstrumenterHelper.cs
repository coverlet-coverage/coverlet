// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
using Xunit;

namespace Coverlet.Core.Tests
{
  static class TestInstrumentationHelper
  {
    private static IServiceProvider s_processWideContainer;

    /// <summary>
    /// caller sample:  TestInstrumentationHelper.GenerateHtmlReport(result, sourceFileFilter: @"+**\Samples\Instrumentation.cs");
    ///                 TestInstrumentationHelper.GenerateHtmlReport(result);
    /// </summary>
    public static void GenerateHtmlReport(CoverageResult coverageResult, IReporter reporter = null, string sourceFileFilter = "", [CallerMemberName] string directory = "")
    {
      var defaultReporter = new JsonReporter();
      reporter ??= new CoberturaReporter();
      DirectoryInfo dir = Directory.CreateDirectory(directory);
      dir.Delete(true);
      dir.Create();
      string reportFile = Path.Combine(dir.FullName, Path.ChangeExtension("report", defaultReporter.Extension));
      File.WriteAllText(reportFile, defaultReporter.Report(coverageResult, new Mock<ISourceRootTranslator>().Object));
      reportFile = Path.Combine(dir.FullName, Path.ChangeExtension("report", reporter.Extension));
      File.WriteAllText(reportFile, reporter.Report(coverageResult, new Mock<ISourceRootTranslator>().Object));
      // i.e. reportgenerator -reports:"C:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\Condition_If\report.cobertura.xml" -targetdir:"C:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\Condition_If" -filefilters:+**\Samples\Instrumentation.cs
      Assert.True(new Generator().GenerateReport(new ReportConfiguration(
      [reportFile],
      dir.FullName,
      [],
      null,
      [],
      [],
      [],
      [],
      string.IsNullOrEmpty(sourceFileFilter) ? [] : new[] { sourceFileFilter },
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
      s_processWideContainer.GetRequiredService<IInstrumentationHelper>().SetLogger(logger.Object);
      var coveragePrepareResultLoaded = CoveragePrepareResult.Deserialize(result);
      var coverage = new Coverage(coveragePrepareResultLoaded, logger.Object, s_processWideContainer.GetService<IInstrumentationHelper>(), new FileSystem(), new SourceRootTranslator(new Mock<ILogger>().Object, new FileSystem()));
      return coverage.GetCoverageResult();
    }

    public static async Task<CoveragePrepareResult> Run<T>(Func<dynamic, Task> callMethod,
                                                           Func<string, string[]> includeFilter = null,
                                                           Func<string, string[]> excludeFilter = null,
                                                           Func<string, string[]> doesNotReturnAttributes = null,
                                                           string persistPrepareResultToFile = null,
                                                           bool disableRestoreModules = false,
                                                           bool skipAutoProps = false,
                                                           string assemblyLocation = null)
    {
      ArgumentNullException.ThrowIfNull(persistPrepareResultToFile);

      // Rename test file to avoid locks
      string location = typeof(T).Assembly.Location;
      string fileName = Path.ChangeExtension($"testgen_{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}", ".dll");
      string logFile = Path.ChangeExtension(fileName, ".log");
      string newPath = Path.Combine(Path.GetDirectoryName(location), fileName);

      File.Copy(location, newPath);
      File.Copy(Path.ChangeExtension(location, ".pdb"), Path.ChangeExtension(newPath, ".pdb"));

      string sourceRootTranslatorModulePath = assemblyLocation ?? newPath;
      SetTestContainer(sourceRootTranslatorModulePath, disableRestoreModules);

      static string[] defaultFilters(string _) => [];

      var parameters = new CoverageParameters
      {
        IncludeFilters = [.. (includeFilter is null ? defaultFilters(fileName) : includeFilter(fileName)),
                          $"[{Path.GetFileNameWithoutExtension(fileName)}*]{GetTypeFullName<T>()}*",],
        IncludeDirectories = [],
        ExcludeFilters = [.. (excludeFilter is null ? defaultFilters(fileName) : excludeFilter(fileName)), "[xunit.*]*", "[coverlet.*]*"],
        ExcludedSourceFiles = [],
        ExcludeAttributes = [],
        IncludeTestAssembly = true,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false,
        SkipAutoProps = skipAutoProps,
        DoesNotReturnAttributes = doesNotReturnAttributes?.Invoke(fileName)
      };

      // Instrument module
      var coverage = new Coverage(newPath, parameters, new Logger(logFile),
      s_processWideContainer.GetService<IInstrumentationHelper>(), s_processWideContainer.GetService<IFileSystem>(), s_processWideContainer.GetService<ISourceRootTranslator>(), s_processWideContainer.GetService<ICecilSymbolHelper>());
      CoveragePrepareResult prepareResult = coverage.PrepareModules();

      Assert.Single(prepareResult.Results);

      // Load new assembly
      var asm = Assembly.LoadFile(newPath);

      // Instance type and call method
      await callMethod(Activator.CreateInstance(asm.GetType(typeof(T).FullName)));

      // Flush tracker
#pragma warning disable CA1307 // Specify StringComparison for clarity
      Type tracker = asm.GetTypes().Single(n => n.FullName.Contains("Coverlet.Core.Instrumentation.Tracker"));
#pragma warning restore CA1307 // Specify StringComparison for clarity

      // For debugging purpose
      // int[] hitsArray = (int[])tracker.GetField("HitsArray").GetValue(null);
      // string hitsFilePath = (string)tracker.GetField("HitsFilePath").GetValue(null);

      // Void UnloadModule(System.Object, System.EventArgs)
      tracker.GetTypeInfo().GetMethod("UnloadModule").Invoke(null, [null, null]);

      // Persist CoveragePrepareResult
      using (var fs = new FileStream(persistPrepareResultToFile, FileMode.Open))
      {
        await CoveragePrepareResult.Serialize(prepareResult).CopyToAsync(fs);
      }

      return prepareResult;
    }

    private static void SetTestContainer(string testModule = null, bool disableRestoreModules = false)
    {
      LazyInitializer.EnsureInitialized(ref s_processWideContainer, () =>
      {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<IRetryHelper, CustomRetryHelper>();
        serviceCollection.AddTransient<IProcessExitHandler, CustomProcessExitHandler>();
        serviceCollection.AddTransient<IFileSystem, FileSystem>();
        serviceCollection.AddTransient<IAssemblyAdapter, AssemblyAdapter>();
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
              new SourceRootTranslator(testModule, serviceProvider.GetRequiredService<ILogger>(), serviceProvider.GetRequiredService<IFileSystem>(), serviceProvider.GetRequiredService<IAssemblyAdapter>()));

        serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();

        return serviceCollection.BuildServiceProvider();
      });
    }

    private static string GetTypeFullName<T>()
    {
      string name = typeof(T).FullName;
      if (typeof(T).IsGenericType && name != null)
      {
        int index = name.IndexOf('`');
        return index == -1 ? name : name[..index];
      }
      return name;
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
        catch (DirectoryNotFoundException)
        {
          throw;
        }
        catch (IOException ex)
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
      // Do not throw exception if we're restoring modules
      if (exceptions.ToString().Contains("RestoreOriginalModules") || exceptions.ToString().Contains("RestoreOriginalModule"))
      {
        return default;
      }
      else
      {
        throw new AggregateException(exceptions);
      }
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

  // We log to files for debugging purpose, we can check if instrumentation is ok
  class Logger : ILogger
  {
    readonly string _logFile;

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
}
