using System.ComponentModel;
using Coverlet.Core.Attributes;

namespace Coverlet.Core.Instrumentation.Tests.TestClasses
{
    public class ClassWithExcludeFromCoverageOnMethods
    {
        [ExcludeFromCoverage]
        public void MethodThatMustBeExcludedFromCoverage()
        {

        }
        [ExcludeFromCoverage]
        public void MethodThatMustBeExcludedFromCoverage(int firstArgument)
        {

        }
        [ExcludeFromCoverage]
        public void MethodThatMustBeExcludedFromCoverage(int firstArgument, string secondArgument)
        {

        }
        [ExcludeFromCoverage]
        public void OtherMethodThatMustBeExcludedFromCoverage()
        {

        }

        public void MethodThatMustBeIncludedToCoverage()
        {

        }
        public void MethodThatMustBeIncludedToCoverage(int firstArgument)
        {

        }
        public void MethodThatMustBeIncludedToCoverage(int firstArgument, string secondArgument)
        {

        }
        [Description("Some descriotion")]
        public void MethodThatMustBeIncludedToCoverageToo()
        {

        }
        public void OtherMethodThatMustBeIncludedToCoverage()
        {

        }

        public override string ToString()
        {
            return base.ToString();
        }

        [ExcludeFromCoverage]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}