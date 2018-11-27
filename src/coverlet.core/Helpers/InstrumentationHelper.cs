using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Coverlet.Core.Helpers
{
    internal static class InstrumentationHelper
    {
        public static string[] GetCoverableModules(string module, string[] includeDirectories)
        {
            Debug.Assert(includeDirectories != null, "Parameter " + nameof(includeDirectories) + " in method " + 
                nameof(InstrumentationHelper) + "." + nameof(GetCoverableModules) + " must not be null");

            string moduleDirectory = Path.GetDirectoryName(module);
            if (moduleDirectory == string.Empty)
            {
                moduleDirectory = Directory.GetCurrentDirectory();
            }

            var dirs = new List<string>(1 + includeDirectories.Length)
            {
                // Add the test assembly's directory.
                moduleDirectory
            };

            // Prepare all the directories in which we probe for modules.
            foreach (var includeDirectory in includeDirectories.Where(d => d != null))
            {
                var fullPath = (!Path.IsPathRooted(includeDirectory)
                    ? Path.GetFullPath(Path.Combine(moduleDirectory, includeDirectory))
                    : includeDirectory).TrimEnd('*');

                if (!Directory.Exists(fullPath)) continue;

                if (includeDirectory.EndsWith("*", StringComparison.Ordinal))
                    dirs.AddRange(Directory.GetDirectories(fullPath));
                else
                    dirs.Add(fullPath);
            }

            // The test module's name must be unique.
            var uniqueModules = new HashSet<string>
            {
                Path.GetFileName(module)
            };

            return dirs.SelectMany(d => Directory.EnumerateFiles(d))
                .Where(m => IsAssembly(m) && uniqueModules.Add(Path.GetFileName(m)))
                .ToArray();
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

        public static string BackupOriginalModule(string module, string identifier)
        {
            var backupPath = GetBackupPath(module, identifier);
            File.Copy(module, backupPath);
            return backupPath;
        }

        public static void RestoreOriginalModule(string module, string identifier)
        {
            var backupPath = GetBackupPath(module, identifier);

            // Restore the original module - retry up to 10 times, since the destination file could be locked
            // See: https://github.com/tonerdo/coverlet/issues/25
            var retryStrategy = CreateRetryStrategy();

            RetryHelper.Retry(() =>
            {
                File.Copy(backupPath, module, true);
                File.Delete(backupPath);
            }, retryStrategy, 10);
        }

        public static void DeleteHitsFile(string path)
        {
            // Retry hitting the hits file - retry up to 10 times, since the file could be locked
            // See: https://github.com/tonerdo/coverlet/issues/25
            var retryStrategy = CreateRetryStrategy();
            RetryHelper.Retry(() => File.Delete(path), retryStrategy, 10);
        }

        public static bool IsValidFilterExpression(string filter)
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

        public static bool IsModuleExcluded(string module, string[] excludeFilters)
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

        public static bool IsModuleIncluded(string module, string[] includeFilters)
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

        public static bool IsTypeExcluded(string module, string type, string[] excludeFilters)
        {
            if (excludeFilters == null || excludeFilters.Length == 0)
                return false;

            module = Path.GetFileNameWithoutExtension(module);
            if (module == null)
                return false;

            return IsTypeFilterMatch(module, type, excludeFilters);
        }

        public static bool IsTypeIncluded(string module, string type, string[] includeFilters)
        {
            if (includeFilters == null || includeFilters.Length == 0)
                return true;

            module = Path.GetFileNameWithoutExtension(module);
            if (module == null)
                return true;

            return IsTypeFilterMatch(module, type, includeFilters);
        }

        public static bool IsLocalMethod(string method)
            => new Regex(WildcardToRegex("<*>*__*|*")).IsMatch(method);

        public static string[] GetExcludedFiles(string[] excludes)
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
                    matcherDict[root].AddInclude(excludeRule.Substring(root.Length));
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

        private static bool IsTypeFilterMatch(string module, string type, string[] filters)
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

        private static IEnumerable<string> ExpandIncludeDirectories(string[] includeDirectories, string moduleDirectory)
        {
            var result = new List<string>(includeDirectories.Length);

            foreach (var includeDirectory in includeDirectories.Where(d => d != null))
            {
                var fullPath = (!Path.IsPathRooted(includeDirectory)
                    ? Path.GetFullPath(Path.Combine(moduleDirectory, includeDirectory))
                    : includeDirectory).TrimEnd('*');

                if (!Directory.Exists(fullPath)) continue;

                if (includeDirectory.EndsWith("*", StringComparison.Ordinal))
                    result.AddRange(Directory.GetDirectories(fullPath));
                else
                    result.Add(fullPath);
            }

            return result;
        }

        private static string GetBackupPath(string module, string identifier)
        {
            return Path.Combine(
                Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(module) + "_" + GetPathHash(Path.GetDirectoryName(module)) + "_" + identifier + ".dll"
            );
        }

        private static Func<TimeSpan> CreateRetryStrategy(int initialSleepSeconds = 6)
        {
            TimeSpan retryStrategy()
            {
                var sleep = TimeSpan.FromMilliseconds(initialSleepSeconds);
                initialSleepSeconds *= 2;
                return sleep;
            }

            return retryStrategy;
        }

        private static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", "?") + "$";
        }

        private static bool IsAssembly(string filePath)
        {
            Debug.Assert(filePath != null, "Parameter " + nameof(filePath) + " in " + nameof(InstrumentationHelper) + 
                "." + nameof(IsAssembly) + " must not be null.");

            if (!(filePath.EndsWith(".exe") || filePath.EndsWith(".dll")))
                return false;

            try
            {
                if (!(filePath.EndsWith(".exe") || filePath.EndsWith(".dll")))
                    return false;

                AssemblyName.GetAssemblyName(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetPathHash(string path)
        {
            using (var md5 = MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(Encoding.Unicode.GetBytes(path)))
                    .Replace("-", string.Empty);
        }
    }
}

