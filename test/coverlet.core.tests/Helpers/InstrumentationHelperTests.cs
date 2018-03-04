using System.IO;

using Xunit;
using Coverlet.Core.Helpers;

namespace Coverlet.Core.Helpers.Tests
{
    public class InstrumentationHelperTests
    {
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
            InstrumentationHelper.CopyCoverletDependency(Path.Combine(directory.FullName, "somemodule.dll"));

            Assert.True(File.Exists(Path.Combine(directory.FullName, "coverlet.core.dll")));
            Directory.Delete(directory.FullName, true);
        }

        [Fact]
        public void TestDontCopyCoverletDependency()
        {
            var tempPath = Path.GetTempPath();
            var directory = Directory.CreateDirectory(Path.Combine(tempPath, "tempdir"));
            InstrumentationHelper.CopyCoverletDependency(Path.Combine(directory.FullName, "coverlet.core.dll"));

            Assert.False(File.Exists(Path.Combine(directory.FullName, "coverlet.core.dll")));
            Directory.Delete(directory.FullName, true);
        }
    }
}