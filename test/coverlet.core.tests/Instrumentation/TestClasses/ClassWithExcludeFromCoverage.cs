using Coverlet.Core.Attributes;

namespace Coverlet.Core.Instrumentation.Tests.TestClasses
{
    [ExcludeFromCoverage]
    public class ClassWithExcludeFromCoverage
    {
        public void MethodThatMustBeExcludedFromCodeCoverage()
        {

        }

        public void OtherMethodThatMustBeExcludedFromCodeCoverage()
        {

        }
    }
}