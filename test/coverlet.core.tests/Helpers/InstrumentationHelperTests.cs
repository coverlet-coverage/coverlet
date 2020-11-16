using System;
using System.IO;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Helpers.Tests
{
    public class InstrumentationHelperTests
    {
        private readonly InstrumentationHelper _instrumentationHelper =
            new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object, new SourceRootTranslator(typeof(InstrumentationHelperTests).Assembly.Location, new Mock<ILogger>().Object, new FileSystem()));

        [Fact]
        public void TestGetDependencies()
        {
            string module = typeof(InstrumentationHelperTests).Assembly.Location;
            var modules = _instrumentationHelper.GetCoverableModules(module, Array.Empty<string>(), false);
            Assert.False(Array.Exists(modules, m => m == module));
        }

        [Fact]
        public void TestGetDependenciesWithTestAssembly()
        {
            string module = typeof(InstrumentationHelperTests).Assembly.Location;
            var modules = _instrumentationHelper.GetCoverableModules(module, Array.Empty<string>(), true);
            Assert.True(Array.Exists(modules, m => m == module));
        }

        [Fact]
        public void TestHasPdb()
        {
            Assert.True(_instrumentationHelper.HasPdb(typeof(InstrumentationHelperTests).Assembly.Location, out bool embeddedPdb));
            Assert.False(embeddedPdb);
        }

        [Fact]
        public void TestBackupOriginalModule()
        {
            string module = typeof(InstrumentationHelperTests).Assembly.Location;
            string identifier = Guid.NewGuid().ToString();

            _instrumentationHelper.BackupOriginalModule(module, identifier);

            var backupPath = Path.Combine(
                Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(module) + "_" + identifier + ".dll"
            );

            Assert.True(File.Exists(backupPath));
        }

        [Fact]
        public void TestIsValidFilterExpression()
        {
            Assert.True(_instrumentationHelper.IsValidFilterExpression("[*]*"));
            Assert.True(_instrumentationHelper.IsValidFilterExpression("[*]*core"));
            Assert.True(_instrumentationHelper.IsValidFilterExpression("[assembly]*"));
            Assert.True(_instrumentationHelper.IsValidFilterExpression("[*]type"));
            Assert.True(_instrumentationHelper.IsValidFilterExpression("[assembly]type"));
            Assert.False(_instrumentationHelper.IsValidFilterExpression("[*]"));
            Assert.False(_instrumentationHelper.IsValidFilterExpression("[-]*"));
            Assert.False(_instrumentationHelper.IsValidFilterExpression("*"));
            Assert.False(_instrumentationHelper.IsValidFilterExpression("]["));
            Assert.False(_instrumentationHelper.IsValidFilterExpression(null));
        }

        [Fact]
        public void TestDeleteHitsFile()
        {
            var tempFile = Path.GetTempFileName();
            Assert.True(File.Exists(tempFile));

            _instrumentationHelper.DeleteHitsFile(tempFile);
            Assert.False(File.Exists(tempFile));
        }

        [Fact]
        public void TestIsModuleExcludedWithoutFilter()
        {
            var result = _instrumentationHelper.IsModuleExcluded("Module.dll", new string[0]);

            Assert.False(result);
        }

        [Fact]
        public void TestIsModuleIncludedWithoutFilter()
        {
            var result = _instrumentationHelper.IsModuleIncluded("Module.dll", new string[0]);

            Assert.True(result);
        }

        [Theory]
        [InlineData("[Module]mismatch")]
        [InlineData("[Mismatch]*")]
        public void TestIsModuleExcludedWithSingleMismatchFilter(string filter)
        {
            var result = _instrumentationHelper.IsModuleExcluded("Module.dll", new[] { filter });

            Assert.False(result);
        }

        [Fact]
        public void TestIsModuleIncludedWithSingleMismatchFilter()
        {
            var result = _instrumentationHelper.IsModuleIncluded("Module.dll", new[] { "[Mismatch]*" });

            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleFilterData))]
        public void TestIsModuleExcludedAndIncludedWithFilter(string filter)
        {
            var result = _instrumentationHelper.IsModuleExcluded("Module.dll", new[] { filter });
            Assert.True(result);

            result = _instrumentationHelper.IsModuleIncluded("Module.dll", new[] { filter });
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleFilterData))]
        public void TestIsModuleExcludedAndIncludedWithMatchingAndMismatchingFilter(string filter)
        {
            var filters = new[] { "[Mismatch]*", filter, "[Mismatch]*" };

            var result = _instrumentationHelper.IsModuleExcluded("Module.dll", filters);
            Assert.True(result);

            result = _instrumentationHelper.IsModuleIncluded("Module.dll", filters);
            Assert.True(result);
        }

        [Fact]
        public void TestIsTypeExcludedWithoutFilter()
        {
            var result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new string[0]);

            Assert.False(result);
        }

        [Fact]
        public void TestIsTypeExcludedNamespace()
        {
            var result = _instrumentationHelper.IsTypeExcluded("Module.dll", "Namespace.Namespace.Type", new string[] { "[Module]Namespace.Namespace.*" });
            Assert.True(result);

            result = _instrumentationHelper.IsTypeExcluded("Module.dll", "Namespace.Namespace.TypeB", new string[] { "[Module]Namespace.Namespace.*" });
            Assert.True(result);

            result = _instrumentationHelper.IsTypeExcluded("Module.dll", "Namespace.Namespace.Type", new string[] { "[Module]Namespace.*" });
            Assert.True(result);

            result = _instrumentationHelper.IsTypeExcluded("Module.dll", "Namespace.Namespace.Type", new string[] { "[Module]Namespace.WrongNamespace.*" });
            Assert.False(result);
        }

        [Fact]
        public void TestIsTypeIncludedWithoutFilter()
        {
            var result = _instrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", new string[0]);

            Assert.True(result);
        }

        [Theory]
        [InlineData("[Module]mismatch")]
        [InlineData("[Mismatch]*")]
        [InlineData("[Mismatch]a.b.Dto")]
        public void TestIsTypeExcludedAndIncludedWithSingleMismatchFilter(string filter)
        {
            var result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new[] { filter });
            Assert.False(result);

            result = _instrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", new[] { filter });
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleAndNamespaceFilterData))]
        public void TestIsTypeExcludedAndIncludedWithFilter(string filter)
        {
            var result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new[] { filter });
            Assert.True(result);

            result = _instrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", new[] { filter });
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleAndNamespaceFilterData))]
        public void TestIsTypeExcludedAndIncludedWithMatchingAndMismatchingFilter(string filter)
        {
            var filters = new[] { "[Mismatch]*", filter, "[Mismatch]*" };

            var result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", filters);
            Assert.True(result);

            result = _instrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", filters);
            Assert.True(result);
        }

        [Fact]
        public void TestIncludeDirectories()
        {
            string module = typeof(InstrumentationHelperTests).Assembly.Location;
            DirectoryInfo newDir = Directory.CreateDirectory("TestIncludeDirectories");
            newDir.Delete(true);
            newDir.Create();
            DirectoryInfo newDir2 = Directory.CreateDirectory("TestIncludeDirectories2");
            newDir2.Delete(true);
            newDir2.Create();

            File.Copy(module, Path.Combine(newDir.FullName, Path.GetFileName(module)));
            module = Path.Combine(newDir.FullName, Path.GetFileName(module));
            File.Copy("coverlet.msbuild.tasks.dll", Path.Combine(newDir.FullName, "coverlet.msbuild.tasks.dll"));
            File.Copy("coverlet.core.dll", Path.Combine(newDir2.FullName, "coverlet.core.dll"));

            var currentDirModules = _instrumentationHelper.GetCoverableModules(module, Array.Empty<string>(), false);
            Assert.Single(currentDirModules);
            Assert.Equal("coverlet.msbuild.tasks.dll", Path.GetFileName(currentDirModules[0]));

            var moreThanOneDirectory = _instrumentationHelper
                                       .GetCoverableModules(module, new string[] { newDir2.FullName }, false)
                                       .OrderBy(f => f).ToArray();

            Assert.Equal(2, moreThanOneDirectory.Length);
            Assert.Equal("coverlet.msbuild.tasks.dll", Path.GetFileName(moreThanOneDirectory[0]));
            Assert.Equal("coverlet.core.dll", Path.GetFileName(moreThanOneDirectory[1]));

            var moreThanOneDirectoryPlusTestAssembly = _instrumentationHelper
                                                       .GetCoverableModules(module, new string[] { newDir2.FullName }, true)
                                                       .OrderBy(f => f).ToArray();

            Assert.Equal(3, moreThanOneDirectoryPlusTestAssembly.Length);
            Assert.Equal("coverlet.core.tests.dll", Path.GetFileName(moreThanOneDirectoryPlusTestAssembly[0]));
            Assert.Equal("coverlet.msbuild.tasks.dll", Path.GetFileName(moreThanOneDirectoryPlusTestAssembly[1]));
            Assert.Equal("coverlet.core.dll", Path.GetFileName(moreThanOneDirectoryPlusTestAssembly[2]));

            newDir.Delete(true);
            newDir2.Delete(true);
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


