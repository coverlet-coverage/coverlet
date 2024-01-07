// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Moq;
using Xunit;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;
using Coverlet.Core;
using Coverlet.Tests.Utils;
using Coverlet.Core.Symbols;

namespace coverlet.core.remote.tests.Instrumentation
{
  public class InstrumenterTests
  {
    private readonly Mock<ILogger> _mockLogger = new();

    private InstrumenterTest CreateInstrumentor(bool fakeCoreLibModule = false, string[] attributesToIgnore = null, string[] excludedFiles = null, bool singleHit = false)
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");
      string identifier = Guid.NewGuid().ToString();

      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), identifier));

      string destModule, destPdb;
      if (fakeCoreLibModule)
      {
        destModule = "System.Private.CoreLib.dll";
        destPdb = "System.Private.CoreLib.pdb";
      }
      else
      {
        destModule = Path.GetFileName(module);
        destPdb = Path.GetFileName(pdb);
      }

      File.Copy(module, Path.Combine(directory.FullName, destModule), true);
      File.Copy(pdb, Path.Combine(directory.FullName, destPdb), true);

      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object, new SourceRootTranslator(new Mock<ILogger>().Object, new FileSystem()));

      module = Path.Combine(directory.FullName, destModule);
      CoverageParameters parameters = new()
      {
        ExcludeAttributes = attributesToIgnore,
        DoesNotReturnAttributes = ["DoesNotReturnAttribute"]
      };
      var instrumenter = new Instrumenter(module, identifier, parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
      return new InstrumenterTest
      {
        Instrumenter = instrumenter,
        Module = module,
        Identifier = identifier,
        Directory = directory
      };
    }

    class InstrumenterTest
    {
      public Instrumenter Instrumenter { get; set; }

      public string Module { get; set; }

      public string Identifier { get; set; }

      public DirectoryInfo Directory { get; set; }
    }

    [Fact]
    public void TestReachabilityHelper()
    {
      int[] allInstrumentableLines =
          [
                    // Throws
                    7, 8,
                    // NoBranches
                    12, 13, 14, 15, 16,
                    // If
                    19, 20, 22, 23, 24, 25, 26, 27, 29, 30,
                    // Switch
                    33, 34, 36, 39, 40, 41, 42, 44, 45, 49, 50, 52, 53, 55, 56, 58, 59, 61, 62, 64, 65, 68, 69,
                    // Subtle
                    72, 73, 75, 78, 79, 80, 82, 83, 86, 87, 88, 91, 92, 95, 96, 98, 99, 101, 102, 103,
                    // UnreachableBranch
                    106, 107, 108, 110, 111, 112, 113, 114,
                    // ThrowsGeneric
                    118, 119,
                    // CallsGenericMethodDoesNotReturn
                    124, 125, 126, 127, 128,
                    // AlsoThrows
                    134, 135,
                    // CallsGenericClassDoesNotReturn
                    140, 141, 142, 143, 144,
                    // WithLeave
                    147, 149, 150, 151, 152, 153, 154, 155, 156, 159, 161, 163, 166, 167, 168,
                    // FiltersAndFinallies
                    171, 173, 174, 175, 176, 177, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 192, 193, 194, 195, 196, 197
          ];
      int[] notReachableLines =
          [
                    // NoBranches
                    15, 16,
                    // If
                    26, 27,
                    // Switch
                    41, 42,
                    // Subtle
                    79, 80, 88, 96, 98, 99,
                    // UnreachableBranch
                    110, 111, 112, 113, 114,
                    // CallsGenericMethodDoesNotReturn
                    127, 128,
                    // CallsGenericClassDoesNotReturn
                    143, 144,
                    // WithLeave
                    163, 164,
                    // FiltersAndFinallies
                    176, 177, 183, 184, 189, 190, 195, 196, 197
          ];

      int[] expectedToBeInstrumented = allInstrumentableLines.Except(notReachableLines).ToArray();

      InstrumenterTest instrumenterTest = CreateInstrumentor();
      InstrumenterResult result = instrumenterTest.Instrumenter.Instrument();

      Document doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Instrumentation.DoesNotReturn.cs");

      // check for instrumented lines
      doc.AssertNonInstrumentedLines(BuildConfiguration.Debug, notReachableLines);
      doc.AssertInstrumentLines(BuildConfiguration.Debug, expectedToBeInstrumented);

      instrumenterTest.Directory.Delete(true);
    }
  }
}
