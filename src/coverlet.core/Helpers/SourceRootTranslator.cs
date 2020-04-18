using Coverlet.Core.Abstracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Coverlet.Core.Helpers
{
    [DebuggerDisplay("ProjectPath = {ProjectPath} OriginalPath = {OriginalPath}")]
    internal class SourceRootMapping
    {
        public string ProjectPath { get; set; }
        public string OriginalPath { get; set; }
    }

    internal class SourceRootTranslator : ISourceRootTranslator
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly Dictionary<string, List<SourceRootMapping>> _sourceRootMapping;
        private const string MappingFileName = "CoverletSourceRootsMapping";
        private Dictionary<string, string> _resolutionCache;

        public SourceRootTranslator(ILogger logger, IFileSystem fileSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _sourceRootMapping = new Dictionary<string, List<SourceRootMapping>>();
        }

        public SourceRootTranslator(string moduleTestPath, ILogger logger, IFileSystem fileSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            if (moduleTestPath is null)
            {
                throw new ArgumentNullException(nameof(moduleTestPath));
            }
            if (!_fileSystem.Exists(moduleTestPath))
            {
                throw new FileNotFoundException("Module test path not found", moduleTestPath);
            }
            _sourceRootMapping = LoadSourceRootMapping(Path.GetDirectoryName(moduleTestPath)) ?? new Dictionary<string, List<SourceRootMapping>>();
        }

        private Dictionary<string, List<SourceRootMapping>> LoadSourceRootMapping(string directory)
        {
            Dictionary<string, List<SourceRootMapping>> mapping = new Dictionary<string, List<SourceRootMapping>>();

            string mappingFilePath = Path.Combine(directory, MappingFileName);
            if (!_fileSystem.Exists(mappingFilePath))
            {
                return mapping;
            }

            foreach (string mappingRecord in _fileSystem.ReadAllLines(mappingFilePath))
            {
                int projecFileSeparatorIndex = mappingRecord.IndexOf('|');
                int pathMappingSeparatorIndex = mappingRecord.IndexOf('=');
                if (projecFileSeparatorIndex == -1 || pathMappingSeparatorIndex == -1)
                {
                    _logger.LogWarning($"Malformed mapping '{mappingRecord}'");
                    continue;
                }
                string projectPath = mappingRecord.Substring(0, projecFileSeparatorIndex);
                string originalPath = mappingRecord.Substring(projecFileSeparatorIndex + 1, pathMappingSeparatorIndex - projecFileSeparatorIndex - 1);
                string mappedPath = mappingRecord.Substring(pathMappingSeparatorIndex + 1);

                if (!mapping.ContainsKey(mappedPath))
                {
                    mapping.Add(mappedPath, new List<SourceRootMapping>());
                }
                mapping[mappedPath].Add(new SourceRootMapping() { OriginalPath = originalPath, ProjectPath = projectPath });
            }

            return mapping;
        }

        public string ResolveFilePath(string originalFileName)
        {
            if (_resolutionCache != null && _resolutionCache.ContainsKey(originalFileName))
            {
                return _resolutionCache[originalFileName];
            }

            foreach (KeyValuePair<string, List<SourceRootMapping>> mapping in _sourceRootMapping)
            {
                if (originalFileName.StartsWith(mapping.Key))
                {
                    foreach (SourceRootMapping srm in mapping.Value)
                    {
                        string pathToCheck;
                        if (_fileSystem.Exists(pathToCheck = Path.GetFullPath(originalFileName.Replace(mapping.Key, srm.OriginalPath))))
                        {
                            (_resolutionCache ??= new Dictionary<string, string>()).Add(originalFileName, pathToCheck);
                            _logger.LogVerbose($"Mapping resolved: '{originalFileName}' -> '{pathToCheck}'");
                            return pathToCheck;
                        }
                    }
                }
            }
            return originalFileName;
        }
    }
}
