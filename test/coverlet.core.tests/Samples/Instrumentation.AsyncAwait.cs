// Remember to use full name because adding new using directives change line numbers

using System.Threading.Tasks;

namespace Coverlet.Core.Samples.Tests
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
            await Task.Delay(1000);
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
    }
}
