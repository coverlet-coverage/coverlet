namespace Coverlet.Core.Abstractions
{
    internal interface ISourceRootTranslator
    {
        string ResolveFilePath(string originalFileName);
    }
}
