// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Reflection;
using coverlet.tests.projectsample.netframework;
using Coverlet.Core.Samples.Tests;
using Coverlet.Core.Symbols;
using Coverlet.Tests.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Coverlet.Core.Tests.Symbols
{
  public class CecilSymbolHelperTests
  {
    private ModuleDefinition _module;
    private readonly CecilSymbolHelper _cecilSymbolHelper;
    private readonly DefaultAssemblyResolver _resolver;
    private readonly ReaderParameters _parameters;

    public CecilSymbolHelperTests()
    {
      string location = GetType().Assembly.Location;
      _resolver = new DefaultAssemblyResolver();
      _resolver.AddSearchDirectory(Path.GetDirectoryName(location));
      _parameters = new ReaderParameters { ReadSymbols = true, AssemblyResolver = _resolver };
      _module = ModuleDefinition.ReadModule(location, _parameters);
      _cecilSymbolHelper = new CecilSymbolHelper();
    }

    [Fact]
    public void GetBranchPoints_OneBranch()
    {
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSingleDecision)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.NotNull(points);
      Assert.Equal(2, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(0, points[0].Path);
      Assert.Equal(1, points[1].Path);
      Assert.Equal(22, points[0].StartLine);
      Assert.Equal(22, points[1].StartLine);
      Assert.NotNull(points[1].Document);
      Assert.Equal(points[0].Document, points[1].Document);
    }

    [Fact]
    public void GetBranchPoints_Using_Where_GeneratedBranchesIgnored()
    {
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSimpleUsingStatement)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      Assert.Equal(2, points.Count);
    }

    [Fact]
    public void GetBranchPoints_GeneratedBranches_DueToCachedAnonymousMethodDelegate_Ignored()
    {
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSimpleTaskWithLambda)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      Assert.Empty(points);
    }

    [Fact]
    public void GetBranchPoints_TwoBranch()
    {
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasTwoDecisions)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.NotNull(points);
      Assert.Equal(4, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(points[2].Offset, points[3].Offset);
      Assert.Equal(28, points[0].StartLine);
      Assert.Equal(29, points[2].StartLine);
    }

    [Fact]
    public void GetBranchPoints_CompleteIf()
    {
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasCompleteIf)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.NotNull(points);
      Assert.Equal(2, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(35, points[0].StartLine);
      Assert.Equal(35, points[1].StartLine);
    }

#if !RELEASE // Issue https://github.com/tonerdo/coverlet/issues/389
    [Fact]
    public void GetBranchPoints_Switch()
    {
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSwitch)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.NotNull(points);
      Assert.Equal(4, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(points[0].Offset, points[2].Offset);
      Assert.Equal(3, points[3].Path);

      Assert.Equal(47, points[0].StartLine);
      Assert.Equal(47, points[1].StartLine);
      Assert.Equal(47, points[2].StartLine);
      Assert.Equal(47, points[3].StartLine);
    }

    [Fact]
    public void GetBranchPoints_SwitchWithDefault()
    {
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSwitchWithDefault)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.NotNull(points);
      Assert.Equal(4, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(points[0].Offset, points[2].Offset);
      Assert.Equal(3, points[3].Path);

      Assert.Equal(61, points[0].StartLine);
      Assert.Equal(61, points[1].StartLine);
      Assert.Equal(61, points[2].StartLine);
      Assert.Equal(61, points[3].StartLine);
    }

    [Fact]
    public void GetBranchPoints_SwitchWithBreaks()
    {
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSwitchWithBreaks)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.NotNull(points);
      Assert.Equal(4, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(points[0].Offset, points[2].Offset);
      Assert.Equal(3, points[3].Path);

      Assert.Equal(77, points[0].StartLine);
      Assert.Equal(77, points[1].StartLine);
      Assert.Equal(77, points[2].StartLine);
      Assert.Equal(77, points[3].StartLine);
    }

    [Fact]
    public void GetBranchPoints_SwitchWithMultipleCases()
    {
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSwitchWithMultipleCases)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.NotNull(points);
      Assert.Equal(4, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(points[0].Offset, points[2].Offset);
      Assert.Equal(points[0].Offset, points[3].Offset);
      Assert.Equal(3, points[3].Path);

      Assert.Equal(95, points[0].StartLine);
      Assert.Equal(95, points[1].StartLine);
      Assert.Equal(95, points[2].StartLine);
      Assert.Equal(95, points[3].StartLine);
    }
