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
      string[] modules = _instrumentationHelper.GetCoverableModules(module, [], false);
      Assert.False(Array.Exists(modules, m => m == module));
    }

    [Fact]
    public void TestGetDependenciesWithTestAssembly()
    {
      string module = typeof(InstrumentationHelperTests).Assembly.Location;
      string[] modules = _instrumentationHelper.GetCoverableModules(module, [], true);
      Assert.True(Array.Exists(modules, m => m == module));
    }

    [Fact]
    public void EmbeddedPortablePDPHasLocalSource_NoDocumentsExist_ReturnsFalse()
    {
      Mock<FileSystem> fileSystem = new() { CallBase = true };
      fileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

      InstrumentationHelper instrumentationHelper =
          new(new ProcessExitHandler(), new RetryHelper(), fileSystem.Object, new Mock<ILogger>().Object, new SourceRootTranslator(typeof(InstrumentationHelperTests).Assembly.Location, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

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
      Mock<FileSystem> fileSystem = new() { CallBase = true };
      fileSystem.SetupSequence(x => x.Exists(It.IsAny<string>()))
          .Returns(false)
          .Returns(() =>
          {
            fileSystem.Setup(y => y.Exists(It.IsAny<string>())).Returns(true);
            return true;
          });

      InstrumentationHelper instrumentationHelper =
          new(new ProcessExitHandler(), new RetryHelper(), fileSystem.Object, new Mock<ILogger>().Object, new SourceRootTranslator(typeof(InstrumentationHelperTests).Assembly.Location, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

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
      string tempFile = Path.GetTempFileName();
      Assert.True(File.Exists(tempFile));

      _instrumentationHelper.DeleteHitsFile(tempFile);
      Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public void TestIsModuleExcludedWithoutFilter()
    {
      bool result = _instrumentationHelper.IsModuleExcluded("Module.dll", []);

      Assert.False(result);
    }

    [Fact]
    public void TestIsModuleIncludedWithoutFilter()
    {
      bool result = _instrumentationHelper.IsModuleIncluded("Module.dll", []);

      Assert.True(result);
    }

    [Theory]
    [InlineData("[Module]mismatch")]
    [InlineData("[Mismatch]*")]
    public void TestIsModuleExcludedWithSingleMismatchFilter(string filter)
    {
      bool result = _instrumentationHelper.IsModuleExcluded("Module.dll", [filter]);

      Assert.False(result);
    }

    [Fact]
    public void TestIsModuleIncludedWithSingleMismatchFilter()
    {
      bool result = _instrumentationHelper.IsModuleIncluded("Module.dll", ["[Mismatch]*"]);

      Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(ValidModuleFilterData))]
    public void TestIsModuleExcludedAndIncludedWithFilter(string filter)
    {
      bool result = _instrumentationHelper.IsModuleExcluded("Module.dll", [filter]);
      Assert.True(result);

      result = _instrumentationHelper.IsModuleIncluded("Module.dll", [filter]);
      Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(ValidModuleFilterData))]
    public void TestIsModuleExcludedAndIncludedWithMatchingAndMismatchingFilter(string filter)
    {
      string[] filters = ["[Mismatch]*", filter, "[Mismatch]*"];

      bool result = _instrumentationHelper.IsModuleExcluded("Module.dll", filters);
      Assert.True(result);

      result = _instrumentationHelper.IsModuleIncluded("Module.dll", filters);
      Assert.True(result);
    }

    [Fact]
    public void TestIsTypeExcludedWithoutFilter()
    {
      bool result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", []);

      Assert.False(result);
    }

    [Fact]
    public void TestIsTypeExcludedNamespace()
    {
      bool result = _instrumentationHelper.IsTypeExcluded("Module.dll", "Namespace.Namespace.Type", ["[Module]Namespace.Namespace.*"]);
      Assert.True(result);

      result = _instrumentationHelper.IsTypeExcluded("Module.dll", "Namespace.Namespace.TypeB", ["[Module]Namespace.Namespace.*"]);
      Assert.True(result);

      result = _instrumentationHelper.IsTypeExcluded("Module.dll", "Namespace.Namespace.Type", ["[Module]Namespace.*"]);
      Assert.True(result);

      result = _instrumentationHelper.IsTypeExcluded("Module.dll", "Namespace.Namespace.Type", ["[Module]Namespace.WrongNamespace.*"]);
      Assert.False(result);
    }

    [Fact]
    public void TestIsTypeIncludedWithoutFilter()
    {
      bool result = _instrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", []);

      Assert.True(result);
    }

    [Theory]
    [InlineData("[Module]mismatch")]
    [InlineData("[Mismatch]*")]
    [InlineData("[Mismatch]a.b.Dto")]
    public void TestIsTypeExcludedAndIncludedWithSingleMismatchFilter(string filter)
    {
      bool result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", [filter]);
      Assert.False(result);

      result = _instrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", [filter]);
      Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(ValidModuleAndNamespaceFilterData))]
    public void TestIsTypeExcludedAndIncludedWithFilter(string filter)
    {
      bool result = _instrumentationHelper.IsTypeExcluded("Module.dll", "a.b.Dto", [filter]);
      Assert.True(result);

      result = _instrumentationHelper.IsTypeIncluded("Module.dll", "a.b.Dto", [filter]);
      Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(ValidModuleAndNamespaceFilterData))]
    public void TestIsTypeExcludedAndIncludedWithMatchingAndMismatchingFilter(string filter)
    {
      string[] filters = ["[Mismatch]*", filter, "[Mismatch]*"];

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

      string[] currentDirModules = _instrumentationHelper.GetCoverableModules(module, [], false);
      Assert.Single(currentDirModules);
      Assert.Equal("coverlet.tests.xunit.extensions.dll", Path.GetFileName(currentDirModules[0]));

      string[] moreThanOneDirectory = _instrumentationHelper
                                 .GetCoverableModules(module, [newDir2.FullName], false)
                                 .OrderBy(f => f).ToArray();

      Assert.Equal(2, moreThanOneDirectory.Length);
      Assert.Equal("coverlet.tests.xunit.extensions.dll", Path.GetFileName(moreThanOneDirectory[0]));
      Assert.Equal("coverlet.core.dll", Path.GetFileName(moreThanOneDirectory[1]));

      string[] moreThanOneDirectoryPlusTestAssembly = _instrumentationHelper
                                                 .GetCoverableModules(module, [newDir2.FullName], true)
                                                 .OrderBy(f => f).ToArray();

      Assert.Equal(3, moreThanOneDirectoryPlusTestAssembly.Length);
      Assert.Equal("coverlet.core.tests.dll", Path.GetFileName(moreThanOneDirectoryPlusTestAssembly[0]));
      Assert.Equal("coverlet.tests.xunit.extensions.dll", Path.GetFileName(moreThanOneDirectoryPlusTestAssembly[1]));
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
