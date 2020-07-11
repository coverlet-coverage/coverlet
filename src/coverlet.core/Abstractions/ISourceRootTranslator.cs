using System.Collections.Generic;
using Coverlet.Core.Helpers;

namespace Coverlet.Core.Abstractions
{
    internal interface ISourceRootTranslator
    {
        string ResolveFilePath(string originalFileName);
        List<SourceRootMapping> ResolvePathRoot(string pathRoot);
    }
}
