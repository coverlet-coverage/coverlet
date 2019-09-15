using System.IO;

namespace Coverlet.Core.Abstracts
{
    public interface IFileSystem
    {
        bool Exists(string path);

        void WriteAllText(string path, string contents);

        string ReadAllText(string path);

        FileStream OpenRead(string path);

        void Copy(string sourceFileName, string destFileName);

        void Delete(string path);

        void AppendAllText(string path, string contents);
    }
}
