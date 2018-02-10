using System.IO;

using Xunit;
using Coverlet.Core.Helpers;

namespace Coverlet.Core.Helpers.Tests
{
    public class InstrumentationHelperTests
    {
        [Fact]
        public void TestGetCoverableModules()
        {
            var coverable = InstrumentationHelper.GetCoverableModules(typeof(InstrumentationHelperTests).Assembly.Location);
            Assert.Single(coverable);
            Assert.Equal(typeof(InstrumentationHelper).Assembly.Location, coverable[0]);
        }

        [Fact]
        public void TestHasPdb()
        {
            Assert.True(InstrumentationHelper.HasPdb(typeof(InstrumentationHelperTests).Assembly.Location));
        }

        [Fact]
        public void TestCopyCoverletDependency()
        {
            var tempPath = Path.GetTempPath();
            var directory = Directory.CreateDirectory(Path.Combine(tempPath, "tempdir"));
            InstrumentationHelper.CopyCoverletDependency(directory.FullName);

            Assert.True(File.Exists(Path.Combine(directory.FullName, "coverlet.core.dll")));
        }
    }
}