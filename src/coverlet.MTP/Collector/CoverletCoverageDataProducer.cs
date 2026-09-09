// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core;
using Coverlet.Core.Enums;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;

namespace Coverlet.MTP.Collector;

internal sealed class CoverletCoverageDataProducer : IDataProducer
{
  private readonly CoverletExtension _extension = new();

  public string Uid => _extension.Uid;

  public string Version => _extension.Version;

  public string DisplayName => "Coverlet Coverage Data Producer";

  public string Description => "Publishes coverlet coverage and threshold data through Microsoft Testing Platform messages.";

  public Type[] DataTypesProduced { get; } =
  [
    typeof(TestCoverageMessage),
    typeof(TestCoverageThresholdMessage),
    typeof(TestCoverageReportMessage),
  ];

  public Task<bool> IsEnabledAsync() => Task.FromResult(true);

  public IReadOnlyList<TestCoverageMessage> CreateCoverageMessages(CoverageResult result, SessionUid sessionUid)
  {
#if NETSTANDARD2_0
    if (result is null)
    {
      throw new ArgumentNullException(nameof(result));
    }
#else
    ArgumentNullException.ThrowIfNull(result);
#endif

    var messages = new List<TestCoverageMessage>();

    // Calculate overall coverage for all metrics (line, branch, method)
    // regardless of ThresholdType configuration. ThresholdType is only for
    // determining which metrics to enforce thresholds on, not what metrics to calculate.
    CoverageDetails overallLine = CoverageSummary.CalculateLineCoverage(result.Modules);
    CoverageDetails overallBranch = CoverageSummary.CalculateBranchCoverage(result.Modules);
    CoverageDetails overallMethod = CoverageSummary.CalculateMethodCoverage(result.Modules);
    messages.Add(CreateCoverageMessage(sessionUid, CoverageScope.Overall, CoverageMetric.Line, overallLine));
    messages.Add(CreateCoverageMessage(sessionUid, CoverageScope.Overall, CoverageMetric.Branch, overallBranch));
    messages.Add(CreateCoverageMessage(sessionUid, CoverageScope.Overall, CoverageMetric.Method, overallMethod));

    // Calculate per-module coverage for all metrics
    foreach (KeyValuePair<string, Documents> module in result.Modules)
    {
      var scope = new CoverageScope(CoverageScopeLevel.Module, module.Key);
      CoverageDetails line = CoverageSummary.CalculateLineCoverage(module.Value);
      CoverageDetails branch = CoverageSummary.CalculateBranchCoverage(module.Value);
      CoverageDetails method = CoverageSummary.CalculateMethodCoverage(module.Value);
      messages.Add(CreateCoverageMessage(sessionUid, scope, CoverageMetric.Line, line));
      messages.Add(CreateCoverageMessage(sessionUid, scope, CoverageMetric.Branch, branch));
      messages.Add(CreateCoverageMessage(sessionUid, scope, CoverageMetric.Method, method));
    }

    return messages;
  }

  public IReadOnlyList<TestCoverageThresholdMessage> CreateThresholdMessages(
    CoverageResult result,
    SessionUid sessionUid,
    IReadOnlyDictionary<ThresholdTypeFlags, double> thresholdValues,
    ThresholdStatistic thresholdStatistic,
    Func<CoverageResult, ThresholdTypeFlags, ThresholdStatistic, double> thresholdCoverageCalculator,
    ThresholdTypeFlags belowThreshold)
  {
#if NETSTANDARD2_0
    if (result is null)
    {
      throw new ArgumentNullException(nameof(result));
    }

    if (thresholdValues is null)
    {
      throw new ArgumentNullException(nameof(thresholdValues));
    }

    if (thresholdCoverageCalculator is null)
    {
      throw new ArgumentNullException(nameof(thresholdCoverageCalculator));
    }
#else
    ArgumentNullException.ThrowIfNull(result);
    ArgumentNullException.ThrowIfNull(thresholdValues);
    ArgumentNullException.ThrowIfNull(thresholdCoverageCalculator);
#endif

    _ = belowThreshold;

    var thresholdMessages = new List<TestCoverageThresholdMessage>();
    CoverageAggregation aggregation = MapAggregation(thresholdStatistic);

    foreach (KeyValuePair<ThresholdTypeFlags, double> thresholdValue in thresholdValues)
    {
      double actualCoverage = thresholdCoverageCalculator(result, thresholdValue.Key, thresholdStatistic);
      CoverageMetric metric = MapMetric(thresholdValue.Key);
      bool hasCoverableData = HasCoverableDataForMetric(result, thresholdValue.Key, thresholdStatistic);

      thresholdMessages.Add(
        new TestCoverageThresholdMessage(
          sessionUid,
          CoverageScope.Overall,
          metric,
          aggregation,
          actualCoverage,
          thresholdValue.Value,
          hasCoverableData,
          Uid,
          aggregatedOver: aggregation == CoverageAggregation.None ? null : CoverageScopeLevel.Module,
          treatNoDataAsFailure: true));
    }

    return thresholdMessages;
  }

