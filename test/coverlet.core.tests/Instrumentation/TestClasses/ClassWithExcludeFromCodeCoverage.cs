using System.Diagnostics.CodeAnalysis;

namespace Coverlet.Core.Instrumentation.Tests.TestClasses
{
    [ExcludeFromCodeCoverage]
    public class ClassWithExcludeFromCodeCoverage
    {
        public void MethodThatMustBeExcludedFromCodeCoverage()
        {
            
        }

        public void OtherMethodThatMustBeExcludedFromCodeCoverage()
        {

        }
    }
}