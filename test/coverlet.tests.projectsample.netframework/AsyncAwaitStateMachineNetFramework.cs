using System.Threading.Tasks;

namespace coverlet.tests.projectsample.netframework
{
    public class AsyncAwaitStateMachineNetFramework
    {
        public async Task AsyncAwait()
        {
            await Task.CompletedTask;
        }
    }
}
