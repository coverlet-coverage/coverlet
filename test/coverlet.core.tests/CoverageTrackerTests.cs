using System.IO;

using Xunit;
using Coverlet.Core;

namespace Coverlet.Core.Tests
{
    public class CoverageTrackerTests
    {
        [Fact]
        public void TestMarkExecuted()
        {
            string path = Path.Combine(Path.GetTempPath(), "testfile");
            string marker = "this.is.a.marker";

            CoverageTracker.MarkExecuted(path, marker);
            Assert.Equal(marker, File.ReadAllLines(path)[0]);
            File.Delete(path);
        }
    }
}