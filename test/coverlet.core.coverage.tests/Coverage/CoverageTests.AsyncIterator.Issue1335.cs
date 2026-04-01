// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
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
    /// Test for Issue #1335: Branch coverage issue with AsyncEnumerable Extension (yield return)
    /// https://github.com/coverlet-coverage/coverlet/issues/1335
    ///
    /// This test verifies that branch coverage correctly handles complex async iterator
    /// patterns like batching, where nested await foreach + yield return generates
    /// many compiler-generated branches that should be filtered out.
    /// </summary>
    [Fact]
    public void AsyncIterator_Issue1335_Batching_FullBatches()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncIteratorIssue1335>(async instance =>
                  {
                    // Test with items that divide evenly into batches
                    // 6 items with batch size 3 = 2 full batches, no partial
                    int result = await (Task<int>)instance.ConsumeBatchedSequence(6, 3);
                    Assert.Equal(6, result);

                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult coverageResult = TestInstrumentationHelper.GetCoverageResult(path);

        // Verify key lines are covered - basic functionality works
        // Line 16: for loop start (hit count includes state machine iterations)
        // Line 25: List<int> batch = new(batchSize)
        // Line 43: int totalItems = 0
        coverageResult
            .Document("Instrumentation.AsyncIterator.Issue1335.cs")
            .AssertLinesCovered(BuildConfiguration.Debug,
                (25, 1), // List<int> batch = new(batchSize)
                (43, 1)  // int totalItems = 0
            );

        // Document current vs expected branch behavior
        // Expected user branches (5 conditions * 2 paths = 10 branches):
        // - Line 16: for loop (continue/exit) = 2 branches
        // - Line 26: await foreach (continue/exit) = 2 branches
        // - Line 29: if (batch.Count >= batchSize) = 2 branches
        // - Line 35: if (batch.Count > 0) = 2 branches
        // - Line 44: await foreach (continue/exit) = 2 branches
        //
        // CURRENT BEHAVIOR (Issue #1335):
        // The test shows 7 branches are reported, which includes phantom branches
        // from compiler-generated state machine code.
        //
        // When this issue is fixed, this value should be reduced to 4-5
        // (only user branches should be counted).
        coverageResult
            .Document("Instrumentation.AsyncIterator.Issue1335.cs")
            .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 7);
      }
      finally
      {
        File.Delete(path);
      }
    }

    /// <summary>
    /// Test for Issue #1335 with partial final batch.
    /// This exercises the if (batch.Count > 0) branch.
    /// </summary>
    [Fact]
    public void AsyncIterator_Issue1335_Batching_PartialFinalBatch()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncIteratorIssue1335>(async instance =>
                  {
                    // Test with items that don't divide evenly
                    // 5 items with batch size 3 = 1 full batch + 1 partial batch
                    int result = await (Task<int>)instance.ConsumeBatchedSequence(5, 3);
                    Assert.Equal(5, result);

                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult coverageResult = TestInstrumentationHelper.GetCoverageResult(path);

        // Verify key lines are covered
        // Line 37: yield return batch (partial batch case)
        coverageResult
            .Document("Instrumentation.AsyncIterator.Issue1335.cs")
            .AssertLinesCovered(BuildConfiguration.Debug,
                (37, 1) // yield return batch (partial batch)
            );

        // Same as full batches - 7 branches currently reported (includes phantom branches)
        coverageResult
            .Document("Instrumentation.AsyncIterator.Issue1335.cs")
            .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 7);
      }
      finally
      {
        File.Delete(path);
      }
    }

    /// <summary>
    /// Test for Issue #1335 with simple transformation pattern.
    /// </summary>
    [Fact]
    public void AsyncIterator_Issue1335_Transform()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncIteratorIssue1335>(async instance =>
                  {
                    // Transform 3 items (1, 2, 3) -> (2, 4, 6) = sum 12
                    int result = await (Task<int>)instance.ConsumeTransformedSequence(3);
                    Assert.Equal(12, result);

                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult coverageResult = TestInstrumentationHelper.GetCoverageResult(path);

        // Verify transformation coverage - key lines
        // Line 62: int sum = 0
        coverageResult
            .Document("Instrumentation.AsyncIterator.Issue1335.cs")
            .AssertLinesCovered(BuildConfiguration.Debug,
                (62, 1) // int sum = 0
            );

        // Expected user branches for transformation:
        // - Line 16: for loop = 2 branches
        // - Line 54: await foreach (TransformAsync) = 2 branches
        // - Line 63: await foreach (ConsumeTransformedSequence) = 2 branches
        // Total expected: 6 user branches
        //
        // CURRENT BEHAVIOR (Issue #1335):
        // 7 branches are reported, which includes phantom branches from
        // compiler-generated state machine code. When this issue is fixed,
        // this value should be reduced to only count user branches.
        coverageResult
            .Document("Instrumentation.AsyncIterator.Issue1335.cs")
            .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 7);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
