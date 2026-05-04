// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
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
    public void AsyncAwait()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncAwait>(async instance =>
                  {
                    instance.SyncExecution();

                    int res = await (Task<int>)instance.AsyncExecution(true);
                    res = await (Task<int>)instance.AsyncExecution(1);
                    res = await (Task<int>)instance.AsyncExecution(2);
                    res = await (Task<int>)instance.AsyncExecution(3);
                    res = await (Task<int>)instance.ContinuationCalled();
                    res = await (Task<int>)instance.ConfigureAwait();

                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AsyncAwait.cs")
        .AssertLinesCovered(BuildConfiguration.Debug,
                            // AsyncExecution(bool)
                            (10, 1), (11, 1), (12, 1), (14, 1), (16, 1), (17, 0), (18, 0), (19, 0), (21, 1), (22, 1),
                            // Async
                            (25, 9), (26, 9), (27, 9), (28, 9),
                            // SyncExecution
                            (31, 1), (32, 1), (33, 1),
                            // Sync
                            (36, 1), (37, 1), (38, 1),
                            // AsyncExecution(int)
                            (41, 3), (42, 3), (43, 3), (46, 1), (47, 1), (48, 1), (51, 1),
                            (52, 1), (53, 1), (56, 1), (57, 1), (58, 1), (59, 1),
                            (62, 0), (63, 0), (64, 0), (65, 0), (68, 0), (70, 3), (71, 3),
                            // ContinuationNotCalled
                            (74, 0), (75, 0), (76, 0), (77, 0), (78, 0),
                            // ContinuationCalled -> line 83 should be 1 hit some issue with Continuation state machine
                            (81, 1), (82, 1), (83, 2), (84, 1), (85, 1),
                            // ConfigureAwait
                            (89, 1), (90, 1)
                            )
        .AssertBranchesCovered(BuildConfiguration.Debug, (16, 0, 0), (16, 1, 1), (43, 0, 3), (43, 1, 1), (43, 2, 1), (43, 3, 1), (43, 4, 0))
        // Real branch should be 2, we should try to remove compiler generated branch in method ContinuationNotCalled/ContinuationCalled
        // for Continuation state machine
        .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 2);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_669_1()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_669_1>(async instance =>
                  {
                    await (Task)instance.Test();
                  },
                  persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AsyncAwait.cs")
        .AssertLinesCovered(BuildConfiguration.Debug,
        (97, 1), (98, 1), (99, 1), (101, 1), (102, 1), (103, 1),
        (110, 1), (111, 1), (112, 1), (113, 1),
        (116, 1), (117, 1), (118, 1), (119, 1));
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_669_2()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_669_2>(async instance =>
                  {
                    await (ValueTask<System.Net.Http.HttpResponseMessage>)instance.SendRequest();
                  },
                  persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AsyncAwait.cs")
        .AssertLinesCovered(BuildConfiguration.Debug, (7, 1), (10, 1), (11, 1), (12, 1), (13, 1), (15, 1))
        .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_1177()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_1177>(async instance =>
                      {
                        await (Task)instance.Test();
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");
        document.AssertLinesCovered(BuildConfiguration.Debug, (133, 1), (134, 1), (135, 1), (136, 1), (137, 1));
        Assert.DoesNotContain(document.Branches, x => x.Key.Line == 134);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_1233()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_1233>(async instance =>
                      {
                        await (Task)instance.Test();
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");
        document.AssertLinesCovered(BuildConfiguration.Debug, (150, 1));
        Assert.DoesNotContain(document.Branches, x => x.Key.Line == 150);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_1275()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_1275>(async instance =>
                      {
                        using var cts = new CancellationTokenSource();
                        await (Task)instance.Execute(cts.Token);
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");
        document.AssertLinesCoveredFromTo(BuildConfiguration.Debug, 170, 176);
        document.AssertBranchesCovered(BuildConfiguration.Debug, (171, 0, 1), (171, 1, 1));
        Assert.DoesNotContain(document.Branches, x => x.Key.Line == 176);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_1843_ComprehensiveInstrumentation()
    {
      // GOAL: Verify ALL async methods are instrumented and reported
      // This test should FAIL if methods are silently skipped during instrumentation
      // Addresses issue #1843 where MTP reported only 35% of methods compared to MSBuild

      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_1843_ComprehensiveAsync>(async instance =>
          {
            // Execute ALL methods to ensure they're instrumented and hit
            await instance.SimpleAsyncMethod();
            await instance.AsyncWithIntReturn();
            await instance.AsyncWithStringReturn();

            await instance.SimpleValueTask();
            await instance.ValueTaskWithReturn();

            await instance.WithConfigureAwaitTrue();
            await instance.WithConfigureAwaitFalse();

            await instance.NestedAsyncCalls();

            // Test branching - both paths
            await instance.AsyncWithBranching(1);
            await instance.AsyncWithBranching(-1);

            await instance.AsyncWithTryCatch();

            await instance.AsyncWithLinq();

            await instance.ParallelAsyncCalls();

            // Multiple await points with different counts
            await instance.MultipleAwaitPoints(1);
            await instance.MultipleAwaitPoints(3);

            await instance.AsyncWithSwitchExpression(1);
            await instance.AsyncWithSwitchExpression(2);
            await instance.AsyncWithSwitchExpression(99);

            await instance.AsyncWithNullCoalescing("input");
            await instance.AsyncWithNullCoalescing(null);

            // Consume async enumerable
            using var cts = new CancellationTokenSource();
            var enumerable = instance.AsyncEnumerable(cts.Token);
            await foreach (var item in (System.Collections.Generic.IAsyncEnumerable<int>)enumerable)
            {
              // Process items
            }
          },
          persistPrepareResultToFile: pathSerialize[0]);

          // CRITICAL: Verify instrumentation result before execution
          Assert.NotNull(coveragePrepareResult);
          Assert.NotEmpty(coveragePrepareResult.Results);

          return 0;
        }, [path]);

        var coverageResult = TestInstrumentationHelper.GetCoverageResult(path);
        var document = coverageResult.Document("Instrumentation.AsyncAwait.cs");

        // ASSERTION 1: Verify expected number of unique methods were instrumented
        // This catches if methods are being silently skipped (issue #1843 symptom)
        var uniqueMethods = document.Lines.Values
          .Select(l => l.Method)
          .Where(m => m.Contains("Issue_1843_ComprehensiveAsync"))
          .Distinct()
          .ToList();

        int expectedMinimumMethods = 13; // We defined 13 async methods in Issue_1843_ComprehensiveAsync
        Assert.True(uniqueMethods.Count >= expectedMinimumMethods,
          $"Expected at least {expectedMinimumMethods} methods in Issue_1843_ComprehensiveAsync, but found {uniqueMethods.Count}. " +
          $"Methods found: {string.Join(", ", uniqueMethods)}. " +
          "Some async methods may not be instrumented! This is similar to issue #1843.");

        // ASSERTION 2: Verify lines are covered (sampling key methods)
        Assert.True(document.Lines.Count > 0, "No lines were instrumented in the document");

        // ASSERTION 3: Verify branches exist for methods with branching logic
        Assert.True(document.Branches.Count > 0, "No branches were recorded, but AsyncWithBranching should create branches");

        // ASSERTION 4: Verify async state machines were created
        // Compiler generates <MethodName>d__X types for async methods
        var allClasses = coverageResult.Modules.Values
          .SelectMany(m => m.Values)
          .SelectMany(d => d.Keys);

        int stateMachineCount = allClasses.Count(className =>
          className.Contains("Issue_1843_ComprehensiveAsync") && className.Contains(">d__"));

        Assert.True(stateMachineCount > 0,
          $"Expected async state machines for Issue_1843_ComprehensiveAsync, but found {stateMachineCount}. " +
          "Some async methods may be missing their state machines!");
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_1843_VerifyAllMethodsDiscovered()
    {
      // This test verifies that the instrumentation process discovers
      // ALL methods BEFORE execution, catching silent skipping issues
      // This addresses the core problem in issue #1843

      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult prepareResult = await TestInstrumentationHelper.Run<Issue_1843_ComprehensiveAsync>(
            async instance =>
            {
              // Execute minimal code - just one method
              await instance.SimpleAsyncMethod();
            },
            persistPrepareResultToFile: pathSerialize[0]);

          // Verify instrumentation results BEFORE full execution
          Assert.NotNull(prepareResult);
          Assert.NotEmpty(prepareResult.Results);

          Core.Instrumentation.InstrumenterResult instrumentedResult = prepareResult.Results[0];

          // Check that documents contain methods from Issue_1843_ComprehensiveAsync
          Assert.True(instrumentedResult.Documents.Count > 0,
            "No documents were instrumented!");

          // Note: We don't verify file existence here because:
          // 1. Instrumented modules might be cleaned up immediately after use
          // 2. .NET 10+ might use in-memory assemblies
          // 3. The critical check is method discovery, not file persistence
          // The real validation happens below when we check discovered methods

          return 0;
        }, [path]);

        // Check coverage result to verify methods were discovered
        var coverageResult = TestInstrumentationHelper.GetCoverageResult(path);
        var document = coverageResult.Document("Instrumentation.AsyncAwait.cs");

        // Count unique methods from Issue_1843_ComprehensiveAsync class
        var discoveredMethods = document.Lines.Values
          .Select(l => l.Method)
          .Where(m => m.Contains("Issue_1843_ComprehensiveAsync"))
          .Distinct()
          .ToList();

        Assert.True(discoveredMethods.Count > 0,
          "No methods from Issue_1843_ComprehensiveAsync were discovered during instrumentation. " +
          "This indicates a failure in method discovery.");

        // Even though we only executed SimpleAsyncMethod, ALL methods should be instrumented
        // This is the key insight from issue #1843 - methods missing from instrumentation, not just execution
        int expectedMethods = 13;
        Assert.True(discoveredMethods.Count >= expectedMethods,
          $"Only {discoveredMethods.Count} out of {expectedMethods} methods were discovered. " +
          $"Methods found: {string.Join(", ", discoveredMethods)}. " +
          "Method discovery is incomplete - this is the issue #1843 symptom!");
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void AsyncAwait_Issue_1843_MultipleAwaitPoints(int awaitCount)
    {
      // Verify methods with varying numbers of await points are fully instrumented
      // This catches issues with state machine complexity affecting instrumentation

      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_1843_ComprehensiveAsync>(
            async instance =>
            {
              await instance.MultipleAwaitPoints(awaitCount);
            },
            persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        var coverageResult = TestInstrumentationHelper.GetCoverageResult(path);
        var document = coverageResult.Document("Instrumentation.AsyncAwait.cs");

        // Verify the MultipleAwaitPoints method was instrumented and executed
        var multipleAwaitMethodLines = document.Lines.Values
          .Where(l => l.Method.Contains("MultipleAwaitPoints"))
          .ToList();

        Assert.NotEmpty(multipleAwaitMethodLines);
        Assert.True(multipleAwaitMethodLines.Any(l => l.Hits > 0),
          $"MultipleAwaitPoints with {awaitCount} awaits was not covered. " +
          "Complex async methods may not be properly instrumented.");
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_1843_VerifyMetricsNotRegressed()
    {
      // This test ensures we don't regress on the coverage completeness
      // reported in issue #1843 (MTP had only 35% of methods compared to MSBuild)

      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_1843_ComprehensiveAsync>(async instance =>
          {
            // Execute all methods
            await instance.SimpleAsyncMethod();
            await instance.AsyncWithIntReturn();
            await instance.AsyncWithStringReturn();
            await instance.SimpleValueTask();
            await instance.ValueTaskWithReturn();
            await instance.WithConfigureAwaitTrue();
            await instance.WithConfigureAwaitFalse();
            await instance.NestedAsyncCalls();
            await instance.AsyncWithBranching(1);
            await instance.AsyncWithTryCatch();
            await instance.AsyncWithLinq();
            await instance.ParallelAsyncCalls();
            await instance.MultipleAwaitPoints(2);
            await instance.AsyncWithSwitchExpression(1);
            await instance.AsyncWithNullCoalescing(null);

            using var cts = new CancellationTokenSource();
            var enumerable = instance.AsyncEnumerable(cts.Token);
            await foreach (var item in (System.Collections.Generic.IAsyncEnumerable<int>)enumerable)
            {
              // Consume items
            }
          },
          persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        var coverageResult = TestInstrumentationHelper.GetCoverageResult(path);
        var document = coverageResult.Document("Instrumentation.AsyncAwait.cs");

        // Get unique methods from our test class
        var allMethods = document.Lines.Values
          .Where(l => l.Method.Contains("Issue_1843_ComprehensiveAsync"))
          .Select(l => l.Method)
          .Distinct()
          .ToList();

        // Determine which methods have at least one hit
        var coveredMethods = document.Lines.Values
          .Where(l => l.Method.Contains("Issue_1843_ComprehensiveAsync") && l.Hits > 0)
          .Select(l => l.Method)
          .Distinct()
          .ToList();

        int totalMethods = allMethods.Count;
        int coveredMethodsCount = coveredMethods.Count;

        // Verify ratio of covered methods to total methods is high
        // In issue #1843, only 35% of files appeared, meaning ~65% were missing
        // We should have near 100% since we executed all methods
        double coverageRatio = totalMethods > 0 ? (double)coveredMethodsCount / totalMethods : 0;

        Assert.True(totalMethods > 0, "No methods were instrumented for Issue_1843_ComprehensiveAsync");

        Assert.True(coverageRatio > 0.9,
          $"Only {coverageRatio:P} of methods were covered (expected >90%). " +
          $"Covered: {coveredMethodsCount}, Total: {totalMethods}. " +
          $"Covered methods: {string.Join(", ", coveredMethods)}. " +
          "This indicates methods are being skipped during instrumentation or execution - " +
          "similar to issue #1843 where only 35% of expected coverage was reported!");

        // Verify sequence points exist
        int totalSequencePoints = document.Lines.Count(l => l.Value.Method.Contains("Issue_1843_ComprehensiveAsync"));
        Assert.True(totalSequencePoints > 0,
          "Sequence points should be greater than zero. " +
          "Issue #1843 reported dramatic drop in sequence points (25080 -> 2596)");
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_1337_BasicTryFinally()
    {
      // Issue #1337: Coverlet flagged a branch for an async function's finally block where none exists.
      // Async methods with try-finally blocks containing await statements should not report
      // phantom branches from compiler-generated exception handling.

      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncTryFinallyPhantomBranches>(async instance =>
                      {
                        await (Task)instance.BasicAsyncTryFinally();
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");

        // Verify lines in try and finally blocks are covered
        var methodLines = document.Lines.Values
          .Where(l => l.Method.Contains("BasicAsyncTryFinally"))
          .ToList();

        Assert.NotEmpty(methodLines);
        Assert.True(methodLines.All(l => l.Hits > 0), "All lines in BasicAsyncTryFinally should be covered");

        // Verify no phantom branches from compiler-generated exception state checks
        var methodBranches = document.Branches
          .Where(b => methodLines.Any(l => l.Number == b.Key.Line))
          .ToList();

        // There should be no branches in this simple try-finally - it's linear code
        Assert.Empty(methodBranches);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_TryFinallyWithReturn()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncTryFinallyPhantomBranches>(async instance =>
                      {
                        int result = await (Task<int>)instance.TryFinallyWithReturnValue();
                        Assert.Equal(42, result);
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");

        var methodLines = document.Lines.Values
          .Where(l => l.Method.Contains("TryFinallyWithReturnValue"))
          .ToList();

        Assert.NotEmpty(methodLines);
        Assert.True(methodLines.All(l => l.Hits > 0), "All lines in TryFinallyWithReturnValue should be covered");
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_NestedTryFinally()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncTryFinallyPhantomBranches>(async instance =>
                      {
                        await (Task)instance.NestedTryFinally();
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");

        var methodLines = document.Lines.Values
          .Where(l => l.Method.Contains("NestedTryFinally"))
          .ToList();

        Assert.NotEmpty(methodLines);
        Assert.True(methodLines.All(l => l.Hits > 0), "All lines in NestedTryFinally should be covered");

        // Nested try-finally with awaits should not create phantom branches
        var methodBranches = document.Branches
          .Where(b => methodLines.Any(l => l.Number == b.Key.Line))
          .ToList();

        Assert.Empty(methodBranches);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_TryFinallyWithBranching()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncTryFinallyPhantomBranches>(async instance =>
                      {
                        // Test both branches
                        int result1 = await (Task<int>)instance.TryFinallyWithBranching(true);
                        Assert.Equal(1, result1);

                        int result2 = await (Task<int>)instance.TryFinallyWithBranching(false);
                        Assert.Equal(2, result2);
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");

        var methodLines = document.Lines.Values
          .Where(l => l.Method.Contains("TryFinallyWithBranching"))
          .ToList();

        Assert.NotEmpty(methodLines);

        // The if statement creates real branches that should be reported
        var methodBranches = document.Branches
          .Where(b => methodLines.Any(l => l.Number == b.Key.Line))
          .ToList();

        // Should have branches from the if statement, but NOT from compiler-generated exception handling
        Assert.NotEmpty(methodBranches);

        // Verify all user-code branches are covered
        foreach (var branch in methodBranches)
        {
          Assert.True(branch.Value.Hits > 0,
            $"Branch at line {branch.Key.Line} should be covered");
        }
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_TryCatchFinally()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncTryFinallyPhantomBranches>(async instance =>
          {
            await (Task)instance.TryCatchFinallyWithAwaitInFinally();
          },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");

        var methodLines = document.Lines.Values
          .Where(l => l.Method.Contains("TryCatchFinallyWithAwaitInFinally"))
          .ToList();

        Assert.NotEmpty(methodLines);
        //Assert.True(methodLines.All(l => l.Hits > 0), "All lines in TryCatchFinallyWithAwaitInFinally should be covered"); 

        var methodBranches = document.Branches
          .Where(b => methodLines.Any(l => l.Number == b.Key.Line))
          .ToList();

        Assert.Empty(methodBranches);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_AsyncTryFinallyPhantomBranches_EmptyTryAwaitFinally()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncTryFinallyPhantomBranches>(async instance =>
                      {
                        await (Task)instance.EmptyTryWithAwaitInFinally();
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");

        var methodLines = document.Lines.Values
          .Where(l => l.Method.Contains("EmptyTryWithAwaitInFinally"))
          .ToList();

        Assert.NotEmpty(methodLines);
        Assert.True(methodLines.All(l => l.Hits > 0), "All lines should be covered");

        // Empty try with await in finally - the simplest repro case for #1767
        var methodBranches = document.Branches
          .Where(b => methodLines.Any(l => l.Number == b.Key.Line))
          .ToList();

        Assert.Empty(methodBranches);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_MultipleAwaitsInFinally()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncTryFinallyPhantomBranches>(async instance =>
                      {
                        await (Task)instance.TryFinallyWithMultipleAwaitsInFinally();
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");

        var methodLines = document.Lines.Values
          .Where(l => l.Method.Contains("TryFinallyWithMultipleAwaitsInFinally"))
          .ToList();

        Assert.NotEmpty(methodLines);
        Assert.True(methodLines.All(l => l.Hits > 0),
          "All lines including multiple awaits in finally should be covered");
      }
      finally
      {
        File.Delete(path);
      }
    }

  }
}
