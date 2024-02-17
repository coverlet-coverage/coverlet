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
      CoverageResult result = new();
      result.Parameters = new CoverageParameters();
      result.Identifier = Guid.NewGuid().ToString();

      Lines lines = new();
      lines.Add(1, 1);
      lines.Add(2, 0);

      Branches branches = new();
      branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 23, EndOffset = 24, Path = 0, Ordinal = 1 });
      branches.Add(new BranchInfo { Line = 1, Hits = 0, Offset = 23, EndOffset = 27, Path = 1, Ordinal = 2 });

      Methods methods = new();
      string methodString = "System.Void Coverlet.Core.Reporters.Tests.LcovReporterTests.TestReport()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;
      methods[methodString].Branches = branches;

      Classes classes = new();
      classes.Add("Coverlet.Core.Reporters.Tests.LcovReporterTests", methods);

      Documents documents = new();
      documents.Add("doc.cs", classes);
      result.Modules = new Modules();
      result.Modules.Add("module", documents);

      LcovReporter reporter = new();
      string report = reporter.Report(result, new Mock<ISourceRootTranslator>().Object);

      Assert.NotEmpty(report);
      Assert.Equal("SF:doc.cs", report.Split(Environment.NewLine)[0]);
    }
  }
}
