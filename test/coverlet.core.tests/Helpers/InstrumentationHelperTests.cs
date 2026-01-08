// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Enums;
using Coverlet.Core.Helpers;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests.Helpers
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

      // Ensure the backup list is used to restore the original module
      _instrumentationHelper.BackupOriginalModule(module, identifier, false);

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
    [InlineData("[*]ClassLibrary1.Tests.*")]
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
    [InlineData("[")]
    [InlineData("[assembly][*")]
    [InlineData("[assembly]*]")]
    [InlineData("[]")]
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
      string[] modules = new[] { "Module.dll" };
      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, new string[0], new string[0]);

      Assert.Equal(modules, result);
    }

    [Theory]
    [InlineData("[Module]mismatch")]
    [InlineData("[Mismatch]*")]
    public void TestIsModuleExcludedWithSingleMismatchFilter(string filter)
    {
      string[] modules = new[] { "Module.dll" };
      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, new string[0], new[] { filter });

      Assert.Equal(modules, result);
    }

    [Fact]
    public void TestIsModuleIncludedWithSingleMismatchFilter()
    {
      string[] modules = new[] { "Module.dll" };
      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, new[] { "[Mismatch]*" }, new string[0]);

      Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(ValidModuleFilterData))]
    public void TestIsModuleExcludedAndIncludedWithFilter(string filter)
    {
      string[] modules = new[] { "Module.dll" };
      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, new[] { filter }, new[] { filter });

      Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(ValidModuleFilterData))]
    public void TestIsModuleExcludedAndIncludedWithMatchingAndMismatchingFilter(string filter)
    {
      string[] modules = new[] { "Module.dll" };
      string[] filters = new[] { "[Mismatch]*", filter, "[Mismatch]*" };

      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, filters, filters);

      Assert.Empty(result);
    }

    [Fact]
    public void TestSelectModulesWithTypeFiltersDoesNotExcludeAssemblyWithType()
    {
      string[] modules = new[] { "Module.dll", "Module.Tests.dll" };
      string[] includeFilters = new[] { "[*]Module*" };
      string[] excludeFilters = new[] { "[*]Module.Tests.*" };

      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, includeFilters, excludeFilters);

      Assert.Equal(modules, result);
    }

    [Fact]
    public void TestSelectModulesWithModuleFilterExcludesExpectedModules()
    {
      string[] modules = new[] { "ModuleA.dll", "ModuleA.Tests.dll", "ModuleB.dll", "Module.B.Tests.dll" };
      string[] includeFilters = new[] { "" };
      string[] excludeFilters = new[] { "[ModuleA*]*" };

      IEnumerable<string> result = _instrumentationHelper.SelectModules(modules, includeFilters, excludeFilters);

      Assert.Equal(["ModuleB.dll", "Module.B.Tests.dll"], result);
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
      File.Copy("coverlet.core.dll", Path.Combine(newDir2.FullName, "coverlet.core.dll"));

      string[] currentDirModules = _instrumentationHelper.GetCoverableModules(module, Array.Empty<string>(), false);
      Assert.Empty(currentDirModules);

      string[] moreThanOneDirectory = _instrumentationHelper
                                 .GetCoverableModules(module, new string[] { newDir2.FullName }, false)
                                 .OrderBy(f => f).ToArray();

      Assert.Single(moreThanOneDirectory);
      Assert.Equal("coverlet.core.dll", Path.GetFileName(moreThanOneDirectory[0]));

      string[] moreThanOneDirectoryPlusTestAssembly = _instrumentationHelper
                                                 .GetCoverableModules(module, new string[] { newDir2.FullName }, true)
                                                 .OrderBy(f => f).ToArray();

      Assert.Equal(2, moreThanOneDirectoryPlusTestAssembly.Length);
      Assert.Equal("coverlet.core.tests.dll", Path.GetFileName(moreThanOneDirectoryPlusTestAssembly[0]));
      Assert.Equal("coverlet.core.dll", Path.GetFileName(moreThanOneDirectoryPlusTestAssembly[1]));

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
                    new object[] { "[*]*" }
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

    #region RestoreOriginalModules Tests

    [Fact]
    public void TestRestoreOriginalModules_WhenBackupListIsEmpty_ReturnsEarly()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockFileSystem = new Mock<IFileSystem>();
      var mockRetryHelper = new Mock<IRetryHelper>();
      var mockProcessExitHandler = new Mock<IProcessExitHandler>();
      var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();

      var instrumentationHelper = new InstrumentationHelper(
        mockProcessExitHandler.Object,
        mockRetryHelper.Object,
        mockFileSystem.Object,
        mockLogger.Object,
        mockSourceRootTranslator.Object);

      // Act - RestoreOriginalModules should return early without any file operations
      instrumentationHelper.RestoreOriginalModules();

      // Assert - No file operations should occur
      mockFileSystem.Verify(x => x.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
      mockFileSystem.Verify(x => x.Delete(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void TestRestoreOriginalModules_WithBackedUpModule_RestoresModule()
    {
      // Arrange
      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      Directory.CreateDirectory(tempDir);

      // Use the actual test assembly as source to copy (valid DLL)
      string sourceAssembly = typeof(InstrumentationHelperTests).Assembly.Location;
      string modulePath = Path.Combine(tempDir, "TestModule.dll");
      string identifier = Guid.NewGuid().ToString();

      try
      {
        // Copy a valid assembly to the temp location
        File.Copy(sourceAssembly, modulePath, true);
        string originalContent = File.ReadAllBytes(modulePath).Length.ToString();

        var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
        var instrumentationHelper = new InstrumentationHelper(
          new ProcessExitHandler(),
          new RetryHelper(),
          new FileSystem(),
          new Mock<ILogger>().Object,
          mockSourceRootTranslator.Object);

        // Backup the module
        instrumentationHelper.BackupOriginalModule(modulePath, identifier, false);

        // Modify the original (append some bytes)
        File.AppendAllText(modulePath, "modified");
        string modifiedLength = File.ReadAllBytes(modulePath).Length.ToString();
        Assert.NotEqual(originalContent, modifiedLength);

        // Act
        instrumentationHelper.RestoreOriginalModules();

        // Assert - Module should be restored to original content
        Assert.Equal(originalContent, File.ReadAllBytes(modulePath).Length.ToString());
      }
      finally
      {
        if (Directory.Exists(tempDir))
        {
          Directory.Delete(tempDir, true);
        }
      }
    }

    [Fact]
    public void TestRestoreOriginalModules_WhenBackupFileNotFound_LogsWarningAndRemovesFromList()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockFileSystem = new Mock<IFileSystem>();
      var mockRetryHelper = new Mock<IRetryHelper>();
      var mockProcessExitHandler = new Mock<IProcessExitHandler>();
      var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();

      string modulePath = Path.Combine(Path.GetTempPath(), "NonExistent.dll");
      string identifier = Guid.NewGuid().ToString();

      // Setup file system to simulate backup exists initially but not during restore
      mockFileSystem.SetupSequence(x => x.Exists(It.IsAny<string>()))
        .Returns(true)  // For backup creation check
        .Returns(false); // For backup file exists check during restore

      mockFileSystem.Setup(x => x.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));

      var instrumentationHelper = new InstrumentationHelper(
        mockProcessExitHandler.Object,
        mockRetryHelper.Object,
        mockFileSystem.Object,
        mockLogger.Object,
        mockSourceRootTranslator.Object);

      // Backup the module (will add to backup list)
      instrumentationHelper.BackupOriginalModule(modulePath, identifier, false);

      // Act
      instrumentationHelper.RestoreOriginalModules();

      // Assert - Warning should be logged
      mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("Backup file not found"))), Times.AtLeastOnce);
    }

    [Fact]
    public void TestRestoreOriginalModules_SkipsCurrentlyRunningAssembly()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
      string currentAssembly = typeof(InstrumentationHelperTests).Assembly.Location;
      string identifier = Guid.NewGuid().ToString();

      var instrumentationHelper = new InstrumentationHelper(
        new ProcessExitHandler(),
        new RetryHelper(),
        new FileSystem(),
        mockLogger.Object,
        mockSourceRootTranslator.Object);

      // Backup the current assembly (which is running)
      instrumentationHelper.BackupOriginalModule(currentAssembly, identifier, false);

      // Act
      instrumentationHelper.RestoreOriginalModules();

      // Assert - Should log that it's skipping the running assembly
      mockLogger.Verify(x => x.LogVerbose(It.Is<string>(s => s.Contains("Skipping restore of currently running assembly"))), Times.AtLeastOnce);
    }

    #endregion

    #region RestoreOriginalModule Tests

    [Fact]
    public void TestRestoreOriginalModule_WhenModuleNotInBackupList_LogsAndReturns()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockFileSystem = new Mock<IFileSystem>();
      var mockRetryHelper = new Mock<IRetryHelper>();
      var mockProcessExitHandler = new Mock<IProcessExitHandler>();
      var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();

      var instrumentationHelper = new InstrumentationHelper(
        mockProcessExitHandler.Object,
        mockRetryHelper.Object,
        mockFileSystem.Object,
        mockLogger.Object,
        mockSourceRootTranslator.Object);

      // Act - Try to restore a module that was never backed up
      instrumentationHelper.RestoreOriginalModule("NonExistentModule.dll", "some-identifier");

      // Assert
      mockLogger.Verify(x => x.LogVerbose(It.Is<string>(s => s.Contains("Module not in backup list"))), Times.Once);
      mockFileSystem.Verify(x => x.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void TestRestoreOriginalModule_WhenBackupFileNotFound_LogsWarning()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockFileSystem = new Mock<IFileSystem>();
      var mockRetryHelper = new Mock<IRetryHelper>();
      var mockProcessExitHandler = new Mock<IProcessExitHandler>();
      var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();

      string modulePath = Path.Combine(Path.GetTempPath(), "TestModule.dll");
      string identifier = Guid.NewGuid().ToString();

      // Setup to allow backup but not find backup during restore
      mockFileSystem.SetupSequence(x => x.Exists(It.IsAny<string>()))
        .Returns(false)  // pdb check during backup
        .Returns(false); // backup file check during restore

      mockFileSystem.Setup(x => x.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));

      var instrumentationHelper = new InstrumentationHelper(
        mockProcessExitHandler.Object,
        mockRetryHelper.Object,
        mockFileSystem.Object,
        mockLogger.Object,
        mockSourceRootTranslator.Object);

      // Backup first to add to list
      instrumentationHelper.BackupOriginalModule(modulePath, identifier, false);

      // Act
      instrumentationHelper.RestoreOriginalModule(modulePath, identifier);

      // Assert
      mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("Backup file not found"))), Times.Once);
    }

    [Fact]
    public void TestRestoreOriginalModule_WithValidBackup_RestoresSuccessfully()
    {
      // Arrange
      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      Directory.CreateDirectory(tempDir);

      // Use the actual test assembly as source to copy (valid DLL)
      string sourceAssembly = typeof(InstrumentationHelperTests).Assembly.Location;
      string modulePath = Path.Combine(tempDir, "TestModule.dll");
      string identifier = Guid.NewGuid().ToString();

      try
      {
        // Copy a valid assembly to the temp location
        File.Copy(sourceAssembly, modulePath, true);
        long originalSize = new FileInfo(modulePath).Length;

        var mockLogger = new Mock<ILogger>();
        var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
        var instrumentationHelper = new InstrumentationHelper(
          new ProcessExitHandler(),
          new RetryHelper(),
          new FileSystem(),
          mockLogger.Object,
          mockSourceRootTranslator.Object);

        // Backup
        instrumentationHelper.BackupOriginalModule(modulePath, identifier, false);

        // Modify original (append data to change size)
        File.AppendAllText(modulePath, "modified data");
        Assert.NotEqual(originalSize, new FileInfo(modulePath).Length);

        // Act
        instrumentationHelper.RestoreOriginalModule(modulePath, identifier);

        // Assert
        Assert.Equal(originalSize, new FileInfo(modulePath).Length);
        mockLogger.Verify(x => x.LogVerbose(It.Is<string>(s => s.Contains("Restored module from backup"))), Times.Once);
      }
      finally
      {
        if (Directory.Exists(tempDir))
        {
          Directory.Delete(tempDir, true);
        }
      }
    }

    [Fact]
    public void TestRestoreOriginalModule_WithSymbolFile_RestoresBothModuleAndSymbols()
    {
      // Arrange
      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      Directory.CreateDirectory(tempDir);

      // Use the actual test assembly as source to copy (valid DLL)
      string sourceAssembly = typeof(InstrumentationHelperTests).Assembly.Location;
      string sourcePdb = Path.ChangeExtension(sourceAssembly, ".pdb");
      string modulePath = Path.Combine(tempDir, "TestModule.dll");
      string symbolPath = Path.Combine(tempDir, "TestModule.pdb");
      string identifier = Guid.NewGuid().ToString();

      try
      {
        // Copy valid assembly and pdb to the temp location
        File.Copy(sourceAssembly, modulePath, true);
        if (File.Exists(sourcePdb))
        {
          File.Copy(sourcePdb, symbolPath, true);
        }
        else
        {
          // Create a dummy pdb file if source doesn't exist
          File.WriteAllText(symbolPath, "original pdb content");
        }

        long originalDllSize = new FileInfo(modulePath).Length;
        long originalPdbSize = new FileInfo(symbolPath).Length;

        var mockLogger = new Mock<ILogger>();
        var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
        var instrumentationHelper = new InstrumentationHelper(
          new ProcessExitHandler(),
          new RetryHelper(),
          new FileSystem(),
          mockLogger.Object,
          mockSourceRootTranslator.Object);

        // Backup
        instrumentationHelper.BackupOriginalModule(modulePath, identifier, false);

        // Modify originals
        File.AppendAllText(modulePath, "modified dll data");
        File.AppendAllText(symbolPath, "modified pdb data");

        Assert.NotEqual(originalDllSize, new FileInfo(modulePath).Length);
        Assert.NotEqual(originalPdbSize, new FileInfo(symbolPath).Length);

        // Act
        instrumentationHelper.RestoreOriginalModule(modulePath, identifier);

        // Assert
        Assert.Equal(originalDllSize, new FileInfo(modulePath).Length);
        Assert.Equal(originalPdbSize, new FileInfo(symbolPath).Length);
      }
      finally
      {
        if (Directory.Exists(tempDir))
        {
          Directory.Delete(tempDir, true);
        }
      }
    }

    #endregion

    #region BackupOriginalModule Tests

    [Fact]
    public void TestBackupOriginalModule_WhenDisableManagedInstrumentationRestoreIsTrue_DoesNotBackup()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockFileSystem = new Mock<IFileSystem>();
      var mockRetryHelper = new Mock<IRetryHelper>();
      var mockProcessExitHandler = new Mock<IProcessExitHandler>();
      var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();

      var instrumentationHelper = new InstrumentationHelper(
        mockProcessExitHandler.Object,
        mockRetryHelper.Object,
        mockFileSystem.Object,
        mockLogger.Object,
        mockSourceRootTranslator.Object);

      // Act
      instrumentationHelper.BackupOriginalModule("SomeModule.dll", "identifier", disableManagedInstrumentationRestore: true);

      // Assert - No copy operations should occur
      mockFileSystem.Verify(x => x.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void TestBackupOriginalModule_WhenModuleAlreadyBackedUp_SkipsAndLogs()
    {
      // Arrange
      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      Directory.CreateDirectory(tempDir);

      // Use the actual test assembly as source to copy (valid DLL)
      string sourceAssembly = typeof(InstrumentationHelperTests).Assembly.Location;
      string modulePath = Path.Combine(tempDir, "TestModule.dll");
      string identifier = Guid.NewGuid().ToString();

      try
      {
        // Copy a valid assembly to the temp location
        File.Copy(sourceAssembly, modulePath, true);

        var mockLogger = new Mock<ILogger>();
        var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
        var instrumentationHelper = new InstrumentationHelper(
          new ProcessExitHandler(),
          new RetryHelper(),
          new FileSystem(),
          mockLogger.Object,
          mockSourceRootTranslator.Object);

        // Backup first time
        instrumentationHelper.BackupOriginalModule(modulePath, identifier, false);

        // Act - Backup same module again
        instrumentationHelper.BackupOriginalModule(modulePath, identifier, false);

        // Assert
        mockLogger.Verify(x => x.LogVerbose(It.Is<string>(s => s.Contains("Module already in backup list"))), Times.Once);
      }
      finally
      {
        if (Directory.Exists(tempDir))
        {
          Directory.Delete(tempDir, true);
        }
      }
    }

    #endregion
  }
}
