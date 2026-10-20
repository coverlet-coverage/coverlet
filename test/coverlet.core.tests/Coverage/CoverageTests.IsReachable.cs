// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Instrumentation;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
  public partial class CoverageTests
  {
    [Fact]
    public void Coverage_BranchWithoutLines_IsReachableCopiedToBranchInfo()
    {
      // Branches with no line entries reach every BranchInfo construction site in Coverage:
      // new document, then existing method, new method in an existing class, and new class.
      var document = new Coverlet.Core.Instrumentation.Document { Index = 0, Path = "doc.cs" };
      document.Branches.Add(new BranchKey(1, 0), new Branch { Number = 1, Class = "ClassA", Method = "MethodA", Ordinal = 0, Path = 0, IsReachable = false });
      document.Branches.Add(new BranchKey(1, 1), new Branch { Number = 1, Class = "ClassA", Method = "MethodA", Ordinal = 1, Path = 1, IsReachable = false });
      document.Branches.Add(new BranchKey(2, 2), new Branch { Number = 2, Class = "ClassA", Method = "MethodB", Ordinal = 2, Path = 0, IsReachable = false });
      document.Branches.Add(new BranchKey(3, 3), new Branch { Number = 3, Class = "ClassB", Method = "MethodC", Ordinal = 3, Path = 0, IsReachable = false });

      var instrumenterResult = new InstrumenterResult { HitsFilePath = "HitsFilePath", ModulePath = "ModulePath" };
      instrumenterResult.Documents.Add("doc.cs", document);
      var prepareResult = new CoveragePrepareResult { Results = [instrumenterResult], Parameters = new CoverageParameters() };

      var coverage = new Coverage(prepareResult, new Mock<ILogger>().Object, new Mock<IInstrumentationHelper>().Object,
          new Mock<IFileSystem>().Object, new Mock<ISourceRootTranslator>().Object);
      CoverageResult result = coverage.GetCoverageResult();

      Classes classes = result.Modules["ModulePath"]["doc.cs"];
      Assert.Equal(new[] { false, false }, classes["ClassA"]["MethodA"].Branches.Select(b => b.IsReachable));
      Assert.False(Assert.Single(classes["ClassA"]["MethodB"].Branches).IsReachable);
      Assert.False(Assert.Single(classes["ClassB"]["MethodC"].Branches).IsReachable);
    }
  }
}
