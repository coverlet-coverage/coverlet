// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coverlet.Core;
using Coverlet.Core.CoverageSamples.Tests;
using Coverlet.Core.Tests;
using Coverlet.Tests.Utils;
using Xunit;

namespace Coverlet.CoreCoverage.Tests
{
  public partial class CoverageTests : ExternalProcessExecutionTest
  {
    [Fact]
    public void PatternMatchingOr_Should_Report_100_Percent_Branch_Coverage()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<PatternMatchingOr>(instance =>
                  {
                    instance.PatternMatching("hello");
                    instance.PatternMatching("world");
                    instance.PatternMatching("other");
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        var methodDocument = result.Document("Instrumentation.PatternMatchingOr.cs")
          .Method("System.Boolean Coverlet.Core.CoverageSamples.Tests.PatternMatchingOr::PatternMatching(System.String)");

        var branches = methodDocument.Branches.Values.OrderBy(x => x.Ordinal).ToArray();

        if (TestUtils.GetAssemblyBuildConfiguration() == BuildConfiguration.Debug)
        {
          Assert.Equal(2, branches.Length);
          Assert.Equal(new uint[] { 0, 1 }, branches.Select(x => x.Ordinal).ToArray());
          Assert.Equal(new[] { 0, 1 }, branches.Select(x => x.Path).ToArray());
        } else {
          Assert.Equal(4, branches.Length);
          Assert.Equal(new uint[] { 0, 1, 2, 3 }, branches.Select(x => x.Ordinal).ToArray());
          Assert.Equal(new[] { 0, 1, 0,1 }, branches.Select(x => x.Path).ToArray());
        }
        Assert.All(branches, branch => Assert.Equal(9, branch.Number));
        Assert.Equal(branches[0].Offset, branches[1].Offset);
        Assert.All(branches, branch => Assert.True(branch.Hits > 0));
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
