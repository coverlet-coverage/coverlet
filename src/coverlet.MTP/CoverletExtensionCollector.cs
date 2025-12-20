// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using coverlet.Extension.Logging;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Reporters;
using Coverlet.Core.Symbols;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Coverlet.MTP.CommandLine;
using Coverlet.Core.Instrumentation;

namespace coverlet.Extension.Collector
{
  /// <summary>
  /// Implements test host process lifetime handling for coverage collection using the Microsoft Testing Platform.
  /// </summary>
  internal sealed class CoverletExtensionCollector : ITestHostProcessLifetimeHandler
  {
    private readonly CoverletLoggerAdapter _logger;
    private readonly CoverletExtensionConfiguration _configuration;
    private IServiceProvider? _serviceProvider;
    private readonly Microsoft.Testing.Platform.Configurations.IConfiguration? _platformConfiguration;
    private Coverage? _coverage;
    private readonly Microsoft.Testing.Platform.Logging.ILoggerFactory _loggerFactory;
    private readonly Microsoft.Testing.Platform.CommandLine.ICommandLineOptions _commandLineOptions;
    private bool _coverageEnabled;
    private string? _testModulePath;

    private readonly CoverletExtension _extension = new();

    // Default exclude filter matching coverlet.collector
    private const string DefaultExcludeFilter = "[xunit.*]*";

    // Debug environment variable - set to "1" to attach debugger
    private const string DebugEnvironmentVariable = "COVERLET_MTP_DEBUG";

    // Diagnostic file environment variable - set path to write diagnostics
    private const string DiagnosticFileEnvironmentVariable = "COVERLET_MTP_DIAGNOSTIC_FILE";

    string IExtension.Uid => _extension.Uid;
    string IExtension.Version => _extension.Version;
    string IExtension.DisplayName => _extension.DisplayName;
    string IExtension.Description => _extension.Description;

    public CoverletExtensionCollector(
      Microsoft.Testing.Platform.Logging.ILoggerFactory loggerFactory,
      Microsoft.Testing.Platform.CommandLine.ICommandLineOptions commandLineOptions,
      Microsoft.Testing.Platform.Configurations.IConfiguration? configuration)
    {
      _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
      _commandLineOptions = commandLineOptions ?? throw new ArgumentNullException(nameof(commandLineOptions));
      _platformConfiguration = configuration; // Allow null - coverage flag checked first
      _configuration = new CoverletExtensionConfiguration();
      _logger = new CoverletLoggerAdapter(_loggerFactory);

      // Only run diagnostics if coverage will be enabled
      // The actual check happens in BeforeTestHostProcessStartAsync
      AttachDebuggerIfRequested();

      LogDiagnostic("CoverletExtensionCollector constructor called");
      LogDiagnostic($"  Process ID: {Process.GetCurrentProcess().Id}");
      LogDiagnostic($"  Current Directory: {Environment.CurrentDirectory}");
    }

    /// <summary>
    /// Attaches debugger if COVERLET_MTP_DEBUG environment variable is set to "1".
    /// </summary>
    private static void AttachDebuggerIfRequested()
    {
      if (int.TryParse(Environment.GetEnvironmentVariable(DebugEnvironmentVariable), out int result) && result == 1)
      {
        Debugger.Launch();
        Debugger.Break();
      }
    }

    /// <summary>
    /// Logs diagnostic information to file if COVERLET_MTP_DIAGNOSTIC_FILE is set.
    /// </summary>
    private void LogDiagnostic(string message)
    {
      string? diagnosticFile = Environment.GetEnvironmentVariable(DiagnosticFileEnvironmentVariable);
      if (!string.IsNullOrEmpty(diagnosticFile))
      {
        try
        {
          string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
          File.AppendAllText(diagnosticFile, $"[{timestamp}] {message}{Environment.NewLine}");
        }
        catch
        {
          // Ignore file write errors in diagnostics
        }
      }

      // Also log to the platform logger
      _logger.LogVerbose($"[DIAG] {message}");
    }

