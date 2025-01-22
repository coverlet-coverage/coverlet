// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Coverlet.Core.Abstractions;
using Moq;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
  public class LcovReporterTests
  {
    [Fact]
    public void TestReport()
    {
      var result = new CoverageResult();
      result.Parameters = new CoverageParameters();
      result.Identifier = Guid.NewGuid().ToString();

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
      string methodString = "System.Void Coverlet.Core.Reporters.Tests.LcovReporterTests.TestReport()";
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
  }
}
