using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class LcovReporterTests
    {
        [Fact]
        public void TestFormat()
        {
            LcovReporter reporter = new LcovReporter();
            CoverageResult result = new CoverageResult { Modules = new Modules() };

            Assert.Equal(string.Empty, reporter.Format(result));
        }
    }
}