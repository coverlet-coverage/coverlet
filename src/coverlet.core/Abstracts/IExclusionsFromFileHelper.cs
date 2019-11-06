using System.IO;

namespace Coverlet.Core.Abstracts
{
    public interface IExclusionsFromFileHelper
    {
        void Init(ILogger logger);
        string[] ImportExclusionsFromFile(string path);
    }
}
