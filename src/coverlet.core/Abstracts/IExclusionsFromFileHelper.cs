using System.IO;

namespace Coverlet.Core.Abstracts
{
    public interface IExclusionsFromFileHelper
    {
        string[] ImportExclusionsFromFile(string path);
    }
}
