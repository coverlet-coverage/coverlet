// Remember to use full name because adding new using directives change line numbers

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Coverlet.Core.Samples.Tests
{
    public class AsyncForeach
    {
        async public ValueTask<int> SumWithATwist(IAsyncEnumerable<int> ints)
        {
            int sum = 0;

            await foreach (int i in ints)
            {
                if (i > 0)
                {
                    sum += i;
                }
                else
                {
                    sum = 0;
                }
            }

            return sum;
        }


        async public ValueTask<int> Sum(IAsyncEnumerable<int> ints)
        {
            int sum = 0;

            await foreach (int i in ints)
            {
                sum += i;
            }

            return sum;
        }


        async public ValueTask<int> SumEmpty()
        {
            int sum = 0;

            await foreach (int i in AsyncEnumerable.Empty<int>())
            {
                sum += i;
            }

            return sum;
        }
    }
}
