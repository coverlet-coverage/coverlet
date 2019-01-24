using System;
using System.IO;

using Xunit;
using Moq;

using Coverlet.Core;
using System.Collections.Generic;
using System.Linq;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Tests.Instrumentation;

namespace Coverlet.Core.Tests
{
    [Collection(nameof(ModuleTrackerTemplate))]
    public class CoverageTests
    {
        [Fact]
        public void TestCoverage()
        {
            string module = GetType().Assembly.Location;
            string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
            File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

            // TODO: Mimic hits by calling ModuleTrackerTemplate.RecordHit before Unload

            var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), string.Empty, false);
            coverage.PrepareModules();

            // The module hit tracker must signal to Coverage that it has done its job, so call it manually
            var instrumenterResult = coverage.Results.Single();
            ModuleTrackerTemplate.HitsArray = new int[instrumenterResult.HitCandidates.Count + ModuleTrackerTemplate.HitsResultHeaderSize];
            ModuleTrackerTemplate.HitsFilePath = instrumenterResult.HitsFilePath;
            ModuleTrackerTemplate.HitsMemoryMapName = instrumenterResult.HitsResultGuid;
            ModuleTrackerTemplate.UnloadModule(null, null);

            var result = coverage.GetCoverageResult();

            Assert.NotEmpty(result.Modules);

            directory.Delete(true);
        }
    }
}