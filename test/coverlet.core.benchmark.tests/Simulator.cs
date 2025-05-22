// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ConsoleTables;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Reporters;
using Coverlet.Core.Symbols;
using Microsoft.Extensions.DependencyInjection;

namespace coverlet.core.benchmark.tests
{
  [SimpleJob(launchCount: 1, warmupCount: 0, iterationCount: 3)]
  public class CoverageWorkflowBenchmark
  {
    //private string _tempRoot;
    //private string _isolatedAssemblyPath;
    private Coverage _coverage;
    private CoveragePrepareResult _coveragePrepareResult;
    private CoverageParameters _coverageParameters;
    private IInstrumentationHelper _instrumentationHelper;
    private ICecilSymbolHelper _cecilSymbolHelper;
    private Coverlet.Core.Abstractions.ILogger _logger;
    private IFileSystem _fileSystem;
    private string _coverletTestSubjectSourcePath;
    private string _coverletTestSubjectDllPath;
    private string _coverletTestSubjectArtifactPath;
    private string _rId;
    private ISourceRootTranslator _sourceRootTranslator;

    [GlobalSetup]
    public void Setup()
    {
      _logger = new ConsoleLogger();

      // Get benchmark directory
      string benchmarkDir = AppContext.BaseDirectory;
      _logger.LogInformation($"benchmark path: {benchmarkDir}");

      // Find solution root by locating Directory.Build.props
      string currentPath = benchmarkDir;
      string solutionRoot = null;
      while (currentPath != null)
      {
        string buildPropsPath = Path.Combine(currentPath, "Directory.Build.props");
        if (File.Exists(buildPropsPath))
        {
          solutionRoot = currentPath;
          break;
        }
        currentPath = Path.GetDirectoryName(currentPath);
      }

      if (solutionRoot == null)
      {
        throw new DirectoryNotFoundException("Could not find solution root containing Directory.Build.props");
      }

      _logger.LogInformation($"Solution root path: {solutionRoot}");
      // Build source location path
      _coverletTestSubjectSourcePath = Path.GetFullPath(Path.Combine(solutionRoot, "test", "coverlet.testsubject"));

      _logger.LogInformation($"Source path: {_coverletTestSubjectSourcePath}");

      if (!Directory.Exists(_coverletTestSubjectSourcePath))
      {
        throw new DirectoryNotFoundException($"Source directory not found: {_coverletTestSubjectSourcePath}");
      }

      _rId = GetPortableRuntimeIdentifier();

      StopBuildServer();

      // Set up service collection like in InstrumentationTask
      IServiceCollection serviceCollection = new ServiceCollection();

      // These can stay transient
      serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
      serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
      serviceCollection.AddTransient<IFileSystem, FileSystem>();
      serviceCollection.AddTransient<Coverlet.Core.Abstractions.ILogger>(_ => _logger);

      // Make all symbol and instrumentation related services singletons
      serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();
      serviceCollection.AddSingleton<IAssemblyAdapter, AssemblyAdapter>();
      serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(provider => new SourceRootTranslator("", provider.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(), provider.GetRequiredService<IFileSystem>()));

      serviceCollection.AddSingleton<IInstrumentationHelper>(serviceProvider =>
          new InstrumentationHelper(
              serviceProvider.GetRequiredService<IProcessExitHandler>(),
              serviceProvider.GetRequiredService<IRetryHelper>(),
              serviceProvider.GetRequiredService<IFileSystem>(),
              serviceProvider.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(),
              serviceProvider.GetRequiredService<ISourceRootTranslator>()));

      var serviceProvider = serviceCollection.BuildServiceProvider();

      // Initialize helpers using the service provider
      _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
      _instrumentationHelper = serviceProvider.GetRequiredService<IInstrumentationHelper>();
      _sourceRootTranslator = serviceProvider.GetRequiredService<ISourceRootTranslator>();
      _cecilSymbolHelper = serviceProvider.GetRequiredService<ICecilSymbolHelper>();
    }

    private void StopBuildServer()
    {
      Process.RunToCompletion(
          DotnetMuxer.Path.FullName,
          $"build-server shutdown",
          workingDirectory: _coverletTestSubjectSourcePath);
    }

    private static string GetPortableRuntimeIdentifier()
    {
      string osPart = OperatingSystem.IsWindows() ? "win" : (OperatingSystem.IsMacOS() ? "osx" : "linux");
      return $"{osPart}-{Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.RuntimeArchitecture}";
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
      try
      {
        // Ensure assemblies are unloaded
        GC.Collect();
        GC.WaitForPendingFinalizers();
        StopBuildServer();
      }
      catch (Exception ex)
      {
        _logger.LogError($"Error during cleanup: {ex}");
      }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
      Process.RunToCompletion(
          DotnetMuxer.Path.FullName,
          $"build-server shutdown",
          workingDirectory: _coverletTestSubjectSourcePath);
    }

