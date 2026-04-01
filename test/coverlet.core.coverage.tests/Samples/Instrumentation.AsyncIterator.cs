// Remember to use full name because adding new using directives change line numbers

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Coverlet.Core.CoverageSamples.Tests
{
  public class AsyncIterator
  {
    async public Task<int> Issue1104_Repro()
    {
      int sum = 0;

      await foreach (int result in CreateSequenceAsync())
      {
        sum += result;
      }

      return sum;
    }

    async private IAsyncEnumerable<int> CreateSequenceAsync()
    {
      for (int i = 0; i < 100; ++i)
      {
        await Task.CompletedTask;
        yield return i;
      }
    }
  }

  /// <summary>
  /// Reproduction case for Issue #1836: Wrong branch rate on IAsyncEnumerable
  /// This class is from PR https://github.com/daveMueller/coverlet/pull/31
  /// </summary>
  public class Issue1836
  {
    public async IAsyncEnumerable<int> GetNumbersAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      int[] items = [1, 2];
      foreach (var item in items)
      {
        await Task.CompletedTask;
        yield return !cancellationToken.IsCancellationRequested ? item : throw new OperationCanceledException();
      }
    }
  }
}
