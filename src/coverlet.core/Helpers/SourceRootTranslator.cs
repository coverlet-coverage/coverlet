// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Coverlet.Core.Abstractions;

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
        private readonly Dictionary<string, List<string>> _sourceToDeterministicPathMapping;
        private readonly string _mappingFileName;
        private Dictionary<string, string> _resolutionCacheFiles;

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
                throw new FileNotFoundException($"Module test path '{moduleTestPath}' not found", moduleTestPath);
            }
            _mappingFileName = $"CoverletSourceRootsMapping";
            _sourceRootMapping = LoadSourceRootMapping(Path.GetDirectoryName(moduleTestPath));
            _sourceToDeterministicPathMapping = LoadSourceToDeterministicPathMapping(_sourceRootMapping);
        }

        private static Dictionary<string, List<string>> LoadSourceToDeterministicPathMapping(Dictionary<string, List<SourceRootMapping>> sourceRootMapping)
        {
            if (sourceRootMapping is null)
            {
                throw new ArgumentNullException(nameof(sourceRootMapping));
            }

            var sourceToDeterministicPathMapping = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, List<SourceRootMapping>> sourceRootMappingEntry in sourceRootMapping)
            {
                foreach (SourceRootMapping originalPath in sourceRootMappingEntry.Value)
                {
                    if (!sourceToDeterministicPathMapping.ContainsKey(originalPath.OriginalPath))
                    {
                        sourceToDeterministicPathMapping.Add(originalPath.OriginalPath, new List<string>());
                    }
                    sourceToDeterministicPathMapping[originalPath.OriginalPath].Add(sourceRootMappingEntry.Key);
                }
            }

            return sourceToDeterministicPathMapping;
        }

        private Dictionary<string, List<SourceRootMapping>> LoadSourceRootMapping(string directory)
        {
            var mapping = new Dictionary<string, List<SourceRootMapping>>();

            string mappingFilePath = Path.Combine(directory, _mappingFileName);
            //throw new Exception(mappingFilePath + " --- " + _fileSystem.Exists(mappingFilePath));
            if (!_fileSystem.Exists(mappingFilePath))
            {
                return mapping;
            }

            foreach (string mappingRecord in _fileSystem.ReadAllLines(mappingFilePath))
            {
                int projectFileSeparatorIndex = mappingRecord.IndexOf('|');
                int pathMappingSeparatorIndex = mappingRecord.IndexOf('=');
                if (projectFileSeparatorIndex == -1 || pathMappingSeparatorIndex == -1)
                {
                    _logger.LogWarning($"Malformed mapping '{mappingRecord}'");
                    continue;
                }
                string projectPath = mappingRecord.Substring(0, projectFileSeparatorIndex);
                string originalPath = mappingRecord.Substring(projectFileSeparatorIndex + 1, pathMappingSeparatorIndex - projectFileSeparatorIndex - 1);
                string mappedPath = mappingRecord.Substring(pathMappingSeparatorIndex + 1);

                if (!mapping.ContainsKey(mappedPath))
                {
                    mapping.Add(mappedPath, new List<SourceRootMapping>());
                }

                foreach (string path in originalPath.Split(';'))
                {
                    mapping[mappedPath].Add(new SourceRootMapping() { OriginalPath = path, ProjectPath = projectPath });
                }
            }

            return mapping;
        }

        public IReadOnlyList<SourceRootMapping> ResolvePathRoot(string pathRoot)
        {
            return _sourceRootMapping.TryGetValue(pathRoot, out List<SourceRootMapping> sourceRootMapping) ? sourceRootMapping.AsReadOnly() : null;
        }

        public string ResolveFilePath(string originalFileName)
        {
            if (_resolutionCacheFiles != null && _resolutionCacheFiles.ContainsKey(originalFileName))
            {
                return _resolutionCacheFiles[originalFileName];
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
                            (_resolutionCacheFiles ??= new Dictionary<string, string>()).Add(originalFileName, pathToCheck);
                            _logger.LogVerbose($"Mapping resolved: '{FileSystem.EscapeFileName(originalFileName)}' -> '{FileSystem.EscapeFileName(pathToCheck)}'");
                            return pathToCheck;
                        }
                    }
                }
            }
            return originalFileName;
        }

        public string ResolveDeterministicPath(string originalFileName)
        {
            foreach (KeyValuePair<string, List<string>> originalPath in _sourceToDeterministicPathMapping)
            {
                if (originalFileName.StartsWith(originalPath.Key))
                {
                    foreach (string deterministicPath in originalPath.Value)
                    {
                        originalFileName = originalFileName.Replace(originalPath.Key, deterministicPath).Replace('\\', '/');
                    }
                }
            }

            return originalFileName;
        }
    }
}
