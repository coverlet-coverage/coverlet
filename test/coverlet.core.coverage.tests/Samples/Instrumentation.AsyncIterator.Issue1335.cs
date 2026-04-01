// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Remember to use full name because adding new using directives change line numbers

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coverlet.Core.CoverageSamples.Tests
{
  // Reproduction case for Issue #1335: Branch coverage issue with AsyncEnumerable Extension
  public class AsyncIteratorIssue1335
  {
    public async IAsyncEnumerable<int> CreateSequenceAsync(int count)
    {
      for (int i = 1; i <= count; i++)
      {
        await Task.CompletedTask;
        yield return i;
      }
    }

    public async IAsyncEnumerable<List<int>> BatchAsync(IAsyncEnumerable<int> source, int batchSize)
    {
      List<int> batch = new(batchSize);
      await foreach (int item in source)
      {
        batch.Add(item);
        if (batch.Count >= batchSize)
        {
          yield return batch;
          batch = new List<int>(batchSize);
        }
      }
      if (batch.Count > 0)
      {
        yield return batch;
      }
    }

    public async Task<int> ConsumeBatchedSequence(int itemCount, int batchSize)
    {
      int totalItems = 0;
      await foreach (List<int> batch in BatchAsync(CreateSequenceAsync(itemCount), batchSize))
      {
        totalItems += batch.Count;
      }

      return totalItems;
    }

    public async IAsyncEnumerable<int> TransformAsync(IAsyncEnumerable<int> source)
    {
      await foreach (int item in source)
      {
        yield return item * 2;
      }
    }

    public async Task<int> ConsumeTransformedSequence(int count)
    {
      int sum = 0;
      await foreach (int value in TransformAsync(CreateSequenceAsync(count)))
      {
        sum += value;
      }

      return sum;
    }
  }
}

