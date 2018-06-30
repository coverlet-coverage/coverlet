using System;
using System.IO;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace Coverlet.Core.Helpers.Tests
{
    public class InstrumentationHelperTests
    {
        [Fact]
        public void TestGetDependencies()
        {
            string module = typeof(InstrumentationHelperTests).Assembly.Location;
            var modules = InstrumentationHelper.GetCoverableModules(module);
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

            Assert.True(File.Exists(Path.Combine(directory.FullName, "coverlet.tracker.dll")));
            Directory.Delete(directory.FullName, true);
        }

        [Fact]
        public void TestIsValidFilterExpression()
        {
            Assert.True(InstrumentationHelper.IsValidFilterExpression("[*]*"));
            Assert.True(InstrumentationHelper.IsValidFilterExpression("[*]*core"));
            Assert.True(InstrumentationHelper.IsValidFilterExpression("[assembly]*"));
            Assert.True(InstrumentationHelper.IsValidFilterExpression("[*]type"));
            Assert.True(InstrumentationHelper.IsValidFilterExpression("[assembly]type"));
            Assert.False(InstrumentationHelper.IsValidFilterExpression("[*]"));
            Assert.False(InstrumentationHelper.IsValidFilterExpression("[-]*"));
            Assert.False(InstrumentationHelper.IsValidFilterExpression("*"));
            Assert.False(InstrumentationHelper.IsValidFilterExpression("]["));
            Assert.False(InstrumentationHelper.IsValidFilterExpression(null));
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
        public void TestDeleteHitsFile()
        {
            var tempFile = Path.GetTempFileName();
            Assert.True(File.Exists(tempFile));

            InstrumentationHelper.DeleteHitsFile(tempFile);
            Assert.False(File.Exists(tempFile));
        }


        public static IEnumerable<object[]> GetExcludedFilesReturnsEmptyArgs =>
        new[]
        {
            new object[]{null},
            new object[]{new string[0]},
            new object[]{new string[]{ Path.GetRandomFileName() }},
            new object[]{new string[]{Path.GetRandomFileName(),
                Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName())}
            }
        };

        [Theory]
        [MemberData(nameof(GetExcludedFilesReturnsEmptyArgs))]
        public void TestGetExcludedFilesReturnsEmpty(string[] excludedFiles)
        {
            Assert.False(InstrumentationHelper.GetExcludedFiles(excludedFiles)?.Any());
        }

        [Fact]
        public void TestGetExcludedFilesUsingAbsFile()
        {
            var file = Path.GetRandomFileName();
            File.Create(file).Dispose();
            var excludedFiles = InstrumentationHelper.GetExcludedFiles(
                new string[] { Path.Combine(Directory.GetCurrentDirectory(), file) }
            );
            File.Delete(file);
            Assert.Single(excludedFiles);
        }

        [Fact]
        public void TestGetExcludedFilesUsingGlobbing()
        {
            var fileExtension = Path.GetRandomFileName();
            var paths = new string[]{
                $"{Path.GetRandomFileName()}.{fileExtension}",
                $"{Path.GetRandomFileName()}.{fileExtension}"
            };

            foreach (var path in paths)
            {
                File.Create(path).Dispose();
            }

            var excludedFiles = InstrumentationHelper.GetExcludedFiles(
                new string[] { $"*.{fileExtension}" });

            foreach (var path in paths)
            {
                File.Delete(path);
            }

            Assert.Equal(paths.Length, excludedFiles.Count());
        }

        [Fact]
        public void TestIsModuleExcludedWithoutFilter()
        {
            var result = InstrumentationHelper.IsModuleExcluded("Module.dll", new string[0]);

            Assert.False(result);
        }

        [Theory]
        [InlineData("[Module]mismatch")]
        [InlineData("[Mismatch]*")]
        public void TestIsModuleExcludedWithSingleMismatchFilter(string filter)
        {
            var result = InstrumentationHelper.IsModuleExcluded("Module.dll", new[] { filter });

            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleFilterData))]
        public void TestIsModuleExcludedWithFilter(string filter)
        {
            var result = InstrumentationHelper.IsModuleExcluded("Module.dll", new[] { filter });

            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleFilterData))]
        public void TestIsModuleExcludedWithMatchingAndMismatchingFilter(string filter)
        {
            var filters = new[] { "[Mismatch]*", filter, "[Mismatch]*" };

            var result = InstrumentationHelper.IsModuleExcluded("Module.dll", filters);

            Assert.True(result);
        }

        [Fact]
        public void TestIsTypeExcludedWithoutFilter()
        {
            var result = InstrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new string[0]);

            Assert.False(result);
        }

        [Theory]
        [InlineData("[Module]mismatch")]
        [InlineData("[Mismatch]*")]
        [InlineData("[Mismatch]a.b.Dto")]
        public void TestIsTypeExcludedWithSingleMismatchFilter(string filter)
        {
            var result = InstrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new[] { filter });

            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleAndNamespaceFilterData))]
        public void TestIsTypeExcludedWithFilter(string filter)
        {
            var result = InstrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new[] { filter });

            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleAndNamespaceFilterData))]
        public void TestIsTypeExcludedWithMatchingAndMismatchingFilter(string filter)
        {
            var filters = new[] { "[Mismatch]*", filter, "[Mismatch]*" };

            var result = InstrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", filters);

            Assert.True(result);
        }

        public static IEnumerable<object[]> ValidModuleFilterData =>
            new List<object[]>
                {
                    new object[] { "[Module]*" },
                    new object[] { "[Module*]*" },
                    new object[] { "[Mod*ule]*" },
                    new object[] { "[M*e]*" },
                    new object[] { "[Mod*le*]*" },
                    new object[] { "[Module?]*" },
                    new object[] { "[ModuleX?]*" },
                };

        public static IEnumerable<object[]> ValidModuleAndNamespaceFilterData =>
            new List<object[]>
                    {
                        new object[] { "[Module]a.b.Dto" },
                        new object[] { "[Module]a.b.Dtos?" },
                        new object[] { "[Module]a.*" },
                        new object[] { "[Module]a*" },
                        new object[] { "[Module]*b.*" },
                    }
                .Concat(ValidModuleFilterData);
    }
}


