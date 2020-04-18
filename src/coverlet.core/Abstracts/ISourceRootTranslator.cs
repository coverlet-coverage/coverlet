namespace Coverlet.Core.Abstracts
{
    internal interface ISourceRootTranslator
    {
        string ResolveFilePath(string originalFileName);
    }
}