    /// <summary>
    /// Called before the test host process starts. Instruments assemblies for coverage collection.
    /// </summary>
    Task ITestHostProcessLifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken cancellationToken)
    {
      LogDiagnostic("=== BeforeTestHostProcessStartAsync START ===");

      try
      {
        // Log all configuration values for debugging
        LogConfigurationDiagnostics();

        // Check if --coverlet-coverage flag was provided
        _coverageEnabled = _commandLineOptions.IsOptionSet(CoverletOptionNames.Coverage);
        LogDiagnostic($"Coverage enabled (--{CoverletOptionNames.Coverage} flag): {_coverageEnabled}");

        if (!_coverageEnabled)
        {
          _logger.LogInformation("Coverage collection is disabled. Use --coverlet-coverage to enable.");
          LogDiagnostic("=== BeforeTestHostProcessStartAsync END (disabled) ===");
          return Task.CompletedTask;
        }

        // Try multiple ways to get the test module path
        _testModulePath = ResolveTestModulePath();
        LogDiagnostic($"Resolved test module path: {_testModulePath ?? "(null)"}");

        if (string.IsNullOrEmpty(_testModulePath))
        {
          _logger.LogError("Could not determine test module path from platform configuration.");
          LogDiagnostic("ERROR: Test module path is null or empty");
          LogDiagnostic("=== BeforeTestHostProcessStartAsync END (no module) ===");
          _coverageEnabled = false;
          return Task.CompletedTask;
        }

        // Verify the module exists and is accessible
        LogModuleDiagnostics(_testModulePath!);

        _logger.LogInformation($"Initializing coverage instrumentation for: {_testModulePath}");

        // Parse additional options from command line
        ParseCommandLineOptions();
        LogParsedConfigurationDiagnostics();

        // Create service provider AFTER we have the test module path
        LogDiagnostic("Creating service provider...");
        _serviceProvider = CreateServiceProvider(_testModulePath!);
        LogDiagnostic("Service provider created successfully");

        // Initialize the Coverage instance for instrumentation
        LogDiagnostic("Resolving services from container...");
        IFileSystem fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();
        ISourceRootTranslator sourceRootTranslator = _serviceProvider.GetRequiredService<ISourceRootTranslator>();
        IInstrumentationHelper instrumentationHelper = _serviceProvider.GetRequiredService<IInstrumentationHelper>();
        ICecilSymbolHelper cecilSymbolHelper = _serviceProvider.GetRequiredService<ICecilSymbolHelper>();
        LogDiagnostic("All services resolved successfully");

        // Add default exclude filter like coverlet.collector does
        string[]? excludeFilters = _configuration.ExcludeFilters;
        if (excludeFilters == null || excludeFilters.Length == 0)
        {
          excludeFilters = new[] { DefaultExcludeFilter };
        }
        else if (!excludeFilters.Contains(DefaultExcludeFilter))
        {
          excludeFilters = excludeFilters.Concat(new[] { DefaultExcludeFilter }).ToArray();
        }
        LogDiagnostic($"Exclude filters: [{string.Join(", ", excludeFilters)}]");

        var parameters = new CoverageParameters
        {
          Module = _testModulePath,
          IncludeFilters = _configuration.IncludeFilters,
          IncludeDirectories = _configuration.IncludeDirectories ?? Array.Empty<string>(),
          ExcludeFilters = excludeFilters,
          ExcludedSourceFiles = _configuration.ExcludedSourceFiles,
          ExcludeAttributes = _configuration.ExcludeAttributes,
          IncludeTestAssembly = _configuration.IncludeTestAssembly,
          SingleHit = _configuration.SingleHit,
          MergeWith = _configuration.MergeWith,
          UseSourceLink = _configuration.UseSourceLink,
          DoesNotReturnAttributes = _configuration.DoesNotReturnAttributes,
          SkipAutoProps = _configuration.SkipAutoProps,
          DeterministicReport = _configuration.DeterministicReport,
          ExcludeAssembliesWithoutSources = _configuration.ExcludeAssembliesWithoutSources,
          DisableManagedInstrumentationRestore = _configuration.DisableManagedInstrumentationRestore
        };

        LogDiagnostic("Creating Coverage instance...");
        _coverage = new Coverage(
          _testModulePath,
          parameters,
          _logger,
          instrumentationHelper,
          fileSystem,
          sourceRootTranslator,
          cecilSymbolHelper);
        LogDiagnostic("Coverage instance created");

        LogDiagnostic("Calling PrepareModules()...");
        CoveragePrepareResult prepareResult = _coverage.PrepareModules();
        LogDiagnostic($"PrepareModules() completed - Identifier: {prepareResult.Identifier}");
        LogDiagnostic($"  Results count: {prepareResult.Results?.Length ?? 0}");
        if (prepareResult.Results != null)
        {
          foreach (InstrumenterResult? result in prepareResult.Results)
          {
            LogDiagnostic($"  - Module: {result.Module}, Documents: {result.Documents?.Count ?? 0}");
          }
        }

        _logger.LogInformation("Coverage instrumentation initialized successfully");
        LogDiagnostic("=== BeforeTestHostProcessStartAsync END (success) ===");
      }
      catch (Exception ex)
      {
        _logger.LogError("Failed to initialize coverage instrumentation");
        _logger.LogError(ex);
        LogDiagnostic($"EXCEPTION: {ex.GetType().FullName}");
        LogDiagnostic($"  Message: {ex.Message}");
        LogDiagnostic($"  StackTrace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
          LogDiagnostic($"  InnerException: {ex.InnerException.GetType().FullName}");
          LogDiagnostic($"  InnerMessage: {ex.InnerException.Message}");
        }
        LogDiagnostic("=== BeforeTestHostProcessStartAsync END (exception) ===");
        _coverageEnabled = false;
      }

      return Task.CompletedTask;
    }

