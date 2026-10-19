// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Symbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Coverlet.Core.Tests.Symbols
{
  /// <summary>
  /// Tests for <see cref="BranchPoint.IsReachable"/>. The samples are compiled at test time, in Debug and
  /// in Release, so every method has its real IL shape, real instruction offsets and real sequence points.
  /// A branch path is reachable exactly when its branch instruction is reachable from the method entry.
  /// </summary>
  public class BranchPointReachabilityTests
  {
    private const string SampleSource = @"using System;

namespace Coverlet.Reachability
{
  public static class Samples
  {
    public static int GuardThrow(string s)
    {
      if (s == null) throw new ArgumentNullException(nameof(s));
      return s.Length;
    }

    public static int EarlyReturn(string s)
    {
      if (s == null) return -1;
      return s.Length;
    }

    public static int IfElse(int x)
    {
      int r;
      if (x > 0) r = 1;
      else r = 2;
      return r;
    }

    public static int Switch(int x)
    {
      switch (x)
      {
        case 0: return 10;
        case 1: return 11;
        case 2: return 12;
        default: return -1;
      }
    }

    public static int TryCatch(string s)
    {
      try
      {
        return int.Parse(s);
      }
      catch (FormatException)
      {
        if (s.Length > 3) return 1;
        return 0;
      }
    }

    public static int NestedTryCatch(string s)
    {
      try
      {
        return int.Parse(s);
      }
      catch (FormatException)
      {
        try
        {
          return 100 / (s.Length - 1);
        }
        catch (DivideByZeroException)
        {
          if (s.Length > 3) return 1;
          return 0;
        }
      }
    }
  }
}
";

    private static readonly ConcurrentDictionary<OptimizationLevel, ModuleDefinition> s_samples = new();

    [Theory]
    [InlineData(OptimizationLevel.Debug)]
    [InlineData(OptimizationLevel.Release)]
    public void GuardClauseWithThrow_BothPathsReachable(OptimizationLevel optimization)
    {
      IReadOnlyList<BranchPoint> points = GetBranchPoints(optimization, "GuardThrow");

      AssertSingleBranch(points, (0, true), (1, true));
    }

    [Theory]
    [InlineData(OptimizationLevel.Debug)]
    [InlineData(OptimizationLevel.Release)]
    public void IfWithEarlyReturn_BothPathsReachable(OptimizationLevel optimization)
    {
      IReadOnlyList<BranchPoint> points = GetBranchPoints(optimization, "EarlyReturn");

      AssertSingleBranch(points, (0, true), (1, true));
    }

    [Theory]
    [InlineData(OptimizationLevel.Debug)]
    [InlineData(OptimizationLevel.Release)]
    public void IfElse_BothPathsReachable(OptimizationLevel optimization)
    {
      IReadOnlyList<BranchPoint> points = GetBranchPoints(optimization, "IfElse");

      AssertSingleBranch(points, (0, true), (1, true));
    }

    [Theory]
    [InlineData(OptimizationLevel.Debug)]
    [InlineData(OptimizationLevel.Release)]
    public void Switch_AllCasesReachable(OptimizationLevel optimization)
    {
      IReadOnlyList<BranchPoint> points = GetBranchPoints(optimization, "Switch");

      AssertSingleBranch(points, (0, true), (1, true), (2, true), (3, true));
    }

    [Theory]
    [InlineData(OptimizationLevel.Debug)]
    [InlineData(OptimizationLevel.Release)]
    public void BranchInsideCatchBlock_BothPathsReachable(OptimizationLevel optimization)
    {
      MethodDefinition method = GetSampleMethod(optimization, "TryCatch");
      IReadOnlyList<BranchPoint> points = new CecilSymbolHelper().GetBranchPoints(method);

      AssertSingleBranch(points, (0, true), (1, true));

      // The branch is in the handler, which is entered only through the exception handler table
      ExceptionHandler handler = Assert.Single(method.Body.ExceptionHandlers);
      Assert.True(points[0].Offset >= handler.HandlerStart.Offset);
      Assert.True(points[0].Offset < handler.HandlerEnd.Offset);
    }

    [Theory]
    [InlineData(OptimizationLevel.Debug)]
    [InlineData(OptimizationLevel.Release)]
    public void BranchInsideCatchBlockNestedInCatchBlock_BothPathsReachable(OptimizationLevel optimization)
    {
      MethodDefinition method = GetSampleMethod(optimization, "NestedTryCatch");
      IReadOnlyList<BranchPoint> points = new CecilSymbolHelper().GetBranchPoints(method);

      AssertSingleBranch(points, (0, true), (1, true));

      // The inner try block starts inside the outer handler, so the inner handler is reachable
      // only after the outer handler is
      Assert.Equal(2, method.Body.ExceptionHandlers.Count);
      ExceptionHandler inner = method.Body.ExceptionHandlers[0];
      ExceptionHandler outer = method.Body.ExceptionHandlers[1];
      Assert.True(inner.TryStart.Offset >= outer.HandlerStart.Offset);
      Assert.True(points[0].Offset >= inner.HandlerStart.Offset);
      Assert.True(points[0].Offset < inner.HandlerEnd.Offset);
    }

    [Fact]
    public void BranchesInDeadCode_PathsUnreachable_BranchInLiveCode_PathsReachable()
    {
      // IL_0000: ldarg.0
      // IL_0001: brtrue.s IL_0005    reachable branch
      // IL_0003: ldc.i4.0
      // IL_0004: ret
      // IL_0005: ldc.i4.1
      // IL_0006: ret
      // IL_0007: ldarg.0
      // IL_0008: brtrue.s IL_000c    nothing reaches this branch
      // IL_000a: ldc.i4.2
      // IL_000b: ret
      // IL_000c: ldc.i4.3
      // IL_000d: ret
      // IL_000e: ldarg.0
      // IL_000f: switch (IL_001e, IL_0020)    nothing reaches this switch
      // IL_001c: ldc.i4.4
      // IL_001d: ret
      // IL_001e: ldc.i4.5
      // IL_001f: ret
      // IL_0020: ldc.i4.6
      // IL_0021: ret
      var module = ModuleDefinition.CreateModule("DeadBranchSample", ModuleKind.Dll);
      var type = new TypeDefinition("Coverlet.Reachability", "Dead", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed, module.TypeSystem.Object);
      module.Types.Add(type);
      var method = new MethodDefinition("DeadBranch", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Int32);
      method.Parameters.Add(new ParameterDefinition("x", Mono.Cecil.ParameterAttributes.None, module.TypeSystem.Int32));
      type.Methods.Add(method);

      ILProcessor il = method.Body.GetILProcessor();
      Instruction liveTarget = il.Create(OpCodes.Ldc_I4_1);
      Instruction deadTarget = il.Create(OpCodes.Ldc_I4_3);
      il.Append(il.Create(OpCodes.Ldarg_0));
      il.Append(il.Create(OpCodes.Brtrue_S, liveTarget));
      il.Append(il.Create(OpCodes.Ldc_I4_0));
      il.Append(il.Create(OpCodes.Ret));
      il.Append(liveTarget);
      il.Append(il.Create(OpCodes.Ret));
      il.Append(il.Create(OpCodes.Ldarg_0));
      il.Append(il.Create(OpCodes.Brtrue_S, deadTarget));
      il.Append(il.Create(OpCodes.Ldc_I4_2));
      il.Append(il.Create(OpCodes.Ret));
      il.Append(deadTarget);
      il.Append(il.Create(OpCodes.Ret));
      Instruction case0 = il.Create(OpCodes.Ldc_I4_5);
      Instruction case1 = il.Create(OpCodes.Ldc_I4_6);
      il.Append(il.Create(OpCodes.Ldarg_0));
      il.Append(il.Create(OpCodes.Switch, new[] { case0, case1 }));
      il.Append(il.Create(OpCodes.Ldc_I4_4));
      il.Append(il.Create(OpCodes.Ret));
      il.Append(case0);
      il.Append(il.Create(OpCodes.Ret));
      il.Append(case1);
      il.Append(il.Create(OpCodes.Ret));

      // Write the module and read it back so that every instruction has its real offset
      using var stream = new MemoryStream();
      module.Write(stream);
      stream.Position = 0;
      MethodDefinition reloaded = ModuleDefinition.ReadModule(stream).GetType("Coverlet.Reachability.Dead").Methods.Single(m => m.Name == "DeadBranch");

      IReadOnlyList<BranchPoint> points = new CecilSymbolHelper().GetBranchPoints(reloaded);

      Assert.Equal(
          new[] { (0x01, 0, true), (0x01, 1, true), (0x08, 0, false), (0x08, 1, false), (0x0f, 0, false), (0x0f, 1, false), (0x0f, 2, false) },
          points.Select(p => (p.Offset, p.Path, p.IsReachable)));
    }

    [Fact]
    public void BranchAfterJmp_PathsUnreachable()
    {
      // jmp leaves the method, so nothing after it is reachable. Cecil classifies jmp as a call.
      // IL_0000: jmp Target(int32)
      // IL_0005: ldarg.0
      // IL_0006: brtrue.s IL_000a
      // IL_0008: ldc.i4.0
      // IL_0009: ret
      // IL_000a: ldc.i4.1
      // IL_000b: ret
      var module = ModuleDefinition.CreateModule("JmpSample", ModuleKind.Dll);
      var type = new TypeDefinition("Coverlet.Reachability", "Jmp", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed, module.TypeSystem.Object);
      module.Types.Add(type);
      var target = new MethodDefinition("Target", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Int32);
      target.Parameters.Add(new ParameterDefinition("x", Mono.Cecil.ParameterAttributes.None, module.TypeSystem.Int32));
      ILProcessor targetIl = target.Body.GetILProcessor();
      targetIl.Append(targetIl.Create(OpCodes.Ldc_I4_0));
      targetIl.Append(targetIl.Create(OpCodes.Ret));
      type.Methods.Add(target);
      var method = new MethodDefinition("Jumper", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Int32);
      method.Parameters.Add(new ParameterDefinition("x", Mono.Cecil.ParameterAttributes.None, module.TypeSystem.Int32));
      type.Methods.Add(method);

      ILProcessor il = method.Body.GetILProcessor();
      Instruction branchTarget = il.Create(OpCodes.Ldc_I4_1);
      il.Append(il.Create(OpCodes.Jmp, target));
      il.Append(il.Create(OpCodes.Ldarg_0));
      il.Append(il.Create(OpCodes.Brtrue_S, branchTarget));
      il.Append(il.Create(OpCodes.Ldc_I4_0));
      il.Append(il.Create(OpCodes.Ret));
      il.Append(branchTarget);
      il.Append(il.Create(OpCodes.Ret));

      using var stream = new MemoryStream();
      module.Write(stream);
      stream.Position = 0;
      MethodDefinition reloaded = ModuleDefinition.ReadModule(stream).GetType("Coverlet.Reachability.Jmp").Methods.Single(m => m.Name == "Jumper");

      IReadOnlyList<BranchPoint> points = new CecilSymbolHelper().GetBranchPoints(reloaded);

      Assert.Equal(new[] { (0x06, 0, false), (0x06, 1, false) }, points.Select(p => (p.Offset, p.Path, p.IsReachable)));
    }

    [Fact]
    public void CoveragePrepareResult_SerializationRoundTrip_KeepsIsReachable()
    {
      // msbuild and the collector pass the instrumentation state between processes this way
      var document = new Coverlet.Core.Instrumentation.Document { Index = 0, Path = "doc.cs" };
      document.Branches.Add(new BranchKey(1, 0), new Branch { Number = 1, Class = "Class", Method = "Method", Ordinal = 0, Path = 0, IsReachable = true });
      document.Branches.Add(new BranchKey(1, 1), new Branch { Number = 1, Class = "Class", Method = "Method", Ordinal = 1, Path = 1, IsReachable = false });
      var instrumenterResult = new InstrumenterResult();
      instrumenterResult.Documents.Add("doc.cs", document);
      var prepareResult = new CoveragePrepareResult { Results = [instrumenterResult] };

      CoveragePrepareResult roundTrip = CoveragePrepareResult.Deserialize(CoveragePrepareResult.Serialize(prepareResult));

      Assert.Equal(new[] { true, false }, roundTrip.Results[0].Documents["doc.cs"].Branches.Values.OrderBy(b => b.Ordinal).Select(b => b.IsReachable));
    }

    [Fact]
    public void CoveragePrepareResult_DeserializeWithoutIsReachable_BranchIsReachable()
    {
      // A state file written before IsReachable existed has no IsReachable element
      var document = new Coverlet.Core.Instrumentation.Document { Index = 0, Path = "doc.cs" };
      document.Branches.Add(new BranchKey(1, 0), new Branch { Number = 1, Class = "Class", Method = "Method", Ordinal = 0, Path = 0, IsReachable = false });
      var instrumenterResult = new InstrumenterResult();
      instrumenterResult.Documents.Add("doc.cs", document);
      var prepareResult = new CoveragePrepareResult { Results = [instrumenterResult] };

      string xml = new StreamReader(CoveragePrepareResult.Serialize(prepareResult)).ReadToEnd();
      string withoutField = System.Text.RegularExpressions.Regex.Replace(xml, @"<(\w+:)?IsReachable>false</(\w+:)?IsReachable>", string.Empty);
      Assert.NotEqual(xml, withoutField);

      CoveragePrepareResult roundTrip = CoveragePrepareResult.Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(withoutField)));

      Assert.True(Assert.Single(roundTrip.Results[0].Documents["doc.cs"].Branches.Values).IsReachable);
    }

    private static void AssertSingleBranch(IReadOnlyList<BranchPoint> points, params (int Path, bool IsReachable)[] expected)
    {
      Assert.Equal(expected, points.Select(p => (p.Path, p.IsReachable)));
      Assert.Single(points.Select(p => p.Offset).Distinct());
    }

    private static IReadOnlyList<BranchPoint> GetBranchPoints(OptimizationLevel optimization, string methodName)
      => new CecilSymbolHelper().GetBranchPoints(GetSampleMethod(optimization, methodName));

    private static MethodDefinition GetSampleMethod(OptimizationLevel optimization, string methodName)
    {
      ModuleDefinition module = s_samples.GetOrAdd(optimization, level =>
      {
        (byte[] peImage, byte[] pdbImage) = Compile(SampleSource, "Samples.cs", "ReachabilitySamples", level);
        return ModuleDefinition.ReadModule(new MemoryStream(peImage), new ReaderParameters
        {
          ReadSymbols = true,
          SymbolStream = new MemoryStream(pdbImage)
        });
      });
      return module.GetType("Coverlet.Reachability.Samples").Methods.Single(m => m.Name == methodName);
    }

    internal static (byte[] PeImage, byte[] PdbImage) Compile(string source, string fileName, string assemblyName, OptimizationLevel optimization)
    {
      CSharpCompilation compilation = CSharpCompilation.Create(
          assemblyName,
          [CSharpSyntaxTree.ParseText(source, path: fileName, encoding: Encoding.UTF8)],
          [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
          new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: optimization));

      using var peStream = new MemoryStream();
      using var pdbStream = new MemoryStream();
      EmitResult emitResult = compilation.Emit(peStream, pdbStream,
          options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: Path.ChangeExtension(fileName, ".pdb")));
      Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
      return (peStream.ToArray(), pdbStream.ToArray());
    }
  }
}
