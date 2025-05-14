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
  public partial class CoverageTests
  {
    [Fact]
    public async Task AsyncForeachAsync()
    {
      string path = Path.GetTempFileName();
      try
      {
        CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncForeach>(async instance =>
                {
                  int res = await (ValueTask<int>)instance.SumWithATwist(AsyncEnumerable.Range(1, 5));
                  res += await (ValueTask<int>)instance.Sum(AsyncEnumerable.Range(1, 3));
                  res += await (ValueTask<int>)instance.SumEmpty();
                  await (ValueTask)instance.GenericAsyncForeach<object>(AsyncEnumerable.Range(1, 3));

                }, persistPrepareResultToFile: path);

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AsyncForeach.cs")
        .AssertLinesCovered(BuildConfiguration.Debug,
                            // SumWithATwist(IAsyncEnumerable<int>)
                            // Apparently due to entering and exiting the async state machine, line 17
                            // (the top of an "await foreach" loop) is reached three times *plus* twice
                            // per loop iteration.  So, in this case, with five loop iterations, we end
                            // up with 3 + 5 * 2 = 13 hits.
                            (13, 1), (15, 13), (16, 5), (17, 5), (18, 5), (19, 5), (20, 5), (22, 0),
                            (23, 0), (24, 0), (25, 5), (27, 1), (28, 1),
                            // Sum(IAsyncEnumerable<int>)
                            (32, 1), (34, 9), (35, 3), (36, 3), (37, 3), (39, 1), (40, 1), (43, 1),
                            // SumEmpty()
                            (44, 1), (48, 0), (51, 1), (52, 1),
                            // GenericAsyncForeach
                            (56, 9), (57, 3), (58, 3), (59, 3), (60, 1)
                            )
        .AssertBranchesCovered(BuildConfiguration.Debug,
                               // SumWithATwist(IAsyncEnumerable<int>)
                               (15, 2, 1), (15, 3, 5), (17, 0, 5), (17, 1, 0),
                               // Sum(IAsyncEnumerable<int>)
                               (34, 0, 1), (34, 1, 3),
                               // SumEmpty()
                               // If we never entered the loop, that's a branch not taken, which is
                               // what we want to see.
                               (46, 0, 1), (46, 1, 0),
                               (56, 0, 1), (56, 1, 3)
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
