using System;
using System.IO;

using Xunit;
using Coverlet.Core.Instrumentation;

namespace Coverlet.Core.Instrumentation.Tests
{
    public class InstrumenterTests
    {
        [Fact]
        public void TestInstrument()
        {
            //var module = typeof(InstrumenterTests).Assembly.Location;
            var module = typeof(Instrumenter).Assembly.Location;
            
            var pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");
            var identifier = Guid.NewGuid().ToString();

            var tempDirectoryPath = Path.Combine(Path.GetTempPath(), identifier);
            var tempDirectory = Directory.CreateDirectory(tempDirectoryPath);
            
            var moduleFileForCopy = Path.Combine(tempDirectory.FullName, Path.GetFileName(module));
            var pdbFileForCopy = Path.Combine(tempDirectory.FullName, Path.GetFileName(pdb));
            File.Copy(module, moduleFileForCopy, true);
            File.Copy(pdb, pdbFileForCopy, true);

            module = Path.Combine(tempDirectory.FullName, Path.GetFileName(module));

            var instrumenter = new Instrumenter(module, identifier);
            var result = instrumenter.Instrument();

            Assert.Equal(Path.GetFileNameWithoutExtension(module), result.Module);
            Assert.Equal(module, result.ModulePath);

            tempDirectory.Delete(true);
        }
    }
}