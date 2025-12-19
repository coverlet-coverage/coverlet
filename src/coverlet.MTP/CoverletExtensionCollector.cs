// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

namespace coverlet.Extension.Collector
{
  /// <summary>
  /// Implements test host process lifetime handling for coverage collection using the Microsoft Testing Platform.
  /// </summary>
  internal sealed class CoverletExtensionCollector : ITestHostProcessLifetimeHandler
  {
    private readonly CoverletLoggerAdapter _logger;
    private readonly CoverletExtensionConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private Coverage? _coverage;
    private readonly Microsoft.Testing.Platform.Logging.ILoggerFactory _loggerFactory;
    private readonly Microsoft.Testing.Platform.CommandLine.ICommandLineOptions _commandLineOptions;
    private bool _coverageEnabled;

    private readonly CoverletExtension _extension = new();

    string IExtension.Uid => _extension.Uid;
    string IExtension.Version => _extension.Version;
    string IExtension.DisplayName => _extension.DisplayName;
    string IExtension.Description => _extension.Description;

    public CoverletExtensionCollector(
      Microsoft.Testing.Platform.Logging.ILoggerFactory loggerFactory,
      Microsoft.Testing.Platform.CommandLine.ICommandLineOptions commandLineOptions)
    {
      _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
      _commandLineOptions = commandLineOptions ?? throw new ArgumentNullException(nameof(commandLineOptions));
      _configuration = new CoverletExtensionConfiguration();
      _logger = new CoverletLoggerAdapter(_loggerFactory);
      _serviceProvider = CreateServiceProvider();
    }

    /// <summary>
    /// Called before the test host process starts. Instruments assemblies for coverage collection.
    /// </summary>
    Task ITestHostProcessLifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken cancellationToken)
    {
      // Check if --coverlet-coverage flag was provided
      _coverageEnabled = _commandLineOptions.IsOptionSet(CoverletOptionNames.Coverage);

      if (!_coverageEnabled)
      {
        _logger.LogInformation("Coverage collection is disabled. Use --coverlet-coverage to enable.");
        return Task.CompletedTask;
      }

      _logger.LogInformation("Initializing coverage instrumentation...");

      try
      {
        // Parse additional options from command line
        ParseCommandLineOptions();

        // Initialize the Coverage instance for instrumentation
        IFileSystem fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();
        ISourceRootTranslator sourceRootTranslator = _serviceProvider.GetRequiredService<ISourceRootTranslator>();
        IInstrumentationHelper instrumentationHelper = _serviceProvider.GetRequiredService<IInstrumentationHelper>();
        ICecilSymbolHelper cecilSymbolHelper = _serviceProvider.GetRequiredService<ICecilSymbolHelper>();

        // Get the test assembly path (the entry assembly being executed)
        string testModule = System.Reflection.Assembly.GetEntryAssembly()?.Location
            ?? throw new InvalidOperationException("Could not determine test assembly location");

        var parameters = new CoverageParameters
        {
          Module = testModule,
          IncludeFilters = _configuration.IncludeFilters,
          IncludeDirectories = _configuration.IncludeDirectories,
          ExcludeFilters = _configuration.ExcludeFilters,
          ExcludedSourceFiles = _configuration.ExcludedSourceFiles,
          ExcludeAttributes = _configuration.ExcludeAttributes,
          IncludeTestAssembly = _configuration.IncludeTestAssembly,
          SingleHit = _configuration.SingleHit,
          MergeWith = _configuration.MergeWith,
          UseSourceLink = _configuration.UseSourceLink,
          DoesNotReturnAttributes = _configuration.DoesNotReturnAttributes,
          SkipAutoProps = _configuration.SkipAutoProps,
          DeterministicReport = _configuration.DeterministicReport,
          ExcludeAssembliesWithoutSources = _configuration.ExcludeAssembliesWithoutSources
        };

        _coverage = new Coverage(
          testModule,
          parameters,
          _logger,
          instrumentationHelper,
          fileSystem,
          sourceRootTranslator,
          cecilSymbolHelper);

        _coverage.PrepareModules();

        _logger.LogInformation("Coverage instrumentation initialized successfully");
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
    /// Called when the test host process has started. Can be used for logging/tracking.
    /// </summary>
    Task ITestHostProcessLifetimeHandler.OnTestHostProcessStartedAsync(
      ITestHostProcessInformation testHostProcessInformation,
      CancellationToken cancellation)
    {
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
      if (!_coverageEnabled || _coverage == null)
      {
        return;
      }

      try
      {
        _logger.LogInformation($"Test host process exited (PID: {testHostProcessInformation.PID}, ExitCode: {testHostProcessInformation.ExitCode})");
        _logger.LogInformation("\nCalculating coverage result...");

        CoverageResult result = _coverage.GetCoverageResult();

        string dOutput = _configuration.OutputDirectory ?? Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar.ToString();
        string directory = Path.GetDirectoryName(dOutput)!;

        if (!Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        ISourceRootTranslator sourceRootTranslator = _serviceProvider.GetRequiredService<ISourceRootTranslator>();
        IFileSystem fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();

        foreach (string format in _configuration.formats)
        {
          IReporter reporter = new ReporterFactory(format).CreateReporter();
          if (reporter == null)
          {
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

            string report = Path.Combine(directory, filename);
            _logger.LogInformation($"  Generating report '{report}'", important: true);
            await Task.Run(() => fileSystem.WriteAllText(report, reporter.Report(result, sourceRootTranslator)), cancellation);
          }
        }

        _logger.LogInformation("Code coverage collection completed");
      }
      catch (Exception ex)
      {
        _logger.LogError("Failed to collect code coverage");
        _logger.LogError(ex);
      }
    }

    /// <summary>
    /// Always returns true so command-line options appear in help.
    /// Actual coverage collection is controlled by --coverlet-coverage flag.
    /// </summary>
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    private void ParseCommandLineOptions()
    {
      // Parse --formats option
      if (_commandLineOptions.TryGetOptionArgumentList(CoverletOptionNames.Formats, out string[]? formats) && formats != null)
      {
        _configuration.formats = formats.SelectMany(f => f.Split(',')).ToArray();
      }

      // Parse other options as needed (--include, --exclude, --output, etc.)
      // Add similar parsing for other CoverletOptionNames
    }

    private IServiceProvider CreateServiceProvider()
    {
      var services = new ServiceCollection();

      services.AddSingleton<Coverlet.Core.Abstractions.ILogger>(_logger);
      services.AddSingleton<IFileSystem, FileSystem>();
      services.AddSingleton<IAssemblyAdapter, AssemblyAdapter>();
      services.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
      services.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();

      services.AddSingleton<ISourceRootTranslator>(provider =>
          new SourceRootTranslator(
              _configuration.sourceMappingFile,
              provider.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(),
              provider.GetRequiredService<IFileSystem>()));

      return services.BuildServiceProvider();
    }
  }
}