    /// <summary>
    /// Logs all platform configuration values for debugging.
    /// </summary>
    private void LogConfigurationDiagnostics()
    {
      if (_platformConfiguration == null)
      {
        LogDiagnostic("Platform Configuration: null (coverage not enabled)");
        return;
      }
      LogDiagnostic("Platform Configuration values:");
      try
      {
        // Try common configuration keys
        string[] keysToCheck = new[]
        {
          "TestModule",
          "TestHostController:Mode",
          "TestHost:Path",
          "TestAssembly",
          "TargetPath"
        };

        foreach (string key in keysToCheck)
        {
          string? value = _platformConfiguration[key];
          LogDiagnostic($"  [{key}] = {value ?? "(null)"}");
        }

        // Try to enumerate known configuration keys (as a fallback)
        string[] knownKeys = new[]
        {
          "TestModule",
          "TestHostController:Mode",
          "TestHost:Path",
          "TestAssembly",
          "TargetPath"
        };

        foreach (string key in knownKeys)
        {
          string? value = _platformConfiguration[key];
          LogDiagnostic($"  Config key: {key} = {value ?? "(null)"}");
        }
      }
      catch (Exception ex)
      {
        LogDiagnostic($"  Error reading configuration: {ex.Message}");
      }

      // Log environment variables that might be relevant
      LogDiagnostic("Relevant environment variables:");
      string[] envVars = new[]
      {
        "TESTINGPLATFORM_TESTHOSTCONTROLLER_CORRELATIONID_" + Process.GetCurrentProcess().Id,
        "DOTNET_ROOT",
        "DOTNET_HOST_PATH",
        DebugEnvironmentVariable,
        DiagnosticFileEnvironmentVariable
      };

      foreach (string envVar in envVars)
      {
        string? value = Environment.GetEnvironmentVariable(envVar);
        LogDiagnostic($"  [{envVar}] = {value ?? "(not set)"}");
      }
    }

    /// <summary>
    /// Logs diagnostics about the test module file.
    /// </summary>
    private void LogModuleDiagnostics(string modulePath)
    {
      LogDiagnostic($"Module diagnostics for: {modulePath}");
      try
      {
        if (File.Exists(modulePath))
        {
          var fileInfo = new FileInfo(modulePath);
          LogDiagnostic($"  File exists: true");
          LogDiagnostic($"  File size: {fileInfo.Length} bytes");
          LogDiagnostic($"  Last modified: {fileInfo.LastWriteTime}");
          LogDiagnostic($"  Directory: {fileInfo.DirectoryName}");

          // Check for PDB
          string pdbPath = Path.ChangeExtension(modulePath, ".pdb");
          LogDiagnostic($"  PDB exists ({pdbPath}): {File.Exists(pdbPath)}");

          // List files in directory
          string? directory = Path.GetDirectoryName(modulePath);
          if (directory != null)
          {
            IEnumerable<string> files = Directory.GetFiles(directory, "*.dll").Take(10);
            LogDiagnostic($"  Other DLLs in directory (first 10): {string.Join(", ", files.Select(Path.GetFileName))}");
          }
        }
        else
        {
          LogDiagnostic($"  File exists: false");
          LogDiagnostic($"  Directory exists: {Directory.Exists(Path.GetDirectoryName(modulePath))}");
        }
      }
      catch (Exception ex)
      {
        LogDiagnostic($"  Error getting module diagnostics: {ex.Message}");
      }
    }

