using Coverlet.Core.Abstracts;
using System;

namespace Coverlet.Core.Helpers
{
    internal class ExclusionsFromFileHelper : IExclusionsFromFileHelper
    {
        private static readonly string[] EmptyResult = new string[0];
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public ExclusionsFromFileHelper(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public string[] ImportExclusionsFromFile(string path)
        {
            try
            {
                return _fileSystem.ReadAllLines(path);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Failed to parse exclusion file at {path}. Received {e.Message}");
                return EmptyResult;
            }
        }
    }
}
