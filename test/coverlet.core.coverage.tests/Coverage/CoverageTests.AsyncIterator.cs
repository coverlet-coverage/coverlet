// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        }, [path]);

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

    /// <summary>
    /// Test for Issue #1836: Wrong branch rate on IAsyncEnumerable with [EnumeratorCancellation]
    /// From PR https://github.com/daveMueller/coverlet/pull/31
    ///
    /// Key finding: The foreach branches on line 40 are reported at ordinals 2 and 3 (not 0 and 1),
    /// because the compiler-generated state machine for [EnumeratorCancellation] token-combining
    /// logic consumes ordinals 0 and 1.
    /// </summary>
    [Fact]
    public void AsyncIterator_Issue1836()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue1836>(async instance =>
                  {
                    // Normal iteration covering all items
                    await foreach (int item in (IAsyncEnumerable<int>)instance.GetNumbersAsync()) { }

                    // Cancelled iteration covering the throw branch of the ternary
                    using var cts = new CancellationTokenSource();
                    cts.Cancel();
                    try
                    {
                      await foreach (int item in (IAsyncEnumerable<int>)instance.GetNumbersAsync(cts.Token)) { }
                    }
                    catch (OperationCanceledException) { }
                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncIterator.cs");
        // Lines adjusted for the Issue1836 class location:
        // Line 43: int[] items = [1, 2]
        // Line 44: foreach (var item in items)
        // Line 46: await Task.CompletedTask
        // Line 47: yield return ternary expression
        document.AssertLinesCoveredFromTo(BuildConfiguration.Debug, 43, 47);
        document.AssertBranchesCovered(BuildConfiguration.Debug,
                                       // foreach loop branches (ordinals start at 2 due to [EnumeratorCancellation] state machine code)
                                       (44, 2, 1), (44, 3, 3),
                                       // ternary conditional branches: false=throw (cancelled), true=return item (normal)
                                       (47, 0, 1), (47, 1, 2));
        document.ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 2);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
