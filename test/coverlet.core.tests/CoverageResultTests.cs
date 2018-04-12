using Xunit;
using Moq;

using Coverlet.Core;
using Coverlet.Core.Reporters;

namespace Coverlet.Core.Tests
{
    public class CoverageResultTests
    {
        [Fact]
        public void TestFormat()
        {
            var mock = new Mock<IReporter>();

            CoverageResult coverageResult = new CoverageResult();
            mock.Object.Report(coverageResult);
            mock.Verify(m => m.Report(coverageResult));
        }
    }
}