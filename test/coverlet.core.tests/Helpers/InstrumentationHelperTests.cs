using System;
using System.IO;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace Coverlet.Core.Helpers.Tests
{
    public class InstrumentationHelperTests
    {
        private readonly InstrumentationHelper _instrumentationHelper = new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem());

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
            var result = _instrumentationHelper.IsTypeExcluded("Module.dll", "Namespace.Namespace.Type", new string[]{ "[Module]Namespace.Namespace.*" });
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

            var currentDirModules = _instrumentationHelper.GetCoverableModules(module,
                new[] { Environment.CurrentDirectory }, false)
                .Where(m => !m.StartsWith("testgen_")).ToArray();

            var parentDirWildcardModules = _instrumentationHelper.GetCoverableModules(module,
                new[] { Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, "*") }, false)
                .Where(m => !m.StartsWith("testgen_")).ToArray();

            // There are at least as many modules found when searching the parent directory's subdirectories
            Assert.True(parentDirWildcardModules.Length >= currentDirModules.Length);

            var relativePathModules = _instrumentationHelper.GetCoverableModules(module,
                new[] { "." }, false)
                .Where(m => !m.StartsWith("testgen_")).ToArray();

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


