using System.IO;
using System.Linq;
using System.Reflection;

using Xunit;
using Coverlet.Core.Samples.Tests;
using coverlet.tests.projectsample.netframework;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Coverlet.Core.Symbols.Tests
{
    public class CecilSymbolHelperTests
    {
        private ModuleDefinition _module;
        private readonly CecilSymbolHelper _cecilSymbolHelper;
        private readonly DefaultAssemblyResolver _resolver;
        private readonly ReaderParameters _parameters;

        public CecilSymbolHelperTests()
        {
            var location = GetType().Assembly.Location;
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
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSingleDecision)}"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(2, points.Count());
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
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSimpleUsingStatement)}"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            Assert.Equal(2, points.Count());
        }

        [Fact]
        public void GetBranchPoints_GeneratedBranches_DueToCachedAnonymousMethodDelegate_Ignored()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSimpleTaskWithLambda)}"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_TwoBranch()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasTwoDecisions)}"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(4, points.Count());
            Assert.Equal(points[0].Offset, points[1].Offset);
            Assert.Equal(points[2].Offset, points[3].Offset);
            Assert.Equal(28, points[0].StartLine);
            Assert.Equal(29, points[2].StartLine);
        }

        [Fact]
        public void GetBranchPoints_CompleteIf()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasCompleteIf)}"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(2, points.Count());
            Assert.Equal(points[0].Offset, points[1].Offset);
            Assert.Equal(35, points[0].StartLine);
            Assert.Equal(35, points[1].StartLine);
        }

#if !RELEASE // Issue https://github.com/tonerdo/coverlet/issues/389
        [Fact]
        public void GetBranchPoints_Switch()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSwitch)}"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(4, points.Count());
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
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSwitchWithDefault)}"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(4, points.Count());
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
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSwitchWithBreaks)}"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(4, points.Count());
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
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.HasSwitchWithMultipleCases)}"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(4, points.Count());
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
            var type = _module.Types.First(x => x.FullName.Contains("f__AnonymousType"));
            var method = type.Methods.First(x => x.FullName.Contains("::Equals"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            foreach (var branchPoint in points)
                Assert.Equal(-1, branchPoint.StartLine);
        }

        [Fact]
        public void GetBranchPoints_UsingWithException_Issue243_IgnoresBranchInFinallyBlock()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains($"::{nameof(DeclaredConstructorClass.UsingWithException_Issue243)}"));

            // check that the method is laid out the way we discovered it to be during the defect
            // @see https://github.com/OpenCover/opencover/issues/243
            Assert.Single(method.Body.ExceptionHandlers);
            Assert.NotNull(method.Body.ExceptionHandlers[0].HandlerStart);
            Assert.Null(method.Body.ExceptionHandlers[0].HandlerEnd);
            Assert.Equal(1, method.Body.Instructions.Count(i => i.OpCode.FlowControl == FlowControl.Cond_Branch));
            Assert.True(method.Body.Instructions.First(i => i.OpCode.FlowControl == FlowControl.Cond_Branch).Offset > method.Body.ExceptionHandlers[0].HandlerStart.Offset);

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_IgnoresSwitchIn_GeneratedMoveNext()
        {
            // arrange
            var nestedName = typeof(Iterator).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(Iterator).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_IgnoresBranchesIn_GeneratedMoveNextForSingletonIterator()
        {
            // arrange
            var nestedName = typeof(SingletonIterator).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(SingletonIterator).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_IgnoresBranchesIn_AsyncAwaitStateMachine()
        {
            // arrange
            var nestedName = typeof(AsyncAwaitStateMachine).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AsyncAwaitStateMachine).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_IgnoresBranchesIn_AsyncAwaitStateMachineNetFramework()
        {
            // arrange
            string location = Directory.GetFiles(Directory.GetCurrentDirectory(), "coverlet.tests.projectsample.netframework.dll").First();
            _resolver.AddSearchDirectory(Path.GetDirectoryName(location));
            _module = ModuleDefinition.ReadModule(location, _parameters);

            var nestedName = typeof(AsyncAwaitStateMachineNetFramework).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AsyncAwaitStateMachineNetFramework).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_IgnoresBranchesIn_AsyncAwaitValueTaskStateMachine()
        {
            // arrange
            var nestedName = typeof(AsyncAwaitValueTaskStateMachine).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AsyncAwaitValueTaskStateMachine).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_IgnoresMostBranchesIn_AwaitForeachStateMachine()
        {
            // arrange
            var nestedName = typeof(AwaitForeachStateMachine).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AwaitForeachStateMachine).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            // We do expect there to be a two-way branch (stay in the loop or not?) on
            // the line containing "await foreach".
            Assert.NotNull(points);
            Assert.Equal(2, points.Count());
            Assert.Equal(points[0].Offset, points[1].Offset);
            Assert.Equal(204, points[0].StartLine);
            Assert.Equal(204, points[1].StartLine);
        }

        [Fact]
        public void GetBranchPoints_IgnoresMostBranchesIn_AwaitForeachStateMachine_WithBranchesWithinIt()
        {
            // arrange
            var nestedName = typeof(AwaitForeachStateMachine_WithBranches).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AwaitForeachStateMachine_WithBranches).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            // We do expect there to be four branch points (two places where we can branch
            // two ways), one being the "stay in the loop or not?" branch on the line
            // containing "await foreach" and the other being the "if" statement inside
            // the loop.
            Assert.NotNull(points);
            Assert.Equal(4, points.Count());
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
            var nestedName = typeof(AsyncIteratorStateMachine).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AsyncIteratorStateMachine).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);
            
            // assert
            // We do expect the "for" loop to be a branch with two branch points, but that's it.
            Assert.NotNull(points);
            Assert.Equal(2, points.Count());
            Assert.Equal(237, points[0].StartLine);
            Assert.Equal(237, points[1].StartLine);
        }

        [Fact]
        public void GetBranchPoints_IgnoreBranchesIn_AwaitUsingStateMachine()
        {
            // arrange
            var nestedName = typeof(AwaitUsingStateMachine).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(AwaitUsingStateMachine).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_IgnoreBranchesIn_ScopedAwaitUsingStateMachine()
        {
            // arrange
            var nestedName = typeof(ScopedAwaitUsingStateMachine).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(ScopedAwaitUsingStateMachine).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_ExceptionFilter()
        {
            // arrange
            var type = _module.Types.Single(x => x.FullName == typeof(ExceptionFilter).FullName);
            var method = type.Methods.Single(x => x.FullName.Contains($"::{nameof(ExceptionFilter.Test)}"));
            // act
            var points = _cecilSymbolHelper.GetBranchPoints(method);

            Assert.Empty(points);
        }
    }
}