using System.Collections.Generic;

namespace Coverlet.Core.Abstractions
{
    interface IFilePathHelper
    {
        IEnumerable<string> GetBasePaths(IEnumerable<string> paths, bool useSourceLink);
        string GetRelativePathFromBase(IEnumerable<string> basePaths, string path, bool useSourceLink);
    }
}
