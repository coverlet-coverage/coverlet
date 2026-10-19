// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Reporters;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests.Reporters
{
  public class LcovReporterTests
  {
    [Fact]
    public void TestReport()
    {
      var result = new CoverageResult
      {
        Parameters = new CoverageParameters(),
        Identifier = Guid.NewGuid().ToString()
      };

      var lines = new Lines
      {
        { 1, 1 },
        { 2, 0 }
      };

      var branches = new Branches
      {
        new BranchInfo { Line = 1, Hits = 1, Offset = 23, EndOffset = 24, Path = 0, Ordinal = 1 },
        new BranchInfo { Line = 1, Hits = 0, Offset = 23, EndOffset = 27, Path = 1, Ordinal = 2 }
      };

      var methods = new Methods();
      string methodString = "System.Void Coverlet.Core.Tests.Reporters.LcovReporterTests.TestReport()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;
      methods[methodString].Branches = branches;

      var classes = new Classes
      {
        { "Coverlet.Core.Reporters.Tests.LcovReporterTests", methods }
      };

      var documents = new Documents
      {
        { "doc.cs", classes }
      };
      result.Modules = new Modules
      {
        { "module", documents }
      };

      var reporter = new LcovReporter();
      string report = reporter.Report(result, new Mock<ISourceRootTranslator>().Object);

      Assert.NotEmpty(report);
      Assert.Equal("SF:doc.cs", report.Split(Environment.NewLine)[0]);
    }

    [Fact]
    public void TestReport_UnreachableBranches_RecordsAgreeWithTotals()
    {
      var lines = new Lines
      {
        { 1, 3 }
      };

      var branches = new Branches
      {
        new BranchInfo { Line = 1, Hits = 3, Offset = 10, EndOffset = 20, Path = 0, Ordinal = 0, IsReachable = true },
        new BranchInfo { Line = 1, Hits = 0, Offset = 10, EndOffset = 30, Path = 1, Ordinal = 1, IsReachable = true },
        new BranchInfo { Line = 1, Hits = 2, Offset = 40, EndOffset = 50, Path = 0, Ordinal = 2, IsReachable = false },
        new BranchInfo { Line = 1, Hits = 0, Offset = 40, EndOffset = 60, Path = 1, Ordinal = 3, IsReachable = false }
      };

      var methods = new Methods();
      string methodString = "System.Void Coverlet.Core.Tests.Reporters.Sample::TestMethod()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;
      methods[methodString].Branches = branches;

      var result = new CoverageResult
      {
        Parameters = new CoverageParameters(),
        Identifier = Guid.NewGuid().ToString(),
        Modules = new Modules
        {
          { "module", new Documents { { "doc.cs", new Classes { { "Coverlet.Core.Tests.Reporters.Sample", methods } } } } }
        }
      };

      string[] report = new LcovReporter().Report(result, new Mock<ISourceRootTranslator>().Object).Split(Environment.NewLine);

      Assert.Equal(new[] { "BRDA:1,10,0,3", "BRDA:1,10,1,0" }, report.Where(l => l.StartsWith("BRDA:", StringComparison.Ordinal)));
      Assert.Contains("BRF:2", report);
      Assert.Contains("BRH:1", report);
    }
  }
}
