using System;
using System.IO;

using Xunit;
using Coverlet.Core.Helpers;

namespace Coverlet.Core.Helpers.Tests
{
    public class InstrumentationHelperTests
    {
        [Fact]
        public void TestGetDependencies()
        {
            string module = typeof(InstrumentationHelperTests).Assembly.Location;
            var modules = InstrumentationHelper.GetDependencies(module);
            Assert.False(Array.Exists(modules, m => m == module));
        }

        [Fact]
        public void TestHasPdb()
        {
            Assert.True(InstrumentationHelper.HasPdb(typeof(InstrumentationHelperTests).Assembly.Location));
        }

        [Fact]
        public void TestBackupOriginalModule()
        {
            string module = typeof(InstrumentationHelperTests).Assembly.Location;
            string identifier = Guid.NewGuid().ToString();

            InstrumentationHelper.BackupOriginalModule(module, identifier);

            var backupPath = Path.Combine(
                Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(module) + "_" + identifier + ".dll"
            );

            Assert.True(File.Exists(backupPath));
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

        [Fact]
        public void TestReadHitsFile()
        {
            var tempFile = Path.GetTempFileName();
            Assert.True(File.Exists(tempFile));

            var lines = InstrumentationHelper.ReadHitsFile(tempFile);
            Assert.NotNull(lines);
        }

        [Fact]
        public void TestDeleteHitsFile()
        {
            var tempFile = Path.GetTempFileName();
            Assert.True(File.Exists(tempFile));

            InstrumentationHelper.DeleteHitsFile(tempFile);
            Assert.False(File.Exists(tempFile));
        }
    }
}