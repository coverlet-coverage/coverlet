using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace Coverlet.Core.Helpers
{
    internal static class InstrumentationHelper
    {
        public static string[] GetCoverableModules(string module)
        {
            IEnumerable<string> modules = Directory.GetFiles(Path.GetDirectoryName(module), "*.dll");
            modules = modules.Where(a => Path.GetFileName(a) != Path.GetFileName(module));
            modules = modules.Where(a => HasPdb(a));
            return modules.ToArray();
        }

        public static bool HasPdb(string module)
        {
            using (var peReader = new PEReader(File.OpenRead(module)))
            {
                foreach (var entry in peReader.ReadDebugDirectory())
                {
                    if (entry.Type == DebugDirectoryEntryType.CodeView)
                    {
                        var codeViewData = peReader.ReadCodeViewDebugDirectoryData(entry);
                        var peDirectory = Path.GetDirectoryName(module);
                        return File.Exists(Path.Combine(peDirectory, Path.GetFileName(codeViewData.Path)));
                    }
                }

                return false;
            }
        }
    }
}