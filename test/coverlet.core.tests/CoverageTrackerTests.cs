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

            if (File.Exists(path))
                File.Delete(path);

            CoverageTracker.MarkExecuted(path, marker);
            Assert.Equal(marker, File.ReadAllLines(path)[0]);
        }
    }
}