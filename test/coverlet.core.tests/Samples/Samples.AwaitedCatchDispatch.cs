using System;
using System.Threading.Tasks;

namespace Coverlet.Core.Samples.Tests
{
  // Sample for https://github.com/coverlet-coverage/coverlet/issues/2085
  // With three or more catch handlers that await, the compiler picks the handler body
  // with a switch on a generated selector. The default arm of that switch can't be reached.
  public class AwaitedCatchDispatch
  {
    public async Task<int> ThreeHandlers(Exception error)
    {
      try { throw error; }
      catch (ArgumentNullException) { await Task.CompletedTask; return 1; }
      catch (ArgumentOutOfRangeException) { await Task.CompletedTask; return 2; }
      catch (InvalidOperationException) { await Task.CompletedTask; return 3; }
    }

    // A real switch in the same method is still counted.
    public async Task<int> ThreeHandlersAndSwitch(Exception error, int value)
    {
      try { throw error; }
      catch (ArgumentNullException) { await Task.CompletedTask; return 1; }
      catch (ArgumentOutOfRangeException) { await Task.CompletedTask; return 2; }
      catch (InvalidOperationException)
      {
        await Task.CompletedTask;
        switch (value)
        {
          case 0: return 3;
          case 1: return 4;
          case 2: return 5;
          default: return 6;
        }
      }
    }
  }
}
