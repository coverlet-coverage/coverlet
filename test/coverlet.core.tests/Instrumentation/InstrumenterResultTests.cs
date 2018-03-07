
using Xunit;
using Coverlet.Core.Instrumentation;

namespace Coverlet.Core.Instrumentation.Tests
{
    public class InstrumenterResultTests
    {
        [Fact]
        public void TestEnsureDocumentsPropertyNotNull()
        {
            InstrumenterResult result = new InstrumenterResult();
            Assert.NotNull(result.Documents);
        }

        [Fact]
        public void TestEnsureLinesPropertyNotNull()
        {
            Document document = new Document();
            Assert.NotNull(document.Lines);
        }
    }
}