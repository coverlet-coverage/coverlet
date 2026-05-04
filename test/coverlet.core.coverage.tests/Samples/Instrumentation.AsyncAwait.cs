// Remember to use full name because adding new using directives change line numbers

using System.Threading.Tasks;

namespace Coverlet.Core.CoverageSamples.Tests
{
  public class AsyncAwait
  {
    async public Task<int> AsyncExecution(bool skipLast)
    {
      int res = 0;
      res += await Async();

      res += await Async();

      if (!skipLast)
      {
        res += await Async();
      }

      return res;
    }

    async public Task<int> Async()
    {
      await Task.Delay(100);
      return 42;
    }

    async public Task SyncExecution()
    {
      await Sync();
    }

    public Task Sync()
    {
      return Task.CompletedTask;
    }

    async public Task<int> AsyncExecution(int val)
    {
      int res = 0;
      switch (val)
      {
        case 1:
          {
            res += await Async();
            break;
          }
        case 2:
          {
            res += await Async() + await Async();
            break;
          }
        case 3:
          {
            res += await Async() + await Async() +
                   await Async();
            break;
          }
        case 4:
          {
            res += await Async() + await Async() +
                   await Async() + await Async();
            break;
          }
        default:
          break;
      }
      return res;
    }

    async public Task<int> ContinuationNotCalled()
    {
      int res = 0;
      res += await Async().ContinueWith(x => x.Result);
      return res;
    }

    async public Task<int> ContinuationCalled()
    {
      int res = 0;
      res += await Async().ContinueWith(x => x.Result);
      return res;
    }

    async public Task<int> ConfigureAwait()
    {
      await Task.Delay(100).ConfigureAwait(false);
      return 42;
    }
  }

  public class Issue_669_1
  {
    async public Task Test()
    {
      var service = new Moq.Mock<IService>();
      service.Setup(c => c.GetCat()).Returns(Task.FromResult("cat"));

      var foo = new Foo(service.Object);
      await foo.Bar();
    }


    public class Foo
    {
      private readonly IService _service;

      public Foo(IService service)
      {
        _service = service;
      }

      public async Task Bar()
      {
        var cat = await _service.GetCat();
        await _service.Process(cat);
      }
    }

    public interface IService
    {
      Task<string> GetCat();
      Task Process(string cat);
    }
  }

  public class Issue_1177
  {
    async public Task Test()
    {
      await Task.CompletedTask;
      using var _ = new System.IO.MemoryStream();
      await Task.CompletedTask;
      await Task.CompletedTask;
      await Task.CompletedTask;
    }
  }

  public class Issue_1233
  {
    async public Task Test()
    {
      try
      {
      }
      finally
      {
        await Task.CompletedTask;
      }
    }
  }

  public class Issue_1275
  {
    public async Task<int> Execute(System.Threading.CancellationToken token)
    {
      int sum = 0;

      await foreach (int result in AsyncEnumerable(token))
      {
        sum += result;
      }

      return sum;
    }

    async System.Collections.Generic.IAsyncEnumerable<int> AsyncEnumerable([System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken)
    {
      for (int i = 0; i < 1; i++)
      {
        await Task.Delay(1, cancellationToken);
        yield return i;
      }
    }
  }

  // Issue #1843: Comprehensive async method coverage validation
  // Tests multiple async patterns to ensure all methods are properly instrumented
  public class Issue_1843_ComprehensiveAsync
  {
    // 1. Simple async Task methods
    public async Task SimpleAsyncMethod()
    {
      await Task.Delay(1);
    }

    // 2. Async Task<T> with different return types
    public async Task<int> AsyncWithIntReturn()
    {
      await Task.Delay(1);
      return 42;
    }

    public async Task<string> AsyncWithStringReturn()
    {
      await Task.Delay(1);
      return "test";
    }

    // 3. ValueTask variants
    public async ValueTask SimpleValueTask()
    {
      await Task.Delay(1);
    }

    public async ValueTask<int> ValueTaskWithReturn()
    {
      await Task.Delay(1);
      return 100;
    }

    // 4. Async methods with ConfigureAwait
    public async Task WithConfigureAwaitTrue()
    {
      await Task.Delay(1).ConfigureAwait(true);
    }

    public async Task WithConfigureAwaitFalse()
    {
      await Task.Delay(1).ConfigureAwait(false);
    }

    // 5. Nested async calls
    public async Task NestedAsyncCalls()
    {
      await SimpleAsyncMethod();
      await AsyncWithIntReturn();
      await ValueTaskWithReturn();
    }

    // 6. Async methods with branching
    public async Task<int> AsyncWithBranching(int value)
    {
      if (value > 0)
      {
        await Task.Delay(1);
        return value;
      }
      else
      {
        await Task.Delay(1);
        return 0;
      }
    }

    // 7. Async methods with exception handling
    public async Task AsyncWithTryCatch()
    {
      try
      {
        await Task.Delay(1);
      }
      catch
      {
        await Task.Delay(1);
      }
    }

