// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// LambdaWorkload.cs
// Purpose: exercises the <>c compiler-generated-class branch-fixup path in
// Coverage.GetCoverageResult and the BranchInCompilerGeneratedClass lookup (P11).
// Methods use lambda captures, LINQ query bodies, local functions, delegate fields, and
// event handlers so that many distinct compiler-synthesised anonymous types are produced.

using System;
using System.Collections.Generic;
using System.Linq;

namespace coverlet.benchmark.subject
{
  /// <summary>
  /// Methods heavy on closures, lambdas, LINQ, and local functions to exercise the
  /// compiler-generated-class handling in <c>Coverage.GetCoverageResult</c>.
  /// </summary>
  public class LambdaWorkload
  {
    // ── simple Func / Action captures ────────────────────────────────────

    public int ApplyTwice(Func<int, int> f, int x) => f(f(x));

    public Func<int, int> Adder(int delta)
    {
      return x => x + delta;     // captures delta
    }

    public Func<int, int> Multiplier(int factor)
    {
      int local = factor;        // captured local
      return x => x * local;
    }

    public Func<int, bool> RangeChecker(int lo, int hi)
    {
      return x => x >= lo && x <= hi;    // captures two locals
    }

    public Action<int> BuildLogger(List<int> log)
    {
      return v =>
      {
        if (v > 0)
          log.Add(v);         // capture + conditional
      };
    }

    // ── LINQ query bodies ─────────────────────────────────────────────────

    public IEnumerable<int> FilterAndDouble(IEnumerable<int> source, int threshold)
    {
      return source
          .Where(x => x > threshold)
          .Select(x => x * 2);
    }

    public IEnumerable<int> TopN(IEnumerable<int> source, int n, int minValue)
    {
      return source
          .Where(x => x >= minValue)
          .OrderByDescending(x => x)
          .Take(n);
    }

    public Dictionary<string, int[]> GroupBySign(IEnumerable<int> source)
    {
      return source
          .GroupBy(x => x > 0 ? "positive" : x < 0 ? "negative" : "zero")
          .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public bool AnyAbove(IEnumerable<int> source, int threshold)
        => source.Any(x => x > threshold);

    public int SumPositive(IEnumerable<int> source)
        => source.Where(x => x > 0).Sum();

    public double AverageNonZero(IEnumerable<double> source)
    {
      IEnumerable<double> nonZero = source.Where(x => x != 0.0);
      return nonZero.Any() ? nonZero.Average() : 0.0;
    }

    // ── chained LINQ with multiple lambdas ────────────────────────────────

    public IEnumerable<string> TransformAndFilter(IEnumerable<int> source, int lo, int hi)
    {
      return source
          .Where(x => x >= lo && x <= hi)
          .Select(x => x switch
          {
            < 0 => $"({x})",
            0 => "zero",
            _ => x.ToString(),
          })
          .Where(s => s.Length > 0)
          .Distinct()
          .OrderBy(s => s.Length)
          .ThenBy(s => s);
    }

    public IEnumerable<(int key, int count)> FrequencyMap(IEnumerable<int> source)
    {
      return source
          .GroupBy(x => x)
          .Select(g => (g.Key, g.Count()))
          .OrderByDescending(t => t.Item2);
    }

    // ── local functions (closures) ────────────────────────────────────────

    public int SumRecursive(int n)
    {
      return Add(n);

      int Add(int x)      // local function captures nothing, but still a closure type
      {
        if (x <= 0) return 0;
        return x + Add(x - 1);
      }
    }

    public IEnumerable<int> FilterAndProcessLocal(IEnumerable<int> source, int threshold)
    {
      return source.Where(IsAbove).Select(Process);

      bool IsAbove(int x) => x > threshold;       // captures threshold
      int Process(int x) => x * 2 + threshold;    // captures threshold
    }

    public Func<int, int> BuildPipeline(int a, int b, int c)
    {
      return x => Step3(Step2(Step1(x)));

      int Step1(int v) => v + a;
      int Step2(int v) => v * b;
      int Step3(int v) => v - c;
    }

    // ── delegate fields / events ──────────────────────────────────────────

    public event EventHandler<int>? ValueChanged;

    public int Value
    {
      get;
      set
      {
        if (field != value)
        {
          field = value;
          ValueChanged?.Invoke(this, value);
        }
      }
    }

    public void Subscribe(Action<int> handler)
    {
      ValueChanged += (_, v) => handler(v);  // lambda capture of handler
    }

    // ── Predicate<T> / Comparison<T> patterns ────────────────────────────

    public List<int> SortWithComparison(List<int> list, bool descending)
    {
      var copy = new List<int>(list);
      copy.Sort(descending
          ? (a, b) => b.CompareTo(a)
          : (a, b) => a.CompareTo(b));
      return copy;
    }

    public List<T> FilterWith<T>(List<T> list, Predicate<T> predicate)
    {
      return list.FindAll(predicate);
    }

    public void ForEachIf<T>(IEnumerable<T> source, Predicate<T> condition, Action<T> action)
    {
      foreach (T item in source)
      {
        if (condition(item))
          action(item);
      }
    }

    // ── memoisation closure ───────────────────────────────────────────────

    public Func<int, int> Memoize(Func<int, int> f)
    {
      var cache = new Dictionary<int, int>();
      return x =>
      {
        if (!cache.TryGetValue(x, out int result))
        {
          result = f(x);
          cache[x] = result;
        }
        return result;
      };
    }

    // ── nested lambdas ────────────────────────────────────────────────────

    public Func<int, Func<int, int>> Curry(Func<int, int, int> f)
    {
      return a => b => f(a, b);   // lambda inside lambda
    }

    public Func<int, int> Compose(Func<int, int> f, Func<int, int> g)
    {
      return x => f(g(x));
    }

    public Func<int, int> ComposeMany(IEnumerable<Func<int, int>> funcs)
    {
      return funcs.Aggregate((f, g) => x => f(g(x)));
    }

    // ── conditional lambda ────────────────────────────────────────────────

    public Func<int, int> ConditionalTransform(bool negate, int offset)
    {
      Func<int, int> baseTransform = negate
          ? x => -x
          : x => x;

      return x =>
      {
        int base_ = baseTransform(x);
        return offset != 0 ? base_ + offset : base_;
      };
    }

    // ── exception inside lambda ───────────────────────────────────────────

    public T TryGet<T>(Func<T> factory, T fallback)
    {
      try
      {
        return factory();
      }
      catch
      {
        return fallback;
      }
    }

    public int SafeTransform(IEnumerable<int> source, Func<int, int> transform)
    {
      return source.Sum(x =>
      {
        try { return transform(x); }
        catch { return 0; }
      });
    }
  }
}
