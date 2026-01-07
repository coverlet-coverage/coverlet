// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Coverlet.Extension.Logging;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Reporters;
using Coverlet.Core.Symbols;
using Coverlet.MTP.CommandLine;
using Coverlet.MTP.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;
using Coverlet.Extension.Collector;
using Coverlet.MTP.Configuration;
using Coverlet.MTP.EnvironmentVariables;

namespace Coverlet.MTP.Collector;

/// <summary>
/// Implements test host process lifetime handling for coverage collection using the Microsoft Testing Platform.
/// This extension runs in a SEPARATE CONTROLLER PROCESS, not the test host process.
/// It instruments assemblies BEFORE the test host starts, avoiding file lock issues.
/// </summary>
internal sealed class CollectorExtension : ITestHostProcessLifetimeHandler, ITestHostEnvironmentVariableProvider
{
  private readonly CoverletLoggerAdapter _logger;
  private readonly CoverletExtensionConfiguration _configuration;
  private IServiceProvider? _serviceProvider;
  private readonly IConfiguration? _platformConfiguration;
  private ICoverage? _coverage;
  private readonly ILoggerFactory _loggerFactory;
  private readonly ICommandLineOptions _commandLineOptions;
  private bool _coverageEnabled;
  private string? _testModulePath;
  private string? _coverageIdentifier;

  private readonly CoverletExtension _extension = new();

  // Default exclude filter matching coverlet.collector
  private const string DefaultExcludeFilter = "[xunit.*]*";

  string IExtension.Uid => _extension.Uid;
  string IExtension.Version => _extension.Version;
  string IExtension.DisplayName => _extension.DisplayName;
  string IExtension.Description => _extension.Description;

  public CollectorExtension(
    Microsoft.Testing.Platform.Logging.ILoggerFactory loggerFactory,
    Microsoft.Testing.Platform.CommandLine.ICommandLineOptions commandLineOptions,
    Microsoft.Testing.Platform.Configurations.IConfiguration? configuration)
  {
    _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    _commandLineOptions = commandLineOptions ?? throw new ArgumentNullException(nameof(commandLineOptions));
    _platformConfiguration = configuration ?? throw new ArgumentNullException(nameof(loggerFactory));
    _configuration = new CoverletExtensionConfiguration();
    _logger = new CoverletLoggerAdapter(_loggerFactory);

    _logger.LogVerbose("[DIAG] CoverletExtensionCollector constructor - running in controller process");
    _logger.LogVerbose($"[DIAG]   Process ID: {Process.GetCurrentProcess().Id}");
  }

