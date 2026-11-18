// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Symbols;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Xunit;

namespace Coverlet.Core.Tests.Symbols
{
  /// <summary>
  /// Regression tests for https://github.com/coverlet-coverage/coverlet/issues/2074.
  ///
  /// .NET 11 runtime-async methods are marked MethodImplAttributes.Async (0x2000) and
  /// have no IAsyncStateMachine. The compiler still hoists an await-in-catch body
  /// behind a flag local, and the flag check is a branch no test can take. The IL
  /// below matches SDK 11.0.100 Debug and Release output for that pattern.
  /// Instrumenter calls SimplifyMacros before GetBranchPoints, so the tests do too.
  /// </summary>
  public class CecilSymbolHelperRuntimeAsyncTests
  {
    private const MethodImplAttributes RuntimeAsyncMethodImplFlag = (MethodImplAttributes)0x2000;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetBranchPoints_RuntimeAsync_AwaitInCatch_SkipsGeneratedFlagBranch(bool debugShape)
    {
      MethodDefinition method = CreateAwaitInCatchMethod(debugShape, runtimeAsync: true, withUserBranch: false);

      IReadOnlyList<BranchPoint> points = new CecilSymbolHelper().GetBranchPoints(method);

      Assert.Empty(points);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetBranchPoints_SameCatchFlagIlWithoutRuntimeAsyncFlag_ReportsBranch(bool debugShape)
    {
      MethodDefinition method = CreateAwaitInCatchMethod(debugShape, runtimeAsync: false, withUserBranch: false);

      IReadOnlyList<BranchPoint> points = new CecilSymbolHelper().GetBranchPoints(method);

      Assert.Equal(2, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(GetGeneratedFlagBranch(method).Offset, points[0].Offset);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetBranchPoints_RuntimeAsync_AwaitInCatch_KeepsUserBranch(bool debugShape)
    {
      MethodDefinition method = CreateAwaitInCatchMethod(debugShape, runtimeAsync: true, withUserBranch: true);

      IReadOnlyList<BranchPoint> points = new CecilSymbolHelper().GetBranchPoints(method);

      Instruction userBranch = method.Body.Instructions.Single(i => i.OpCode == OpCodes.Brfalse);
      Instruction generatedBranch = GetGeneratedFlagBranch(method);

      Assert.Equal(2, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(userBranch.Offset, points[0].Offset);
      Assert.DoesNotContain(points, p => p.Offset == generatedBranch.Offset);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetBranchPoints_UserBranchAndCatchFlagWithoutRuntimeAsyncFlag_ReportsBoth(bool debugShape)
    {
      MethodDefinition method = CreateAwaitInCatchMethod(debugShape, runtimeAsync: false, withUserBranch: true);

      IReadOnlyList<BranchPoint> points = new CecilSymbolHelper().GetBranchPoints(method);

      Instruction userBranch = method.Body.Instructions.Single(i => i.OpCode == OpCodes.Brfalse);
      Instruction generatedBranch = GetGeneratedFlagBranch(method);

      Assert.Equal(4, points.Count);
      Assert.Contains(points, p => p.Offset == userBranch.Offset);
      Assert.Contains(points, p => p.Offset == generatedBranch.Offset);
    }

    private static Instruction GetGeneratedFlagBranch(MethodDefinition method)
    {
      return method.Body.Instructions.Single(i => i.OpCode == OpCodes.Beq || i.OpCode == OpCodes.Bne_Un);
    }

    private static MethodDefinition CreateAwaitInCatchMethod(bool debugShape, bool runtimeAsync, bool withUserBranch)
    {
      string typeName = $"RuntimeAsyncHost_{debugShape}_{runtimeAsync}_{withUserBranch}";
      var module = ModuleDefinition.CreateModule("RuntimeAsyncCatch", ModuleKind.Dll);
      var type = new TypeDefinition("Coverage", typeName, TypeAttributes.Class | TypeAttributes.Public, module.TypeSystem.Object);
      module.Types.Add(type);

      var method = new MethodDefinition("LoadAsync", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, module.TypeSystem.String);
      method.Parameters.Add(new ParameterDefinition("useFallbackFirst", ParameterAttributes.None, module.TypeSystem.Boolean));
      if (runtimeAsync)
      {
        method.ImplAttributes = RuntimeAsyncMethodImplFlag;
      }

      method.Body.InitLocals = true;
      method.Body.Variables.Add(new VariableDefinition(module.TypeSystem.Int32));
      method.Body.Variables.Add(new VariableDefinition(module.TypeSystem.String));
      type.Methods.Add(method);

      // Labels are wired after all instructions are created.
      Instruction userBranchTarget = Instruction.Create(OpCodes.Ldc_I4_0);
      Instruction handlerStart = Instruction.Create(OpCodes.Pop);
      Instruction handlerEnd = Instruction.Create(OpCodes.Ldloc_0);
      Instruction catchBody = debugShape ? Instruction.Create(OpCodes.Nop) : Instruction.Create(OpCodes.Ldstr, "fallback");
      Instruction unreachable = Instruction.Create(OpCodes.Ldnull);
      Instruction afterTry = Instruction.Create(OpCodes.Ldloc_1);

      if (withUserBranch)
      {
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse_S, userBranchTarget));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "fallback"));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
      }

      method.Body.Instructions.Add(userBranchTarget);
      method.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));

      Instruction tryStart = Instruction.Create(OpCodes.Ldstr, "primary");
      method.Body.Instructions.Add(tryStart);
      method.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));
      method.Body.Instructions.Add(Instruction.Create(OpCodes.Leave_S, afterTry));

      method.Body.Instructions.Add(handlerStart);
      if (debugShape)
      {
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Leave_S, handlerEnd));

        // Debug: ldloc.0 / ldc.i4.1 / beq.s catchBody / br.s unreachable
        method.Body.Instructions.Add(handlerEnd);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Beq_S, catchBody));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Br_S, unreachable));
        method.Body.Instructions.Add(catchBody);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "fallback"));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Br_S, afterTry));
      }
      else
      {
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Leave_S, handlerEnd));

        // Release: ldloc.0 / ldc.i4.1 / bne.un.s unreachable
        method.Body.Instructions.Add(handlerEnd);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Bne_Un_S, unreachable));
        method.Body.Instructions.Add(catchBody);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
      }

      method.Body.Instructions.Add(unreachable);
      method.Body.Instructions.Add(Instruction.Create(OpCodes.Throw));
      method.Body.Instructions.Add(afterTry);
      method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
      {
        CatchType = module.ImportReference(typeof(System.InvalidOperationException)),
        TryStart = tryStart,
        TryEnd = handlerStart,
        HandlerStart = handlerStart,
        HandlerEnd = handlerEnd
      });

      using var stream = new MemoryStream();
      module.Write(stream);
      stream.Position = 0;
      ModuleDefinition reloaded = ModuleDefinition.ReadModule(stream);
      TypeDefinition reloadedType = reloaded.Types.Single(x => x.Name == typeName);
      MethodDefinition reloadedMethod = reloadedType.Methods.Single(x => x.Name == "LoadAsync");
      reloadedMethod.Body.SimplifyMacros();
      return reloadedMethod;
    }
  }
}
