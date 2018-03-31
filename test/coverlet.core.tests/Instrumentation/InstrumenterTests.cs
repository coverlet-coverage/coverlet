using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Coverlet.Core.Attributes;
using Xunit;
using Coverlet.Core.Instrumentation;

namespace Coverlet.Core.Instrumentation.Tests
{
    public class InstrumenterTests : IDisposable
    {
        private readonly string _module;
        private readonly string _identifier;
        private readonly DirectoryInfo _tempDirectory;

        public InstrumenterTests()
        {
            //var module = typeof(InstrumenterTests).Assembly.Location;
            var module = typeof(Instrumenter).Assembly.Location;

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
        public void TestInstrument()
        {
            var instrumenter = new Instrumenter(_module, _identifier);
            var result = instrumenter.Instrument();

            Assert.Equal(Path.GetFileNameWithoutExtension(_module), result.Module);
            Assert.Equal(_module, result.ModulePath);
        }

        public static List<object[]> ExcludeFromCoverageAttributeNames = new List<object[]>
        {
            new object[] {typeof(ExcludeFromCoverageAttribute).Name},
            new object[] { RemoveAttributeSuffix(typeof(ExcludeFromCoverageAttribute).Name) }
        };

        private static string RemoveAttributeSuffix(string attributeName)
        {
            return attributeName.Replace("Attribute", "");
        }

        [Theory]
        [MemberData(nameof(ExcludeFromCoverageAttributeNames))]
        public void Instrument__ClassesWithExcludeFromCoverageAttributeMustExcludedFromResult(
            string excludeAttributeName)
        {
            // Arrange
            var validClassesThatMustBeExcluded = new List<string>() { };
            var validMethodsThatMustBeExcluded = new List<string>() { };
            // Act
            var instrumenter = new Instrumenter(_module, _identifier);
            var result = instrumenter.Instrument();
            var documents = result.Documents;
            // Assert
        }


        public static List<object[]> ExcludeFromCodeCoverageAttributeNames = new List<object[]>
        {
            new object[] {typeof(ExcludeFromCodeCoverageAttribute).Name},
            new object[] { RemoveAttributeSuffix(typeof(ExcludeFromCodeCoverageAttribute).Name) }
        };

        [Theory]
        [MemberData(nameof(ExcludeFromCoverageAttributeNames))]
        public void Instrument__ClassesWithExcludeFromCodeCoverageAttributeMustExcludedFromResult(
            string excludeAttributeName)
        {
            // Arrange

            // Act

            // Assert
        }
    }
}