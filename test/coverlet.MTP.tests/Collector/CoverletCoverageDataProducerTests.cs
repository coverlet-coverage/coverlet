// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core;
using Coverlet.Core.Enums;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using Xunit;

namespace Coverlet.MTP.Collector.Tests;

public class CoverletCoverageDataProducerTests
{
  [Fact]
  public void CreateCoverageMessagesCreatesOverallAndModuleMessagesForLineBranchMethod()
  {
    var producer = new CoverletCoverageDataProducer();
    CoverageResult result = CreateCoverageResult(hits: 1);

    IReadOnlyList<TestCoverageMessage> messages = producer.CreateCoverageMessages(result, new SessionUid("session"));

    Assert.Equal(6, messages.Count);
    Assert.Contains(messages, message => message.Scope.Level == CoverageScopeLevel.Overall && message.Metric == CoverageMetric.Line);
    Assert.Contains(messages, message => message.Scope.Level == CoverageScopeLevel.Overall && message.Metric == CoverageMetric.Branch);
    Assert.Contains(messages, message => message.Scope.Level == CoverageScopeLevel.Overall && message.Metric == CoverageMetric.Method);
    Assert.Contains(messages, message => message.Scope.Level == CoverageScopeLevel.Module && message.Metric == CoverageMetric.Line);
    Assert.Contains(messages, message => message.Scope.Level == CoverageScopeLevel.Module && message.Metric == CoverageMetric.Branch);
    Assert.Contains(messages, message => message.Scope.Level == CoverageScopeLevel.Module && message.Metric == CoverageMetric.Method);
  }

  [Fact]
  public void CreateThresholdMessagesMapsThresholdStatisticToCoverageAggregation()
  {
    var producer = new CoverletCoverageDataProducer();
    CoverageResult result = CreateCoverageResult(hits: 1);
    var thresholds = new Dictionary<ThresholdTypeFlags, double>
    {
      [ThresholdTypeFlags.Line] = 70,
    };

    IReadOnlyList<TestCoverageThresholdMessage> messages = producer.CreateThresholdMessages(
      result,
      new SessionUid("session"),
      thresholds,
      ThresholdStatistic.Average,
      static (_, _, _) => 100,
      ThresholdTypeFlags.None);

    TestCoverageThresholdMessage message = Assert.Single(messages);
    Assert.Equal(CoverageAggregation.Average, message.Aggregation);
    Assert.Equal(CoverageScopeLevel.Module, message.AggregatedOver);
  }

  [Theory]
  [InlineData("cobertura", CoverageReportFormat.Cobertura, null)]
  [InlineData("opencover", CoverageReportFormat.OpenCover, null)]
  [InlineData("lcov", CoverageReportFormat.Lcov, null)]
  [InlineData("json", CoverageReportFormat.Custom, "coverlet.json")]
  public void CreateReportMessageMapsKnownFormats(string reportFormat, CoverageReportFormat expectedFormat, string? expectedCustomFormat)
  {
    var producer = new CoverletCoverageDataProducer();

    TestCoverageReportMessage message = producer.CreateReportMessage(new SessionUid("session"), "/fake/path/report", reportFormat);

    Assert.Equal(expectedFormat, message.Format);
    Assert.Equal(expectedCustomFormat, message.CustomFormatName);
  }

  private static CoverageResult CreateCoverageResult(int hits)
  {
    var methods = new Methods
    {
      ["System.Void TestClass::TestMethod()"] = new Method
      {
        Lines = new Lines { { 1, hits } },
        Branches = [new BranchInfo { Line = 1, Hits = hits }],
      },
    };
    var classes = new Classes { ["TestClass"] = methods };
    var documents = new Documents { ["TestClass.cs"] = classes };

    return new CoverageResult
    {
      Identifier = "test-id",
      Modules = new Modules { ["test.dll"] = documents },
      Parameters = new CoverageParameters(),
    };
  }
}
