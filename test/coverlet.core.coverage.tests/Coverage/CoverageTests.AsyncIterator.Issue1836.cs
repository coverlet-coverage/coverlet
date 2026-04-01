// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    /// <summary>
    /// Test for Issue #1836: Wrong branch rate on IAsyncEnumerable
    /// https://github.com/coverlet-coverage/coverlet/issues/1836
    ///
    /// This test verifies that branch coverage correctly handles async iterators
    /// with the [EnumeratorCancellation] attribute.
    /// </summary>
    [Fact]
    public void AsyncIterator_Issue1836_WithoutCancellation()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncIteratorIssue1836>(async instance =>
                  {
                    // Consume the async iterator without cancellation
                    int result = await (Task<int>)instance.ConsumeWithoutCancellation();
                    Assert.Equal(3, result); // 1 + 2 = 3

                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult coverageResult = TestInstrumentationHelper.GetCoverageResult(path);

        // Verify basic line coverage for the sample file
        // Line 20: int[] items = [1, 2]
        // Line 30: int sum = 0
        coverageResult
            .Document("Instrumentation.AsyncIterator.Issue1836.cs")
            .AssertLinesCovered(BuildConfiguration.Debug,
                (20, 1),  // int[] items = [1, 2]
                (30, 1)   // int sum = 0
            );

        // Expected user branches:
        // - Line 21: foreach loop (continue/exit) = 2 branches
        // - Line 24: ternary operator (true/false) = 2 branches
        // Total expected: 4 user branches
        //
        // Current behavior: 4 branches reported which is correct
        // for the user-written code in this sample.
        coverageResult
            .Document("Instrumentation.AsyncIterator.Issue1836.cs")
            .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 4);
      }
      finally
      {
        File.Delete(path);
      }
    }

    /// <summary>
    /// Test for Issue #1836 with cancellation token exercised.
    /// This tests the ternary operator's throw branch.
    /// </summary>
    [Fact]
    public void AsyncIterator_Issue1836_WithCancellation()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncIteratorIssue1836>(async instance =>
                  {
                    // First, consume without cancellation to cover true branch
                    int result = await (Task<int>)instance.ConsumeWithoutCancellation();
                    Assert.Equal(3, result);

                    // Then, try with a pre-cancelled token to cover the throw branch
                    using var cts = new CancellationTokenSource();
                    cts.Cancel();
                    try
                    {
                      await (Task<int>)instance.ConsumeWithCancellation(cts.Token);
                      Assert.Fail("Expected OperationCanceledException");
                    }
                    catch (OperationCanceledException)
                    {
                      // Expected - this exercises the false branch of the ternary
                    }

                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult coverageResult = TestInstrumentationHelper.GetCoverageResult(path);

        // Verify both methods were called
        coverageResult
            .Document("Instrumentation.AsyncIterator.Issue1836.cs")
            .AssertLinesCovered(BuildConfiguration.Debug,
                (30, 1), // ConsumeWithoutCancellation: int sum = 0
                (41, 1)  // ConsumeWithCancellation: int sum = 0
            );

        // The number of branches should be consistent
        coverageResult
            .Document("Instrumentation.AsyncIterator.Issue1836.cs")
            .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 4);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
