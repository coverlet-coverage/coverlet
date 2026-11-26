// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Coverlet.Core.CoverageSamples.Tests;
using Coverlet.Core.Tests;
using Coverlet.Tests.Utils;
using Xunit;

namespace Coverlet.CoreCoverage.Tests
{
  public partial class CoverageTests
  {
    [Fact]
    public void AwaitedCatchDispatch_Issue2085()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          await TestInstrumentationHelper.Run<AwaitedCatchDispatch>(async instance =>
                  {
                    Assert.Equal(1, await (Task<int>)instance.ThreeHandlers(new ArgumentNullException()));
                    Assert.Equal(2, await (Task<int>)instance.ThreeHandlers(new ArgumentOutOfRangeException()));
                    Assert.Equal(3, await (Task<int>)instance.ThreeHandlers(new InvalidOperationException()));

                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AwaitedCatchDispatch.cs")
        .AssertLinesCoveredAllBut(BuildConfiguration.Debug | BuildConfiguration.Release)
        .ExpectedTotalNumberOfBranches(0);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
