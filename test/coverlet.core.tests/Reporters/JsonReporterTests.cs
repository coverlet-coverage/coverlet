// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Abstractions;
using Moq;
using System;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{

  public class JsonReporterTests
  {
    private static readonly string s_resultModule = @"{
  ""module"": {
    ""doc.cs"": {
      ""Coverlet.Core.Reporters.Tests.JsonReporterTests"": {
        ""System.Void Coverlet.Core.Reporters.Tests.JsonReporterTests.TestReport()"": {
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

      var lines = new Lines();
      lines.Add(1, 1);
      lines.Add(2, 0);

      var methods = new Methods();
      string methodString = "System.Void Coverlet.Core.Reporters.Tests.JsonReporterTests.TestReport()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;

      var classes = new Classes();
      classes.Add("Coverlet.Core.Reporters.Tests.JsonReporterTests", methods);

      var documents = new Documents();
      documents.Add("doc.cs", classes);

      result.Modules = new Modules();
      result.Modules.Add("module", documents);

      var reporter = new JsonReporter();
      Assert.Equal(s_resultModule, reporter.Report(result, new Mock<ISourceRootTranslator>().Object), ignoreLineEndingDifferences: true);
    }
  }
}