    [Benchmark(Description = "Simulate Workflow")]
    public void SimulateWorkflow()
    {
      _logger.LogInformation($"SimulateWorkflow Directory: {Directory.GetCurrentDirectory()}");
      _coverletTestSubjectArtifactPath = Directory.GetCurrentDirectory();
      _coverletTestSubjectDllPath = Path.Combine(Directory.GetCurrentDirectory(), "coverlet.testsubject.dll");

      string pdbPath = Path.ChangeExtension(_coverletTestSubjectDllPath, ".pdb");
      if (!File.Exists(pdbPath))
      {
        throw new FileNotFoundException($"Test subject PDB not found at: {pdbPath}");
      }

      _coverageParameters = new CoverageParameters
      {
        Module = _coverletTestSubjectDllPath,
        IncludeFilters = ["[coverlet.testsubject]*"],
        IncludeDirectories = [_coverletTestSubjectArtifactPath],
        ExcludeFilters = null,
        ExcludedSourceFiles = null,
        ExcludeAttributes = null,
        IncludeTestAssembly = true,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false,
        SkipAutoProps = true,
        DeterministicReport = false,
        ExcludeAssembliesWithoutSources = "None",
      };

      Phase1_InstrumentAssemblies();
      Phase2_GenerateHits();
      Phase3_ProcessResults();
    }

    /// <summary>
    /// Instruments SUT assembly 'coverlet.testsubject' for code coverage analysis.
    /// </summary>
    /// <remarks>This method processes the SUT assembly, instruments them for code coverage.
    /// If no modules are instrumented, an <see cref="InvalidOperationException"/> is thrown.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if no modules are instrumented during the operation.</exception>

    public void Phase1_InstrumentAssemblies()
    {
      _logger.LogInformation($"Instrumenting assembly at: {_coverletTestSubjectDllPath}");

      // First verify all required assemblies are available
      var requiredAssemblies = new[]
      {
        ("Mono.Cecil", typeof(Mono.Cecil.ModuleDefinition).Assembly.Location),
        ("Target Assembly", _coverletTestSubjectDllPath)
    };

      foreach (var (name, path) in requiredAssemblies)
      {
        if (!File.Exists(path))
        {
          throw new FileNotFoundException($"Required assembly {name} not found at {path}");
        }
        _logger.LogVerbose($"Found required assembly {name} at {path}");

        try
        {
          // Only validate the assembly format
          if (IsDotNetAssembly(path).Result)
          {
            _logger.LogVerbose($"Successfully validated {name} ({Path.GetFileName(path)})");
          }
          else
          {
            throw new InvalidOperationException($"File is not a valid .NET assembly: {path}");
          }
        }
        catch (Exception ex)
        {
          _logger.LogError($"Failed to validate {name}: {ex}");
          throw;
        }
      }

      // Initialize CoveragePrepareResult with parameters
      _coveragePrepareResult = new CoveragePrepareResult
      {
        Identifier = Guid.NewGuid().ToString(),
        ModuleOrDirectory = _coverletTestSubjectDllPath,
        Results = Array.Empty<InstrumenterResult>(),
        Parameters = _coverageParameters
      };

      // Verify PDB state before proceeding
      if (!_instrumentationHelper.HasPdb(_coverletTestSubjectDllPath, out bool embedded))
      {
        throw new InvalidOperationException($"No PDB found for {_coverletTestSubjectDllPath}");
      }
      _logger.LogVerbose($"Found PDB (embedded: {embedded})");

      // Log coverage parameters
      _logger.LogVerbose("Coverage Parameters:");
      _logger.LogVerbose($"  Module: {_coveragePrepareResult.Parameters.Module}");
      _logger.LogVerbose($"  IncludeFilters: {string.Join(", ", _coveragePrepareResult.Parameters.IncludeFilters)}");
      _logger.LogVerbose($"  IncludeDirectories: {string.Join(", ", _coveragePrepareResult.Parameters.IncludeDirectories)}");

      try
      {
        // Create coverage instance
        _coverage = new Coverage(
            _coverletTestSubjectDllPath,
            _coverageParameters,
            _logger,
            _instrumentationHelper,
            _fileSystem,
            _sourceRootTranslator,
            _cecilSymbolHelper
            );

        // Prepare modules for instrumentation
        _coveragePrepareResult = _coverage.PrepareModules();

        if (_coveragePrepareResult.Results.Length == 0)
        {
          throw (new InvalidOperationException("Instrumentation failed: _coveragePrepareResult.Results missing"));
        }

      }
      catch (Exception ex)
      {
        _logger.LogError($"Instrumentation failed: {ex}");
        if (ex.InnerException != null)
        {
          _logger.LogError($"Inner exception: {ex.InnerException}");
        }
        throw;
      }
    }

