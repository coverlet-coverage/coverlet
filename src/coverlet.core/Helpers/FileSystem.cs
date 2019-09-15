using Coverlet.Core.Abstracts;
using System.IO;

namespace Coverlet.Core.Helpers
{
    public class FileSystem : IFileSystem
    {
        public bool Exists(string path)
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

        public FileStream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public void Copy(string sourceFileName, string destFileName)
        {
            File.Copy(sourceFileName, destFileName);
        }

        public void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            File.Copy(sourceFileName, destFileName, overwrite);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public void AppendAllText(string path, string contents)
        {
            File.AppendAllText(path, contents);
        }
    }
}
