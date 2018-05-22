using coverlet.core.Logging;
using coverlet.core.Logging.Decorators;
using Coverlet.Core.Instrumentation;
using Xunit;

namespace Coverlet.Core.Tests.Logging
{
    public class EnableLoggingTests
    {
        const string Module = "MOD";
        const string Identifier = "123";
        private string[] _filters;
        private string[] _excludedFiles;

        [Fact]
        public void TestExecuteShouldRegisterDecoraterToFactory()
        {
            // arrange
            _filters = new string[0];
            _excludedFiles = new string[0];

            // assume
            var before = InstrumenterFactory.Create(Module, Identifier, _filters, _excludedFiles);
            Assert.IsNotType<InstrumenterLoggerDecorator>(before);

            // act
            EnableLogging.Execute();

            // assert
            var after = InstrumenterFactory.Create(Module, Identifier, _filters, _excludedFiles);
            Assert.IsType<InstrumenterLoggerDecorator>(after);
        }
    }
}