#endif

    [Fact]
    public void GetBranchPoints_AssignsNegativeLineNumberToBranchesInMethodsThatHaveNoInstrumentablePoints()
    {
      /* 
       * Yes these actually exist - the compiler is very inventive
       * in this case for an anonymous class the compiler will dynamically create an Equals 'utility' method. 
       */
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName.Contains("f__AnonymousType"));
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains("::Equals"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.NotNull(points);
      foreach (BranchPoint branchPoint in points)
        Assert.Equal(-1, branchPoint.StartLine);
    }

    [Fact]
    public void GetBranchPoints_UsingWithException_Issue243_IgnoresBranchInFinallyBlock()
    {
      // arrange
      TypeDefinition type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
      MethodDefinition method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.UsingWithException_Issue243)}"));

      // check that the method is laid out the way we discovered it to be during the defect
      // @see https://github.com/OpenCover/opencover/issues/243
      Assert.Single(method.Body.ExceptionHandlers);
      Assert.NotNull(method.Body.ExceptionHandlers[0].HandlerStart);
      Assert.Null(method.Body.ExceptionHandlers[0].HandlerEnd);
      Assert.Equal(1, method.Body.Instructions.Count(i => i.OpCode.FlowControl == FlowControl.Cond_Branch));
      Assert.True(method.Body.Instructions.First(i => i.OpCode.FlowControl == FlowControl.Cond_Branch).Offset > method.Body.ExceptionHandlers[0].HandlerStart.Offset);

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.Empty(points);
    }

    [Fact]
    public void GetBranchPoints_IgnoresSwitchIn_GeneratedMoveNext()
    {
      // arrange
      string nestedName = typeof(Iterator).GetNestedTypes(BindingFlags.NonPublic)[0].Name;
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(Iterator).FullName);
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.Empty(points);
    }

    [Fact]
    public void GetBranchPoints_IgnoresBranchesIn_GeneratedMoveNextForSingletonIterator()
    {
      // arrange
      string nestedName = typeof(SingletonIterator).GetNestedTypes(BindingFlags.NonPublic)[0].Name;
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(SingletonIterator).FullName);
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.Empty(points);
    }

    [Fact]
    public void GetBranchPoints_IgnoresBranchesIn_AsyncAwaitStateMachine()
    {
      // arrange
      string nestedName = typeof(AsyncAwaitStateMachine).GetNestedTypes(BindingFlags.NonPublic)[0].Name;
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AsyncAwaitStateMachine).FullName);
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.Empty(points);
    }

    [Fact]
    public void GetBranchPoints_IgnoresBranchesIn_AsyncAwaitStateMachineNetFramework()
    {
      // arrange
      string location = Directory.GetFiles(Directory.GetCurrentDirectory(), "coverlet.tests.projectsample.netframework.dll")[0];
      _resolver.AddSearchDirectory(Path.GetDirectoryName(location));
      _module = ModuleDefinition.ReadModule(location, _parameters);

      string nestedName = typeof(AsyncAwaitStateMachineNetFramework).GetNestedTypes(BindingFlags.NonPublic)[0].Name;
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AsyncAwaitStateMachineNetFramework).FullName);
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.Empty(points);
    }

    [Fact]
    public void GetBranchPoints_IgnoresBranchesIn_AsyncAwaitValueTaskStateMachine()
    {
      // arrange
      string nestedName = typeof(AsyncAwaitValueTaskStateMachine).GetNestedTypes(BindingFlags.NonPublic)[0].Name;
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AsyncAwaitValueTaskStateMachine).FullName);
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.Empty(points);
    }

    [Fact]
    public void GetBranchPoints_IgnoresMostBranchesIn_AwaitForeachStateMachine()
    {
      // arrange
      string nestedName = typeof(AwaitForeachStateMachine).GetNestedTypes(BindingFlags.NonPublic)[0].Name;
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AwaitForeachStateMachine).FullName);
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      // We do expect there to be a two-way branch (stay in the loop or not?) on
      // the line containing "await foreach".
      Assert.NotNull(points);
      Assert.Equal(2, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(204, points[0].StartLine);
      Assert.Equal(204, points[1].StartLine);
    }

    [Fact]
    public void GetBranchPoints_IgnoresMostBranchesIn_AwaitForeachStateMachine_WithBranchesWithinIt()
    {
      // arrange
      string nestedName = typeof(AwaitForeachStateMachine_WithBranches).GetNestedTypes(BindingFlags.NonPublic)[0].Name;
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AwaitForeachStateMachine_WithBranches).FullName);
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      // We do expect there to be four branch points (two places where we can branch
      // two ways), one being the "stay in the loop or not?" branch on the line
      // containing "await foreach" and the other being the "if" statement inside
      // the loop.
      Assert.NotNull(points);
      Assert.Equal(4, points.Count);
      Assert.Equal(points[0].Offset, points[1].Offset);
      Assert.Equal(points[2].Offset, points[3].Offset);
      Assert.Equal(219, points[0].StartLine);
      Assert.Equal(219, points[1].StartLine);
      Assert.Equal(217, points[2].StartLine);
      Assert.Equal(217, points[3].StartLine);
    }

    [Fact]
    public void GetBranchPoints_IgnoresExtraBranchesIn_AsyncIteratorStateMachine()
    {
      // arrange
      string nestedName = typeof(AsyncIteratorStateMachine).GetNestedTypes(BindingFlags.NonPublic)[0].Name;
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AsyncIteratorStateMachine).FullName);
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      // We do expect the "for" loop to be a branch with two branch points, but that's it.
      Assert.NotNull(points);
      Assert.Equal(2, points.Count);
      Assert.Equal(237, points[0].StartLine);
      Assert.Equal(237, points[1].StartLine);
    }

    [Fact]
    public void GetBranchPoints_IgnoreBranchesIn_AwaitUsingStateMachine()
    {
      // arrange
      string nestedName = typeof(AwaitUsingStateMachine).GetNestedTypes(BindingFlags.NonPublic)[0].Name;
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AwaitUsingStateMachine).FullName);
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.Empty(points);
    }

    [Fact]
    public void GetBranchPoints_IgnoreBranchesIn_ScopedAwaitUsingStateMachine()
    {
      // arrange
      string nestedName = typeof(ScopedAwaitUsingStateMachine).GetNestedTypes(BindingFlags.NonPublic)[0].Name;
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(ScopedAwaitUsingStateMachine).FullName);
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      Assert.Empty(points);
    }

    [Fact]
    public void GetBranchPoints_ExceptionFilter()
    {
      // arrange
      TypeDefinition type = _module.Types.Single(x => x.FullName == typeof(ExceptionFilter).FullName);
      MethodDefinition method = type.Methods.Single(x => x.FullName.Contains($"::{nameof(ExceptionFilter.Test)}"));
      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      Assert.Empty(points);
    }

    /// <summary>
    /// Issue #1335: Tests combined await foreach + yield return pattern.
    /// The TransformAsync method is both an async iterator (produces IAsyncEnumerable)
    /// and consumes another IAsyncEnumerable via await foreach.
    /// </summary>
    [Fact]
    public void GetBranchPoints_Issue1335_AsyncIteratorWithAwaitForeach_Transform()
    {
      // arrange - get the nested state machine type for TransformAsync
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AsyncIteratorWithAwaitForeach).FullName);
      Assert.NotNull(type);

      // Find the compiler-generated state machine for TransformAsync
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.Contains("<TransformAsync>"));
      Assert.NotNull(nestedType);

      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));
      Assert.NotNull(method);

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      // TransformAsync has one user branch: the await foreach (loop or exit)
      // All other branches should be filtered out as compiler-generated
      // Expected: 2 branch points (the await foreach continue/exit condition)
      Assert.NotNull(points);

      // Output diagnostic info for debugging
      foreach (BranchPoint bp in points)
      {
        System.Diagnostics.Debug.WriteLine($"BranchPoint: Line={bp.StartLine}, Offset={bp.Offset}, Path={bp.Path}, Ordinal={bp.Ordinal}");
      }

      // The TransformAsync method should have only 2 branch points (await foreach loop condition)
      // If this fails with more than 2, we have phantom branches that need to be filtered
      Assert.Equal(2, points.Count);
    }

    /// <summary>
    /// Issue #1335: Tests the more complex BatchAsync pattern with conditional yield return.
    /// </summary>
    [Fact]
    public void GetBranchPoints_Issue1335_AsyncIteratorWithAwaitForeach_Batch()
    {
      // arrange - get the nested state machine type for BatchAsync
      TypeDefinition type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AsyncIteratorWithAwaitForeach).FullName);
      Assert.NotNull(type);

      // Find the compiler-generated state machine for BatchAsync
      TypeDefinition nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.Contains("<BatchAsync>"));
      Assert.NotNull(nestedType);

      MethodDefinition method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));
      Assert.NotNull(method);

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert
      // BatchAsync has three user branches:
      // 1. await foreach (loop or exit) - 2 branch points
      // 2. if (batch.Count >= batchSize) - 2 branch points
      // 3. if (batch.Count > 0) - 2 branch points
      // Total expected: 6 branch points
      Assert.NotNull(points);

      // Output diagnostic info for debugging
      foreach (BranchPoint bp in points)
      {
        System.Diagnostics.Debug.WriteLine($"BranchPoint: Line={bp.StartLine}, Offset={bp.Offset}, Path={bp.Path}, Ordinal={bp.Ordinal}");
      }

      // If this fails with more than 6, we have phantom branches that need to be filtered
      Assert.Equal(6, points.Count);
    }

    [Fact]
    public void GetBranchPoints_RelationalAndPattern_CompoundAnd_PhantomBranchSkipped()
    {
      // https://github.com/coverlet-coverage/coverlet/issues/1313
      // `prefix.StartsWith("x") && c is >= 'a' and <= 'z'` compiles differently per configuration:
      //
      // Debug IL: brfalse + blt each target a private { ldc.i4.0; br merge } stub that is
      //   never reachable from user code. Coverlet's SkipBranchGeneratedByRelationalPattern
      //   heuristic suppresses those stubs -> 4 branch points (2 real decisions x 2 paths).
      //
      // Release IL: brfalse + blt + bgt all jump directly to `ldc.i4.0; ret`.
      //   There is no intermediate `br` stub so the heuristic does not fire and all three
      //   conditional branches are genuine decision points -> 6 branch points.

      // arrange
      TypeDefinition type = _module.Types.Single(x => x.FullName == typeof(RelationalPatternBranch).FullName);
      MethodDefinition method = type.Methods.Single(x => x.FullName.Contains($"::{nameof(RelationalPatternBranch.IsLowerInCompoundCondition)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert — debug and release compilers emit structurally different IL for this pattern
#if DEBUG
      // Debug: phantom blt stub suppressed by heuristic -> 2 real decisions x 2 paths = 4
      Assert.Equal(4, points.Count);
#else
      // Release: all three conditional branches are genuine -> 3 decisions x 2 paths = 6
      Assert.Equal(6, points.Count);
#endif
    }

    [Fact]
    public void GetBranchPoints_RelationalAndPattern_SimpleIf_BltIsRealBranch()
    {
      // https://github.com/coverlet-coverage/coverlet/issues/1313
      // In `if (c is >= 'a' and <= 'z')` the blt and the brfalse SHARE the same false block
      // (ends with stloc, not br). The blt is therefore a real source-level decision point and
      // must NOT be skipped. Expected: 4 branch points (blt lower-bound + brfalse result).

      // arrange
      TypeDefinition type = _module.Types.Single(x => x.FullName == typeof(RelationalPatternBranch).FullName);
      MethodDefinition method = type.Methods.Single(x => x.FullName.Contains($"::{nameof(RelationalPatternBranch.IsLowerInSimpleIf)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> points = _cecilSymbolHelper.GetBranchPoints(method);

      // assert - 4 paths: blt (2) + brfalse (2); blt is NOT phantom here
      Assert.Equal(4, points.Count);
    }

    [Fact]
    public void GetBranchPoints_PatternMatchingOr_ShouldMatchOrOperator_AndStayOnSourceLine()
    {
      // https://github.com/coverlet-coverage/coverlet/issues/1969
      // Pattern matching `or` must not report extra synthetic branches compared to `||`.

      // arrange
      TypeDefinition type = _module.Types.Single(x => x.FullName == typeof(PatternMatchingOr).FullName);
      MethodDefinition orOperatorMethod = type.Methods.Single(x => x.FullName.Contains($"::{nameof(PatternMatchingOr.OrOperator)}"));
      MethodDefinition patternMatchingOrMethod = type.Methods.Single(x => x.FullName.Contains($"::{nameof(PatternMatchingOr.PatternMatchingOrMethod)}"));

      // act
      System.Collections.Generic.IReadOnlyList<BranchPoint> orOperatorPoints = _cecilSymbolHelper.GetBranchPoints(orOperatorMethod);
      System.Collections.Generic.IReadOnlyList<BranchPoint> patternMatchingOrPoints = _cecilSymbolHelper.GetBranchPoints(patternMatchingOrMethod);

      // assert
      // OrOperator (text == "hello" || text == "world") compiles to ONE conditional branch
      // (brtrue.s for the first comparison; the second feeds directly into br.s/stloc) = 2 branch points.
      // PatternMatchingOrMethod (text is "hello" or "world") compiles to TWO brtrue.s instructions;
      // the heuristic skips the last one (whose fall-through is an unconditional br) so the
      // effective count equals OrOperator: 2 branch points.
      Assert.Equal(2, orOperatorPoints.Count);
      if (TestUtils.GetAssemblyBuildConfiguration() == BuildConfiguration.Debug)
      {
        Assert.Equal(2, patternMatchingOrPoints.Count);
      }
      else
      {
        Assert.Equal(4, patternMatchingOrPoints.Count);
      }
      // All branch points must map to the same user-visible source line (the or-expression itself).
      // We derive the expected line from the branch points rather than from the first sequence point,
      // because in Debug builds the first sequence point is the opening brace on the preceding line.
      int branchLine = patternMatchingOrPoints[0].StartLine;
      Assert.True(branchLine > 0, "Branch points must map to a valid source line, not a compiler-generated prologue");
      Assert.All(patternMatchingOrPoints, x => Assert.Equal(branchLine, x.StartLine));
    }
  }
}
