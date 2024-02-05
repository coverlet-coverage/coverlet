// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using coverlet.core.remote.samples.tests;
using Coverlet.Core;
using Coverlet.Tests.Utils;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace coverlet.core.remote.tests
{
  public partial class CoverageTests
  {
    [Fact]
    public void AsyncForeach()
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = [arg0];
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncForeach>(async instance =>
                  {
                    int res = await (ValueTask<int>)instance.SumWithATwist(AsyncEnumerable.Range(1, 5));
                    res += await (ValueTask<int>)instance.Sum(AsyncEnumerable.Range(1, 3));
                    res += await (ValueTask<int>)instance.SumEmpty();
                    await (ValueTask)instance.GenericAsyncForeach<object>(AsyncEnumerable.Range(1, 3));

                  }, persistPrepareResultToFile: pathSerialize[0]);
        }, path, new RemoteInvokeOptions { RollForward = "Major", StartInfo = _startInfo });
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AsyncForeach.cs")
        .AssertLinesCovered(BuildConfiguration.Debug,
                            // SumWithATwist(IAsyncEnumerable<int>)
                            // Apparently due to entering and exiting the async state machine, line 17
                            // (the top of an "await foreach" loop) is reached three times *plus* twice
                            // per loop iteration.  So, in this case, with five loop iterations, we end
                            // up with 3 + 5 * 2 = 13 hits.
                            (14, 1), (15, 1), (17, 13), (18, 5), (19, 5), (20, 5), (21, 5), (22, 5),
                            (24, 0), (25, 0), (26, 0), (27, 5), (29, 1), (30, 1),
                            // Sum(IAsyncEnumerable<int>)
                            (34, 1), (35, 1), (37, 9), (38, 3), (39, 3), (40, 3), (42, 1), (43, 1),
                            // SumEmpty()
                            (47, 1), (48, 1), (50, 3), (51, 0), (52, 0), (53, 0), (55, 1), (56, 1),
                            // GenericAsyncForeach
                            (59, 1), (60, 9), (61, 3), (62, 3), (63, 3), (64, 1)
                            )
        .AssertBranchesCovered(BuildConfiguration.Debug,
                               // SumWithATwist(IAsyncEnumerable<int>)
                               (17, 2, 1), (17, 3, 5), (19, 0, 5), (19, 1, 0),
                               // Sum(IAsyncEnumerable<int>)
                               (37, 0, 1), (37, 1, 3),
                               // SumEmpty()
                               // If we never entered the loop, that's a branch not taken, which is
                               // what we want to see.
                               (50, 0, 1), (50, 1, 0),
                               (60, 0, 1), (60, 1, 3)
                               )
        .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 5);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
