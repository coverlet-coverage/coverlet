// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Reporters;
using Coverlet.Core.Symbols;
using Coverlet.MTP.CommandLine;
using Coverlet.MTP.Configuration;
using Coverlet.MTP.Diagnostics;
using Coverlet.MTP.EnvironmentVariables;
using Coverlet.MTP.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;

namespace Coverlet.MTP.Collector;

/// <summary>
/// Implements test host process lifetime handling for coverage collection using the Microsoft Testing Platform.
/// This extension runs in a SEPARATE CONTROLLER PROCESS, not the test host process.
/// It instruments assemblies BEFORE the test host starts, avoiding file lock issues.
/// </summary>
internal sealed class CollectorExtension : ITestHostProcessLifetimeHandler, ITestHostEnvironmentVariableProvider, IOutputDeviceDataProducer
{
  private readonly CoverletLoggerAdapter _logger;
  private readonly IFileSystem _fileSystem;
  private readonly CoverletExtensionConfiguration _configuration;
  private IServiceProvider? _serviceProvider;
  private readonly IConfiguration? _platformConfiguration;
  private readonly IOutputDevice _outputDisplay;
  private ICoverage? _coverage;
  private readonly ILoggerFactory _loggerFactory;
  private readonly ICommandLineOptions _commandLineOptions;
  private bool _coverageEnabled;
  private string? _testModulePath;
  private string? _coverageIdentifier;

  private readonly CoverletExtension _extension = new();

  string IExtension.Uid => _extension.Uid;
  string IExtension.Version => _extension.Version;
  string IExtension.DisplayName => _extension.DisplayName;
  string IExtension.Description => _extension.Description;

  public CollectorExtension(
    Microsoft.Testing.Platform.Logging.ILoggerFactory loggerFactory,
    Microsoft.Testing.Platform.CommandLine.ICommandLineOptions commandLineOptions,
    Microsoft.Testing.Platform.OutputDevice.IOutputDevice? outputDevice,
    Microsoft.Testing.Platform.Configurations.IConfiguration? configuration,
    IFileSystem? fileSystem = null)  // Add optional parameter for backward compatibility
  {
    _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    _commandLineOptions = commandLineOptions ?? throw new ArgumentNullException(nameof(commandLineOptions));
    _platformConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _outputDisplay = outputDevice ?? throw new ArgumentNullException(nameof(outputDevice));
    _fileSystem = fileSystem ?? new FileSystem();  // Use provided or create default
    _configuration = new CoverletExtensionConfiguration();
    _logger = new CoverletLoggerAdapter(_loggerFactory);
    _logger.LogVerbose("[DIAG] CoverletExtensionCollector constructor - running in controller process");
    _logger.LogVerbose($"[DIAG]   Process ID: {Environment.ProcessId}");
  }

  /// <summary>
  /// Called before the test host process starts. Instruments assemblies for coverage collection.
  /// This runs in the CONTROLLER process, separate from the test host.
  /// </summary>
  Task ITestHostProcessLifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken cancellationToken)
  {
    _logger.LogVerbose("=== BeforeTestHostProcessStartAsync START ===");
    _logger.LogVerbose($"Controller PID: {Environment.ProcessId}");

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

      // Create service provider for coverage
      _serviceProvider = ServiceProviderOverride ?? CreateServiceProvider(_testModulePath!);

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
    return environmentVariables.TryGetVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, out OwnedEnvironmentVariable? existing) &&
        existing.Owner != this as IExtension
      ? Task.FromResult(ValidationResult.Invalid(
        $"Environment variable {CoverletMtpEnvironmentVariables.CoverageEnabled} is already set by another extension."))
      : Task.FromResult(ValidationResult.Valid());
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

  // Add internal setter for service provider (for testing)
  internal IServiceProvider? ServiceProviderOverride { get; set; }

  // Modify InitializeCoverage to use factory
  private void InitializeCoverage()
  {
    if (string.IsNullOrEmpty(_testModulePath))
    {
      return;
    }

    var parameters = new CoverageParameters
    {
      Module = _testModulePath,
      IncludeFilters = _configuration.IncludeFilters,
      IncludeDirectories = _configuration.IncludeDirectories ?? [],
      ExcludeFilters = _configuration.ExcludeFilters ?? [],
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
    {
      return;
    }

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
    // Use the results directory captured during initialization, or fall back to test module directory
    string outputDirectory = _platformConfiguration?.GetTestResultDirectory()
      ?? Path.GetDirectoryName(_testModulePath)
      ?? Environment.CurrentDirectory;

    _logger.LogVerbose($"Coverage output directory: {outputDirectory}");

    // Ensure directory exists
    if (!_fileSystem.Exists(outputDirectory))
    {
      Directory.CreateDirectory(outputDirectory);
    }

    ISourceRootTranslator sourceRootTranslator = _serviceProvider!.GetRequiredService<ISourceRootTranslator>();
    IFileSystem fileSystem = _serviceProvider!.GetRequiredService<IFileSystem>();

    var generatedReports = new List<string>();

    foreach (string format in _configuration.formats)
    {
      IReporter reporter = new ReporterFactory(format).CreateReporter() ??
        throw new InvalidOperationException($"Specified output format '{format}' is not supported");

      string filename = $"coverage.{reporter.Extension}";
      string reportPath = Path.Combine(outputDirectory, filename);
      await Task.Run(() => fileSystem.WriteAllText(reportPath, reporter.Report(result, sourceRootTranslator)), cancellation);
      generatedReports.Add(reportPath);
    }

    // Output successfully generated reports to console (like TRX report extension does)
    if (generatedReports.Count > 0)
    {
      // Use FormattedTextOutputDeviceData for proper display like TRX extension
      var outputBuilder = new StringBuilder();
      outputBuilder.AppendLine();
      outputBuilder.AppendLine("  Out of process file artifacts produced:");
      foreach (string reportPath in generatedReports)
      {
        outputBuilder.AppendLine($"    - {reportPath}");
      }

      await _outputDisplay.DisplayAsync(
        this,
        new FormattedTextOutputDeviceData(outputBuilder.ToString()),
        cancellation).ConfigureAwait(false);
    }
  }

  private string GetHitsFilePath()
  {
    // The hits file is in the same directory as the instrumented module
    if (string.IsNullOrEmpty(_testModulePath))
    {
      return string.Empty;
    }

    string? directory = Path.GetDirectoryName(_testModulePath);
    return directory ?? string.Empty;
  }

  private string? ResolveTestModulePath()
  {
    // Try platform configuration
    if (_platformConfiguration != null)
    {
      string? testModule = _platformConfiguration["TestModule"];
      if (!string.IsNullOrEmpty(testModule) && _fileSystem.Exists(testModule))  // Use injected file system
      {
        return testModule;
      }

      testModule = _platformConfiguration["TestHost:Path"];
      if (!string.IsNullOrEmpty(testModule) && _fileSystem.Exists(testModule))  // Use injected file system
      {
        return testModule;
      }
    }

    // Try entry assembly
    string? entryAssembly = System.Reflection.Assembly.GetEntryAssembly()?.Location;
    if (!string.IsNullOrEmpty(entryAssembly) && _fileSystem.Exists(entryAssembly))  // Use injected file system
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

  private ServiceProvider CreateServiceProvider(string testModule)
  {
    var services = new ServiceCollection();

    services.AddSingleton<Coverlet.Core.Abstractions.ILogger>(_logger);
    services.AddSingleton<IFileSystem>(_fileSystem);  // Use the injected file system instance
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
