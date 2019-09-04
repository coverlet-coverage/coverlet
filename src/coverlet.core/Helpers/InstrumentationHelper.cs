using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Coverlet.Core.Abstracts;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Coverlet.Core.Helpers
{
    internal class InstrumentationHelper : IInstrumentationHelper
    {
        private readonly ConcurrentDictionary<string, string> _backupList = new ConcurrentDictionary<string, string>();
        private IRetryHelper _retryHelper;

        public InstrumentationHelper(IProcessExitHandler processExitHandler, IRetryHelper retryHelper)
        {
            processExitHandler.Add((s, e) => RestoreOriginalModules());
            _retryHelper = retryHelper;
        }

        public string[] GetCoverableModules(string module, string[] directories, bool includeTestAssembly)
        {
            Debug.Assert(directories != null);

            string moduleDirectory = Path.GetDirectoryName(module);
            if (moduleDirectory == string.Empty)
            {
                moduleDirectory = Directory.GetCurrentDirectory();
            }

            var dirs = new List<string>()
            {
                // Add the test assembly's directory.
                moduleDirectory
            };

            // Prepare all the directories we probe for modules.
            foreach (string directory in directories)
            {
                if (string.IsNullOrWhiteSpace(directory)) continue;

                string fullPath = (!Path.IsPathRooted(directory)
                    ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), directory))
                    : directory).TrimEnd('*');

                if (!Directory.Exists(fullPath)) continue;

                if (directory.EndsWith("*", StringComparison.Ordinal))
                    dirs.AddRange(Directory.GetDirectories(fullPath));
                else
                    dirs.Add(fullPath);
            }

            // The module's name must be unique.
            var uniqueModules = new HashSet<string>();

            if (!includeTestAssembly)
                uniqueModules.Add(Path.GetFileName(module));

            return dirs.SelectMany(d => Directory.EnumerateFiles(d))
                .Where(m => IsAssembly(m) && uniqueModules.Add(Path.GetFileName(m)))
                .ToArray();
        }

        public bool HasPdb(string module, out bool embedded)
        {
            embedded = false;
            using (var moduleStream = File.OpenRead(module))
            using (var peReader = new PEReader(moduleStream))
            {
                foreach (var entry in peReader.ReadDebugDirectory())
                {
                    if (entry.Type == DebugDirectoryEntryType.CodeView)
                    {
                        var codeViewData = peReader.ReadCodeViewDebugDirectoryData(entry);
                        if (codeViewData.Path == $"{Path.GetFileNameWithoutExtension(module)}.pdb")
                        {
                            // PDB is embedded
                            embedded = true;
                            return true;
                        }

                        return File.Exists(codeViewData.Path);
                    }
                }

                return false;
            }
        }

        public bool EmbeddedPortablePdbHasLocalSource(string module)
        {
            using (FileStream moduleStream = File.OpenRead(module))
            using (var peReader = new PEReader(moduleStream))
            {
                foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
                {
                    if (entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
                    {
                        using (MetadataReaderProvider embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry))
                        {
                            MetadataReader metadataReader = embeddedMetadataProvider.GetMetadataReader();
                            foreach (DocumentHandle docHandle in metadataReader.Documents)
                            {
                                Document document = metadataReader.GetDocument(docHandle);
                                string docName = metadataReader.GetString(document.Name);

                                // We verify all docs and return false if not all are present in local
                                // We could have false negative if doc is not a source
                                // Btw check for all possible extension could be weak approach
                                if (!File.Exists(docName))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            // If we don't have EmbeddedPortablePdb entry return true, for instance empty dll
            // We should call this method only on embedded pdb module
            return true;
        }

        public void BackupOriginalModule(string module, string identifier)
        {
            var backupPath = GetBackupPath(module, identifier);
            var backupSymbolPath = Path.ChangeExtension(backupPath, ".pdb");
            File.Copy(module, backupPath, true);
            if (!_backupList.TryAdd(module, backupPath))
            {
                throw new ArgumentException($"Key already added '{module}'");
            }

            var symbolFile = Path.ChangeExtension(module, ".pdb");
            if (File.Exists(symbolFile))
            {
                File.Copy(symbolFile, backupSymbolPath, true);
                if (!_backupList.TryAdd(symbolFile, backupSymbolPath))
                {
                    throw new ArgumentException($"Key already added '{module}'");
                }
            }
        }

        public void RestoreOriginalModule(string module, string identifier)
        {
            var backupPath = GetBackupPath(module, identifier);
            var backupSymbolPath = Path.ChangeExtension(backupPath, ".pdb");

            // Restore the original module - retry up to 10 times, since the destination file could be locked
            // See: https://github.com/tonerdo/coverlet/issues/25
            var retryStrategy = CreateRetryStrategy();

            _retryHelper.Retry(() =>
            {
                File.Copy(backupPath, module, true);
                File.Delete(backupPath);
                _backupList.TryRemove(module, out string _);
            }, retryStrategy, 10);

            _retryHelper.Retry(() =>
            {
                if (File.Exists(backupSymbolPath))
                {
                    string symbolFile = Path.ChangeExtension(module, ".pdb");
                    File.Copy(backupSymbolPath, symbolFile, true);
                    File.Delete(backupSymbolPath);
                    _backupList.TryRemove(symbolFile, out string _);
                }
            }, retryStrategy, 10);
        }

        public void RestoreOriginalModules()
        {
            // Restore the original module - retry up to 10 times, since the destination file could be locked
            // See: https://github.com/tonerdo/coverlet/issues/25
            var retryStrategy = CreateRetryStrategy();

            foreach (string key in _backupList.Keys.ToList())
            {
                string backupPath = _backupList[key];
                _retryHelper.Retry(() =>
                {
                    File.Copy(backupPath, key, true);
                    File.Delete(backupPath);
                    _backupList.TryRemove(key, out string _);
                }, retryStrategy, 10);
            }
        }

        public void DeleteHitsFile(string path)
        {
            // Retry hitting the hits file - retry up to 10 times, since the file could be locked
            // See: https://github.com/tonerdo/coverlet/issues/25
            var retryStrategy = CreateRetryStrategy();
            _retryHelper.Retry(() => File.Delete(path), retryStrategy, 10);
        }

        public bool IsValidFilterExpression(string filter)
        {
            if (filter == null)
                return false;

            if (!filter.StartsWith("["))
                return false;

            if (!filter.Contains("]"))
                return false;

            if (filter.Count(f => f == '[') > 1)
                return false;

            if (filter.Count(f => f == ']') > 1)
                return false;

            if (filter.IndexOf(']') < filter.IndexOf('['))
                return false;

            if (filter.IndexOf(']') - filter.IndexOf('[') == 1)
                return false;

            if (filter.EndsWith("]"))
                return false;

            if (new Regex(@"[^\w*]").IsMatch(filter.Replace(".", "").Replace("?", "").Replace("[", "").Replace("]", "")))
                return false;

            return true;
        }

        public bool IsModuleExcluded(string module, string[] excludeFilters)
        {
            if (excludeFilters == null || excludeFilters.Length == 0)
                return false;

            module = Path.GetFileNameWithoutExtension(module);
            if (module == null)
                return false;

            foreach (var filter in excludeFilters)
            {
                string typePattern = filter.Substring(filter.IndexOf(']') + 1);

                if (typePattern != "*")
                    continue;

                string modulePattern = filter.Substring(1, filter.IndexOf(']') - 1);
                modulePattern = WildcardToRegex(modulePattern);

                var regex = new Regex(modulePattern);

                if (regex.IsMatch(module))
                    return true;
            }

            return false;
        }

        public bool IsModuleIncluded(string module, string[] includeFilters)
        {
            if (includeFilters == null || includeFilters.Length == 0)
                return true;

            module = Path.GetFileNameWithoutExtension(module);
            if (module == null)
                return false;

            foreach (var filter in includeFilters)
            {
                string modulePattern = filter.Substring(1, filter.IndexOf(']') - 1);

                if (modulePattern == "*")
                    return true;

                modulePattern = WildcardToRegex(modulePattern);

                var regex = new Regex(modulePattern);

                if (regex.IsMatch(module))
                    return true;
            }

            return false;
        }

        public bool IsTypeExcluded(string module, string type, string[] excludeFilters)
        {
            if (excludeFilters == null || excludeFilters.Length == 0)
                return false;

            module = Path.GetFileNameWithoutExtension(module);
            if (module == null)
                return false;

            return IsTypeFilterMatch(module, type, excludeFilters);
        }

        public bool IsTypeIncluded(string module, string type, string[] includeFilters)
        {
            if (includeFilters == null || includeFilters.Length == 0)
                return true;

            module = Path.GetFileNameWithoutExtension(module);
            if (module == null)
                return true;

            return IsTypeFilterMatch(module, type, includeFilters);
        }

        public bool IsLocalMethod(string method)
            => new Regex(WildcardToRegex("<*>*__*|*")).IsMatch(method);

        public string[] GetExcludedFiles(string[] excludes)
        {
            const string RELATIVE_KEY = nameof(RELATIVE_KEY);
            string parentDir = Directory.GetCurrentDirectory();

            if (excludes == null || !excludes.Any()) return Array.Empty<string>();

            var matcherDict = new Dictionary<string, Matcher>() { { RELATIVE_KEY, new Matcher() } };
            foreach (var excludeRule in excludes)
            {
                if (Path.IsPathRooted(excludeRule))
                {
                    var root = Path.GetPathRoot(excludeRule);
                    if (!matcherDict.ContainsKey(root))
                    {
                        matcherDict.Add(root, new Matcher());
                    }
                    matcherDict[root].AddInclude(Path.GetFullPath(excludeRule).Substring(root.Length));
                }
                else
                {
                    matcherDict[RELATIVE_KEY].AddInclude(excludeRule);
                }
            }

            var files = new List<string>();
            foreach (var entry in matcherDict)
            {
                var root = entry.Key;
                var matcher = entry.Value;
                var directoryInfo = new DirectoryInfo(root.Equals(RELATIVE_KEY) ? parentDir : root);
                var fileMatchResult = matcher.Execute(new DirectoryInfoWrapper(directoryInfo));
                var currentFiles = fileMatchResult.Files
                    .Select(f => Path.GetFullPath(Path.Combine(directoryInfo.ToString(), f.Path)));
                files.AddRange(currentFiles);
            }

            return files.Distinct().ToArray();
        }

        private bool IsTypeFilterMatch(string module, string type, string[] filters)
        {
            Debug.Assert(module != null);
            Debug.Assert(filters != null);

            foreach (var filter in filters)
            {
                string typePattern = filter.Substring(filter.IndexOf(']') + 1);
                string modulePattern = filter.Substring(1, filter.IndexOf(']') - 1);

                typePattern = WildcardToRegex(typePattern);
                modulePattern = WildcardToRegex(modulePattern);

                if (new Regex(typePattern).IsMatch(type) && new Regex(modulePattern).IsMatch(module))
                    return true;
            }

            return false;
        }

        private string GetBackupPath(string module, string identifier)
        {
            return Path.Combine(
                Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(module) + "_" + identifier + ".dll"
            );
        }

        private Func<TimeSpan> CreateRetryStrategy(int initialSleepSeconds = 6)
        {
            TimeSpan retryStrategy()
            {
                var sleep = TimeSpan.FromMilliseconds(initialSleepSeconds);
                initialSleepSeconds *= 2;
                return sleep;
            }

            return retryStrategy;
        }

        private string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", "?") + "$";
        }

        private bool IsAssembly(string filePath)
        {
            Debug.Assert(filePath != null);

            if (!(filePath.EndsWith(".exe") || filePath.EndsWith(".dll")))
                return false;

            try
            {
                AssemblyName.GetAssemblyName(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}