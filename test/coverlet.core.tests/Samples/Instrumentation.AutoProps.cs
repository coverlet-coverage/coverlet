using System;

namespace Coverlet.Core.Samples.Tests
{
    public class AutoProps
    {
        private int _myVal = 0;
        public AutoProps()
        {
            _myVal = new Random().Next();
        }
        public int AutoPropsNonInit { get; set; }
        public int AutoPropsInit { get; set; } = 10;
    }
}
