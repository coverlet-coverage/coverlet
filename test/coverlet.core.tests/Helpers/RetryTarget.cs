using System;

namespace Coverlet.Core.Helpers.Tests
{
    public class RetryTarget
    {
        public int Calls { get; private set; }
        public void TargetActionThrows() 
        {
            Calls++;
            throw new Exception("Simulating Failure");
        }
        public void TargetActionThrows5Times() 
        {
            Calls++;
            if (Calls < 6) throw new Exception("Simulating Failure");
        }
    }
}