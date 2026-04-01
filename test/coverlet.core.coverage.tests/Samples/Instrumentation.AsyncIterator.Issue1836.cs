// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Remember to use full name because adding new using directives change line numbers

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Coverlet.Core.CoverageSamples.Tests
{
  // Reproduction case for Issue #1836: Wrong branch rate on IAsyncEnumerable
  public class AsyncIteratorIssue1836
  {
    public async IAsyncEnumerable<int> GetNumbersAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      int[] items = [1, 2];
      foreach (int item in items)
      {
        await Task.CompletedTask;
        yield return !cancellationToken.IsCancellationRequested ? item : throw new OperationCanceledException();
      }
    }

    public async Task<int> ConsumeWithoutCancellation()
    {
      int sum = 0;
      await foreach (int number in GetNumbersAsync())
      {
        sum += number;
      }

      return sum;
    }

    public async Task<int> ConsumeWithCancellation(CancellationToken cancellationToken)
    {
      int sum = 0;
      await foreach (int number in GetNumbersAsync(cancellationToken))
      {
        sum += number;
      }

      return sum;
    }
  }
}

