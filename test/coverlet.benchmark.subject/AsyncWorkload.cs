// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// AsyncWorkload.cs
// Purpose: exercises IAsyncStateMachine type detection and IsCompilerGeneratedStateMachineType
// fast-path during instrumentation.  Every method generates its own compiler-synthesised
// state-machine nested type, so the benchmark subject produces many distinct
// <>d__N types alongside the normal ones.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace coverlet.benchmark.subject
{
    /// <summary>
    /// A collection of async methods covering common real-world patterns:
    /// simple awaits, nested calls, exception handling, loops, and cancellation.
    /// </summary>
    public class AsyncWorkload
    {
        // ── simple value-returning async ────────────────────────────────────

        public async Task<int> ComputeAsync(int x, int y, CancellationToken ct = default)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            if (x < 0 || y < 0)
                throw new ArgumentOutOfRangeException(nameof(x), "must be non-negative");
            return x + y;
        }

        public async Task<string> FormatAsync(int value, CancellationToken ct = default)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            return value switch
            {
                < 0 => "negative",
                0 => "zero",
                < 10 => "small",
                < 100 => "medium",
                _ => "large",
            };
        }

        // ── nested async calls ───────────────────────────────────────────────

        public async Task<int> NestedComputeAsync(int a, int b, int c, CancellationToken ct = default)
        {
            int ab = await ComputeAsync(a, b, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            int abc = await ComputeAsync(ab, c, ct).ConfigureAwait(false);
            return abc;
        }

        public async Task<string> NestedFormatAsync(int a, int b, CancellationToken ct = default)
        {
            int sum = await NestedComputeAsync(a, b, 0, ct).ConfigureAwait(false);
            return await FormatAsync(sum, ct).ConfigureAwait(false);
        }

        // ── async loops ──────────────────────────────────────────────────────

        public async Task<int> SumSequenceAsync(IEnumerable<int> values, CancellationToken ct = default)
        {
            int total = 0;
            foreach (int v in values)
            {
                ct.ThrowIfCancellationRequested();
                total += await ComputeAsync(total, v, ct).ConfigureAwait(false);
            }
            return total;
        }

        public async Task<List<string>> FormatManyAsync(IEnumerable<int> values, CancellationToken ct = default)
        {
            var results = new List<string>();
            foreach (int v in values)
            {
                ct.ThrowIfCancellationRequested();
                results.Add(await FormatAsync(v, ct).ConfigureAwait(false));
            }
            return results;
        }

        // ── try/catch/finally in async ───────────────────────────────────────

        public async Task<int> SafeComputeAsync(int x, int y, CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int result = await ComputeAsync(x, y, ct).ConfigureAwait(false);
                return result;
            }
            catch (ArgumentOutOfRangeException)
            {
                return -1;
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            finally
            {
                await Task.Yield();
            }
        }

        public async Task<string> SafeFormatAsync(int value, CancellationToken ct = default)
        {
            try
            {
                return await FormatAsync(value, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return "error";
            }
        }

        // ── async with multiple await points ────────────────────────────────

        public async Task<(int sum, string label)> ComputeAndFormatAsync(
            int a, int b, CancellationToken ct = default)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            int sum = await ComputeAsync(a, b, ct).ConfigureAwait(false);
            await Task.Yield();
            string label = await FormatAsync(sum, ct).ConfigureAwait(false);
            await Task.Yield();
            return (sum, label);
        }

        public async Task<bool> IsPositiveSumAsync(int a, int b, CancellationToken ct = default)
        {
            await Task.Yield();
            (int sum, _) = await ComputeAndFormatAsync(a, b, ct).ConfigureAwait(false);
            return sum > 0;
        }

        // ── Task.WhenAll / Task.WhenAny ──────────────────────────────────────

        public async Task<int[]> ParallelComputeAsync(int[] inputs, CancellationToken ct = default)
        {
            Task<int>[] tasks = new Task<int>[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                int x = inputs[i];
                tasks[i] = ComputeAsync(x, x, ct);
            }
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task<string> FirstResultAsync(int a, int b, CancellationToken ct = default)
        {
            Task<string> t1 = FormatAsync(a, ct);
            Task<string> t2 = FormatAsync(b, ct);
            Task<string> winner = await Task.WhenAny(t1, t2).ConfigureAwait(false);
            return await winner.ConfigureAwait(false);
        }

        // ── cancellation-aware with timeouts ────────────────────────────────

        public async Task<int> ComputeWithTimeoutAsync(int x, int y, int timeoutMs)
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                return await ComputeAsync(x, y, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return -1;
            }
        }

        // ── async void (fire-and-forget, exercises separate code path) ──────

        public void FireAndForgetCompute(int x, int y) => _ = ComputeAsync(x, y);

        // ── ValueTask variants ───────────────────────────────────────────────

        public async ValueTask<int> ComputeValueTaskAsync(int x, int y, CancellationToken ct = default)
        {
            if (x == 0 && y == 0)
                return 0;           // synchronous fast-path (no allocation)

            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            return x + y;
        }

        public async ValueTask<bool> IsZeroAsync(int value, CancellationToken ct = default)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            return value == 0;
        }

        // ── deeply nested exception handling ────────────────────────────────

        public async Task<int> DeeplyNestedAsync(int depth, CancellationToken ct = default)
        {
            if (depth <= 0)
            {
                await Task.Yield();
                return 0;
            }

            try
            {
                ct.ThrowIfCancellationRequested();
                int inner = await DeeplyNestedAsync(depth - 1, ct).ConfigureAwait(false);
                return inner + depth;
            }
            catch (InvalidOperationException)
            {
                return -depth;
            }
        }

        // ── async stream consumer (IAsyncEnumerable) ────────────────────────

        public async Task<int> ConsumeStreamAsync(IAsyncEnumerable<int> source, CancellationToken ct = default)
        {
            int total = 0;
            await foreach (int item in source.WithCancellation(ct).ConfigureAwait(false))
            {
                total += item;
            }
            return total;
        }

        // ── ConfigureAwait variations ────────────────────────────────────────

        public async Task<int> WithConfigureAwaitFalseAsync(int x, CancellationToken ct = default)
        {
            await Task.Delay(0, ct).ConfigureAwait(false);
            int a = await ComputeAsync(x, 1, ct).ConfigureAwait(false);
            int b = await ComputeAsync(a, 2, ct).ConfigureAwait(false);
            return b;
        }

        public async Task<int> WithConfigureAwaitTrueAsync(int x, CancellationToken ct = default)
        {
            await Task.Delay(0, ct).ConfigureAwait(true);
            int a = await ComputeAsync(x, 1, ct).ConfigureAwait(true);
            return a;
        }

        // ── async method with out-param-style helper ─────────────────────────

        public async Task<(bool success, int value)> TryComputeAsync(int x, int y, CancellationToken ct = default)
        {
            try
            {
                int result = await ComputeAsync(x, y, ct).ConfigureAwait(false);
                return (true, result);
            }
            catch
            {
                return (false, 0);
            }
        }

        // ── I/O-like pattern (MemoryStream so no real I/O) ───────────────────

        public async Task<byte[]> ReadBytesAsync(byte[] data, CancellationToken ct = default)
        {
            using var ms = new MemoryStream(data);
            var buffer = new byte[data.Length];
            int bytesRead = await ms.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (bytesRead != data.Length)
                throw new IOException("Short read");
            return buffer;
        }

        // ── batch processing ─────────────────────────────────────────────────

        public async Task<int[]> BatchProcessAsync(int[] items, int batchSize, CancellationToken ct = default)
        {
            var results = new int[items.Length];
            for (int i = 0; i < items.Length; i += batchSize)
            {
                ct.ThrowIfCancellationRequested();
                int end = Math.Min(i + batchSize, items.Length);
                for (int j = i; j < end; j++)
                {
                    results[j] = await ComputeAsync(items[j], j, ct).ConfigureAwait(false);
                }
                await Task.Yield();
            }
            return results;
        }
    }

    /// <summary>
    /// Async producer that generates an <see cref="IAsyncEnumerable{T}"/> stream.
    /// Exercises the async-iterator state machine (IAsyncEnumerator).
    /// </summary>
    public class AsyncStreamProducer
    {
        public async IAsyncEnumerable<int> ProduceAsync(
            int count,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return i * i;
            }
        }

        public async IAsyncEnumerable<string> ProduceStringsAsync(
            IEnumerable<int> values,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (int v in values)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return v.ToString();
            }
        }
    }
}
