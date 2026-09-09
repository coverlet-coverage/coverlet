// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD2_0
using System.Diagnostics;
#endif
using System.Text;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Enums;
using Coverlet.Core.Helpers;
using Coverlet.Core.Symbols;
using Coverlet.MTP.CommandLine;
using Coverlet.MTP.Configuration;
using Coverlet.MTP.Diagnostics;
using Coverlet.MTP.EnvironmentVariables;
using Coverlet.MTP.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.TestHost;

namespace Coverlet.MTP.Collector;

/// <summary>
/// Implements test host process lifetime handling for coverage collection using the Microsoft Testing Platform.
/// This extension runs in a SEPARATE CONTROLLER PROCESS, not the test host process.
/// It instruments assemblies BEFORE the test host starts, avoiding file lock issues.
/// </summary>
internal sealed class CollectorExtension : ITestHostProcessLifetimeHandler, ITestHostEnvironmentVariableProvider, IOutputDeviceDataProducer
{
  private readonly CoverletLoggerAdapter _logger;
  private readonly Coverlet.Core.Abstractions.IFileSystem _fileSystem;
  private readonly CoverletExtensionConfiguration _configuration;
  private IServiceProvider? _serviceProvider;
  private readonly Microsoft.Testing.Platform.Configurations.IConfiguration? _platformConfiguration;
  private readonly Microsoft.Testing.Platform.OutputDevice.IOutputDevice _outputDisplay;
  private readonly IMessageBus? _messageBus;
  private readonly CoverletCoverageDataProducer _coverageDataProducer;
  private ICoverage? _coverage;
  private readonly Microsoft.Testing.Platform.Logging.ILoggerFactory _loggerFactory;
  private readonly Microsoft.Testing.Platform.CommandLine.ICommandLineOptions _commandLineOptions;
  private string? _testModulePath;
  private string? _coverageIdentifier;
  private bool? _isCoverageEnabled;

  private bool IsCoverageEnabled => _isCoverageEnabled ??= _commandLineOptions.IsOptionSet(CoverletOptionNames.Coverage);

  private static readonly char[] s_ignoredExitCodeSeparators = [',', ';'];

  private readonly CoverletExtension _extension = new();
  private readonly IReporterFactory _reporterFactory;

  string IExtension.Uid => _extension.Uid;
  string IExtension.Version => _extension.Version;
  string IExtension.DisplayName => _extension.DisplayName;
  string IExtension.Description => _extension.Description;

  public CollectorExtension(
    Microsoft.Testing.Platform.Logging.ILoggerFactory loggerFactory,
    Microsoft.Testing.Platform.CommandLine.ICommandLineOptions commandLineOptions,
    Microsoft.Testing.Platform.OutputDevice.IOutputDevice? outputDevice,
    Microsoft.Testing.Platform.Configurations.IConfiguration? configuration,
    IFileSystem? fileSystem = null,
    IReporterFactory? reporterFactory = null,
    IMessageBus? messageBus = null,
    CoverletCoverageDataProducer? coverageDataProducer = null)
  {
    _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    _commandLineOptions = commandLineOptions ?? throw new ArgumentNullException(nameof(commandLineOptions));
    _platformConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _outputDisplay = outputDevice ?? throw new ArgumentNullException(nameof(outputDevice));
    _fileSystem = fileSystem ?? new FileSystem();  // Use provided or create default
    _reporterFactory = reporterFactory ?? new DefaultReporterFactory();
    _messageBus = messageBus;
    _coverageDataProducer = coverageDataProducer ?? new CoverletCoverageDataProducer();
    _configuration = new CoverletExtensionConfiguration();
    _logger = new CoverletLoggerAdapter(_loggerFactory);

    _logger.LogVerbose("[DIAG] CoverletExtensionCollector constructor - running in controller process");
#if NETSTANDARD2_0
    _logger.LogVerbose($"[DIAG]   .NET Version: {Process.GetCurrentProcess().Id}");
#else
    _logger.LogVerbose($"[DIAG]   Process ID: {Environment.ProcessId}");
#endif
  }

