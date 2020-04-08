using Coverlet.Core.Abstracts;
using System.IO;

namespace Coverlet.Core.Helpers
{
    internal class FileSystem : IFileSystem
    {
        // We need to partial mock this method on tests
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

        // We need to partial mock this method on tests
        public virtual Stream OpenRead(string path)
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

        // We need to partial mock this method on tests
        public virtual Stream NewFileStream(string path, FileMode mode)
        {
            return new FileStream(path, mode);
        }

        // We need to partial mock this method on tests
        public virtual Stream NewFileStream(string path, FileMode mode, FileAccess access)
        {
            return new FileStream(path, mode, access);
        }

        public string[] ReadAllLines(string path)
        {
            return File.ReadAllLines(path);
        }
    }
}
