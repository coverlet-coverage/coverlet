// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Coverlet.Core.Abstractions;
using Moq;
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
      CoverageResult result = new()
      {
        Identifier = Guid.NewGuid().ToString()
      };

      Lines lines = new();
      lines.Add(1, 1);
      lines.Add(2, 0);

      Methods methods = new();
      string methodString = "System.Void Coverlet.Core.Reporters.Tests.JsonReporterTests.TestReport()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;

      Classes classes = new();
      classes.Add("Coverlet.Core.Reporters.Tests.JsonReporterTests", methods);

      Documents documents = new();
      documents.Add("doc.cs", classes);

      result.Modules = new Modules();
      result.Modules.Add("module", documents);

      JsonReporter reporter = new();
      Assert.Equal(s_resultModule, reporter.Report(result, new Mock<ISourceRootTranslator>().Object), ignoreLineEndingDifferences: true);
    }
  }
}