    /// <summary>
    /// Run SUT assembly 'coverlet.testsubject' to generate coverage hits.
    /// </summary>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void Phase2_GenerateHits()
    {
      _logger.LogInformation($"Execute BigClass: {_coverletTestSubjectDllPath}");
      if (!File.Exists(_coverletTestSubjectDllPath))
      {
        throw new FileNotFoundException($"Instrumented assembly not found at: {_coverletTestSubjectDllPath}");
      }

      Process.RunToCompletion(
          DotnetMuxer.Path.FullName,
          $" {_coverletTestSubjectDllPath}",
          workingDirectory: _coverletTestSubjectArtifactPath);

    }

    /// <summary>
    /// Collects and processes the coverage results from SUT assembly 'coverlet.testsubject'.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Phase3_ProcessResults()
    {
      _logger.LogInformation("\nCalculating code coverage results: {_coverletTestSubjectDllPath}");
      if (_coveragePrepareResult?.Results == null)
      {
        throw new InvalidOperationException("No coverage results available to process");
      }

      // Get coverage result
      CoverageResult result = _coverage.GetCoverageResult();

      IReporter reporter = new ReporterFactory("teamcity").CreateReporter();
      if (reporter == null)
      {
        throw new InvalidOperationException($"Creating code coverage report failed");
      }

      if (reporter.OutputType == ReporterOutputType.Console)
      {
        _logger.LogInformation("  Outputting results to console", important: true);
        _logger.LogInformation(reporter.Report(result, _sourceRootTranslator), important: true);
      }

      var coverageTable = new ConsoleTable("Module", "Line", "Branch", "Method");

      CoverageDetails linePercentCalculation = CoverageSummary.CalculateLineCoverage(result.Modules);
      CoverageDetails branchPercentCalculation = CoverageSummary.CalculateBranchCoverage(result.Modules);
      CoverageDetails methodPercentCalculation = CoverageSummary.CalculateMethodCoverage(result.Modules);

      double totalLinePercent = linePercentCalculation.Percent;
      double totalBranchPercent = branchPercentCalculation.Percent;
      double totalMethodPercent = methodPercentCalculation.Percent;

      double averageLinePercent = linePercentCalculation.AverageModulePercent;
      double averageBranchPercent = branchPercentCalculation.AverageModulePercent;
      double averageMethodPercent = methodPercentCalculation.AverageModulePercent;

      foreach (KeyValuePair<string, Documents> _module in result.Modules)
      {
        double linePercent = CoverageSummary.CalculateLineCoverage(_module.Value).Percent;
        double branchPercent = CoverageSummary.CalculateBranchCoverage(_module.Value).Percent;
        double methodPercent = CoverageSummary.CalculateMethodCoverage(_module.Value).Percent;

        coverageTable.AddRow(Path.GetFileNameWithoutExtension(_module.Key), $"{InvariantFormat(linePercent)}%", $"{InvariantFormat(branchPercent)}%", $"{InvariantFormat(methodPercent)}%");
      }

      _logger.LogInformation(coverageTable.ToStringAlternative());

    }

    static string InvariantFormat(double value) => value.ToString(CultureInfo.InvariantCulture);

    static async Task<bool> IsDotNetAssembly(string fileName)
    {
      await using var stream = File.OpenRead(fileName);
      return IsDotNetAssembly(stream);
    }