    // 8. Async methods with LINQ and lambdas
    public async Task<System.Collections.Generic.List<int>> AsyncWithLinq()
    {
      var data = System.Linq.Enumerable.Range(1, 10);
      await Task.Delay(1);
      return System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(data, x => x % 2 == 0));
    }

    // 9. Async method calling other async methods in parallel
    public async Task ParallelAsyncCalls()
    {
      await Task.WhenAll(
        SimpleAsyncMethod(),
        AsyncWithIntReturn(),
        ValueTaskWithReturn().AsTask()
      );
    }

    // 10. Async method with multiple await points
    public async Task MultipleAwaitPoints(int count)
    {
      for (int i = 0; i < count; i++)
      {
        await Task.Delay(1);
      }
    }

    // 11. Async method with switch expression
    public async Task<string> AsyncWithSwitchExpression(int value)
    {
      await Task.Delay(1);
      return value switch
      {
        1 => "one",
        2 => "two",
        _ => "other"
      };
    }

    // 12. Async method with null coalescing
    public async Task<string> AsyncWithNullCoalescing(string input)
    {
      await Task.Delay(1);
      return input ?? "default";
    }

    // 13. Async IEnumerable
    public async System.Collections.Generic.IAsyncEnumerable<int> AsyncEnumerable([System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
    {
      for (int i = 0; i < 5; i++)
      {
        await Task.Delay(1, cancellationToken);
        yield return i;
      }
    }
  }

  // Async methods with try-finally blocks containing await statements
  // Tests various patterns of async try-finally to ensure correct coverage
  public class AsyncTryFinallyPhantomBranches
  {
    // Test 1: Basic async try-finally with await in finally block
    public async Task BasicAsyncTryFinally()
    {
      try
      {
        await Task.Delay(1);
      }
      finally
      {
        await Task.Delay(1);
      }
    }

    // Test 2: Try-finally with code in try and await in finally
    public async Task<int> TryFinallyWithReturnValue()
    {
      int result = 0;
      try
      {
        result = 42;
        await Task.Delay(1);
      }
      finally
      {
        await Task.CompletedTask;
      }
      return result;
    }

    // Test 3: Try-finally with exception handling
    public async Task TryFinallyWithException()
    {
      try
      {
        await Task.Delay(1);
        throw new System.Exception("Test exception");
      }
      finally
      {
        await Task.CompletedTask;
      }
    }

    // Test 4: Nested try-finally blocks with await
    public async Task NestedTryFinally()
    {
      try
      {
        await Task.Delay(1);
        try
        {
          await Task.Delay(1);
        }
        finally
        {
          await Task.CompletedTask;
        }
      }
      finally
      {
        await Task.CompletedTask;
      }
    }

    // Test 5: Try-finally with branching logic
    public async Task<int> TryFinallyWithBranching(bool condition)
    {
      int result = 0;
      try
      {
        if (condition)
        {
          result = 1;
          await Task.Delay(1);
        }
        else
        {
          result = 2;
          await Task.Delay(2);
        }
      }
      finally
      {
        await Task.CompletedTask;
      }
      return result;
    }

    // Test 6: Try-finally with multiple await statements in finally
    public async Task TryFinallyWithMultipleAwaitsInFinally()
    {
      try
      {
        await Task.Delay(1);
      }
      finally
      {
        await Task.Delay(1);
        await Task.Delay(1);
        await Task.CompletedTask;
      }
    }

    // Test 7: Try-catch-finally with await in finally
    public async Task TryCatchFinallyWithAwaitInFinally()
    {
      try
      {
        await Task.Delay(1);
      }
      catch (System.Exception)
      {
        await Task.Delay(1);
      }
      finally
      {
        await Task.CompletedTask;
      }
    }

    // Test 8: Empty try block with await in finally
    public async Task EmptyTryWithAwaitInFinally()
    {
      try
      {
        // Empty try block
      }
      finally
      {
        await Task.CompletedTask;
      }
    }

    // Test 9: Try-finally with ConfigureAwait
    public async Task TryFinallyWithConfigureAwait()
    {
      try
      {
        await Task.Delay(1).ConfigureAwait(false);
      }
      finally
      {
        await Task.Delay(1).ConfigureAwait(false);
      }
    }

    // Test 10: Try-finally with ValueTask
    public async ValueTask TryFinallyWithValueTask()
    {
      try
      {
        await Task.Delay(1);
      }
      finally
      {
        await new ValueTask(Task.CompletedTask);
      }
    }

    // Test 11: Try-finally with loop in try block
    public async Task TryFinallyWithLoopInTry(int count)
    {
      try
      {
        for (int i = 0; i < count; i++)
        {
          await Task.Delay(1);
        }
      }
      finally
      {
        await Task.CompletedTask;
      }
    }

    // Test 12: Try-finally with using statement and await
    public async Task TryFinallyWithUsingAndAwait()
    {
      try
      {
        using (var ms = new System.IO.MemoryStream())
        {
          await Task.Delay(1);
        }
      }
      finally
      {
        await Task.CompletedTask;
      }
    }
  }
}
