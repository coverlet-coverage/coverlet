using System.IO;

using Coverlet.Core.Abstractions;
using Coverlet.Tests.Xunit.Extensions;
using Moq;
using Xunit;

namespace Coverlet.Core.Helpers.Tests
{
    public class SourceRootTranslatorTests
    {
        [ConditionalFact]
        [SkipOnOS(OS.Linux, "Windows path format only")]
        [SkipOnOS(OS.MacOS, "Windows path format only")]
        public void Translate_Success()
        {
            string fileToTranslate = "/_/src/coverlet.core/obj/Debug/netstandard2.0/coverlet.core.pdb";
            Mock<ILogger> logger = new Mock<ILogger>();
            Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string p) =>
            {
                if (p == "testLib.dll" || p == @"C:\git\coverlet\src\coverlet.core\obj\Debug\netstandard2.0\coverlet.core.pdb" || p == "CoverletSourceRootsMapping") return true;
                return false;
            });
            fileSystem.Setup(f => f.ReadAllLines(It.IsAny<string>())).Returns(File.ReadAllLines(@"TestAssets/CoverletSourceRootsMappingTest"));
            SourceRootTranslator translator = new SourceRootTranslator("testLib.dll", logger.Object, fileSystem.Object);
            Assert.Equal(@"C:\git\coverlet\src\coverlet.core\obj\Debug\netstandard2.0\coverlet.core.pdb", translator.ResolveFilePath(fileToTranslate));
            Assert.Equal(@"C:\git\coverlet\src\coverlet.core\obj\Debug\netstandard2.0\coverlet.core.pdb", translator.ResolveFilePath(fileToTranslate));
        }

        [ConditionalFact]
        [SkipOnOS(OS.Linux, "Windows path format only")]
        [SkipOnOS(OS.MacOS, "Windows path format only")]
        public void TranslatePathRoot_Success()
        {
            Mock<ILogger> logger = new Mock<ILogger>();
            Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string p) =>
            {
                if (p == "testLib.dll" || p == @"C:\git\coverlet\src\coverlet.core\obj\Debug\netstandard2.0\coverlet.core.pdb" || p == "CoverletSourceRootsMapping") return true;
                return false;
            });
            fileSystem.Setup(f => f.ReadAllLines(It.IsAny<string>())).Returns(File.ReadAllLines(@"TestAssets/CoverletSourceRootsMappingTest"));
            SourceRootTranslator translator = new SourceRootTranslator("testLib.dll", logger.Object, fileSystem.Object);
            Assert.Equal(@"C:\git\coverlet\", translator.ResolvePathRoot("/_/")[0].OriginalPath);
        }

        [Fact]
        public void Translate_EmptyFile()
        {
            string fileToTranslate = "/_/src/coverlet.core/obj/Debug/netstandard2.0/coverlet.core.pdb";
            Mock<ILogger> logger = new Mock<ILogger>();
            Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string p) =>
            {
                if (p == "testLib.dll" || p == "CoverletSourceRootsMapping") return true;
                return false;
            });
            fileSystem.Setup(f => f.ReadAllLines(It.IsAny<string>())).Returns(new string[0]);
            SourceRootTranslator translator = new SourceRootTranslator("testLib.dll", logger.Object, fileSystem.Object);
            Assert.Equal(fileToTranslate, translator.ResolveFilePath(fileToTranslate));
        }

        [Fact]
        public void Translate_MalformedFile()
        {
            string fileToTranslate = "/_/src/coverlet.core/obj/Debug/netstandard2.0/coverlet.core.pdb";
            Mock<ILogger> logger = new Mock<ILogger>();
            Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string p) =>
            {
                if (p == "testLib.dll" || p == "CoverletSourceRootsMapping") return true;
                return false;
            });
            fileSystem.Setup(f => f.ReadAllLines(It.IsAny<string>())).Returns(new string[1] { "malformedRow" });
            SourceRootTranslator translator = new SourceRootTranslator("testLib.dll", logger.Object, fileSystem.Object);
            Assert.Equal(fileToTranslate, translator.ResolveFilePath(fileToTranslate));
            logger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
        }
    }
}
