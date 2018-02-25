using System.IO;
using Coverlet.Core.Attributes;

namespace Coverlet.Core
{
    public static class CoverageTracker
    {
        [ExcludeFromCoverage]
        public static void MarkExecuted(string path, string marker)
        {
            using (var stream = new FileStream(path, FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                using (var streamWriter = new StreamWriter(stream))
                {
                    streamWriter.WriteLine(marker);
                }
            }
        }
    }
}