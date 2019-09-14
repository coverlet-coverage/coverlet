namespace Coverlet.Core.Abstracts
{
    internal interface IFileSystem
    {
        bool Exists(string path);
    }
}
