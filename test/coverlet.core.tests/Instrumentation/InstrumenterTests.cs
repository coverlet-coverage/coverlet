using System;
using System.IO;
using System.Linq;
using Xunit;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Samples.Tests;

namespace Coverlet.Core.Instrumentation.Tests
{
    public class InstrumenterTests
    {
        [Fact]
        public void TestInstrument()
        {
            var instrumenterTest = CreateInstrumentor();

            var result = instrumenterTest.Instrumenter.Instrument();

            Assert.Equal(Path.GetFileNameWithoutExtension(instrumenterTest.Module), result.Module);
            Assert.Equal(instrumenterTest.Module, result.ModulePath);

            instrumenterTest.Directory.Delete(true);
        }

        [Theory]
        [InlineData(typeof(ClassExcludedByCodeAnalysisCodeCoverageAttr))]
        [InlineData(typeof(ClassExcludedByCoverletCodeCoverageAttr))]
        public void TestInstrument_ClassesWithExcludeAttributeAreExcluded(Type excludedType)
        {
            var instrumenterTest = CreateInstrumentor();
            var result = instrumenterTest.Instrumenter.Instrument();

            var doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Samples.cs");
            Assert.NotNull(doc);

            var found = doc.Lines.Values.Any(l => l.Class == excludedType.FullName);
            Assert.False(found, "Class decorated with with exclude attribute should be excluded");

            instrumenterTest.Directory.Delete(true);
        }

        private InstrumenterTest CreateInstrumentor()
        {
            string module = GetType().Assembly.Location;
            string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");
            string identifier = Guid.NewGuid().ToString();

            DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), identifier));

            File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
            File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

            module = Path.Combine(directory.FullName, Path.GetFileName(module));
            Instrumenter instrumenter = new Instrumenter(module, identifier, Array.Empty<string>(), Array.Empty<string>());
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
    }
}