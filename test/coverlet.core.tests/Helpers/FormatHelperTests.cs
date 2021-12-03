using System.Globalization;
using System.Threading;
using Xunit;

namespace Coverlet.Core.Helpers.Tests
{
    public class FormatHelperTests
    {
        [Theory]
        [InlineData(2.2d, "2.2")]
        public void TestInvariantFormattableString(double number, string expected)
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            
            var actual = new FormatHelper().Invariant($"{number}");

            Assert.Equal(expected, actual);
            Thread.CurrentThread.CurrentCulture = currentCulture;
        }
    }
}
