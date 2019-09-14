using Coverlet.Core.Abstracts;
using System.IO;

namespace Coverlet.Core.Helpers
{
    internal class FileSystem : IFileSystem
    {
        public bool Exists(string path)
        {
            return File.Exists(path);
        }
    }
}
