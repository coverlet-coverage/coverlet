using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Coverlet.Core.Samples.Tests
{
    public class IntegerOverflow
    {
        const long max = (long)int.MaxValue + 1;
        
        public void Test()
        {
            for (long i = 0; i < max; i++)
            {
                _ = 1;
            }
        }
    }
}
