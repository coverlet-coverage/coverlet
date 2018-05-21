namespace Coverlet.Core.Logging
{
    public static class LoggerFactory
    {
        public static ILogger GetLogger()
        {
            return ConsoleLogger.Instance;
        }
    }
}