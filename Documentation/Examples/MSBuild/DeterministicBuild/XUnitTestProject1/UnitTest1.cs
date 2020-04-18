using System;
using Xunit;

namespace XUnitTestProject1
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            new ClassLibrary1.Class1().Method();
        }
    }
}
