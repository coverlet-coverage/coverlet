using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Coverlet.Core.Helpers
{
    internal static class InstrumentationHelper
    {
        public static string[] GetDependencies(string module)
        {
            IEnumerable<string> modules = Directory.GetFiles(Path.GetDirectoryName(module), "*.dll");
            modules = modules.Where(a => IsAssembly(a) && Path.GetFileName(a) != Path.GetFileName(module));
            return modules.ToArray();
        }

        public static bool HasPdb(string module)
        {
            using (var moduleStream = File.OpenRead(module))
            using (var peReader = new PEReader(moduleStream))
            {
                foreach (var entry in peReader.ReadDebugDirectory())
                {
                    if (entry.Type == DebugDirectoryEntryType.CodeView)
                    {
                        var codeViewData = peReader.ReadCodeViewDebugDirectoryData(entry);
                        var peDirectory = Path.GetDirectoryName(module);
                        return File.Exists(Path.Combine(peDirectory, Path.GetFileName(codeViewData.Path)));
                    }
                }

                return false;
            }
        }

        public static void CopyCoverletDependency(string module)
        {
            var directory = Path.GetDirectoryName(module);
            var moduleFileName = Path.GetFileName(module);

            var assembly = typeof(Coverage).Assembly;
            string name = Path.GetFileName(assembly.Location);
            if (name == moduleFileName)
                return;

            File.Copy(assembly.Location, Path.Combine(directory, name), true);
        }

        public static void BackupOriginalModule(string module, string identifier)
        {
            var backupPath = GetBackupPath(module, identifier);
            File.Copy(module, backupPath);
        }

        public static void RestoreOriginalModule(string module, string identifier)
        {
            var backupPath = GetBackupPath(module, identifier);

            // Restore the original module - retry up to 10 times, since the destination file could be locked
            // See: https://github.com/tonerdo/coverlet/issues/25
            var currentSleep = 6;
            TimeSpan retryStrategy()
            {
                var sleep = TimeSpan.FromMilliseconds(currentSleep);
                currentSleep *= 2;
                return sleep;
            }

            RetryHelper.Retry(() => {
                File.Copy(backupPath, module, true);
                File.Delete(backupPath);
            }, retryStrategy, 10);
        }

        public static IEnumerable<string> ReadHitsFile(string path)
        {
            // Retry hitting the hits file - retry up to 10 times, since the file could be locked
            // See: https://github.com/tonerdo/coverlet/issues/25
            var currentSleep = 6;
            TimeSpan retryStrategy()
            {
                var sleep = TimeSpan.FromMilliseconds(currentSleep);
                currentSleep *= 2;
                return sleep;
            }

            return RetryHelper.Do(() => File.ReadLines(path), retryStrategy, 10);
        }

        public static void DeleteHitsFile(string path)
        {
            // Retry hitting the hits file - retry up to 10 times, since the file could be locked
            // See: https://github.com/tonerdo/coverlet/issues/25
            var currentSleep = 6;
            TimeSpan retryStrategy()
            {
                var sleep = TimeSpan.FromMilliseconds(currentSleep);
                currentSleep *= 2;
                return sleep;
            }

            RetryHelper.Retry(() => File.Delete(path), retryStrategy, 10);
        }

        public static IEnumerable<string> GetExcludedFiles(IEnumerable<string> excludeRules,
                                                           string parentDir = null)
        {
            const string RELATIVE_KEY = nameof(RELATIVE_KEY);
            parentDir = string.IsNullOrWhiteSpace(parentDir)? Directory.GetCurrentDirectory() : parentDir;

            if (excludeRules == null || !excludeRules.Any()) return Enumerable.Empty<string>();

            var matcherDict = new Dictionary<string, Matcher>(){ {RELATIVE_KEY, new Matcher()}};
            foreach (var excludeRule in excludeRules)
            {
                if (Path.IsPathRooted(excludeRule)) {
                    var root = Path.GetPathRoot(excludeRule);
                    if (!matcherDict.ContainsKey(root)) {
                        matcherDict.Add(root, new Matcher());
                    }
                    matcherDict[root].AddInclude(excludeRule.Substring(root.Length));
                } else {
                    matcherDict[RELATIVE_KEY].AddInclude(excludeRule);
                }
            }

            var files = new List<string>();
            foreach(var entry in matcherDict)
            {
                var root = entry.Key;
                var matcher = entry.Value;
                var directoryInfo = new DirectoryInfo(root.Equals(RELATIVE_KEY) ? parentDir : root);
                var fileMatchResult = matcher.Execute(new DirectoryInfoWrapper(directoryInfo));
                var currentFiles = fileMatchResult.Files
                    .Select(f => Path.GetFullPath(Path.Combine(directoryInfo.ToString(), f.Path)));
                files.AddRange(currentFiles);
            }

            return files.Distinct();
        }

        private static bool IsAssembly(string filePath)
        {
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

        private static string GetBackupPath(string module, string identifier)
        {
            return Path.Combine(
                Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(module) + "_" + identifier + ".dll"
            );
        }
    }
}

