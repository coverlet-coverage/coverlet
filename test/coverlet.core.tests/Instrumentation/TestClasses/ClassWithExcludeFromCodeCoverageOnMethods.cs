using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Coverlet.Core.Instrumentation.Tests.TestClasses
{
    public class ClassWithExcludeFromCodeCoverageOnMethods
    {
        [ExcludeFromCodeCoverage]
        public void MethodThatMustBeExcludedFromCoverage()
        {

        }
        [ExcludeFromCodeCoverage]
        public void MethodThatMustBeExcludedFromCoverage(int firstArgument)
        {

        }
        [ExcludeFromCodeCoverage]
        public void MethodThatMustBeExcludedFromCoverage(int firstArgument, string secondArgument)
        {

        }
        [ExcludeFromCodeCoverage]
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
        [Description("Some descriotion 2")]
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

        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}