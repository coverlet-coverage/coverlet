using Coverlet.Core.Logging;
using Xunit;

namespace Coverlet.Core.Tests.Logging
{
    public class LoggerFactoryTests
    {
        [Fact]
        public void TestGetLoggerReturnsConsoleLoggerInstance()
        {
            // arrange

            // act
            var result = LoggerFactory.GetLogger();

            // assert
            Assert.Equal(ConsoleLogger.Instance, result);
        }
    }
}