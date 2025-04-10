﻿// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.InteropServices;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests.Helpers
{
  public class SourceRootTranslatorTests
  {
    [Fact]
    public void Translate_Success()
    {
      Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Windows");
      string fileToTranslate = "/_/src/coverlet.core/obj/Debug/netstandard2.0/coverlet.core.pdb";
      var logger = new Mock<ILogger>();
      var assemblyAdapter = new Mock<IAssemblyAdapter>();
      assemblyAdapter.Setup(x => x.GetAssemblyName(It.IsAny<string>())).Returns("testLib");
      var fileSystem = new Mock<IFileSystem>();
      fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string p) =>
      {
        if (p == "testLib.dll" || p == @"C:\git\coverlet\src\coverlet.core\obj\Debug\netstandard2.0\coverlet.core.pdb" || p == "CoverletSourceRootsMapping_testLib") return true;
        return false;
      });
      fileSystem.Setup(f => f.ReadAllLines(It.IsAny<string>())).Returns(File.ReadAllLines(@"TestAssets/CoverletSourceRootsMappingTest"));
      var translator = new SourceRootTranslator("testLib.dll", logger.Object, fileSystem.Object, assemblyAdapter.Object);
      Assert.Equal(@"C:\git\coverlet\src\coverlet.core\obj\Debug\netstandard2.0\coverlet.core.pdb", translator.ResolveFilePath(fileToTranslate));
      Assert.Equal(@"C:\git\coverlet\src\coverlet.core\obj\Debug\netstandard2.0\coverlet.core.pdb", translator.ResolveFilePath(fileToTranslate));
    }

    [Fact]
    public void TranslatePathRoot_Success()
    {
      Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Windows");
      var logger = new Mock<ILogger>();
      var assemblyAdapter = new Mock<IAssemblyAdapter>();
      assemblyAdapter.Setup(x => x.GetAssemblyName(It.IsAny<string>())).Returns("testLib");
      var fileSystem = new Mock<IFileSystem>();
      fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string p) =>
      {
        if (p == "testLib.dll" || p == @"C:\git\coverlet\src\coverlet.core\obj\Debug\netstandard2.0\coverlet.core.pdb" || p == "CoverletSourceRootsMapping_testLib") return true;
        return false;
      });
      fileSystem.Setup(f => f.ReadAllLines(It.IsAny<string>())).Returns(File.ReadAllLines(@"TestAssets/CoverletSourceRootsMappingTest"));
      var translator = new SourceRootTranslator("testLib.dll", logger.Object, fileSystem.Object, assemblyAdapter.Object);
      Assert.Equal(@"C:\git\coverlet\", translator.ResolvePathRoot("/_/")[0].OriginalPath);
    }

    [Fact]
    public void TranslateWithDirectFile_Success()
    {
      Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Windows");
      var logger = new Mock<ILogger>();
      var assemblyAdapter = new Mock<IAssemblyAdapter>();
      assemblyAdapter.Setup(x => x.GetAssemblyName(It.IsAny<string>())).Returns("testLib");
      var fileSystem = new Mock<IFileSystem>();
      fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string p) =>
      {
        if (p == "testLib.dll" || p == @"C:\git\coverlet\src\coverlet.core\obj\Debug\netstandard2.0\coverlet.core.pdb" || p == "CoverletSourceRootsMapping_testLib") return true;
        return false;
      });
      fileSystem.Setup(f => f.ReadAllLines(It.IsAny<string>())).Returns(File.ReadAllLines(@"TestAssets/CoverletSourceRootsMappingTest"));
      var translator = new SourceRootTranslator("CoverletSourceRootsMapping_testLib", logger.Object, fileSystem.Object);
      Assert.Equal(@"C:\git\coverlet\", translator.ResolvePathRoot("/_/")[0].OriginalPath);
    }

    [Fact]
    public void Translate_EmptyFile()
    {
      string fileToTranslate = "/_/src/coverlet.core/obj/Debug/netstandard2.0/coverlet.core.pdb";
      var logger = new Mock<ILogger>();
      var assemblyAdapter = new Mock<IAssemblyAdapter>();
      assemblyAdapter.Setup(x => x.GetAssemblyName(It.IsAny<string>())).Returns("testLib");
      var fileSystem = new Mock<IFileSystem>();
      fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string p) =>
      {
        if (p == "testLib.dll" || p == "CoverletSourceRootsMapping_testLib") return true;
        return false;
      });
      fileSystem.Setup(f => f.ReadAllLines(It.IsAny<string>())).Returns(new string[0]);
      var translator = new SourceRootTranslator("testLib.dll", logger.Object, fileSystem.Object, assemblyAdapter.Object);
      Assert.Equal(fileToTranslate, translator.ResolveFilePath(fileToTranslate));
    }

    [Fact]
    public void Translate_MalformedFile()
    {
      string fileToTranslate = "/_/src/coverlet.core/obj/Debug/netstandard2.0/coverlet.core.pdb";
      var logger = new Mock<ILogger>();
      var assemblyAdapter = new Mock<IAssemblyAdapter>();
      assemblyAdapter.Setup(x => x.GetAssemblyName(It.IsAny<string>())).Returns("testLib");
      var fileSystem = new Mock<IFileSystem>();
      fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string p) =>
      {
        if (p == "testLib.dll" || p == "CoverletSourceRootsMapping_testLib") return true;
        return false;
      });
      fileSystem.Setup(f => f.ReadAllLines(It.IsAny<string>())).Returns(new string[1] { "malformedRow" });
      var translator = new SourceRootTranslator("testLib.dll", logger.Object, fileSystem.Object, assemblyAdapter.Object);
      Assert.Equal(fileToTranslate, translator.ResolveFilePath(fileToTranslate));
      logger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
    }
  }
}
