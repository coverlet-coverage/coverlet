using System;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Coverlet.Tests.Xunit.Extensions
{
    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer("Coverlet.Tests.Xunit.Extensions." + nameof(ConditionalFactDiscoverer), "coverlet.tests.xunit.extensions")]
    public class ConditionalFact : FactAttribute { }

    internal class ConditionalFactDiscoverer : FactDiscoverer
    {
        public ConditionalFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink) { }

        protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            return new SkippableTestCase(testMethod.EvaluateSkipConditions(), DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod);
        }
    }

    internal class SkippableTestCase : XunitTestCase
    {
        private readonly string _skipReason;

        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public SkippableTestCase() { }

        public SkippableTestCase(string skipReason, IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
        {
            _skipReason = skipReason;
        }
        protected override string GetSkipReason(IAttributeInfo factAttribute)
        {
            return _skipReason ?? base.GetSkipReason(factAttribute);
        }
    }
}