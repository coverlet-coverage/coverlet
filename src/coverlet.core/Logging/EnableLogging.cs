using coverlet.core.Logging.Decorators;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Logging;

namespace coverlet.core.Logging
{
    public static class EnableLogging
    {
        public static void Execute()
        {
            InstrumenterFactory.RegisterDecorator(instrumenter => new InstrumenterLoggerDecorator(instrumenter, LoggerFactory.GetLogger()));
        }
    }
}