using System;
using System.Text;
using coverlet.core.Logging.Decorators;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Logging;
using Xunit;

namespace Coverlet.Core.Tests.Logging.Decorators
{
    public class InstrumenterLoggerDecoratorTests
    {
        private readonly FakeInstumenter _decoratee;
        private readonly FakeLogger _logger;
        private readonly InstrumenterLoggerDecorator _sut;

        public InstrumenterLoggerDecoratorTests()
        {
            _decoratee = new FakeInstumenter();
            _logger = new FakeLogger();
            _sut = new InstrumenterLoggerDecorator(_decoratee, _logger);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestCanInstrumentIsForwardedToDecoratee(bool setupCanInstrument)
        {
            // arrange
            var canInstrumentCalled = false;
            _decoratee.SetCanInstrument(() =>
            {
                canInstrumentCalled = true;
                return setupCanInstrument;
            });

            // act
            var result = _sut.CanInstrument();

            // assert
            Assert.True(canInstrumentCalled);
            Assert.Equal(setupCanInstrument, result);
        }


        [Fact]
        public void TestInstrumentIsForwardedToDecorateeAndLoggedInLogger()
        {
            // arrange
            var setupResult = new InstrumenterResult { Module = "m0dule"};
            var instrumentCalled = false;
            _decoratee.SetInstrument(() =>
            {
                instrumentCalled = true;
                return setupResult;
            });

            // act
            var result = _sut.Instrument();

            // assert
            Assert.True(instrumentCalled);
            Assert.Equal(setupResult, result);
            Assert.Equal($"Module {result.Module} was instrumented.{Environment.NewLine}", _logger.ToString());
        }
    }

    public class FakeLogger : ILogger
    {
        private readonly StringBuilder _sb;

        public FakeLogger()
        {
            _sb = new StringBuilder();
        }

        public void Log(string text)
        {
            _sb.AppendLine(text);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }

    internal class FakeInstumenter : IInstrumenter
    {
        private Func<bool> _canInstrument = () => throw new NotImplementedException();
        private Func<InstrumenterResult> _instrumenterResult = () => throw new NotImplementedException();

        public void SetCanInstrument(Func<bool> func)
        {
            _canInstrument = func ?? throw new ArgumentNullException(nameof(func));
        }

        public bool CanInstrument()
        {
            return _canInstrument.Invoke();
        }

        public void SetInstrument(Func<InstrumenterResult> instrumenterResult)
        {
            _instrumenterResult = instrumenterResult ?? throw new ArgumentNullException(nameof(instrumenterResult));
        }

        public InstrumenterResult Instrument()
        {
            return _instrumenterResult.Invoke();
        }

    }
}