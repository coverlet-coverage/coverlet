using Coverlet.Core.Abstracts;
using System;
using System.IO;

namespace Coverlet.Core.Helpers
{
    internal class ExclusionsFromFileHelper : IExclusionsFromFileHelper
    {
        private static readonly string[] EmptyResult = new string[0];
        private readonly IFileSystem _fileSystem;
        private ILogger _logger;

        public ExclusionsFromFileHelper(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string[] ImportExclusionsFromFile(string path)
        {
            try
            {
                if (!_fileSystem.FileExists(path))
                {
                    _logger?.LogWarning($"File with exclusions does not exists. Returning empty. Path: {path}");
                    return EmptyResult;
                }

                return _fileSystem.ReadAllLines(path);
            }
            catch (Exception e)
            {
                _logger?.LogWarning($"Failed to parse exclusion file at {path}. Received {e.Message}. Returning empty");
                return EmptyResult;
            }
        }

        public void Init(ILogger logger)
        {
            _logger = logger;
        }
    }
}
