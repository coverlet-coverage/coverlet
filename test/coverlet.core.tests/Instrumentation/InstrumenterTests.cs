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
            string module = GetType().Assembly.Location;
            string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");
            string identifier = Guid.NewGuid().ToString();

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), identifier));

            File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
            File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

            module = Path.Combine(directory.FullName, Path.GetFileName(module));
            Instrumenter instrumenter = new Instrumenter(module, identifier, Array.Empty<string>(), Array.Empty<string>());
            var result = instrumenter.Instrument();

            Assert.Equal(Path.GetFileNameWithoutExtension(module), result.Module);
            Assert.Equal(module, result.ModulePath);

            directory.Delete(true);
        }
    }
}