    /// <summary>
    /// Logs parsed configuration values.
    /// </summary>
    private void LogParsedConfigurationDiagnostics()
    {
      LogDiagnostic("Parsed configuration:");
      LogDiagnostic($"  formats: [{string.Join(", ", _configuration.formats)}]");
      LogDiagnostic($"  IncludeFilters: [{string.Join(", ", _configuration.IncludeFilters ?? Array.Empty<string>())}]");
      LogDiagnostic($"  ExcludeFilters: [{string.Join(", ", _configuration.ExcludeFilters ?? Array.Empty<string>())}]");
      LogDiagnostic($"  IncludeTestAssembly: {_configuration.IncludeTestAssembly}");
      LogDiagnostic($"  OutputDirectory: {_configuration.OutputDirectory ?? "(null)"}");
      LogDiagnostic($"  SingleHit: {_configuration.SingleHit}");
      LogDiagnostic($"  SkipAutoProps: {_configuration.SkipAutoProps}");
      LogDiagnostic($"  UseSourceLink: {_configuration.UseSourceLink}");
    }

    /// <summary>
    /// Resolves the test module path from various sources.
    /// </summary>
    private string? ResolveTestModulePath()
    {
      LogDiagnostic("Resolving test module path...");

      // Only try platform configuration if it exists
      if (_platformConfiguration != null)
      {
        string? testModule = _platformConfiguration["TestModule"];
        LogDiagnostic($"  From config 'TestModule': {testModule ?? "(null)"}");
        if (!string.IsNullOrEmpty(testModule) && File.Exists(testModule))
        {
          LogDiagnostic($"  -> Using TestModule from config");
          return testModule;
        }

        // Try TestHost:Path
        testModule = _platformConfiguration["TestHost:Path"];
        LogDiagnostic($"  From config 'TestHost:Path': {testModule ?? "(null)"}");
        if (!string.IsNullOrEmpty(testModule) && File.Exists(testModule))
        {
          LogDiagnostic($"  -> Using TestHost:Path from config");
          return testModule;
        }
      }

      // Try to get from command line arguments
      LogDiagnostic("  Checking command line arguments...");
      if (_commandLineOptions.TryGetOptionArgumentList("--", out string[]? args) && args?.Length > 0)
      {
        LogDiagnostic($"    Args after '--': [{string.Join(", ", args)}]");
        string potentialPath = args[0];
        if (File.Exists(potentialPath) && (potentialPath.EndsWith(".dll") || potentialPath.EndsWith(".exe")))
        {
          LogDiagnostic($"  -> Using first arg after '--'");
          return potentialPath;
        }
      }
      else
      {
        LogDiagnostic("    No args after '--' found");
      }

      // Try entry assembly location as fallback
      string? entryAssembly = System.Reflection.Assembly.GetEntryAssembly()?.Location;
      LogDiagnostic($"  Entry assembly location: {entryAssembly ?? "(null)"}");
      if (!string.IsNullOrEmpty(entryAssembly) && File.Exists(entryAssembly))
      {
        // Check if this looks like a test assembly (not the test host itself)
        string fileName = Path.GetFileName(entryAssembly);
        if (!fileName.StartsWith("testhost", StringComparison.OrdinalIgnoreCase) &&
            !fileName.StartsWith("dotnet", StringComparison.OrdinalIgnoreCase))
        {
          LogDiagnostic($"  -> Using entry assembly");
          return entryAssembly;
        }
        else
        {
          LogDiagnostic($"  Entry assembly appears to be test host, skipping");
        }
      }

      // Try executing assembly
      string? executingAssembly = System.Reflection.Assembly.GetExecutingAssembly()?.Location;
      LogDiagnostic($"  Executing assembly location: {executingAssembly ?? "(null)"}");

      // Try calling assembly
      string? callingAssembly = System.Reflection.Assembly.GetCallingAssembly()?.Location;
      LogDiagnostic($"  Calling assembly location: {callingAssembly ?? "(null)"}");

      LogDiagnostic("  -> Could not resolve test module path");
      return null;
    }

