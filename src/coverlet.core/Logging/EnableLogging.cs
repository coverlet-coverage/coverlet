using coverlet.core.Logging.Decorators;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Logging;

namespace coverlet.core.Logging
{
    public static class EnableLogging
    {
        public static void Execute()
        {
            LoggerFactory.GetLogger().Log("Logging enabled.");
            InstrumenterFactory.RegisterDecorator(instrumenter => new InstrumenterLoggerDecorator(instrumenter, LoggerFactory.GetLogger()));
        }
    }
}