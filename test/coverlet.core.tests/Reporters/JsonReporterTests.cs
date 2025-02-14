// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Reporters;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests.Reporters
{

  public class JsonReporterTests
  {
    private static readonly string s_resultModule = @"{
  ""module"": {
    ""doc.cs"": {
      ""Coverlet.Core.Tests.Reporters.JsonReporterTests"": {
        ""System.Void Coverlet.Core.Tests.Reporters.JsonReporterTests.TestReport()"": {
          ""Lines"": {
            ""1"": 1,
            ""2"": 0
          },
          ""Branches"": []
        }
      }
    }
  }
}";

    [Fact]
    public void TestReport()
    {
      var result = new CoverageResult
      {
        Identifier = Guid.NewGuid().ToString()
      };

      var lines = new Lines
      {
        { 1, 1 },
        { 2, 0 }
      };

      var methods = new Methods();
      string methodString = "System.Void Coverlet.Core.Tests.Reporters.JsonReporterTests.TestReport()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;

      var classes = new Classes
      {
        { "Coverlet.Core.Tests.Reporters.JsonReporterTests", methods }
      };

      var documents = new Documents
      {
        { "doc.cs", classes }
      };

      result.Modules = new Modules
      {
        { "module", documents }
      };

      var reporter = new JsonReporter();
      Assert.Equal(s_resultModule, reporter.Report(result, new Mock<ISourceRootTranslator>().Object), ignoreLineEndingDifferences: true);
    }
  }
}
