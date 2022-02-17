// Remember to use full name because adding new using directives change line numbers

using System;
using System.IO;
using System.Threading.Tasks;

namespace Coverlet.Core.Samples.Tests
{
    public class AsyncAwaitValueTask
    {
        async public ValueTask<int> AsyncExecution(bool skipLast)
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

            var stream = new MemoryStream(bytes);
            stream.Position = 0;

            int res = 0;
            res += await Async(stream);

            res += await Async(stream);

            if (!skipLast)
            {
                res += await Async(stream);
            }

            return res;
        }

        async public ValueTask<int> Async(System.IO.MemoryStream stream)
        {
            var buffer = new byte[4];
            await stream.ReadAsync(buffer.AsMemory());      // This overload of ReadAsync() returns a ValueTask<int>
            return buffer[0] + buffer[1] + buffer[2] + buffer[3];
        }

        async public ValueTask<int> AsyncExecution(int val)
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            var stream = new MemoryStream(bytes);
            stream.Position = 0;

            int res = 0;
            switch (val)
            {
                case 1:
                    {
                        res += await Async(stream);
                        break;
                    }
                case 2:
                    {
                        res += await Async(stream) + await Async(stream);
                        break;
                    }
                case 3:
                    {
                        res += await Async(stream) + await Async(stream) +
                               await Async(stream);
                        break;
                    }
                case 4:
                    {
                        res += await Async(stream) + await Async(stream) +
                               await Async(stream) + await Async(stream);
                        break;
                    }
                default:
                    break;
            }
            return res;
        }

        async public ValueTask SyncExecution()
        {
            await Sync();
        }

        public ValueTask Sync()
        {
            return default(ValueTask);
        }

        async public ValueTask<int> ConfigureAwait()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            var stream = new MemoryStream(bytes);
            stream.Position = 0;

            await Async(stream).ConfigureAwait(false);
            return 42;
        }

        async public Task<int> WrappingValueTaskAsTask()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            var stream = new MemoryStream(bytes);
            stream.Position = 0;

            var task = Async(stream).AsTask();

            return await task;
        }
    }
}
