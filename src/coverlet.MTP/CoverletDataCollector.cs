// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Symbols;
using Coverlet.Core.Reporters;
using Coverlet.MTP.Configuration;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using Microsoft.Testing.Platform.Services;

namespace Coverlet.MTP;

/// <summary>
/// Data collector for Coverlet code coverage.
/// Compatible with Microsoft.Testing.Platform V2.0.2
/// </summary>
internal sealed class CoverletDataCollector : IDataProducer, ITestSessionLifetimeHandler
{
  private readonly CoverageConfiguration _configuration;
  private readonly IMessageBus _messageBus;
  private Coverage? _coverage;

  public CoverletDataCollector(
    CoverageConfiguration configuration,
    IMessageBus messageBus)
  {
    _configuration = configuration;
    _messageBus = messageBus;
  }

  public string Uid => nameof(CoverletDataCollector);
  public string Version => "1.0.0";
  public string DisplayName => "Coverlet Code Coverage Collector";
  public string Description => "Collects code coverage data using Coverlet instrumentation";

  public Type[] DataTypesProduced => new[] { typeof(SessionFileArtifact) };

  public Task<bool> IsEnabledAsync()
  {
    // Only enable if --coverlet-coverage flag is present
    return Task.FromResult(_configuration.IsCoverageEnabled);
  }

  public async Task OnTestSessionStartingAsync(SessionUid sessionUid, CancellationToken cancellationToken)
  {
    if (!_configuration.IsCoverageEnabled)
    {
      return; // Skip instrumentation if coverage not requested
    }

    await _messageBus.PublishAsync(this, new SessionFileArtifact(
      sessionUid,
      new FileInfo("coverage-init.log"),
      "Initializing Coverlet code coverage instrumentation...",
      "Coverlet Initialization"));

    try
    {
      // Get test assembly path using standard .NET APIs
      string testModule = CoverageConfiguration.GetTestAssemblyPath();

      // Initialize coverlet with configuration
      var parameters = new CoverageParameters
      {
        IncludeFilters = _configuration.GetIncludeFilters(),
        ExcludeFilters = _configuration.GetExcludeFilters(),
        ExcludedSourceFiles = _configuration.GetExcludeByFileFilters(),
        ExcludeAttributes = _configuration.GetExcludeByAttributeFilters(),
        IncludeDirectories = _configuration.GetIncludeDirectories(),
        SingleHit = _configuration.UseSingleHit,
        IncludeTestAssembly = _configuration.IncludeTestAssembly,
        SkipAutoProps = _configuration.SkipAutoProps,
        DoesNotReturnAttributes = _configuration.GetDoesNotReturnAttributes(),
        ExcludeAssembliesWithoutSources = _configuration.ExcludeAssembliesWithoutSources ? "IncludeAll" : "MissingAny"
      };

      var logger = new ConsoleLogger();
      var fileSystem = new FileSystem();
      var processExitHandler = new ProcessExitHandler();
      var retryHelper = new RetryHelper();
      var sourceRootTranslator = new SourceRootTranslator(testModule, logger, fileSystem);
      var instrumentationHelper = new InstrumentationHelper(
        processExitHandler,
        retryHelper,
        fileSystem,
        logger,
        sourceRootTranslator);
      var cecilSymbolHelper = new CecilSymbolHelper();

      _coverage = new Coverage(
        testModule,
        parameters,
        logger,
        instrumentationHelper,
        fileSystem,
        sourceRootTranslator,
        cecilSymbolHelper);

      // Prepare instrumentation
      await Task.Run(() => _coverage.PrepareModules(), cancellationToken);
    }
    catch (Exception ex)
    {
      await _messageBus.PublishAsync(this, new SessionFileArtifact(
        sessionUid,
        new FileInfo("coverage-error.log"),
        $"Coverage initialization failed: {ex.Message}\n{ex.StackTrace}",
        "Coverlet Error"));

      throw;
    }
  }

  public async Task OnTestSessionFinishingAsync(SessionUid sessionUid, CancellationToken cancellationToken)
  {
    if (!_configuration.IsCoverageEnabled || _coverage == null)
    {
      return; // Skip if not enabled or not initialized
    }

    try
    {
      // Collect coverage results
      CoverageResult result = _coverage.GetCoverageResult();

      //string outputPath = _configuration.GetOutputPath();
      //Directory.CreateDirectory(outputPath);

      string[] formats = _configuration.GetOutputFormats();

      foreach (string format in formats)
      {
        string fileName = format.ToLowerInvariant() switch
        {
          "json" => "coverage.json",
          "lcov" => "coverage.info",
          "opencover" => "coverage.opencover.xml",
          "cobertura" => "coverage.cobertura.xml",
          _ => $"coverage.{format}"
        };

        string fullPath = fileName;
        //string fullPath = Path.Combine(outputPath, fileName);

        // Generate report using appropriate reporter
        IReporter reporter = format.ToLowerInvariant() switch
        {
          "json" => new JsonReporter(),
          "lcov" => new LcovReporter(),
          "opencover" => new OpenCoverReporter(),
          "cobertura" => new CoberturaReporter(),
          _ => new JsonReporter()
        };

        var fileSystem = new FileSystem();
        var logger = new ConsoleLogger();
        string testModule = CoverageConfiguration.GetTestAssemblyPath();
        var sourceRootTranslator = new SourceRootTranslator(
          testModule,
          logger,
          fileSystem);

        string reportContent = reporter.Report(result, sourceRootTranslator);

        // Write file with .NET Standard 2.0 compatibility
        await Task.Run(() => File.WriteAllText(fullPath, reportContent), cancellationToken);

        await _messageBus.PublishAsync(this, new SessionFileArtifact(
          sessionUid,
          new FileInfo(fullPath),
          reportContent,
          $"Coverage Report ({format})"));
      }
    }
    catch (Exception ex)
    {
      await _messageBus.PublishAsync(this, new SessionFileArtifact(
        sessionUid,
        new FileInfo("coverage-report-error.log"),
        $"Coverage report generation failed: {ex.Message}\n{ex.StackTrace}",
        "Coverlet Report Error"));

      throw;
    }
  }

  // ITestSessionLifetimeHandler overloads with ITestSessionContext
  public Task OnTestSessionStartingAsync(ITestSessionContext testSessionContext)
  {
    return OnTestSessionStartingAsync(testSessionContext.SessionUid, testSessionContext.CancellationToken);
  }

  public Task OnTestSessionFinishingAsync(ITestSessionContext testSessionContext)
  {
    return OnTestSessionFinishingAsync(testSessionContext.SessionUid, testSessionContext.CancellationToken);
  }
}

/// <summary>
/// Simple console logger for coverlet.core
/// </summary>
sealed file class ConsoleLogger : ILogger
{
  public void LogVerbose(string message) { }
  public void LogInformation(string message) => Console.WriteLine($"[Coverlet] {message}");
  public void LogInformation(string message, bool important) => Console.WriteLine($"[Coverlet] {message}");
  public void LogWarning(string message) => Console.WriteLine($"[Coverlet Warning] {message}");
  public void LogError(string message) => Console.Error.WriteLine($"[Coverlet Error] {message}");
  public void LogError(Exception exception) => Console.Error.WriteLine($"[Coverlet Error] {exception}");
}