    /// <summary>
    /// Called when the test host process has started. Can be used for logging/tracking.
    /// </summary>
    Task ITestHostProcessLifetimeHandler.OnTestHostProcessStartedAsync(
      ITestHostProcessInformation testHostProcessInformation,
      CancellationToken cancellation)
    {
      LogDiagnostic($"OnTestHostProcessStartedAsync - PID: {testHostProcessInformation.PID}");
      if (_coverageEnabled)
      {
        _logger.LogInformation($"Test host process started (PID: {testHostProcessInformation.PID})");
      }
      return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the test host process has exited. Collects coverage data and generates reports.
    /// </summary>
    async Task ITestHostProcessLifetimeHandler.OnTestHostProcessExitedAsync(
      ITestHostProcessInformation testHostProcessInformation,
      CancellationToken cancellation)
    {
      LogDiagnostic($"=== OnTestHostProcessExitedAsync START ===");
      LogDiagnostic($"  PID: {testHostProcessInformation.PID}, ExitCode: {testHostProcessInformation.ExitCode}");
      LogDiagnostic($"  CoverageEnabled: {_coverageEnabled}, Coverage: {(_coverage != null ? "set" : "null")}, ServiceProvider: {(_serviceProvider != null ? "set" : "null")}");

      if (!_coverageEnabled || _coverage == null || _serviceProvider == null)
      {
        LogDiagnostic("=== OnTestHostProcessExitedAsync END (skipped) ===");
        return;
      }

      try
      {
        _logger.LogInformation($"Test host process exited (PID: {testHostProcessInformation.PID}, ExitCode: {testHostProcessInformation.ExitCode})");
        _logger.LogInformation("\nCalculating coverage result...");

        LogDiagnostic("Calling GetCoverageResult()...");
        CoverageResult result = _coverage.GetCoverageResult();
        LogDiagnostic($"GetCoverageResult() completed - Modules: {result.Modules?.Count ?? 0}");

        string dOutput = _configuration.OutputDirectory ?? Path.GetDirectoryName(_testModulePath) + Path.DirectorySeparatorChar.ToString();
        string directory = Path.GetDirectoryName(dOutput)!;
        LogDiagnostic($"Output directory: {directory}");

        if (directory != null && !Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
          LogDiagnostic($"Created output directory: {directory}");
        }

        ISourceRootTranslator sourceRootTranslator = _serviceProvider.GetRequiredService<ISourceRootTranslator>();
        IFileSystem fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();

        foreach (string format in _configuration.formats)
        {
          LogDiagnostic($"Generating report in format: {format}");
          IReporter reporter = new ReporterFactory(format).CreateReporter();
          if (reporter == null)
          {
            LogDiagnostic($"  Reporter for format '{format}' is null!");
            throw new InvalidOperationException($"Specified output format '{format}' is not supported");
          }

          if (reporter.OutputType == ReporterOutputType.Console)
          {
            _logger.LogInformation("  Outputting results to console", important: true);
            _logger.LogInformation(reporter.Report(result, sourceRootTranslator), important: true);
          }
          else
          {
            string filename = Path.GetFileName(dOutput);
            filename = (filename == string.Empty) ? $"coverage.{reporter.Extension}" : filename;
            filename = Path.HasExtension(filename) ? filename : $"{filename}.{reporter.Extension}";

            string report = Path.Combine(directory!, filename);
            LogDiagnostic($"  Writing report to: {report}");
            _logger.LogInformation($"  Generating report '{report}'", important: true);
            await Task.Run(() => fileSystem.WriteAllText(report, reporter.Report(result, sourceRootTranslator)), cancellation);
            LogDiagnostic($"  Report written successfully");
          }
        }

        _logger.LogInformation("Code coverage collection completed");
        LogDiagnostic("=== OnTestHostProcessExitedAsync END (success) ===");
      }
      catch (Exception ex)
      {
        _logger.LogError("Failed to collect code coverage");
        _logger.LogError(ex);
        LogDiagnostic($"EXCEPTION: {ex.GetType().FullName}: {ex.Message}");
        LogDiagnostic($"StackTrace: {ex.StackTrace}");
        LogDiagnostic("=== OnTestHostProcessExitedAsync END (exception) ===");
      }
    }

    /// <summary>
    /// Always returns true so command-line options appear in help.
    /// Actual coverage collection is controlled by --coverlet-coverage flag.
    /// </summary>
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    private void ParseCommandLineOptions()
    {
      LogDiagnostic("Parsing command line options...");

      // Parse --formats option
      if (_commandLineOptions.TryGetOptionArgumentList(CoverletOptionNames.Formats, out string[]? formats) && formats != null)
      {
        _configuration.formats = formats.SelectMany(f => f.Split(',')).ToArray();
        LogDiagnostic($"  Parsed formats: [{string.Join(", ", _configuration.formats)}]");
      }

      // Parse --include option
      if (_commandLineOptions.TryGetOptionArgumentList(CoverletOptionNames.Include, out string[]? includes) && includes != null)
      {
        _configuration.IncludeFilters = includes;
        LogDiagnostic($"  Parsed include: [{string.Join(", ", includes)}]");
      }

      // Parse --exclude option
      if (_commandLineOptions.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out string[]? excludes) && excludes != null)
      {
        _configuration.ExcludeFilters = excludes;
        LogDiagnostic($"  Parsed exclude: [{string.Join(", ", excludes)}]");
      }

      // Parse --include-test-assembly option
      if (_commandLineOptions.IsOptionSet(CoverletOptionNames.IncludeTestAssembly))
      {
        _configuration.IncludeTestAssembly = true;
        LogDiagnostic($"  Parsed include-test-assembly: true");
      }
    }

