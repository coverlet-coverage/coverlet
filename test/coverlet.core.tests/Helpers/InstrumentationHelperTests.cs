// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Enums;
using Moq;
using Xunit;

namespace Coverlet.Core.Helpers.Tests
{
  public class InstrumentationHelperTests
  {
    private readonly InstrumentationHelper _instrumentationHelper =
        new(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object, new SourceRootTranslator(typeof(InstrumentationHelperTests).Assembly.Location, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

    [Fact]
    public void TestGetDependencies()
    {
      string module = typeof(InstrumentationHelperTests).Assembly.Location;
      string[] modules = _instrumentationHelper.GetCoverableModules(module, Array.Empty<string>(), false);
      Assert.False(Array.Exists(modules, m => m == module));
    }

    [Fact]
    public void TestGetDependenciesWithTestAssembly()
    {
      string module = typeof(InstrumentationHelperTests).Assembly.Location;
      string[] modules = _instrumentationHelper.GetCoverableModules(module, Array.Empty<string>(), true);
      Assert.True(Array.Exists(modules, m => m == module));
    }

    [Fact]
    public void EmbeddedPortablePDPHasLocalSource_NoDocumentsExist_ReturnsFalse()
    {
      var fileSystem = new Mock<FileSystem> { CallBase = true };
      fileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), fileSystem.Object, new Mock<ILogger>().Object, new SourceRootTranslator(typeof(InstrumentationHelperTests).Assembly.Location, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      Assert.False(instrumentationHelper.PortablePdbHasLocalSource(typeof(InstrumentationHelperTests).Assembly.Location, AssemblySearchType.MissingAny));
      Assert.False(instrumentationHelper.PortablePdbHasLocalSource(typeof(InstrumentationHelperTests).Assembly.Location, AssemblySearchType.MissingAll));
    }

    [Fact]
    public void EmbeddedPortablePDPHasLocalSource_AllDocumentsExist_ReturnsTrue()
    {
      Assert.True(_instrumentationHelper.PortablePdbHasLocalSource(typeof(InstrumentationHelperTests).Assembly.Location, AssemblySearchType.MissingAny));
      Assert.True(_instrumentationHelper.PortablePdbHasLocalSource(typeof(InstrumentationHelperTests).Assembly.Location, AssemblySearchType.MissingAll));
    }

    [Theory]
    [InlineData(AssemblySearchType.MissingAny, false)]
    [InlineData(AssemblySearchType.MissingAll, true)]
    public void EmbeddedPortablePDPHasLocalSource_FirstDocumentDoesNotExist_ReturnsExpectedValue(object assemblySearchType, bool result)
    {
      var fileSystem = new Mock<FileSystem> { CallBase = true };
      fileSystem.SetupSequence(x => x.Exists(It.IsAny<string>()))
          .Returns(false)
          .Returns(() =>
          {
            fileSystem.Setup(y => y.Exists(It.IsAny<string>())).Returns(true);
            return true;
          });

      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), fileSystem.Object, new Mock<ILogger>().Object, new SourceRootTranslator(typeof(InstrumentationHelperTests).Assembly.Location, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      Assert.Equal(result, instrumentationHelper.PortablePdbHasLocalSource(typeof(InstrumentationHelperTests).Assembly.Location, (AssemblySearchType)assemblySearchType));
    }

    [Fact]
    public void TestHasPdbOfLocalAssembly()
    {
      Assert.True(_instrumentationHelper.HasPdb(typeof(InstrumentationHelperTests).Assembly.Location, out bool embeddedPdb));
      Assert.False(embeddedPdb);
    }

    [Fact]
    public void TestHasPdbOfExternalAssembly()
    {
      string testAssemblyLocation = GetType().Assembly.Location;

      string externalAssemblyFileName = Path.Combine(
          Path.GetDirectoryName(testAssemblyLocation),
          "TestAssets",
          "75d9f96508d74def860a568f426ea4a4.dll"
      );

      Assert.True(_instrumentationHelper.HasPdb(externalAssemblyFileName, out bool embeddedPdb));
      Assert.False(embeddedPdb);
    }

    [Fact]
    public void TestBackupOriginalModule()
    {
      string module = typeof(InstrumentationHelperTests).Assembly.Location;
      string identifier = Guid.NewGuid().ToString();

      _instrumentationHelper.BackupOriginalModule(module, identifier);

      string backupPath = Path.Combine(
          Path.GetTempPath(),
          Path.GetFileNameWithoutExtension(module) + "_" + identifier + ".dll"
      );

      Assert.True(File.Exists(backupPath));
    }

    [Theory]
    [InlineData("[*]*")]
    [InlineData("[*]*core")]
    [InlineData("[assembly]*")]
    [InlineData("[*]type")]
    [InlineData("[assembly]type")]
    [InlineData("[coverlet.*.tests?]*")]
    [InlineData("[*]Coverlet.Core*")]
    [InlineData("[coverlet.*]*")]
    public void TestIsValidFilterExpression(string pattern)
    {
      Assert.True(_instrumentationHelper.IsValidFilterExpression(pattern));
    }

    [Theory]
    [InlineData("[*]")]
    [InlineData("[-]*")]
    [InlineData("*")]
    [InlineData("][")]
    [InlineData(null)]
    public void TestInValidFilterExpression(string pattern)
    {
      Assert.False(_instrumentationHelper.IsValidFilterExpression(pattern));
    }

    [Fact]
    public void TestDeleteHitsFile()
    {
      string tempFile = Path.GetTempFileName();
      Assert.True(File.Exists(tempFile));

      _instrumentationHelper.DeleteHitsFile(tempFile);
      Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public void TestSelectModulesWithoutIncludeAndExcludedFilters()
    {
      string[] modules = new [] {"Module.dll"};
      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, new string[0], new string[0]);

      Assert.Equal(modules, result);
    }

    [Theory]
    [InlineData("[Module]mismatch")]
    [InlineData("[Mismatch]*")]
    public void TestIsModuleExcludedWithSingleMismatchFilter(string filter)
    {
      string[] modules = new [] {"Module.dll"};
      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, new string[0], new[] {filter});

      Assert.Equal(modules, result);
    }

    [Fact]
    public void TestIsModuleIncludedWithSingleMismatchFilter()
    {
      string[] modules = new [] {"Module.dll"};
      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, new[] { "[Mismatch]*" }, new string[0]);

      Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(ValidModuleFilterData))]
    public void TestIsModuleExcludedAndIncludedWithFilter(string filter)
    {
      string[] modules = new [] {"Module.dll"};
      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, new[] { filter }, new[] { filter });

      Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(ValidModuleFilterData))]
    public void TestIsModuleExcludedAndIncludedWithMatchingAndMismatchingFilter(string filter)
    {
      string[] modules = new[] {"Module.dll"};
      string[] filters = new[] {"[Mismatch]*", filter, "[Mismatch]*"};

      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, filters, filters);

      Assert.Empty(result);
    }

    [Fact]
    public void TestIsTypeExcludedWithoutFilter()
    {
      bool result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new string[0]);

      Assert.False(result);
    }

    [Fact]
    public void TestIsTypeExcludedNamespace()
    {
      bool result = _instrumentationHelper.IsTypeExcluded("Module.dll", "Namespace.Namespace.Type", new string[] { "[Module]Namespace.Namespace.*" });
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
      bool result = _instrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", new string[0]);

      Assert.True(result);
    }

    [Theory]
    [InlineData("[Module]mismatch")]
    [InlineData("[Mismatch]*")]
    [InlineData("[Mismatch]a.b.Dto")]
    public void TestIsTypeExcludedAndIncludedWithSingleMismatchFilter(string filter)
    {
      bool result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new[] { filter });
      Assert.False(result);

      result = _instrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", new[] { filter });
      Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(ValidModuleAndNamespaceFilterData))]
    public void TestIsTypeExcludedAndIncludedWithFilter(string filter)
    {
      bool result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", new[] { filter });
      Assert.True(result);

      result = _instrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", new[] { filter });
      Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(ValidModuleAndNamespaceFilterData))]
    public void TestIsTypeExcludedAndIncludedWithMatchingAndMismatchingFilter(string filter)
    {
      string[] filters = new[] { "[Mismatch]*", filter, "[Mismatch]*" };

      bool result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", filters);
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
      File.Copy("coverlet.tests.xunit.extensions.dll", Path.Combine(newDir.FullName, "coverlet.tests.xunit.extensions.dll"));
      File.Copy("coverlet.core.dll", Path.Combine(newDir2.FullName, "coverlet.core.dll"));

      string[] currentDirModules = _instrumentationHelper.GetCoverableModules(module, Array.Empty<string>(), false);
      Assert.Single(currentDirModules);
      Assert.Equal("coverlet.tests.xunit.extensions.dll", Path.GetFileName(currentDirModules[0]));

      string[] moreThanOneDirectory = _instrumentationHelper
                                 .GetCoverableModules(module, new string[] { newDir2.FullName }, false)
                                 .OrderBy(f => f).ToArray();

      Assert.Equal(2, moreThanOneDirectory.Length);
      Assert.Equal("coverlet.tests.xunit.extensions.dll", Path.GetFileName(moreThanOneDirectory[0]));
      Assert.Equal("coverlet.core.dll", Path.GetFileName(moreThanOneDirectory[1]));

      string[] moreThanOneDirectoryPlusTestAssembly = _instrumentationHelper
                                                 .GetCoverableModules(module, new string[] { newDir2.FullName }, true)
                                                 .OrderBy(f => f).ToArray();

      Assert.Equal(3, moreThanOneDirectoryPlusTestAssembly.Length);
      Assert.Equal("coverlet.core.tests.dll", Path.GetFileName(moreThanOneDirectoryPlusTestAssembly[0]));
      Assert.Equal("coverlet.tests.xunit.extensions.dll", Path.GetFileName(moreThanOneDirectoryPlusTestAssembly[1]));
      Assert.Equal("coverlet.core.dll", Path.GetFileName(moreThanOneDirectoryPlusTestAssembly[2]));

      newDir.Delete(true);
      newDir2.Delete(true);
    }

    [Theory]
    [InlineData("<TestMethod>g__LocalFunction|0_0", true)]
    [InlineData("TestMethod", false)]
    public void InstrumentationHelper_IsLocalMethod_ReturnsExpectedResult(string method, bool result)
    {
      Assert.Equal(_instrumentationHelper.IsLocalMethod(method), result);
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