  public TestCoverageReportMessage CreateReportMessage(SessionUid sessionUid, string reportPath, string reportFormat)
  {
    if (string.IsNullOrWhiteSpace(reportPath))
    {
      throw new ArgumentException("Report path must not be null or empty.", nameof(reportPath));
    }

    if (string.IsNullOrWhiteSpace(reportFormat))
    {
      throw new ArgumentException("Report format must not be null or empty.", nameof(reportFormat));
    }

    (CoverageReportFormat format, string? customFormat) = MapReportFormat(reportFormat);
    return new TestCoverageReportMessage(sessionUid, reportPath, format, Uid, customFormat);
  }

  private TestCoverageMessage CreateCoverageMessage(
    SessionUid sessionUid,
    CoverageScope scope,
    CoverageMetric metric,
    CoverageDetails details)
  {
    return new TestCoverageMessage(
      sessionUid,
      scope,
      metric,
      coveredCount: checked((long)details.Covered),
      coverableCount: details.Total,
      producerId: Uid);
  }

  private static CoverageAggregation MapAggregation(ThresholdStatistic thresholdStatistic) =>
    thresholdStatistic switch
    {
      ThresholdStatistic.Total => CoverageAggregation.Total,
      ThresholdStatistic.Average => CoverageAggregation.Average,
      ThresholdStatistic.Minimum => CoverageAggregation.Minimum,
      _ => throw new ArgumentOutOfRangeException(nameof(thresholdStatistic), thresholdStatistic, null),
    };

  private static CoverageMetric MapMetric(ThresholdTypeFlags thresholdType) =>
    thresholdType switch
    {
      ThresholdTypeFlags.Line => CoverageMetric.Line,
      ThresholdTypeFlags.Branch => CoverageMetric.Branch,
      ThresholdTypeFlags.Method => CoverageMetric.Method,
      _ => throw new ArgumentOutOfRangeException(nameof(thresholdType), thresholdType, "A single coverage threshold type is required."),
    };

  private static (CoverageReportFormat Format, string? CustomFormatName) MapReportFormat(string reportFormat)
  {
    return reportFormat.Trim().ToLowerInvariant() switch
    {
      "cobertura" => (CoverageReportFormat.Cobertura, null),
      "opencover" => (CoverageReportFormat.OpenCover, null),
      "lcov" => (CoverageReportFormat.Lcov, null),
      "json" => (CoverageReportFormat.Custom, "coverlet.json"),
      _ => (CoverageReportFormat.Custom, reportFormat),
    };
  }

  private static bool HasCoverableDataForMetric(CoverageResult result, ThresholdTypeFlags thresholdType, ThresholdStatistic thresholdStatistic)
  {
    if (thresholdStatistic == ThresholdStatistic.Minimum)
    {
      return result.Modules.Count > 0;
    }

    CoverageDetails coverage = thresholdType switch
    {
      ThresholdTypeFlags.Line => CoverageSummary.CalculateLineCoverage(result.Modules),
      ThresholdTypeFlags.Branch => CoverageSummary.CalculateBranchCoverage(result.Modules),
      ThresholdTypeFlags.Method => CoverageSummary.CalculateMethodCoverage(result.Modules),
      _ => throw new ArgumentOutOfRangeException(nameof(thresholdType), thresholdType, "A single coverage threshold type is required."),
    };

    return coverage.Total > 0;
  }
}
