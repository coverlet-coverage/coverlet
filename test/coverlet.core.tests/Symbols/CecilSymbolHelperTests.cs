// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Reflection;
using coverlet.tests.projectsample.netframework;
using Coverlet.Core.Samples.Tests;
using Coverlet.Core.Symbols;
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
  }
}
