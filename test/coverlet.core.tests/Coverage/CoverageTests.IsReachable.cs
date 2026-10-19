// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Symbols;
using Coverlet.Core.Tests.Symbols;
using Microsoft.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
  public partial class CoverageTests
  {
    // Live(int) gets a conditional branch appended after its last ret; DeadBranchLine is the line
    // of the comment that the injected sequence point points to.
    private const string EndToEndSource = @"namespace Coverlet.Reachability
{
  public static class EndToEnd
  {
    public static int Live(int x)
    {
      if (x > 0) return 1;
      return 0;
      // dead branch injected here
    }
  }
}
";

    private const int LiveBranchLine = 7;
    private const int DeadBranchLine = 9;

    [Fact]
    public void Coverage_DeadBranch_IsReachableFalseSurvivesInstrumenterAndCoverage()
    {
      string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      Directory.CreateDirectory(directory);
      try
      {
        string module = BuildEndToEndModule(directory);
        var logger = new Mock<ILogger>();
        var fileSystem = new FileSystem();
        var sourceRootTranslator = new SourceRootTranslator(logger.Object, fileSystem);
        var instrumentationHelper = new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), fileSystem, logger.Object, sourceRootTranslator);
        var parameters = new CoverageParameters
        {
          IncludeFilters = [],
          IncludeDirectories = [],
          ExcludeFilters = [],
          ExcludedSourceFiles = [],
          ExcludeAttributes = [],
          IncludeTestAssembly = true,
          ExcludeAssembliesWithoutSources = "None"
        };

        var coverage = new Coverage(module, parameters, logger.Object, instrumentationHelper, fileSystem, sourceRootTranslator, new CecilSymbolHelper());
        CoveragePrepareResult prepareResult = coverage.PrepareModules();

        // Instrumenter: Branch carries the flag
        InstrumenterResult instrumenterResult = Assert.Single(prepareResult.Results);
        Branch[] instrumented = [.. instrumenterResult.Documents.Values.SelectMany(d => d.Branches.Values).OrderBy(b => b.Ordinal)];
        Assert.Equal(
            new[] { (LiveBranchLine, 0, true), (LiveBranchLine, 1, true), (DeadBranchLine, 0, false), (DeadBranchLine, 1, false) },
            instrumented.Select(b => (b.Number, b.Path, b.IsReachable)));

        // Coverage: BranchInfo carries the flag, and the summary counts only reachable branches
        CoverageResult result = coverage.GetCoverageResult();
        Method method = result.Modules.Single().Value.Single().Value.Single().Value.Single().Value;
        Assert.Equal(
            new[] { (LiveBranchLine, 0, true), (LiveBranchLine, 1, true), (DeadBranchLine, 0, false), (DeadBranchLine, 1, false) },
            method.Branches.OrderBy(b => b.Ordinal).Select(b => (b.Line, b.Path, b.IsReachable)));

        CoverageDetails branchCoverage = CoverageSummary.CalculateBranchCoverage(method.Branches);
        Assert.Equal(2, branchCoverage.Total);
        Assert.Equal(0, branchCoverage.Covered);
      }
      finally
      {
        Directory.Delete(directory, true);
      }
    }

    [Fact]
    public void Coverage_BranchWithoutLines_IsReachableCopiedToBranchInfo()
    {
      // Branches with no line entries reach every BranchInfo construction site in Coverage:
      // new document, then existing method, new method in an existing class, and new class.
      var document = new Coverlet.Core.Instrumentation.Document { Index = 0, Path = "doc.cs" };
      document.Branches.Add(new BranchKey(1, 0), new Branch { Number = 1, Class = "ClassA", Method = "MethodA", Ordinal = 0, Path = 0, IsReachable = false });
      document.Branches.Add(new BranchKey(1, 1), new Branch { Number = 1, Class = "ClassA", Method = "MethodA", Ordinal = 1, Path = 1, IsReachable = false });
      document.Branches.Add(new BranchKey(2, 2), new Branch { Number = 2, Class = "ClassA", Method = "MethodB", Ordinal = 2, Path = 0, IsReachable = false });
      document.Branches.Add(new BranchKey(3, 3), new Branch { Number = 3, Class = "ClassB", Method = "MethodC", Ordinal = 3, Path = 0, IsReachable = false });

      var instrumenterResult = new InstrumenterResult { HitsFilePath = "HitsFilePath", ModulePath = "ModulePath" };
      instrumenterResult.Documents.Add("doc.cs", document);
      var prepareResult = new CoveragePrepareResult { Results = [instrumenterResult], Parameters = new CoverageParameters() };

      var coverage = new Coverage(prepareResult, new Mock<ILogger>().Object, new Mock<IInstrumentationHelper>().Object,
          new Mock<IFileSystem>().Object, new Mock<ISourceRootTranslator>().Object);
      CoverageResult result = coverage.GetCoverageResult();

      Classes classes = result.Modules["ModulePath"]["doc.cs"];
      Assert.Equal(new[] { false, false }, classes["ClassA"]["MethodA"].Branches.Select(b => b.IsReachable));
      Assert.False(Assert.Single(classes["ClassA"]["MethodB"].Branches).IsReachable);
      Assert.False(Assert.Single(classes["ClassB"]["MethodC"].Branches).IsReachable);
    }

    /// <summary>
    /// Compiles <see cref="EndToEndSource"/> and appends a conditional branch that nothing reaches
    /// after the last ret of Live(int):
    ///   ldarg.0 ; brtrue.s TARGET ; ldarg.0 ; pop ; TARGET: ldc.i4.2 ; ret
    /// Path 0 gets an inline counter and path 1 a trampoline, so both Branch construction sites
    /// in Instrumenter are used.
    /// </summary>
    private static string BuildEndToEndModule(string directory)
    {
      string modulePath = Path.Combine(directory, "ReachabilityEndToEnd.dll");
      string pdbPath = Path.ChangeExtension(modulePath, ".pdb");
      (byte[] peImage, byte[] pdbImage) = BranchPointReachabilityTests.Compile(EndToEndSource, Path.Combine(directory, "EndToEnd.cs"), "ReachabilityEndToEnd", OptimizationLevel.Release);

      using ModuleDefinition module = ModuleDefinition.ReadModule(new MemoryStream(peImage), new ReaderParameters
      {
        ReadSymbols = true,
        SymbolStream = new MemoryStream(pdbImage)
      });
      MethodDefinition method = module.GetType("Coverlet.Reachability.EndToEnd").Methods.Single(m => m.Name == "Live");
      ILProcessor il = method.Body.GetILProcessor();
      Instruction first = il.Create(OpCodes.Ldarg_0);
      Instruction target = il.Create(OpCodes.Ldc_I4_2);
      il.Append(first);
      il.Append(il.Create(OpCodes.Brtrue_S, target));
      il.Append(il.Create(OpCodes.Ldarg_0));
      il.Append(il.Create(OpCodes.Pop));
      il.Append(target);
      il.Append(il.Create(OpCodes.Ret));

      Mono.Cecil.Cil.Document sourceDocument = method.DebugInformation.SequencePoints[0].Document;
      method.DebugInformation.SequencePoints.Add(new SequencePoint(first, sourceDocument)
      {
        StartLine = DeadBranchLine,
        StartColumn = 7,
        EndLine = DeadBranchLine,
        EndColumn = 37
      });

      module.Write(modulePath, new WriterParameters { WriteSymbols = true, SymbolWriterProvider = new PortablePdbWriterProvider() });
      Assert.True(File.Exists(pdbPath));
      return modulePath;
    }
  }
}
