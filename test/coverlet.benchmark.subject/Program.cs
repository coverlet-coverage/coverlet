// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace coverlet.benchmark.subject
{
  /// <summary>
  /// Benchmark subject entry point used by <c>coverlet.core.benchmark.tests</c>.
  /// </summary>
  /// <remarks>
  /// This program intentionally executes representative paths across all workload types so
  /// <c>CoverageWorkflowBenchmark</c> can run the instrumented assembly and produce tracker hit files.
  /// It is benchmark harness code, not shipping product behavior.
  /// </remarks>
  internal static class Program
  {
    private static int Main(string[] args)
    {
      Run();
      return 0;
    }

    private static void Run()
    {
      RunSafely(nameof(RunAsyncWorkload), RunAsyncWorkload);
      RunSafely(nameof(RunAutoPropsWorkload), RunAutoPropsWorkload);
      RunSafely(nameof(RunDeepNestingWorkload), RunDeepNestingWorkload);
      RunSafely(nameof(RunExcludedWorkload), RunExcludedWorkload);
      RunSafely(nameof(RunGenericWorkload), RunGenericWorkload);
      RunSafely(nameof(RunIteratorWorkload), RunIteratorWorkload);
      RunSafely(nameof(RunLambdaWorkload), RunLambdaWorkload);
      RunSafely(nameof(RunSwitchWorkload), RunSwitchWorkload);
    }

    private static void RunSafely(string workloadName, Action action)
    {
      ArgumentNullException.ThrowIfNull(workloadName);
      ArgumentNullException.ThrowIfNull(action);

      try
      {
        action();
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"[subject] {workloadName} failed: {ex.GetType().FullName}: {ex.Message}");
      }
    }

    private static void RunAsyncWorkload()
    {
      var workload = new AsyncWorkload();
      _ = workload.ComputeAsync(1, 2).GetAwaiter().GetResult();
      _ = workload.FormatAsync(42).GetAwaiter().GetResult();
      _ = workload.NestedComputeAsync(1, 2, 3).GetAwaiter().GetResult();
      _ = workload.NestedFormatAsync(1, 2).GetAwaiter().GetResult();
      _ = workload.SumSequenceAsync([1, 2, 3]).GetAwaiter().GetResult();
      _ = workload.FormatManyAsync([1, 2, 3]).GetAwaiter().GetResult();
      _ = workload.SafeComputeAsync(1, 2).GetAwaiter().GetResult();
      _ = workload.SafeFormatAsync(3).GetAwaiter().GetResult();
      _ = workload.ComputeAndFormatAsync(2, 3).GetAwaiter().GetResult();
      _ = workload.IsPositiveSumAsync(1, 2).GetAwaiter().GetResult();
      _ = workload.ParallelComputeAsync([1, 2, 3]).GetAwaiter().GetResult();
      _ = workload.FirstResultAsync(1, 2).GetAwaiter().GetResult();
      _ = workload.ComputeWithTimeoutAsync(1, 2, 1000).GetAwaiter().GetResult();
      _ = workload.ComputeValueTaskAsync(1, 2).AsTask().GetAwaiter().GetResult();
      _ = workload.IsZeroAsync(0).AsTask().GetAwaiter().GetResult();
      _ = workload.DeeplyNestedAsync(4).GetAwaiter().GetResult();
    }

    private static void RunAutoPropsWorkload()
    {
      _ = new AutoProps10();
      _ = new AutoProps25();
      var noCtor = new RecordNoCtor { Name = "A", Age = 1 };
      var emptyCtor = new RecordEmptyCtor { Name = "B", Age = 2 };
      var withCtor = new RecordWithPrimaryCtor("C", 3);
      _ = noCtor.Display();
      _ = emptyCtor.Display();
      _ = withCtor.Display();
      _ = new CircleRecord { Radius = 2 }.Area();
      _ = new RectangleRecord { Width = 2, Height = 3 }.Area();
      _ = new OrderRecord("id", 10, "pending").Describe();
      _ = new ProductRecord("sku", "name", 5, 1).StockStatus();
      var aggregator = new RecordAggregator
      {
        NoCtor = noCtor,
        EmptyCtor = emptyCtor,
        WithCtor = withCtor
      };

      _ = aggregator.Summary();
    }

    private static void RunDeepNestingWorkload()
    {
      var level1 = new Level1();
      _ = level1.Compute(1);
      _ = level1.Inner.Compute(1);
      _ = level1.Inner.Inner.Compute(1);
      _ = level1.Inner.Inner.Inner.Compute(1);
      _ = level1.Inner.Inner.Inner.Inner.Compute(1);
      _ = level1.Inner.Inner.Inner.Inner.IsPositive(1);
      _ = level1.Inner.Inner.Inner.Inner.Label(1);
      _ = level1.Label(1);
      _ = level1.Inner.Label(1);
      _ = level1.Inner.Inner.Label(1);

      var covered = new CoveredOuter();
      _ = covered.TopMethod(1);
      _ = new CoveredOuter.CoveredInner1().Method(1);
      _ = new CoveredOuter.CoveredInner1.CoveredSiblingInner2().Check(1);
      _ = new CoveredOuter.CoveredInner1B().Label(1);
      _ = new ContainerWithStaticNested().InstanceMethod(1);
      _ = ContainerWithStaticNested.GetDefault();
      _ = ContainerWithStaticNested.GetMaxRetries();

      var host = new ModuleHost();
      host.Register(new ModuleHost.DefaultPlugin());
      host.Register(new ModuleHost.AdvancedPlugin("x"));
      _ = host.Plugins.Count;

      var deep = new Deep7();
      _ = deep.Depth;
      _ = new Deep7.L2.L3.L4.L5.L6.L7().IsMax(7);
    }

    private static void RunExcludedWorkload()
    {
      _ = new PartiallyExcludedClass().Add(1, 2);
      _ = new PartiallyExcludedClass().Multiply(2, 3);
      _ = new PartiallyExcludedClass().IsPositive(1);
      _ = new PartiallyExcludedClass().Subtract(3, 1);

      var propertyExclusion = new PropertyExclusionClass { Value = 1, Tag = "x" };
      _ = propertyExclusion.Compute();

      var outer = new OuterClass();
      _ = outer.CoveredMethod(1);
      _ = new OuterClass.CoveredNestedClass().Calc(2);
      _ = new CoveredSiblingA().Method1(1);
      _ = new CoveredSiblingC().Check(10);
      _ = new CoveredSiblingE().Label(1);
      _ = new MethodLevelCustomExclusion().Covered(1);
      _ = new MethodLevelCustomExclusion().AlsoCovered(1);

      IWorkItem item = new CoveredWorkItem(1);
      item.Execute();
      _ = item.Priority;
    }

    private static void RunGenericWorkload()
    {
      var pair = new Pair<int, string>(1, "a");
      _ = pair.Swap();

      var repository = new Repository<string>();
      int id = repository.Add("x");
      _ = repository.TryGet(id, out string? value);
      _ = value;
      _ = repository.Count;

      var stack = new BoundedStack<int>(4);
      _ = stack.TryPush(1);
      _ = stack.TryPeek(out int top);
      _ = top;

      var workload = new GenericWorkload();
      _ = workload.Identity(1);
      _ = workload.FirstOrNull(["a", "b"], x => x == "a");
      _ = workload.Transform(2, x => x + 1);
      _ = workload.Select([1, 2, 3], x => x * 2);
      _ = workload.Filter([1, 2, 3], x => x > 1);
      _ = workload.MakePair(1, "b");
      _ = workload.TryFind([1, 2, 3], x => x == 2);
      _ = workload.Max(2, 3);
      _ = workload.Min(2, 3);
      _ = workload.Clamp(3, 1, 5);
      _ = workload.Sort([3, 2, 1]);
      _ = workload.Contains([1, 2, 3], 2);
      _ = workload.BinarySearch([1, 2, 3], 2);
      _ = workload.CreateDefault<List<int>>();
      _ = workload.CreateList<List<int>>(2);
      _ = workload.FillArray(3, 1);
      _ = workload.FromNullable<int>(1);
      _ = workload.AllSatisfy([1, 2, 3], x => x > 0);
      _ = workload.Reduce([1, 2, 3], 0, (acc, x) => acc + x);
    }

    private static void RunIteratorWorkload()
    {
      var workload = new IteratorWorkload();
      Consume(workload.Range(0, 3));
      Consume(workload.Evens(4));
      Consume(workload.Odds(5));
      Consume(workload.Squares(3));
      Consume(workload.Fibonacci(5));
      Consume(workload.PositiveValues([-1, 0, 1, 2]));
      Consume(workload.TakeWhilePositive([1, 2, 0, 3]));
      Consume(workload.SkipNegatives([-2, -1, 0, 1]));
      Consume(workload.ToStrings([-1, 0, 1], "p"));
      Consume(workload.Flatten([[1, 2], [3, 4]]));
      Consume(workload.Interleave([1, 3], [2, 4]));
      Consume(workload.WithCleanup([1, 2], () => { }));
      Consume(workload.SafeRange(0, 3));
      Consume(workload.Indexed([1, 2, 3]));
      Consume(workload.Batch([1, 2, 3, 4], 2));
      Consume(workload.BoxedRange(0, 3));
      Consume(workload.Chain(3));
      Consume(workload.ZipSum([1, 2], [3, 4]));
      Consume(workload.RunningSum([1, 2, 3]));
      Consume(workload.MovingAverage([1, 2, 3, 4], 2));
    }

    private static void RunLambdaWorkload()
    {
      var workload = new LambdaWorkload();
      _ = workload.ApplyTwice(x => x + 1, 1);
      _ = workload.Adder(2)(3);
      _ = workload.Multiplier(3)(2);
      _ = workload.RangeChecker(1, 3)(2);

      var log = new List<int>();
      workload.BuildLogger(log)(1);

      _ = workload.FilterAndDouble([1, 2, 3], 1);
      _ = workload.TopN([1, 2, 3], 2, 1);
      _ = workload.GroupBySign([-1, 0, 1]);
      _ = workload.AnyAbove([1, 2, 3], 2);
      _ = workload.SumPositive([-1, 1, 2]);
      _ = workload.AverageNonZero([0.0, 2.0]);
      _ = workload.TransformAndFilter([1, 2, 3], 1, 3);
      _ = workload.FrequencyMap([1, 1, 2]);
      _ = workload.SumRecursive(4);
      _ = workload.FilterAndProcessLocal([1, 2, 3], 1);
      _ = workload.BuildPipeline(1, 2, 1)(3);

      workload.Subscribe(_ => { });
      workload.Value = 1;

      _ = workload.SortWithComparison([1, 3, 2], descending: true);
      _ = workload.FilterWith([1, 2, 3], x => x > 1);
      workload.ForEachIf([1, 2, 3], x => x > 1, _ => { });
      _ = workload.Memoize(x => x + 1)(1);
      _ = workload.Curry((a, b) => a + b)(1)(2);
      _ = workload.Compose(x => x + 1, x => x * 2)(2);
      _ = workload.ComposeMany([x => x + 1, x => x * 2])(2);
      _ = workload.ConditionalTransform(true, 1)(2);
      _ = workload.TryGet(() => 1, 0);
      _ = workload.SafeTransform([1, 2, 3], x => x + 1);
    }

    private static void RunSwitchWorkload()
    {
      var workload = new SwitchWorkload();
      _ = workload.ClassifyScore(80);
      _ = workload.MapCodeToValue(10);
      _ = workload.ParseMonthNumber("jan");
      _ = workload.GetSeason(6);
      _ = workload.DescribeObject("x");
      _ = workload.ClassifyPair(1, -1);
      _ = workload.ClassifyTriangle(3, 4, 5);
      _ = workload.ComputePoints("gold", 10);
      _ = workload.GetDeadlineHours(SwitchWorkload.Priority.High);
      _ = workload.GetPriorityColor(SwitchWorkload.Priority.Critical);
      _ = workload.ClassifyCharacter('x');
      _ = workload.TransformArray([1, 2, 3], "square");
      _ = workload.Dispatch(1, -1, 1);
    }

    private static void Consume<T>(IEnumerable<T> values)
    {
      foreach (T _ in values)
      {
      }
    }

    private static void Consume(IEnumerable values)
    {
      foreach (object _ in values)
      {
      }
    }
  }
}
