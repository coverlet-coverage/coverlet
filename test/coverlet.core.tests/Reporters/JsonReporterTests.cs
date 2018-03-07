using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class JsonReporterTests
    {
        [Fact]
        public void TestFormat()
        {
            JsonReporter reporter = new JsonReporter();
            CoverageResult result = new CoverageResult { Modules = new Modules() };

            Assert.Equal("{\n}", reporter.Format(result));
        }
    }
}