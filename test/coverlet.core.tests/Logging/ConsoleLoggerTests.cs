using System;
using System.IO;
using Coverlet.Core.Logging;
using Xunit;

namespace Coverlet.Core.Tests.Logging
{
    public class ConsoleLoggerTests
    {
        private readonly TextWriter _textWriter;

        public ConsoleLoggerTests()
        {
            _textWriter = new StringWriter();
            Console.SetOut(_textWriter);
        }

        [Fact]
        public void TestConsoleLoggerLogShouldWriteMessageToConsole()
        {
            // arrange
            const string message = "this is a test message";

            // act
            ConsoleLogger.Instance.Log(message);

            // assert
            _textWriter.Flush();
            Assert.Equal($"{message}{Environment.NewLine}" , _textWriter.ToString());
        }

        [Fact]
        public void TestConsoleLoggerLogShouldWriteNothingWhenMessageIsNull()
        {
            // arrange

            // act
            ConsoleLogger.Instance.Log(null);

            // assert
            _textWriter.Flush();
            Assert.Empty(_textWriter.ToString());
        }
    }
}