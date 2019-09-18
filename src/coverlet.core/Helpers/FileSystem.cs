using Coverlet.Core.Abstracts;
using System.IO;

namespace Coverlet.Core.Helpers
{
    public class FileSystem : IFileSystem
    {
        public virtual bool Exists(string path)
        {
            return File.Exists(path);
        }

        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            File.Copy(sourceFileName, destFileName, overwrite);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public Stream NewFileStream(string path, FileMode mode)
        {
            return new FileStream(path, mode);
        }

        public Stream NewFileStream(string path, FileMode mode, FileAccess access)
        {
            return new FileStream(path, mode, access);
        }
    }
}
