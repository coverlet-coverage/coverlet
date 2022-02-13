// Remember to use full name because adding new using directives change line numbers

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Coverlet.Core.Samples.Tests
{
    public class AwaitUsing
    {
        async public ValueTask HasAwaitUsing()
        {
            await using (var ms = new MemoryStream(Encoding.ASCII.GetBytes("Boo")))
            {
            }
        }


        async public Task Issue914_Repro()
        {
            await Issue914_Repro_Example1();
            await Issue914_Repro_Example2();
        }


        async private Task Issue914_Repro_Example1()
        {
            await using var transaction = new MyTransaction();
        }


        async private Task Issue914_Repro_Example2()
        {
            var transaction = new MyTransaction();
            await transaction.DisposeAsync();
        }


        private class MyTransaction : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                await default(ValueTask);
            }
        }
    }
}
