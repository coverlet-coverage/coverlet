using Coverlet.Core.Instrumentation;
using Coverlet.Core.Logging;

namespace coverlet.core.Logging.Decorators
{
    internal class InstrumenterLoggerDecorator : IInstrumenter
    {
        private readonly IInstrumenter _decoratee;
        private readonly ILogger _logger;

        public InstrumenterLoggerDecorator(IInstrumenter decoratee, ILogger logger)
        {
            _decoratee = decoratee;
            _logger = logger;
        }

        public bool CanInstrument()
        {
            return _decoratee.CanInstrument();
        }

        public InstrumenterResult Instrument()
        {
            var result = _decoratee.Instrument();
            _logger.Log($"Module {result.Module} was instrumented.");
            return result;
        }
    }
}