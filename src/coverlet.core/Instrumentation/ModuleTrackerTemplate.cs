using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace Coverlet.Core.Instrumentation
{
    /// <summary>
    /// This static class will be injected on a module being instrumented in order to direct on module hits
    /// to a single location.
    /// </summary>
    /// <remarks>
    /// As this type is going to be customized for each instrumeted module it doesn't follow typical practices
    /// regarding visibility of members, etc.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public static class ModuleTrackerTemplate
    {
        public static string hitsDirectoryPath;
        public static int hitsArraySize;

        [ThreadStatic]
        public static MemoryMappedViewAccessor memoryMappedViewAccessor;

        static ModuleTrackerTemplate()
        {
            // At the end of the instrumentation of a module, the instrumenter needs to add code here
            // to initialize the static setup fields according to the values derived from the instrumentation of
            // the module.
        }

        static void SetupThread()
        {
            var threadHitsFile = Path.Combine(hitsDirectoryPath, Guid.NewGuid().ToString());
            var fileStream = new FileStream(threadHitsFile, FileMode.CreateNew, FileAccess.ReadWrite);
            try
            {
                //write the header
                using (var writer = new BinaryWriter(fileStream, Encoding.Default, true))
                    writer.Write(hitsArraySize);

                var bytesRequired = (hitsArraySize + 1) * sizeof(int);
                using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileStream, null, bytesRequired, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
                    memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor();
            }
            catch
            {
                fileStream.Dispose();
                File.Delete(threadHitsFile);
                throw;
            }
        }

        public static void RecordHit(int hitLocationIndex)
        {
            if (memoryMappedViewAccessor == null)
                SetupThread();

            var buffer = memoryMappedViewAccessor.SafeMemoryMappedViewHandle;

            //+1 for header
            var locationIndex = ((uint)hitLocationIndex + 1) * sizeof(int);
            buffer.Write(locationIndex, buffer.Read<int>(locationIndex) + 1);
        }
    }
}