  /// <summary>
  /// Called before the test host process starts. Instruments assemblies for coverage collection.
  /// This runs in the CONTROLLER process, separate from the test host.
  /// </summary>
  Task ITestHostProcessLifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken cancellationToken)
  {
    _logger.LogVerbose("=== BeforeTestHostProcessStartAsync START ===");
    _logger.LogVerbose($"Controller PID: {Process.GetCurrentProcess().Id}");

    try
    {
      // Check if --coverlet flag was provided
      _coverageEnabled = _commandLineOptions.IsOptionSet(CoverletOptionNames.Coverage);
      _logger.LogVerbose($"Coverage enabled (--{CoverletOptionNames.Coverage} flag): {_coverageEnabled}");

      if (!_coverageEnabled)
      {
        _logger.LogInformation("Coverage collection is disabled. Use --coverlet to enable.");
        return Task.CompletedTask;
      }

      // Resolve test module path - critical for instrumentation
      _testModulePath = ResolveTestModulePath();
      if (string.IsNullOrEmpty(_testModulePath))
      {
        _logger.LogError("Could not determine test module path. Coverage disabled.");
        _coverageEnabled = false;
        return Task.CompletedTask;
      }

      _logger.LogVerbose($"Test module path: {_testModulePath}");

      // Parse command line options
      ParseCommandLineOptions();

      // Create service provider for coverage
      _serviceProvider = CreateServiceProvider(_testModulePath!);

      // Initialize Coverage instance
      InitializeCoverage();

      // Perform instrumentation BEFORE test host starts
      if (_coverage != null)
      {
        _logger.LogInformation($"Instrumenting module: {_testModulePath}");
        CoveragePrepareResult prepareResult = _coverage.PrepareModules();
        _coverageIdentifier = prepareResult.Identifier;
        _logger.LogVerbose($"Instrumentation complete. Identifier: {_coverageIdentifier}");
        _logger.LogVerbose($"Instrumented modules: {prepareResult.Results?.Length ?? 0}");
      }
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to initialize coverage instrumentation");
      _logger.LogError(ex);
      _coverageEnabled = false;
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Provides environment variables to the test host process.
  /// This allows the in-process handler to know coverage is enabled.
  /// </summary>
  Task ITestHostEnvironmentVariableProvider.UpdateAsync(IEnvironmentVariables environmentVariables)
  {
    if (_coverageEnabled && !string.IsNullOrEmpty(_coverageIdentifier))
    {
      _logger.LogVerbose($"Setting environment variables for test host");

      // Tell the test host that coverage is enabled
      environmentVariables.SetVariable(new EnvironmentVariable(
        CoverletMtpEnvironmentVariables.CoverageEnabled,
        "true",
        isSecret: false,
        isLocked: true));

      // Pass the coverage identifier for result correlation
      environmentVariables.SetVariable(new EnvironmentVariable(
        CoverletMtpEnvironmentVariables.CoverageIdentifier,
        _coverageIdentifier,
        isSecret: false,
        isLocked: true));

      // Pass the hits file path if available
      string hitsPath = GetHitsFilePath();
      if (!string.IsNullOrEmpty(hitsPath))
      {
        environmentVariables.SetVariable(new EnvironmentVariable(
          CoverletMtpEnvironmentVariables.HitsFilePath,
          hitsPath,
          isSecret: false,
          isLocked: true));
      }
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Validates that environment variables don't conflict.
  /// </summary>
  Task<ValidationResult> ITestHostEnvironmentVariableProvider.ValidateTestHostEnvironmentVariablesAsync(
    IReadOnlyEnvironmentVariables environmentVariables)
  {
    // Check for conflicts with our variables
    if (environmentVariables.TryGetVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, out OwnedEnvironmentVariable? existing) &&
        existing.Owner != this as IExtension)
    {
      return Task.FromResult(ValidationResult.Invalid(
        $"Environment variable {CoverletMtpEnvironmentVariables.CoverageEnabled} is already set by another extension."));
    }

    return Task.FromResult(ValidationResult.Valid());
  }

  /// <summary>
  /// Called when the test host process has started.
  /// </summary>
  Task ITestHostProcessLifetimeHandler.OnTestHostProcessStartedAsync(
    ITestHostProcessInformation testHostProcessInformation,
    CancellationToken cancellation)
  {
    _logger.LogVerbose($"Test host started - PID: {testHostProcessInformation.PID}");
    return Task.CompletedTask;
  }

  /// <summary>
  /// Called when the test host process has exited. Collects coverage data and generates reports.
  /// The test host has already written hit data to files during execution.
  /// </summary>
  async Task ITestHostProcessLifetimeHandler.OnTestHostProcessExitedAsync(
    ITestHostProcessInformation testHostProcessInformation,
    CancellationToken cancellation)
  {
    _logger.LogVerbose($"=== OnTestHostProcessExitedAsync START ===");
    _logger.LogVerbose($"Test host PID: {testHostProcessInformation.PID}, ExitCode: {testHostProcessInformation.ExitCode}");

    if (!_coverageEnabled || _coverage == null || _serviceProvider == null)
    {
      _logger.LogVerbose("Coverage collection skipped (not enabled or not initialized)");
      return;
    }

    try
    {
      _logger.LogInformation("Calculating coverage result...");

      // Get coverage result - this reads the hits files written by the test host
      CoverageResult result = _coverage.GetCoverageResult();
      _logger.LogVerbose($"Coverage result modules: {result.Modules?.Count ?? 0}");

      // Generate reports
      await GenerateReportsAsync(result, cancellation);

      _logger.LogInformation("Code coverage collection completed");
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to collect code coverage");
      _logger.LogError(ex);
    }
  }

  public Task<bool> IsEnabledAsync()
  {
    DebugHelper.HandleDebuggerAttachment(nameof(CoverletExtension));
    return Task.FromResult(true);
  }

  // Add internal setter for testing
  internal ICoverageFactory? CoverageFactory { get; set; }

  // Modify InitializeCoverage to use factory
  private void InitializeCoverage()
  {
    if (string.IsNullOrEmpty(_testModulePath))
      return;

    // Add default exclude filter
    string[] excludeFilters = _configuration.ExcludeFilters ?? [];
    if (!excludeFilters.Contains(DefaultExcludeFilter))
    {
      excludeFilters = [.. excludeFilters, DefaultExcludeFilter];
    }

    var parameters = new CoverageParameters
    {
      Module = _testModulePath,
      IncludeFilters = _configuration.IncludeFilters,
      IncludeDirectories = _configuration.IncludeDirectories ?? [],
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

    // Use factory if available (for testing), otherwise create directly
    if (CoverageFactory is not null)
    {
      _coverage = CoverageFactory.Create(_testModulePath, parameters);
      return;
    }

    // Only resolve services when creating Coverage directly
    if (_serviceProvider is null)
      return;

    IFileSystem fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();
    ISourceRootTranslator sourceRootTranslator = _serviceProvider.GetRequiredService<ISourceRootTranslator>();
    IInstrumentationHelper instrumentationHelper = _serviceProvider.GetRequiredService<IInstrumentationHelper>();
    ICecilSymbolHelper cecilSymbolHelper = _serviceProvider.GetRequiredService<ICecilSymbolHelper>();

    _coverage = new Coverage(
      _testModulePath,
      parameters,
      _logger,
      instrumentationHelper,
      fileSystem,
      sourceRootTranslator,
      cecilSymbolHelper);
  }

  private async Task GenerateReportsAsync(CoverageResult result, CancellationToken cancellation)
  {
    string outputDirectory = _platformConfiguration!.GetTestResultDirectory() ??
      Path.GetDirectoryName(_testModulePath) + Path.DirectorySeparatorChar;

    string directory = Path.GetDirectoryName(outputDirectory)!;
    if (!Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }

    ISourceRootTranslator sourceRootTranslator = _serviceProvider!.GetRequiredService<ISourceRootTranslator>();
    IFileSystem fileSystem = _serviceProvider!.GetRequiredService<IFileSystem>();

    var generatedReports = new List<string>();

    foreach (string format in _configuration.formats)
    {
      IReporter reporter = new ReporterFactory(format).CreateReporter();
      if (reporter == null)
      {
        throw new InvalidOperationException($"Specified output format '{format}' is not supported");
      }

      if (reporter.OutputType == ReporterOutputType.Console)
      {
        _logger.LogInformation(reporter.Report(result, sourceRootTranslator), important: true);
      }
      else
      {
        string filename = $"coverage.{reporter.Extension}";
        string reportPath = Path.Combine(directory, filename);
        await Task.Run(() => fileSystem.WriteAllText(reportPath, reporter.Report(result, sourceRootTranslator)), cancellation);
        generatedReports.Add(reportPath);
      }
    }

    // Output successfully generated reports to console
    if (generatedReports.Count > 0)
    {
      _logger.LogInformation("Coverage reports generated:", important: true);
      foreach (string reportPath in generatedReports)
      {
        _logger.LogInformation($"  {reportPath}", important: true);
      }
    }
  }

  private string GetHitsFilePath()
  {
    // The hits file is in the same directory as the instrumented module
    if (string.IsNullOrEmpty(_testModulePath))
      return string.Empty;

    string? directory = Path.GetDirectoryName(_testModulePath);
    return directory ?? string.Empty;
  }

  private string? ResolveTestModulePath()
  {
    // Try platform configuration
    if (_platformConfiguration != null)
    {
      string? testModule = _platformConfiguration["TestModule"];
      if (!string.IsNullOrEmpty(testModule) && File.Exists(testModule))
        return testModule;

      testModule = _platformConfiguration["TestHost:Path"];
      if (!string.IsNullOrEmpty(testModule) && File.Exists(testModule))
        return testModule;
    }

    // Try entry assembly
    string? entryAssembly = System.Reflection.Assembly.GetEntryAssembly()?.Location;
    if (!string.IsNullOrEmpty(entryAssembly) && File.Exists(entryAssembly))
    {
      string fileName = Path.GetFileName(entryAssembly);
      if (!fileName.StartsWith("testhost", StringComparison.OrdinalIgnoreCase) &&
          !fileName.StartsWith("dotnet", StringComparison.OrdinalIgnoreCase))
      {
        return entryAssembly;
      }
    }

    return null;
  }

  private void ParseCommandLineOptions()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(CoverletOptionNames.Formats, out string[]? formats) && formats != null)
    {
      _configuration.formats = formats; // No splitting
    }

    if (_commandLineOptions.TryGetOptionArgumentList(CoverletOptionNames.Include, out string[]? includes) && includes != null)
    {
      _configuration.IncludeFilters = includes;
    }

    if (_commandLineOptions.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out string[]? excludes) && excludes != null)
    {
      _configuration.ExcludeFilters = excludes;
    }

    if (_commandLineOptions.IsOptionSet(CoverletOptionNames.IncludeTestAssembly))
    {
      _configuration.IncludeTestAssembly = true;
    }
  }

  private IServiceProvider CreateServiceProvider(string testModule)
  {
    var services = new ServiceCollection();

    services.AddSingleton<Coverlet.Core.Abstractions.ILogger>(_logger);
    services.AddSingleton<IFileSystem, FileSystem>();
    services.AddSingleton<IAssemblyAdapter, AssemblyAdapter>();
    services.AddSingleton<IRetryHelper, RetryHelper>();

    // Use MTP-specific process exit handler that doesn't restore modules prematurely
    services.AddSingleton<IProcessExitHandler>(new MtpProcessExitHandler(isOutOfProcess: true));

    services.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
    services.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();

    services.AddSingleton<ISourceRootTranslator>(provider =>
      new SourceRootTranslator(
        testModule,
        provider.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(),
        provider.GetRequiredService<IFileSystem>(),
        provider.GetRequiredService<IAssemblyAdapter>()));

    return services.BuildServiceProvider();
  }
}