    static bool IsDotNetAssembly(Stream stream)
    {
      try
      {
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata)
          return false;

        // If peReader.PEHeaders doesn't throw, it is a valid PEImage
        _ = peReader.PEHeaders.CorHeader;

        var reader = peReader.GetMetadataReader();
        return reader.IsAssembly;
      }
      catch (BadImageFormatException)
      {
        return false;
      }
    }
  }

  public class ConsoleLogger : Coverlet.Core.Abstractions.ILogger
  {
    private static readonly object s_sync = new();

    public void LogVerbose(string message)
    {
      lock (s_sync)
      {
        WriteColoredMessage("[Verbose] ", ConsoleColor.Gray, message);
      }
    }

    public void LogInformation(string message, bool important = false)
    {
      lock (s_sync)
      {
        var color = important ? ConsoleColor.Green : ConsoleColor.Gray;
        WriteColoredMessage("[Info] ", color, message);
      }
    }

    public void LogWarning(string message)
    {
      lock (s_sync)
      {
        WriteColoredMessage("[Warning] ", ConsoleColor.Yellow, message);
      }
    }

    public void LogError(string message)
    {
      lock (s_sync)
      {
        WriteColoredMessage("[Error] ", ConsoleColor.Red, message);
      }
    }

    public void LogError(Exception exception)
    {
      lock (s_sync)
      {
        WriteColoredMessage("[Error] ", ConsoleColor.Red, exception.ToString());
      }
    }

    private static void WriteColoredMessage(string prefix, ConsoleColor color, string message)
    {
      var originalColor = Console.ForegroundColor;
      try
      {
        Console.ForegroundColor = color;
        Console.Write(prefix);
        Console.ForegroundColor = originalColor;
        Console.WriteLine(message);
      }
      finally
      {
        Console.ForegroundColor = originalColor;
      }
    }
  }
  public class RemoteExecution : IDisposable
  {
    private const int FailWaitTimeoutMilliseconds = 60 * 1000;
    private readonly string _exceptionFile;

    public RemoteExecution(System.Diagnostics.Process process, string className, string methodName, string exceptionFile)
    {
      Process = process;
      ClassName = className;
      MethodName = methodName;
      _exceptionFile = exceptionFile;
    }

    public System.Diagnostics.Process Process { get; private set; }
    public string ClassName { get; }
    public string MethodName { get; }

    public void Dispose()
    {
      GC.SuppressFinalize(this); // before Dispose(true) in case the Dispose call throws
      Dispose(disposing: true);
    }

    private void Dispose(bool disposing)
    {
      //Assert.True(disposing, $"A test {ClassName}.{MethodName} forgot to Dispose() the result of RemoteInvoke()");

      if (Process != null)
      {
        //Assert.True(Process.WaitForExit(FailWaitTimeoutMilliseconds),
        //$"Timed out after {FailWaitTimeoutMilliseconds}ms waiting for remote process {Process.Id}");

        // A bit unorthodox to do throwing operations in a Dispose, but by doing it here we avoid
        // needing to do this in every derived test and keep each test much simpler.
        try
        {
          if (File.Exists(_exceptionFile))
          {
            throw new RemoteExecutionException(File.ReadAllText(_exceptionFile));
          }
        }
        finally
        {
          if (File.Exists(_exceptionFile))
          {
            File.Delete(_exceptionFile);
          }

          // Cleanup
          try { Process.Kill(); }
          catch { } // ignore all cleanup errors
        }

        Process.Dispose();
        Process = null;
      }
    }

    private sealed class RemoteExecutionException : Exception
    {
      private readonly string _stackTrace;

      internal RemoteExecutionException(string stackTrace)
          : base("Remote process failed with an unhandled exception.")
      {
        _stackTrace = stackTrace;
      }

      public override string StackTrace => _stackTrace ?? base.StackTrace;
    }
  }
  internal static class DotnetMuxer
  {
    public static FileInfo Path { get; }

    static DotnetMuxer()
    {
      var muxerFileName = ExecutableName("dotnet");
      var fxDepsFile = GetDataFromAppDomain("FX_DEPS_FILE");

      if (string.IsNullOrEmpty(fxDepsFile))
      {
        return;
      }

      var muxerDir = new FileInfo(fxDepsFile).Directory?.Parent?.Parent?.Parent;

      if (muxerDir is null)
      {
        return;
      }

      var muxerCandidate = new FileInfo(System.IO.Path.Combine(muxerDir.FullName, muxerFileName));

      if (muxerCandidate.Exists)
      {
        Path = muxerCandidate;
      }
      else
      {
        throw new InvalidOperationException("no muxer!");
      }
    }

    public static string GetDataFromAppDomain(string propertyName)
    {
      return AppContext.GetData(propertyName) as string;
    }

    public static string ExecutableName(this string withoutExtension) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? withoutExtension + ".exe"
            : withoutExtension;
  }
  public static class Process
  {
    public static int RunToCompletion(
        string command,
        string args,
        Action<string> stdOut = null,
        Action<string> stdErr = null,
        string workingDirectory = null,
        params (string key, string value)[] environmentVariables)
    {
      args ??= "";

      var process = new System.Diagnostics.Process
      {
        StartInfo =
            {
                Arguments = args,
                FileName = command,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            }
      };

      if (!string.IsNullOrWhiteSpace(workingDirectory))
      {
        process.StartInfo.WorkingDirectory = workingDirectory;
      }

      if (environmentVariables.Length > 0)
      {
        for (var i = 0; i < environmentVariables.Length; i++)
        {
          var (key, value) = environmentVariables[i];
          process.StartInfo.Environment.Add(key, value);
        }
      }

      if (stdOut != null)
      {
        process.OutputDataReceived += (sender, eventArgs) =>
        {
          if (eventArgs.Data != null)
          {
            stdOut(eventArgs.Data);
          }
        };
      }

      if (stdErr != null)
      {
        process.ErrorDataReceived += (sender, eventArgs) =>
        {
          if (eventArgs.Data != null)
          {
            stdErr(eventArgs.Data);
          }
        };
      }

      process.Start();

      process.BeginOutputReadLine();
      process.BeginErrorReadLine();

      process.WaitForExit();

      return process.ExitCode;
    }
  }
}

