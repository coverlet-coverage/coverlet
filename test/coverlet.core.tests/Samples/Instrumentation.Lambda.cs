// Remember to use full name because adding new using directives change line numbers

using System.Linq;
using System.Threading.Tasks;

namespace Coverlet.Core.Samples.Tests
{
    public class Lambda_Issue343
    {
        protected T WriteToStream<T>(System.Func<System.IO.Stream, bool, T> getResultFunction)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                var result = getResultFunction(stream, false);
                return result;
            }
        }

        public bool InvokeAnonymous()
        {
            return WriteToStream((stream, condition) =>
            {
                if (condition)
                    stream.WriteByte(1);
                else
                    stream.WriteByte(0);
                return condition;
            }
            );
        }

        public bool InvokeAnonymous_Test()
        {
            Lambda_Issue343 demoClass = new Lambda_Issue343();
            return demoClass.InvokeAnonymous();
        }

        protected async Task<T> WriteToStreamAsync<T>(System.Func<System.IO.Stream, bool, Task<T>> getResultFunction)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                var result = await getResultFunction(stream, false);
                return result;
            }
        }

        async public Task<bool> InvokeAnonymousAsync()
        {
            return await WriteToStreamAsync(async (stream, condition) =>
            {
                if (condition)
                    stream.WriteByte(1);
                else
                    stream.WriteByte(0);
                return await Task.FromResult(condition);
            });
        }

        async public Task<bool> InvokeAnonymousAsync_Test()
        {
            Lambda_Issue343 demoClass = new Lambda_Issue343();
            return await demoClass.InvokeAnonymousAsync();
        }
    }

    public class Issue_730
    {
        async public Task Invoke()
        {
            await DoSomethingAsyncWithLinq(new object[100]);
        }
        async public Task DoSomethingAsyncWithLinq(System.Collections.Generic.IEnumerable<object> objects)
        {
            await Task.Delay(System.TimeSpan.FromMilliseconds(1));
            var selected = System.Linq.Enumerable.Select(objects, o => o);
            _ = System.Linq.Enumerable.ToArray(selected);
        }
    }

    public class Issue_760
    {
        public async Task<int> If()
        {
            var numbers = (System.Collections.Generic.IEnumerable<int>)new[] { 1, 2, 3, 4, 5 };
            var result = 0;
            if (numbers.Select(i => i * 2).Count() == 5)
            {
                result = 1;
            }
            await Task.Delay(100);
            return result;
        }

        public async Task<int> Foreach()
        {
            var numbers = (System.Collections.Generic.IEnumerable<int>)new[] { 1, 2, 3, 4, 5 };
            var sum = 0;
            foreach (var i in numbers.Select(n => n * 2))
            {
                sum += i;
            }
            await Task.Delay(100);
            return sum;
        }
    }
}