    private IServiceProvider CreateServiceProvider(string testModule)
    {
      LogDiagnostic($"CreateServiceProvider with testModule: {testModule}");

      var services = new ServiceCollection();

      services.AddSingleton<Coverlet.Core.Abstractions.ILogger>(_logger);
      services.AddSingleton<IFileSystem, FileSystem>();
      services.AddSingleton<IAssemblyAdapter, AssemblyAdapter>();
      services.AddSingleton<IRetryHelper, RetryHelper>();

      // Detect if we're running out-of-process by checking for controller correlation ID
      bool isOutOfProcess = IsRunningOutOfProcess();
      LogDiagnostic($"  IsOutOfProcess: {isOutOfProcess}");

      // Use appropriate ProcessExitHandler based on execution mode
      services.AddSingleton<IProcessExitHandler>(new MtpProcessExitHandler(isOutOfProcess));

      services.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
      services.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();

      // Create SourceRootTranslator with the test module path (like coverlet.collector does)
      services.AddSingleton<ISourceRootTranslator>(provider =>
      {
        LogDiagnostic($"  Creating SourceRootTranslator with module: {testModule}");
        return new SourceRootTranslator(
            testModule,
            provider.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(),
            provider.GetRequiredService<IFileSystem>(),
            provider.GetRequiredService<IAssemblyAdapter>());
      });

      return services.BuildServiceProvider();
    }

    /// <summary>
    /// Detects if the extension is running in out-of-process mode.
    /// </summary>
    private bool IsRunningOutOfProcess()
    {
      // MTP sets this environment variable when running in controller mode
      // Check for correlation ID which indicates out-of-process execution
      int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
      string envVarName = $"TESTINGPLATFORM_TESTHOSTCONTROLLER_CORRELATIONID_{processId}";
      string? correlationId = Environment.GetEnvironmentVariable(envVarName);

      LogDiagnostic($"  Checking {envVarName}: {correlationId ?? "(not set)"}");

      // Also check the configuration for explicit mode setting
      string? executionMode = _platformConfiguration?["TestHostController:Mode"];
      LogDiagnostic($"  TestHostController:Mode: {executionMode ?? "(not set)"}");

      return !string.IsNullOrEmpty(correlationId) || executionMode == "OutOfProcess";
    }
  }
}
