// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Symbols;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
  public partial class CoverageTests
  {
    [Fact]
    public void TestCoverageSkipModule__AssemblyMarkedAsExcludeFromCodeCoverage()
    {
      var partialMockFileSystem = new Mock<FileSystem>();
      partialMockFileSystem.CallBase = true;
      partialMockFileSystem.Setup(fs => fs.NewFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>())).Returns((string path, FileMode mode, FileAccess access) =>
      {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      });
      var loggerMock = new Mock<ILogger>();

      string excludedbyattributeDll = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), "coverlet.tests.projectsample.excludedbyattribute.dll").First();

      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(excludedbyattributeDll, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      var parameters = new CoverageParameters
      {
        IncludeFilters = new string[] { "[coverlet.tests.projectsample.excludedbyattribute*]*" },
        IncludeDirectories = Array.Empty<string>(),
        ExcludeFilters = Array.Empty<string>(),
        ExcludedSourceFiles = Array.Empty<string>(),
        ExcludeAttributes = Array.Empty<string>(),
        IncludeTestAssembly = true,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false
      };

      // test skip module include test assembly feature
      var coverage = new Coverage(excludedbyattributeDll, parameters, loggerMock.Object, instrumentationHelper, partialMockFileSystem.Object,
                                  new SourceRootTranslator(loggerMock.Object, new FileSystem()), new CecilSymbolHelper());
      CoveragePrepareResult result = coverage.PrepareModules();
      Assert.Empty(result.Results);
      loggerMock.Verify(l => l.LogVerbose(It.IsAny<string>()));
    }
  }
}
