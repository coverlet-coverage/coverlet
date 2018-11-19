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
    public sealed class ModuleTrackerTemplate : IDisposable
    {
        public static string DefaultHitsFilePath;
        public static int DefaultHitsArraySize;

        private static readonly ModuleTrackerTemplate singleton;

        private const string MemoryMappedFileNamePostfix = ".coverlet_memory_mapped";

        private readonly MemoryMappedFile memoryMappedFile;
        private readonly MemoryMappedViewAccessor memoryMappedViewAccessor;
        private readonly int hitsArraySize;

        static ModuleTrackerTemplate()
        {
            // At the end of the instrumentation of a module, the instrumenter needs to add code here
            // to initialize the static setup fields according to the values derived from the instrumentation of
            // the module.

            if (DefaultHitsFilePath != null)
                singleton = new ModuleTrackerTemplate(DefaultHitsFilePath, DefaultHitsArraySize);

            //we always keep the view accessor around without disposing it. So, regardless of what happens, be it the module unloading or the process terminating, it will close properly when the finalizers are run and, failing that, when the kernel does process cleanup
        }

        public static void RecordHit(int hitLocationIndex)
        {
            if (singleton == null)
                throw new InvalidOperationException("Singleton not initialized!");
            singleton.InstanceRecordHit(hitLocationIndex);
        }

        public ModuleTrackerTemplate(string hitsFilePath, int hitsArraySize)
        {
            if (hitsFilePath == null)
                throw new ArgumentNullException(nameof(hitsFilePath));

            if (hitsArraySize < 0)
                throw new ArgumentOutOfRangeException(nameof(hitsArraySize), hitsArraySize, "hitsArraySize must not be less than 0!");

            this.hitsArraySize = hitsArraySize;

            //now HitsFilePath and HitsArraySize should be populated

            //first, try to create the hits file if it doesn't exist already
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(hitsFilePath, FileMode.CreateNew, FileAccess.ReadWrite);
            }
            catch (IOException)
            {
                //likely failed to create a new file
            }

            var bytesRequired = (hitsArraySize + 1) * sizeof(int);
            var memoryMappedFileName = (hitsFilePath + MemoryMappedFileNamePostfix).Replace('\\', '/'); //backslashes can't be used on windows

            if (fileStream != null)
                //we can safely close the fileStream after creating the MMF since we don't use it directly
                //The kernel file object remains open until all mapped files/views are closed
                try
                {
                    using (fileStream)
                    {
                        //we're responsible initializing the file
                        //no worries about race conditions here (unless the assembly being instrumented is really weird and loads itself during static initialization somehow)

                        //write the header
                        using (var writer = new BinaryWriter(fileStream, Encoding.Default, true))
                            writer.Write(hitsArraySize);

                        //write the zeros
                        var zerosRequired = bytesRequired - sizeof(int);
                        var zeroArray = new byte[zerosRequired];
                        fileStream.Write(zeroArray, 0, zerosRequired);

                        memoryMappedFile = MemoryMappedFile.CreateFromFile(fileStream, memoryMappedFileName, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
                    }
                }
                catch
                {
                    File.Delete(hitsFilePath);
                    throw;
                }
            else
                //open the existing memory map that SHOULD exist
                memoryMappedFile = MemoryMappedFile.OpenExisting(memoryMappedFileName, MemoryMappedFileRights.ReadWrite, HandleInheritability.None);

            //although the view accessor will keep the mapped file open, we need to not dispose the actual MMF handle
            //doing so will cause the calls to MemoryMappedFile.OpenExisting above to fail
            memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor();
        }

        /// <summary>
        /// Disposes the <see cref="ModuleTrackerTemplate"/>. Mainly for the convienience of test code
        /// </summary>
        public void Dispose()
        {
            memoryMappedViewAccessor.Dispose();
            memoryMappedFile.Dispose();
        }

        public void InstanceRecordHit(int hitLocationIndex)
        {
            if (hitLocationIndex < 0 || hitLocationIndex >= hitsArraySize)
                throw new ArgumentOutOfRangeException(nameof(hitLocationIndex), hitLocationIndex, "hitLocationIndex falls outside of hitsArraySize!");

            var buffer = memoryMappedViewAccessor.SafeMemoryMappedViewHandle;

            //even though this is just template code, we have to compile with /unsafe ¯\_(ツ)_/¯
            //anyway, this is so we can get proper cross-thread/process atomicity by using Interlocked on the MMF view directly
            unsafe
            {
                byte* pointer = null;
                buffer.AcquirePointer(ref pointer);
                try
                {
                    var intPointer = (int*)pointer;
                    var hitLocationArrayOffset = intPointer + hitLocationIndex + 1;	//+1 for header
                    Interlocked.Increment(ref *hitLocationArrayOffset);
                }
                //finally mostly for show, cause if we segfault above it's already ogre
                finally
                {
                    buffer.ReleasePointer();
                }
            }
        }
    }
}
