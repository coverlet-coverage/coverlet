// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// IteratorWorkload.cs
// Purpose: exercises IEnumerator / IEnumerator<T> interface detection during instrumentation.
// Every iterator method generates a compiler-synthesised state-machine type that implements
// IEnumerator<T>, which triggers the IsCompilerGeneratedStateMachineType fast-path.

using System;
using System.Collections.Generic;

namespace coverlet.benchmark.subject
{
    /// <summary>
    /// Iterator methods covering common real-world patterns: simple sequences,
    /// conditional yields, recursive-style yields, and early exit via yield break.
    /// </summary>
    public class IteratorWorkload
    {
        // ── simple sequences ─────────────────────────────────────────────────

        public IEnumerable<int> Range(int start, int count)
        {
            for (int i = 0; i < count; i++)
                yield return start + i;
        }

        public IEnumerable<int> Evens(int max)
        {
            for (int i = 0; i <= max; i += 2)
                yield return i;
        }

        public IEnumerable<int> Odds(int max)
        {
            for (int i = 1; i <= max; i += 2)
                yield return i;
        }

        public IEnumerable<int> Squares(int count)
        {
            for (int i = 0; i < count; i++)
                yield return i * i;
        }

        public IEnumerable<int> Fibonacci(int count)
        {
            if (count <= 0) yield break;
            int a = 0, b = 1;
            yield return a;
            for (int i = 1; i < count; i++)
            {
                yield return b;
                (a, b) = (b, a + b);
            }
        }

        // ── conditional yields ───────────────────────────────────────────────

        public IEnumerable<int> PositiveValues(IEnumerable<int> source)
        {
            foreach (int value in source)
            {
                if (value > 0)
                    yield return value;
            }
        }

        public IEnumerable<int> TakeWhilePositive(IEnumerable<int> source)
        {
            foreach (int value in source)
            {
                if (value <= 0)
                    yield break;
                yield return value;
            }
        }

        public IEnumerable<int> SkipNegatives(IEnumerable<int> source)
        {
            bool seenPositive = false;
            foreach (int value in source)
            {
                if (!seenPositive && value < 0)
                    continue;
                seenPositive = true;
                yield return value;
            }
        }

        public IEnumerable<string> ToStrings(IEnumerable<int> source, string prefix = "")
        {
            foreach (int value in source)
            {
                if (value < 0)
                    yield return $"{prefix}({value})";
                else if (value == 0)
                    yield return $"{prefix}zero";
                else
                    yield return $"{prefix}{value}";
            }
        }

        // ── nested iterators ─────────────────────────────────────────────────

        public IEnumerable<int> Flatten(IEnumerable<IEnumerable<int>> sources)
        {
            foreach (IEnumerable<int> inner in sources)
            {
                foreach (int value in inner)
                    yield return value;
            }
        }

        public IEnumerable<int> Interleave(IEnumerable<int> first, IEnumerable<int> second)
        {
            using IEnumerator<int> e1 = first.GetEnumerator();
            using IEnumerator<int> e2 = second.GetEnumerator();
            bool has1 = e1.MoveNext();
            bool has2 = e2.MoveNext();
            while (has1 || has2)
            {
                if (has1)
                {
                    yield return e1.Current;
                    has1 = e1.MoveNext();
                }
                if (has2)
                {
                    yield return e2.Current;
                    has2 = e2.MoveNext();
                }
            }
        }

        // ── try/finally inside iterators (stresses exception-handler ranges) ─

        public IEnumerable<int> WithCleanup(IEnumerable<int> source, Action onComplete)
        {
            try
            {
                foreach (int value in source)
                    yield return value;
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        public IEnumerable<int> SafeRange(int start, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            try
            {
                for (int i = 0; i < count; i++)
                    yield return start + i;
            }
            finally
            {
                // clean-up in iterator
            }
        }

        // ── IEnumerable<T> with ref-struct-safe workarounds ──────────────────

        public IEnumerable<(int index, int value)> Indexed(IEnumerable<int> source)
        {
            int index = 0;
            foreach (int value in source)
                yield return (index++, value);
        }

        public IEnumerable<int[]> Batch(IEnumerable<int> source, int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            var batch = new List<int>(size);
            foreach (int item in source)
            {
                batch.Add(item);
                if (batch.Count == size)
                {
                    yield return [.. batch];
                    batch.Clear();
                }
            }
            if (batch.Count > 0)
                yield return [.. batch];
        }

        // ── non-generic IEnumerable ───────────────────────────────────────────

        public System.Collections.IEnumerable BoxedRange(int start, int count)
        {
            for (int i = 0; i < count; i++)
                yield return (object)(start + i);
        }

        // ── chained iterators (exercises multiple state machines per class) ──

        public IEnumerable<int> Chain(int n)
        {
            foreach (int a in Range(0, n))
                foreach (int b in Squares(a + 1))
                    yield return a + b;
        }

        public IEnumerable<int> ZipSum(IEnumerable<int> left, IEnumerable<int> right)
        {
            using IEnumerator<int> l = left.GetEnumerator();
            using IEnumerator<int> r = right.GetEnumerator();
            while (l.MoveNext() && r.MoveNext())
                yield return l.Current + r.Current;
        }

        // ── iterator with state that spans yields ────────────────────────────

        public IEnumerable<int> RunningSum(IEnumerable<int> source)
        {
            int acc = 0;
            foreach (int v in source)
            {
                acc += v;
                yield return acc;
            }
        }

        public IEnumerable<int> MovingAverage(IEnumerable<int> source, int window)
        {
            if (window <= 0) throw new ArgumentOutOfRangeException(nameof(window));
            var buf = new Queue<int>(window);
            foreach (int v in source)
            {
                buf.Enqueue(v);
                if (buf.Count > window) buf.Dequeue();
                int sum = 0;
                foreach (int x in buf) sum += x;
                yield return sum / buf.Count;
            }
        }
    }
}
