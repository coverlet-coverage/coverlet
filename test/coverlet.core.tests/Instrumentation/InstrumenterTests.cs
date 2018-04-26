using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Coverlet.Core.Attributes;
using Xunit;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Instrumentation.Tests.TestClasses;

namespace Coverlet.Core.Instrumentation.Tests
{
    [ExcludeFromCodeCoverage]
    public class InstrumenterTests : IDisposable
    {
        private readonly string _module;
        private readonly string _identifier;
        private readonly DirectoryInfo _tempDirectory;

        public InstrumenterTests()
        {
            var module = typeof(InstrumenterTests).Assembly.Location;

            var pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");
            _identifier = Guid.NewGuid().ToString();

            var tempDirectoryPath = Path.Combine(Path.GetTempPath(), _identifier);
            _tempDirectory = Directory.CreateDirectory(tempDirectoryPath);

            var moduleFileForCopy = Path.Combine(_tempDirectory.FullName, Path.GetFileName(module));
            var pdbFileForCopy = Path.Combine(_tempDirectory.FullName, Path.GetFileName(pdb));
            File.Copy(module, moduleFileForCopy, true);
            File.Copy(pdb, pdbFileForCopy, true);

            _module = Path.Combine(_tempDirectory.FullName, Path.GetFileName(module));
        }

        public void Dispose()
        {
            _tempDirectory.Delete(true);
        }

        [Fact]
        public void Instrument__ResultModuleIsValid()
        {
            // Arrange
            var instrumenter = new Instrumenter(_module, _identifier);
            // Act
            var result = instrumenter.Instrument();
            // Assert
            var validModuleName = Path.GetFileNameWithoutExtension(_module);
            Assert.Equal(validModuleName, result.Module);
            Assert.Equal(_module, result.ModulePath);
        }
        [Fact]
        public void Instrument__ResultModulePathIsValid()
        {
            // Arrange
            var instrumenter = new Instrumenter(_module, _identifier);
            // Act
            var result = instrumenter.Instrument();
            // Assert
            Assert.Equal(_module, result.ModulePath);
        }

        private List<string> GetMethodsThatMustBeIncludedToCoverage(Type classType, Type excludeAttributeType)
        {
            var methodsForIncludingToCodeCoverage = classType
                .GetMethods()
                .Where(e => e.DeclaringType.Name == classType.Name &&
                            (!e.CustomAttributes.Any() ||
                             e.CustomAttributes.Any(a => a.AttributeType.Name != excludeAttributeType.Name)))
                .Select(e => e.GetMethodNameInMonoCecilFormat())
                .ToList();
            return methodsForIncludingToCodeCoverage;
        }

        private List<string> GetMethodsThatMustBeExcludedFromCoverage(Type classType, Type excludeAttributeType)
        {
            var methodsForExcludingFromCodeCoverage = classType
                .GetMethods()
                .Where(e => e.CustomAttributes.Any(a => a.AttributeType.Name == excludeAttributeType.Name))
                .Select(e => e.GetMethodNameInMonoCecilFormat())
                .ToList();
            return methodsForExcludingFromCodeCoverage;
        }

        public static List<object[]> ClassesWithExcludingAttributes = new List<object[]>
        {
            new object[] {typeof(ClassWithExcludeFromCoverage)},
            new object[] {typeof(ClassWithExcludeFromCodeCoverage)}
        };

        [Theory]
        [MemberData(nameof(ClassesWithExcludingAttributes))]
        public void Instrument__ClassWithExcludeFromCodeCoverageAttributeMustExcludedFromResult(
            Type targetClassType)
        {
            // Arrange
            var classForExcludingFromCodeCoverage = targetClassType.Name;
            // Act
            var instrumenter = new Instrumenter(_module, _identifier);
            var result = instrumenter.Instrument();
            var documents = result.Documents;
            // Assert
            var classNamesForIncludingInCodeCoverage =
                documents.Select(e => Path.GetFileNameWithoutExtension(e.Path)).ToList();
            Assert.NotEmpty(classNamesForIncludingInCodeCoverage);
            Assert.DoesNotContain(classForExcludingFromCodeCoverage, classNamesForIncludingInCodeCoverage);
            //Assert.Null(classNamesForIncludingInCodeCoverage.FirstOrDefault(e => e == classForExcludingFromCodeCoverage));
        }

        public static List<object[]> ClassesWithMethodsWithExcludingAttributes = new List<object[]>
        {
            new object[] {typeof(ClassWithExcludeFromCoverageOnMethods), typeof(ExcludeFromCoverageAttribute)},
            new object[] {typeof(ClassWithExcludeFromCodeCoverageOnMethods), typeof(ExcludeFromCodeCoverageAttribute) }
        };
        [Theory]
        [MemberData(nameof(ClassesWithMethodsWithExcludingAttributes))]
        public void Instrument__MethodsWithoutExcludeAttributeMustBeIncludedToResult(
            Type targetClassType, Type targetExcludeAttributeType)
        {
            // Arrange
            var targetClassName = targetClassType.Name;
            var methodsForIncludingToCodeCoverage =
                GetMethodsThatMustBeIncludedToCoverage(targetClassType, targetExcludeAttributeType);
            // Act
            var instrumenter = new Instrumenter(_module, _identifier);
            var result = instrumenter.Instrument();
            var documents = result.Documents;
            var foundClass = documents.FirstOrDefault(e =>
                Path.GetFileNameWithoutExtension(e.Path) == targetClassName);
            // Assert
            Assert.NotNull(foundClass);
            var methodsForIncluding = foundClass.Lines.Select(l => l.Method).Distinct().ToList();
            var intersections = methodsForIncluding.Intersect(methodsForIncludingToCodeCoverage);
            Assert.NotEmpty(intersections);
            Assert.Equal(methodsForIncludingToCodeCoverage.Count, methodsForIncluding.Count);
        }
        [Theory]
        [MemberData(nameof(ClassesWithMethodsWithExcludingAttributes))]
        public void Instrument__MethodsWithExcludeAttributeMustBeExcludedFromResult(
            Type targetClassType, Type targetExcludeAttributeType)
        {
            // Arrange
            var targetClassName = targetClassType.Name;
            var methodsForExcludingFromCodeCoverage =
                GetMethodsThatMustBeExcludedFromCoverage(targetClassType, targetExcludeAttributeType);
            // Act
            var instrumenter = new Instrumenter(_module, _identifier);
            var result = instrumenter.Instrument();
            var documents = result.Documents;
            var foundClass = documents.FirstOrDefault(e =>
                Path.GetFileNameWithoutExtension(e.Path) == targetClassName);
            // Assert
            Assert.NotNull(foundClass);
            var methodsForIncluding = foundClass.Lines.Select(l => l.Method).Distinct().ToList();
            var intersections = methodsForIncluding.Intersect(methodsForExcludingFromCodeCoverage);
            Assert.Empty(intersections);
        }
    }
}