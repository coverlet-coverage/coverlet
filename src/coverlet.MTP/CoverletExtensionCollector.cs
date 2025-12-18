// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// see details here: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-architecture-extensions#the-itestsessionlifetimehandler-extensions
// Coverlet instrumentation should be done before any test is executed, and the coverage data should be collected after all tests have run.
// Coverlet collects code coverage data and does not need to be aware of the test framework being used. It also does not need test case details or test results.

using coverlet.Extension.Logging;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Enums;
using Coverlet.Core.Helpers;
using Coverlet.Core.Reporters;
using Coverlet.Core.Symbols;
using Coverlet.MTP.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.TestHost;

namespace coverlet.Extension.Collector
{
  /// <summary>
  /// Implements test session lifetime handling for coverage collection using the Microsoft Testing Platform.
  /// </summary>
  internal sealed class CoverletExtensionCollector : ITestHostProcessLifetimeHandler
  {
    private readonly CoverletLoggerAdapter _logger;
    private readonly CoverletExtensionConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private Coverage? _coverage;
    private readonly Microsoft.Testing.Platform.Logging.ILoggerFactory _loggerFactory;
    private readonly Microsoft.Testing.Platform.CommandLine.ICommandLineOptions _commandLineOptions;

    private readonly CoverletExtension _extension = new();

    string IExtension.Uid => _extension.Uid;

    string IExtension.Version => _extension.Version;

    string IExtension.DisplayName => _extension.DisplayName;

    string IExtension.Description => _extension.Description;

    /// <summary>
    /// Initializes a new instance of the CoverletCollectorExtension class.
    /// </summary>
    public CoverletExtensionCollector(Microsoft.Testing.Platform.Logging.ILoggerFactory loggerFactory, Microsoft.Testing.Platform.CommandLine.ICommandLineOptions commandLineOptions)
    {
      _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
      _commandLineOptions = commandLineOptions ?? throw new ArgumentNullException(nameof(commandLineOptions));
      _configuration = new CoverletExtensionConfiguration();
      _logger = new CoverletLoggerAdapter(_loggerFactory);
      _serviceProvider = CreateServiceProvider();
    }

    /// <inheritdoc/>
    public async Task BeforeRunAsync(CancellationToken cancellationToken)
    {
      // Only collect coverage if --coverlet-coverage flag is explicitly provided
      bool isCoverageEnabled = _commandLineOptions.IsOptionSet(CoverletOptionNames.Coverage);

      if (!isCoverageEnabled)
      {
        _logger.LogInformation($"Coverage collection is disabled. Use --{CoverletOptionNames.Coverage} to enable it.");
        return;
      }

      try
      {
        var parameters = new CoverageParameters
        {
          IncludeFilters = _configuration.IncludePatterns,
          ExcludeFilters = _configuration.ExcludePatterns,
          IncludeTestAssembly = _configuration.IncludeTestAssembly,
          SingleHit = false,
          UseSourceLink = true,
          SkipAutoProps = true,
          ExcludeAssembliesWithoutSources = AssemblySearchType.MissingAll.ToString().ToLowerInvariant(),
        };

        string moduleDirectory = Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty;

        _coverage = new Coverage(
            moduleDirectory,
            parameters,
            _logger,
            _serviceProvider.GetRequiredService<IInstrumentationHelper>(),
            _serviceProvider.GetRequiredService<IFileSystem>(),
            _serviceProvider.GetRequiredService<ISourceRootTranslator>(),
            _serviceProvider.GetRequiredService<ICecilSymbolHelper>());

        // Instrument assemblies before any test execution
        await Task.Run(() =>
        {
          CoveragePrepareResult prepareResult = _coverage.PrepareModules();
          _logger.LogInformation($"Code coverage instrumentation completed. Instrumented {prepareResult.Results.Length} modules");
        }, cancellationToken);

      }
      catch (Exception ex)
      {
        _logger.LogError("Failed to initialize code coverage");
        _logger.LogError(ex);
      }
    }

    /// <inheritdoc/>
    public async Task AfterRunAsync(int exitCode, CancellationToken cancellation)
    {
      try
      {
        if (_coverage == null)
        {
          _logger.LogInformation("Coverage was not collected.");
          return;
        }

        _logger.LogInformation("\nCalculating coverage result...");
        CoverageResult result = _coverage!.GetCoverageResult();

        string dOutput = _configuration.OutputDirectory != null ? _configuration.OutputDirectory : Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar.ToString();

        string directory = Path.GetDirectoryName(dOutput)!;

        if (!Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        ISourceRootTranslator sourceRootTranslator = _serviceProvider.GetRequiredService<ISourceRootTranslator>();
        IFileSystem fileSystem = _serviceProvider.GetService<IFileSystem>()!;

        // Convert to coverlet format
        foreach (string format in _configuration.formats)
        {
          IReporter reporter = new ReporterFactory(format).CreateReporter();
          if (reporter == null)
          {
            throw new InvalidOperationException($"Specified output format '{format}' is not supported");
          }

          if (reporter.OutputType == ReporterOutputType.Console)
          {
            // Output to console
            _logger.LogInformation("  Outputting results to console", important: true);
            _logger.LogInformation(reporter.Report(result, sourceRootTranslator), important: true);
          }
          else
          {
            // Output to file
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

    public Task OnTestSessionStartingAsync(SessionUid sessionUid, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public Task OnTestSessionFinishingAsync(SessionUid sessionUid, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    Task ITestHostProcessLifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    Task ITestHostProcessLifetimeHandler.OnTestHostProcessStartedAsync(ITestHostProcessInformation testHostProcessInformation, CancellationToken cancellation)
    {
      throw new NotImplementedException();
    }

    Task ITestHostProcessLifetimeHandler.OnTestHostProcessExitedAsync(ITestHostProcessInformation testHostProcessInformation, CancellationToken cancellation)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Determines if the extension is enabled.
    /// Always returns true so that command-line options appear in help.
    /// Actual coverage collection is controlled by the --coverlet-coverage flag.
    /// </summary>
    public Task<bool> IsEnabledAsync()
    {
      // Always enable the extension so options appear in help
      // Coverage collection is controlled by checking the --coverlet-coverage flag in BeforeRunAsync
      return Task.FromResult(true);
    }
  }
}
