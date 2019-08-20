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
            var modules = InstrumentationHelper.GetCoverableModules(module, Array.Empty<string>(), false);
            Assert.False(Array.Exists(modules, m => m == module));
        }

        [Fact]
        public void TestGetDependenciesWithTestAssembly()
        {
            string module = typeof(InstrumentationHelperTests).Assembly.Location;
            var modules = InstrumentationHelper.GetCoverableModules(module, Array.Empty<string>(), true);
            Assert.True(Array.Exists(modules, m => m == module));
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
        public void TestDeleteHitsFile()
        {
            var tempFile = Path.GetTempFileName();
            Assert.True(File.Exists(tempFile));

            InstrumentationHelper.DeleteHitsFile(tempFile);
            Assert.False(File.Exists(tempFile));
        }

        [Fact]
        public void TestIsModuleExcludedWithoutFilter()
        {
            var result = InstrumentationHelper.IsModuleExcluded("Module.dll", new string[0]);

            Assert.False(result);
        }

        [Fact]
        public void TestIsModuleIncludedWithoutFilter()
        {
            var result = InstrumentationHelper.IsModuleIncluded("Module.dll", new string[0]);

            Assert.True(result);
        }

        [Theory]
        [InlineData("[Module]mismatch")]
        [InlineData("[Mismatch]*")]
        public void TestIsModuleExcludedWithSingleMismatchFilter(string filter)
        {
            var result = InstrumentationHelper.IsModuleExcluded("Module.dll", new[] { filter });

            Assert.False(result);
        }

        [Fact]
        public void TestIsModuleIncludedWithSingleMismatchFilter()
        {
            var result = InstrumentationHelper.IsModuleIncluded("Module.dll", new[] { "[Mismatch]*" });

            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleFilterData))]
        public void TestIsModuleExcludedAndIncludedWithFilter(string filter)
        {
            var result = InstrumentationHelper.IsModuleExcluded("Module.dll", new[] { filter });
            Assert.True(result);

            result = InstrumentationHelper.IsModuleIncluded("Module.dll", new[] { filter });
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleFilterData))]
        public void TestIsModuleExcludedAndIncludedWithMatchingAndMismatchingFilter(string filter)
        {
            var filters = new[] { "[Mismatch]*", filter, "[Mismatch]*" };

            var result = InstrumentationHelper.IsModuleExcluded("Module.dll", filters);
            Assert.True(result);

            result = InstrumentationHelper.IsModuleIncluded("Module.dll", filters);
            Assert.True(result);
        }

        [Fact]
        public void TestIsTypeExcludedWithoutFilter()
        {
            var result = InstrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new string[0]);

            Assert.False(result);
        }

        [Fact]
        public void TestIsTypeIncludedWithoutFilter()
        {
            var result = InstrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", new string[0]);

            Assert.True(result);
        }

        [Theory]
        [InlineData("[Module]mismatch")]
        [InlineData("[Mismatch]*")]
        [InlineData("[Mismatch]a.b.Dto")]
        public void TestIsTypeExcludedAndIncludedWithSingleMismatchFilter(string filter)
        {
            var result = InstrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new[] { filter });
            Assert.False(result);

            result = InstrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", new[] { filter });
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleAndNamespaceFilterData))]
        public void TestIsTypeExcludedAndIncludedWithFilter(string filter)
        {
            var result = InstrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new[] { filter });
            Assert.True(result);

            result = InstrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", new[] { filter });
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(ValidModuleAndNamespaceFilterData))]
        public void TestIsTypeExcludedAndIncludedWithMatchingAndMismatchingFilter(string filter)
        {
            var filters = new[] { "[Mismatch]*", filter, "[Mismatch]*" };

            var result = InstrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", filters);
            Assert.True(result);

            result = InstrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", filters);
            Assert.True(result);
        }

        [Fact]
        public void TestIncludeDirectories()
        {
            string module = typeof(InstrumentationHelperTests).Assembly.Location;

            var currentDirModules = InstrumentationHelper.GetCoverableModules(module,
                new[] { Environment.CurrentDirectory }, false);

            var parentDirWildcardModules = InstrumentationHelper.GetCoverableModules(module,
                new[] { Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, "*") }, false);

            // There are at least as many modules found when searching the parent directory's subdirectories
            Assert.True(parentDirWildcardModules.Length >= currentDirModules.Length);

            var relativePathModules = InstrumentationHelper.GetCoverableModules(module,
                new[] { "." }, false);

            // Same number of modules found when using a relative path
            Assert.Equal(currentDirModules.Length, relativePathModules.Length);
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


