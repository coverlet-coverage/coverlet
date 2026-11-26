// Remember to use full name because adding new using directives change line numbers

namespace Coverlet.Core.CoverageSamples.Tests
{
  // https://github.com/coverlet-coverage/coverlet/issues/2085
  public class AwaitedCatchDispatch
  {
    public async System.Threading.Tasks.Task<int> ThreeHandlers(System.Exception error)
    {
      try { throw error; }
      catch (System.ArgumentNullException) { await System.Threading.Tasks.Task.CompletedTask; return 1; }
      catch (System.ArgumentOutOfRangeException) { await System.Threading.Tasks.Task.CompletedTask; return 2; }
      catch (System.InvalidOperationException) { await System.Threading.Tasks.Task.CompletedTask; return 3; }
    }
  }
}
