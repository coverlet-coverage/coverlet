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
}