  /// <summary>
  /// Called before the test host process starts. Instruments assemblies for coverage collection.
  /// This runs in the CONTROLLER process, separate from the test host.
  /// </summary>
  Task ITestHostProcessLifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken cancellationToken)
  {
    _logger.LogVerbose("=== BeforeTestHostProcessStartAsync START ===");
#if NETSTANDARD2_0
    _logger.LogVerbose($"Controller PID: {Process.GetCurrentProcess().Id}");
#else
    _logger.LogVerbose($"Controller PID: {Environment.ProcessId}");
#endif

    try
    {
      // Check if --coverlet flag was provided
      _logger.LogVerbose($"Coverage enabled (--{CoverletOptionNames.Coverage} flag): {IsCoverageEnabled}");

      if (!IsCoverageEnabled)
      {
        _logger.LogInformation("Coverage collection is disabled. Use --coverlet to enable.");
        return Task.CompletedTask;
      }

      // Resolve test module path - critical for instrumentation
      _testModulePath = ResolveTestModulePath();
      if (string.IsNullOrEmpty(_testModulePath))
      {
        _logger.LogError("Could not determine test module path. Coverage disabled.");
        _isCoverageEnabled = false;
        return Task.CompletedTask;
      }

      // Load configuration file settings (coverlet.mtp.appsettings.json)
      CoverletMTPSettings? configFileSettings = LoadConfigurationFileSettings(_testModulePath!);

      // Create merged configuration: command-line options take precedence over config file.
      // testAssemblyName is passed so the dynamic exclude-filter logic can omit the test assembly itself.
      var config = new CoverageConfiguration(
        _commandLineOptions,
        configFileSettings,
        testModulePath: _testModulePath,
        logger: _loggerFactory.CreateLogger(nameof(CollectorExtension)));
      _configuration.DeterministicReport = config.DeterministicReport;
      _configuration.DisableManagedInstrumentationRestore = false;
      _configuration.DoesNotReturnAttributes = config.GetDoesNotReturnAttributes();
      _configuration.ExcludeAssembliesWithoutSources = config.GetExcludeAssembliesWithoutSources();
      _configuration.ExcludeAttributes = config.GetExcludeByAttributeFilters();
      _configuration.ExcludeFilters = config.GetExcludeFilters();
      _configuration.ExcludedSourceFiles = config.GetExcludeByFileFilters();
      _configuration.IncludeDirectories = config.GetIncludeDirectories();
      _configuration.IncludeFilters = config.GetIncludeFilters();
      _configuration.IncludeTestAssembly = config.IncludeTestAssembly;
      _configuration.MergeWith = "";
      _configuration.SingleHit = config.UseSingleHit;
      _configuration.SkipAutoProps = config.SkipAutoProps;
      _configuration.formats = config.GetOutputFormats();
      _configuration.FilePrefix = config.GetFilePrefix();
      _configuration.Threshold = config.GetThreshold();
      _configuration.ThresholdStat = config.GetThresholdStatistic();
      _configuration.ThresholdType = config.GetThresholdTypes();
      _configuration.UseSourceLink = false;

      _logger.LogVerbose($"Test module path: {_testModulePath}");
      _configuration.TestModule = _testModulePath;

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
      _isCoverageEnabled = false;

      // Display error to user via IOutputDevice (visible in console output)
      DisplayErrorToUserAsync(ex).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Displays an error message to the user via the output device.
  /// This ensures the error is visible in console output, not just diagnostic logs.
  /// </summary>
  private async Task DisplayErrorToUserAsync(Exception ex)
  {
    string errorMessage = FormatErrorForDisplay(ex);

    await _outputDisplay.DisplayAsync(
      this,
      new FormattedTextOutputDeviceData($"[Coverlet] Coverage instrumentation failed:")
      {
        ForegroundColor = new SystemConsoleColor { ConsoleColor = ConsoleColor.Red }
      },
      CancellationToken.None);

    await _outputDisplay.DisplayAsync(
      this,
      new FormattedTextOutputDeviceData($"  {errorMessage}")
      {
        ForegroundColor = new SystemConsoleColor { ConsoleColor = ConsoleColor.Yellow }
      },
      CancellationToken.None);

    await _outputDisplay.DisplayAsync(
      this,
      new TextOutputDeviceData("  Use --diagnostic for detailed logs."),
      CancellationToken.None);
  }

  /// <summary>
  /// Formats an exception for user-friendly display.
  /// </summary>
  private static string FormatErrorForDisplay(Exception ex)
  {
    // Handle AggregateException by extracting inner exceptions
    if (ex is AggregateException aggEx && aggEx.InnerExceptions.Count > 0)
    {
      var distinctMessages = aggEx.InnerExceptions
#pragma warning disable IDE0200
        .Select(e => GetConciseErrorMessage(e))
        .Distinct()
        .ToList();
#pragma warning restore IDE0200

      return string.Join(Environment.NewLine + "  ", distinctMessages);
    }

    return GetConciseErrorMessage(ex);
  }

  /// <summary>
  /// Gets a concise error message suitable for console display.
  /// </summary>
  private static string GetConciseErrorMessage(Exception ex)
  {
    // For IOException (file access issues), include the specific file info
    return ex is IOException ioEx ? ioEx.Message : ex.Message;
  }

  /// <summary>
  /// Provides environment variables to the test host process.
  /// This allows the in-process handler to know coverage is enabled.
  /// </summary>
  Task ITestHostEnvironmentVariableProvider.UpdateAsync(IEnvironmentVariables environmentVariables)
  {
    if (IsCoverageEnabled && !string.IsNullOrEmpty(_coverageIdentifier))
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

    if (!IsCoverageEnabled || _coverage == null || _serviceProvider == null)
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
      await GenerateReportsAsync(result, testHostProcessInformation.ExitCode, cancellation);

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
    return Task.FromResult(IsCoverageEnabled);
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
      Module = _configuration.TestModule,
      DeterministicReport = _configuration.DeterministicReport,
      DisableManagedInstrumentationRestore = _configuration.DisableManagedInstrumentationRestore,
      DoesNotReturnAttributes = _configuration.DoesNotReturnAttributes,
      ExcludeAssembliesWithoutSources = _configuration.ExcludeAssembliesWithoutSources,
      ExcludeAttributes = _configuration.ExcludeAttributes,
      ExcludeFilters = _configuration.ExcludeFilters,
      ExcludedSourceFiles = _configuration.ExcludedSourceFiles,
      IncludeDirectories = _configuration.IncludeDirectories,
      IncludeFilters = _configuration.IncludeFilters,
      IncludeTestAssembly = _configuration.IncludeTestAssembly,
      MergeWith = _configuration.MergeWith,
      SingleHit = _configuration.SingleHit,
      SkipAutoProps = _configuration.SkipAutoProps,
      UseSourceLink = _configuration.UseSourceLink
    };

    _logger.LogVerbose($"Coverlet configuration: {_configuration}");

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

  // Add internal property for testing
  internal IReporterFactory? ReporterFactoryOverride { get; set; }

  /// <summary>
  /// Generates coverage report files. Separated for testability.
  /// </summary>
  internal (List<string> FileReports, List<string> ConsoleOutputs) GenerateCoverageReportFiles(
    CoverageResult result,
    ISourceRootTranslator sourceRootTranslator,
    IFileSystem fileSystem,
    string outputDirectory,
    string[] formats,
    string? filePrefix = null)
  {
    var generatedReports = new List<string>();
    var consoleOutputs = new List<string>();

    foreach (string format in formats)
    {
      // Use override if set (for testing), otherwise use injected factory
      IReporterFactory factory = ReporterFactoryOverride ?? _reporterFactory;
      IReporter reporter = factory.CreateReporter(format) ??
        throw new InvalidOperationException($"Specified output format '{format}' is not supported");

      if (reporter.OutputType == ReporterOutputType.Console)
      {
        string consoleOutput = reporter.Report(result, sourceRootTranslator);
        consoleOutputs.Add(consoleOutput);
      }
      else
      {
        // Defensive validation of filePrefix to prevent path traversal
        string? sanitizedPrefix = SanitizeFilePrefix(filePrefix);
        string filename = string.IsNullOrEmpty(sanitizedPrefix)
          ? $"coverage.{reporter.Extension}"
          : $"{sanitizedPrefix}.coverage.{reporter.Extension}";
        string filenameWithTimestamp = InjectTimestamp(filename, DateTime.UtcNow);
        string reportPath = Path.Combine(outputDirectory, filenameWithTimestamp);
        fileSystem.WriteAllText(reportPath, reporter.Report(result, sourceRootTranslator));
        generatedReports.Add(reportPath);
      }
    }

    return (generatedReports, consoleOutputs);
  }

  private static string InjectTimestamp(string filename, DateTime utcNow)
  {
    string timestamp = utcNow.ToString("ddMMyyHHmmssfff");
    int lastDot = filename.LastIndexOf('.');
    return lastDot > 0 ? filename.Insert(lastDot, $".{timestamp}") : $"{filename}.{timestamp}";
  }

  /// <summary>
  /// Displays console-type reporter outputs (e.g. teamcity) directly to the output device.
  /// An empty line is written before each output to separate it visually.
  /// </summary>
  private async Task DisplayConsoleReportOutputsAsync(List<string> consoleOutputs, CancellationToken cancellation)
  {
    foreach (string output in consoleOutputs)
    {
      await _outputDisplay.DisplayAsync(
        this,
        new TextOutputDeviceData(Environment.NewLine + output),
        cancellation).ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Publishes coverage metrics, threshold evaluations, and report references to the MTP message bus.
  /// </summary>
  private async Task PublishCoverageDataAsync(CoverageResult result, IReadOnlyList<string> generatedReports, CancellationToken cancellation)
  {
    cancellation.ThrowIfCancellationRequested();

    if (_messageBus is null)
    {
      _logger.LogVerbose("MTP message bus is unavailable. Skipping structured coverage data publishing.");
      return;
    }

    SessionUid sessionUid = CreateSessionUid(result);

    foreach (TestCoverageMessage coverageMessage in _coverageDataProducer.CreateCoverageMessages(result, sessionUid))
    {
      cancellation.ThrowIfCancellationRequested();
      await _messageBus.PublishAsync(_coverageDataProducer, coverageMessage).ConfigureAwait(false);
    }

    foreach (string generatedReport in generatedReports)
    {
      cancellation.ThrowIfCancellationRequested();
      string fileName = Path.GetFileName(generatedReport);
      string fileNameLower = fileName.ToLowerInvariant();
      string reportFormat = fileNameLower.Contains(".cobertura.") ? "cobertura"
        : fileNameLower.Contains(".opencover.") ? "opencover"
        : fileNameLower.EndsWith(".info", StringComparison.Ordinal) ? "lcov"
        : fileNameLower.EndsWith(".json", StringComparison.Ordinal) ? "json"
        : Path.GetExtension(fileNameLower).TrimStart('.');
      if (string.IsNullOrWhiteSpace(reportFormat))
      {
        continue;
      }

      TestCoverageReportMessage reportMessage = _coverageDataProducer.CreateReportMessage(sessionUid, generatedReport, reportFormat);
      await _messageBus.PublishAsync(_coverageDataProducer, reportMessage).ConfigureAwait(false);
    }

    if (!_configuration.Threshold.HasValue)
    {
      return;
    }

    ThresholdStatistic thresholdStat = _configuration.ThresholdStat;
    Dictionary<ThresholdTypeFlags, double> thresholdValues = BuildThresholdValues(
      _configuration.ThresholdType,
      _configuration.Threshold.Value);
    ThresholdTypeFlags belowThreshold = result.GetThresholdTypesBelowThreshold(thresholdValues, thresholdStat);

    foreach (TestCoverageThresholdMessage thresholdMessage in _coverageDataProducer.CreateThresholdMessages(
      result,
      sessionUid,
      thresholdValues,
      thresholdStat,
      GetThresholdCoverage,
      belowThreshold))
    {
      cancellation.ThrowIfCancellationRequested();
      await _messageBus.PublishAsync(_coverageDataProducer, thresholdMessage).ConfigureAwait(false);
    }
  }

  private SessionUid CreateSessionUid(CoverageResult result)
  {
    string? coverageIdentifier = _coverageIdentifier;
    if (!string.IsNullOrWhiteSpace(coverageIdentifier))
    {
      return new SessionUid(coverageIdentifier!);
    }

    string? resultIdentifier = result.Identifier;
    if (!string.IsNullOrWhiteSpace(resultIdentifier))
    {
      return new SessionUid(resultIdentifier!);
    }

    string fallbackIdentifier = Guid.NewGuid().ToString("N");
    _logger.LogWarning("Coverage identifier is missing. Falling back to a generated session identifier.");
    return new SessionUid(fallbackIdentifier);
  }

  private static Dictionary<ThresholdTypeFlags, double> BuildThresholdValues(IEnumerable<string> thresholdTypes, double threshold)
  {
    var values = new Dictionary<ThresholdTypeFlags, double>();
    foreach (string thresholdType in thresholdTypes)
    {
      values[ParseThresholdType(thresholdType)] = threshold;
    }

    return values;
  }

  private static ThresholdTypeFlags ParseThresholdType(string thresholdType) =>
    thresholdType.Trim().ToLowerInvariant() switch
    {
      "line" => ThresholdTypeFlags.Line,
      "branch" => ThresholdTypeFlags.Branch,
      "method" => ThresholdTypeFlags.Method,
      _ => throw new InvalidOperationException($"Invalid threshold type '{thresholdType}'. Valid values are line, branch, and method.")
    };

  private static double GetThresholdCoverage(CoverageResult result, ThresholdTypeFlags thresholdType, ThresholdStatistic thresholdStat)
  {
    if (thresholdStat == ThresholdStatistic.Minimum)
    {
      return result.Modules.Values
        .Select(module => GetCoveragePercent(module, thresholdType))
        .DefaultIfEmpty(0)
        .Min();
    }

    CoverageDetails coverage = thresholdType switch
    {
      ThresholdTypeFlags.Line => CoverageSummary.CalculateLineCoverage(result.Modules),
      ThresholdTypeFlags.Branch => CoverageSummary.CalculateBranchCoverage(result.Modules),
      ThresholdTypeFlags.Method => CoverageSummary.CalculateMethodCoverage(result.Modules),
      _ => throw new ArgumentOutOfRangeException(nameof(thresholdType), thresholdType, "A single coverage threshold type is required.")
    };

    return thresholdStat == ThresholdStatistic.Average
      ? coverage.AverageModulePercent
      : coverage.Percent;
  }

  private static double GetCoveragePercent(Documents module, ThresholdTypeFlags thresholdType) =>
    thresholdType switch
    {
      ThresholdTypeFlags.Line => CoverageSummary.CalculateLineCoverage(module).Percent,
      ThresholdTypeFlags.Branch => CoverageSummary.CalculateBranchCoverage(module).Percent,
      ThresholdTypeFlags.Method => CoverageSummary.CalculateMethodCoverage(module).Percent,
      _ => throw new ArgumentOutOfRangeException(nameof(thresholdType), thresholdType, "A single coverage threshold type is required.")
    };

  private bool IsCoverageThresholdExitCodeIgnored()
  {
    const string ignoreExitCodeOption = "ignore-exit-code";

    if (!_commandLineOptions.TryGetOptionArgumentList(ignoreExitCodeOption, out string[]? ignoredExitCodes) || ignoredExitCodes is null)
    {
      return false;
    }

    foreach (string ignoredExitCode in ignoredExitCodes)
    {
      string[] values = ignoredExitCode.Split(s_ignoredExitCodeSeparators, StringSplitOptions.RemoveEmptyEntries);
      foreach (string value in values)
      {
        if (string.Equals(value.Trim(), "14", StringComparison.Ordinal))
        {
          return true;
        }
      }
    }

    return false;
  }

  /// <summary>
  /// Displays generated report paths to output device.
  /// </summary>
  private async Task DisplayGeneratedReportsAsync(List<string> generatedReports, CancellationToken cancellation)
  {
    if (generatedReports.Count == 0)
    {
      return;
    }

    _logger.LogInformation("Coverage reports generated:", important: true);
    foreach (string reportPath in generatedReports)
    {
      _logger.LogInformation($"  {reportPath}", important: true);
    }

    var outputBuilder = new StringBuilder();
    outputBuilder.AppendLine();
    outputBuilder.AppendLine("  Out of process file artifacts produced:");
    foreach (string reportPath in generatedReports)
    {
      outputBuilder.AppendLine($"    - {reportPath}");
    }

    await _outputDisplay.DisplayAsync(
      this,
      new TextOutputDeviceData(outputBuilder.ToString()),
      cancellation).ConfigureAwait(false);
  }

  // Refactor GenerateReportsAsync to use extracted methods
  private async Task GenerateReportsAsync(CoverageResult result, int testHostExitCode, CancellationToken cancellation)
  {
    string outputDirectory = _platformConfiguration!.GetTestResultDirectory() ??
      Path.GetDirectoryName(_testModulePath) + Path.DirectorySeparatorChar;

    _logger.LogVerbose($"Coverage output directory: {outputDirectory}");

    // Ensure directory exists
    if (!_fileSystem.Exists(outputDirectory))
    {
      _fileSystem.CreateDirectory(outputDirectory);
    }

    ISourceRootTranslator sourceRootTranslator = _serviceProvider!.GetRequiredService<ISourceRootTranslator>();
    IFileSystem fileSystem = _serviceProvider!.GetRequiredService<IFileSystem>();

    // Generate reports (now testable separately)
    (List<string> generatedReports, List<string> consoleOutputs) = GenerateCoverageReportFiles(
      result,
      sourceRootTranslator,
      fileSystem,
      outputDirectory,
      _configuration.formats,
      _configuration.FilePrefix);

    // Display results
    await DisplayGeneratedReportsAsync(generatedReports, cancellation);

    // Publish code coverage summary and threshold data to the MTP message bus.
    await PublishCoverageDataAsync(result, generatedReports, cancellation);

    // Display console-type report output (e.g. teamcity) directly to the output device
    await DisplayConsoleReportOutputsAsync(consoleOutputs, cancellation);

    // Coverage threshold exit-code behavior is owned by Microsoft Testing Platform.
    if (_configuration.Threshold.HasValue)
    {
      ThresholdStatistic thresholdStat = _configuration.ThresholdStat;
      Dictionary<ThresholdTypeFlags, double> thresholdValues = BuildThresholdValues(
        _configuration.ThresholdType,
        _configuration.Threshold.Value);
      ThresholdTypeFlags belowThreshold = result.GetThresholdTypesBelowThreshold(thresholdValues, thresholdStat);
      if (belowThreshold != ThresholdTypeFlags.None)
      {
        if (IsCoverageThresholdExitCodeIgnored())
        {
          _logger.LogInformation("Coverage thresholds not met, but exit code 14 is ignored by --ignore-exit-code.");
        }
        else
        {
          _logger.LogError("Coverage thresholds not met.");
        }
      }
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

  /// <summary>
  /// Sanitizes the file prefix to ensure it's a safe filename segment.
  /// Returns null if the prefix is invalid or empty.
  /// </summary>
  private static string? SanitizeFilePrefix(string? filePrefix)
  {
    if (string.IsNullOrWhiteSpace(filePrefix))
    {
      return null;
    }

    // At this point, filePrefix is guaranteed to be non-null and non-whitespace
    string prefix = filePrefix!;

    // Reject directory separators (path traversal prevention)
    if (prefix.Contains('/') || prefix.Contains('\\'))
    {
      return null;
    }

    // Reject rooted paths
    if (Path.IsPathRooted(prefix))
    {
      return null;
    }

    // Reject path traversal patterns
    if (prefix.Equals("..", StringComparison.Ordinal) || prefix.StartsWith("..", StringComparison.Ordinal))
    {
      return null;
    }

    // Reject invalid filename characters
    char[] invalidChars = Path.GetInvalidFileNameChars();
    return prefix.IndexOfAny(invalidChars) >= 0 ? null : prefix;
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
      if (!fileName.StartsWith("testhost", StringComparison.OrdinalIgnoreCase))
      {
        return entryAssembly;
      }
    }

    return null;
  }

  /// <summary>
  /// Loads configuration file settings from testconfig.json or coverlet.mtp.appsettings.json.
  /// Priority: [appname].testconfig.json > testconfig.json > coverlet.mtp.appsettings.json
  /// The config file is expected to be in the same directory as the test module.
  /// </summary>
  /// <param name="testModulePath">Path to the test assembly</param>
  /// <returns>Parsed settings, or null if no config file found</returns>
  private CoverletMTPSettings? LoadConfigurationFileSettings(string testModulePath)
  {
    // Try testconfig.json first (new MTP standard format)
    CoverletMTPSettings? settings = LoadTestConfigSettings(testModulePath);
    if (settings is not null)
    {
      return settings;
    }

    // Fall back to coverlet.mtp.appsettings.json (legacy format)
    return LoadLegacyAppSettings(testModulePath);
  }

  /// <summary>
  /// Loads configuration from testconfig.json (Microsoft Testing Platform standard format).
  /// </summary>
  /// <param name="testModulePath">Path to the test assembly</param>
  /// <returns>Parsed settings if testconfig.json exists with Coverlet section, null otherwise</returns>
  private CoverletMTPSettings? LoadTestConfigSettings(string testModulePath)
  {
    try
    {
      // Log which testconfig.json file we're looking for
      string? configPath = TestConfigParser.FindTestConfigFile(testModulePath, _fileSystem);
      if (configPath is null)
      {
        // Log both candidate paths so users understand what was searched
        string? directory = Path.GetDirectoryName(testModulePath);
        string appName = Path.GetFileNameWithoutExtension(testModulePath);
        _logger.LogVerbose($"No testconfig.json found for module: {testModulePath}");
        _logger.LogVerbose($"  Searched: {appName}{TestConfigParser.TestConfigFileNameSuffix}, {TestConfigParser.GenericTestConfigFileName}");
        return null;
      }

      _logger.LogVerbose($"Found testconfig.json: {configPath}");

      // Use the already-discovered configPath to avoid duplicate filesystem lookups
      CoverletMTPSettings? settings = TestConfigParser.ParseFromFile(configPath, testModulePath, _fileSystem);
      if (settings is null)
      {
        _logger.LogVerbose($"testconfig.json exists but contains no Coverlet section: {configPath}");
        return null;
      }

      _logger.LogVerbose($"testconfig.json settings loaded: Format={string.Join(",", settings.ReportFormats)}, IncludeTestAssembly={settings.IncludeTestAssembly}");
      return settings;
    }
    catch (Exception ex)
    {
      _logger.LogWarning($"Failed to load testconfig.json: {ex}");
      return null;
    }
  }

  /// <summary>
  /// Loads configuration from coverlet.mtp.appsettings.json (legacy format).
  /// </summary>
  /// <param name="testModulePath">Path to the test assembly</param>
  /// <returns>Parsed settings if config file exists, null otherwise</returns>
  private CoverletMTPSettings? LoadLegacyAppSettings(string testModulePath)
  {
    try
    {
      string? directory = Path.GetDirectoryName(testModulePath);
      if (string.IsNullOrEmpty(directory))
      {
        return null;
      }

      string configFilePath = Path.Combine(directory, CoverletMTPConstants.ConfigFileName);
      if (!_fileSystem.Exists(configFilePath))
      {
        _logger.LogVerbose($"Configuration file not found: {configFilePath}");
        return null;
      }

      _logger.LogInformation($"Loading configuration from: {configFilePath}");

      // Read file content via IFileSystem to honor the abstraction end-to-end (enables unit testing/mocking)
      string jsonContent = _fileSystem.ReadAllText(configFilePath);

      // Build configuration from the JSON content using a MemoryStream
      var configBuilder = new ConfigurationBuilder();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
      configBuilder.AddJsonStream(stream);
      Microsoft.Extensions.Configuration.IConfiguration configuration = configBuilder.Build();

      CoverletMTPSettings settings = CoverletMTPSettingsParser.Parse(configuration, testModulePath);
      settings.IsFromConfigFile = true;
      _logger.LogVerbose($"Configuration file settings loaded: Format={string.Join(",", settings.ReportFormats)}, IncludeTestAssembly={settings.IncludeTestAssembly}");

      return settings;
    }
    catch (Exception ex)
    {
      _logger.LogWarning($"Failed to load configuration file '{CoverletMTPConstants.ConfigFileName}': {ex}");
      return null;
    }
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
