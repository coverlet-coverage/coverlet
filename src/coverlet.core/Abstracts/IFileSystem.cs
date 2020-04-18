using System.IO;

namespace Coverlet.Core.Abstracts
{
    internal interface IFileSystem
    {
        bool Exists(string path);

        void WriteAllText(string path, string contents);

        string ReadAllText(string path);

        Stream OpenRead(string path);

        void Copy(string sourceFileName, string destFileName, bool overwrite);

        void Delete(string path);

        Stream NewFileStream(string path, FileMode mode);

        Stream NewFileStream(string path, FileMode mode, FileAccess access);

        string[] ReadAllLines(string path);
    }
}
