using System.Diagnostics.CodeAnalysis;
using Xunit;
using Moq;

using Coverlet.Core;
using Coverlet.Core.Reporters;

namespace Coverlet.Core.Tests
{
    [ExcludeFromCodeCoverage]
    public class CoverageResultTests
    {
        [Fact]
        public void TestFormat()
        {
            var mock = new Mock<IReporter>();

            CoverageResult coverageResult = new CoverageResult();
            coverageResult.Format(mock.Object);

            mock.Verify(m => m.Format(coverageResult));
        }
    }
}