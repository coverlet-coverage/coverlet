// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Coverlet.Core.Samples.Tests;
using Coverlet.Tests.Utils;
using Xunit;

namespace Coverlet.Core.Tests
{
  public partial class CoverageTests
  {
    [Fact]
    public void AsyncIterator()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncIterator>(async instance =>
                  {
                    int res = await (Task<int>)instance.Issue1104_Repro();

                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, new string[] { path });

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AsyncIterator.cs")
        .AssertLinesCovered(BuildConfiguration.Debug,
                            // Issue1104_Repro()
                            (14, 1), (15, 1), (17, 203), (18, 100), (19, 100), (20, 100), (22, 1), (23, 1),
                            // CreateSequenceAsync()
                            (26, 1), (27, 202), (28, 100), (29, 100), (30, 100), (31, 100), (32, 1)
                            )
        .AssertBranchesCovered(BuildConfiguration.Debug,
                               // Issue1104_Repro(),
                               (17, 0, 1), (17, 1, 100),
                               // CreateSequenceAsync()
                               (27, 0, 1), (27, 1, 100)
                               )
        .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 2);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
