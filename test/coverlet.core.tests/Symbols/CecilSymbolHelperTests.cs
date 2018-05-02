using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Xunit;
using Coverlet.Core.Samples.Tests;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Coverlet.Core.Symbols.Tests
{
    public class CecilSymbolHelperTests
    {
        private ModuleDefinition _module;
        public CecilSymbolHelperTests()
        {
            var location = GetType().Assembly.Location;
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(location));
            var parameters = new ReaderParameters { ReadSymbols = true, AssemblyResolver = resolver };
            _module = ModuleDefinition.ReadModule(location, parameters);
        }

        [Fact]
        public void GetBranchPoints_OneBranch()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains("::HasSingleDecision"));

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(2, points.Count());
            Assert.Equal(points[0].Offset, points[1].Offset);
            Assert.Equal(0, points[0].Path);
            Assert.Equal(1, points[1].Path);
            Assert.Equal(19, points[0].StartLine);
            Assert.Equal(19, points[1].StartLine);
            Assert.NotNull(points[1].Document);
            Assert.Equal(points[0].Document, points[1].Document);
        }

        [Fact]
        public void GetBranchPoints_Using_Where_GeneratedBranchesIgnored()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains("::HasSimpleUsingStatement"));

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);

            Assert.Equal(2, points.Count());
        }

        [Fact]
        public void GetBranchPoints_GeneratedBranches_DueToCachedAnonymousMethodDelegate_Ignored()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains("::HasSimpleTaskWithLambda"));

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);

            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_TwoBranch()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains("::HasTwoDecisions"));

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(4, points.Count());
            Assert.Equal(points[0].Offset, points[1].Offset);
            Assert.Equal(points[2].Offset, points[3].Offset);
            Assert.Equal(25, points[0].StartLine);
            Assert.Equal(26, points[2].StartLine);
        }

        [Fact]
        public void GetBranchPoints_CompleteIf()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains("::HasCompleteIf"));

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(2, points.Count());
            Assert.Equal(points[0].Offset, points[1].Offset);
            Assert.Equal(32, points[0].StartLine);
            Assert.Equal(32, points[1].StartLine);
        }

        [Fact]
        public void GetBranchPoints_Switch()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains("::HasSwitch"));

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(4, points.Count());
            Assert.Equal(points[0].Offset, points[1].Offset);
            Assert.Equal(points[0].Offset, points[2].Offset);            
            Assert.Equal(3, points[3].Path);
            
            Assert.Equal(44, points[0].StartLine);
            Assert.Equal(44, points[1].StartLine);
            Assert.Equal(44, points[2].StartLine);
            Assert.Equal(44, points[3].StartLine);
        }

        [Fact]
        public void GetBranchPoints_SwitchWithDefault()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains("::HasSwitchWithDefault"));

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(4, points.Count());
            Assert.Equal(points[0].Offset, points[1].Offset);
            Assert.Equal(points[0].Offset, points[2].Offset);
            Assert.Equal(3, points[3].Path);
            
            Assert.Equal(58, points[0].StartLine);
            Assert.Equal(58, points[1].StartLine);
            Assert.Equal(58, points[2].StartLine);
            Assert.Equal(58, points[3].StartLine);
        }

        [Fact]
        public void GetBranchPoints_SwitchWithBreaks()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains("::HasSwitchWithBreaks"));

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.NotNull(points);
            Assert.Equal(4, points.Count());
            Assert.Equal(points[0].Offset, points[1].Offset);
            Assert.Equal(points[0].Offset, points[2].Offset);
            Assert.Equal(3, points[3].Path);

            Assert.Equal(74, points[0].StartLine);
            Assert.Equal(74, points[1].StartLine);
            Assert.Equal(74, points[2].StartLine);
            Assert.Equal(74, points[3].StartLine);
        }

        [Fact]
        public void GetBranchPoints_SwitchWithMultipleCases()
        {
            // arrange
            var type = _module.Types.First(x => x.FullName == typeof(DeclaredConstructorClass).FullName);
            var method = type.Methods.First(x => x.FullName.Contains("::HasSwitchWithMultipleCases"));

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);
            
            // assert
            Assert.NotNull(points);
            Assert.Equal(4, points.Count()); 
            Assert.Equal(points[0].Offset, points[1].Offset);
            Assert.Equal(points[0].Offset, points[2].Offset);
            Assert.Equal(points[0].Offset, points[3].Offset);
            Assert.Equal(3, points[3].Path);

            Assert.Equal(92, points[0].StartLine);
            Assert.Equal(92, points[1].StartLine);
            Assert.Equal(92, points[2].StartLine);
            Assert.Equal(92, points[3].StartLine);
        }

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
            var points = CecilSymbolHelper.GetBranchPoints(method);

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
            var method = type.Methods.First(x => x.FullName.Contains("::UsingWithException_Issue243"));

            // check that the method is laid out the way we discovered it to be during the defect
            // @see https://github.com/OpenCover/opencover/issues/243
            Assert.Single(method.Body.ExceptionHandlers);
            Assert.NotNull(method.Body.ExceptionHandlers[0].HandlerStart);
            Assert.Null(method.Body.ExceptionHandlers[0].HandlerEnd);
            Assert.Equal(1, method.Body.Instructions.Count(i => i.OpCode.FlowControl == FlowControl.Cond_Branch));
            Assert.True(method.Body.Instructions.First(i => i.OpCode.FlowControl == FlowControl.Cond_Branch).Offset > method.Body.ExceptionHandlers[0].HandlerStart.Offset);

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.Empty(points);
        }

        [Fact]
        public void GetBranchPoints_IgnoresSwitchIn_GeneratedMoveNext()
        {
            // arrange
            var nestedName = typeof (Iterator).GetNestedTypes(BindingFlags.NonPublic).First().Name;
            var type = _module.Types.FirstOrDefault(x => x.FullName == typeof(Iterator).FullName);
            var nestedType = type.NestedTypes.FirstOrDefault(x => x.FullName.EndsWith(nestedName));
            var method = nestedType.Methods.First(x => x.FullName.EndsWith("::MoveNext()"));

            // act
            var points = CecilSymbolHelper.GetBranchPoints(method);

            // assert
            Assert.Empty(points);

        }
    }
}