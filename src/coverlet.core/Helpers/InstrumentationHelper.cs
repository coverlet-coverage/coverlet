// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
using Coverlet.Core.Abstractions;
using Coverlet.Core.Enums;

namespace Coverlet.Core.Helpers
{
  internal class InstrumentationHelper : IInstrumentationHelper
  {
    private const int RetryAttempts = 12;
    private readonly ConcurrentDictionary<string, string> _backupList = new();
    private readonly IRetryHelper _retryHelper;
    private readonly IFileSystem _fileSystem;
    private readonly ISourceRootTranslator _sourceRootTranslator;
    private ILogger _logger;
    private static readonly RegexOptions s_regexOptions =
      RegexOptions.Multiline | RegexOptions.Compiled;

    public InstrumentationHelper(IProcessExitHandler processExitHandler, IRetryHelper retryHelper, IFileSystem fileSystem, ILogger logger, ISourceRootTranslator sourceRootTranslator)
    {
      processExitHandler.Add((s, e) => RestoreOriginalModules());
      _retryHelper = retryHelper;
      _fileSystem = fileSystem;
      _logger = logger;
      _sourceRootTranslator = sourceRootTranslator;
    }

    public string[] GetCoverableModules(string moduleOrAppDirectory, string[] directories, bool includeTestAssembly)
    {
      Debug.Assert(directories != null);
      Debug.Assert(moduleOrAppDirectory != null);

      bool isAppDirectory = !File.Exists(moduleOrAppDirectory) && Directory.Exists(moduleOrAppDirectory);
      string moduleDirectory = isAppDirectory ? moduleOrAppDirectory : Path.GetDirectoryName(moduleOrAppDirectory);

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

      if (!includeTestAssembly && !isAppDirectory)
        uniqueModules.Add(Path.GetFileName(moduleOrAppDirectory));

      return dirs.SelectMany(d => Directory.EnumerateFiles(d))
          .Where(m => IsAssembly(m) && uniqueModules.Add(Path.GetFileName(m)))
          .ToArray();
    }

    public bool HasPdb(string module, out bool embedded)
    {
      embedded = false;
      using Stream moduleStream = _fileSystem.OpenRead(module);
      using var peReader = new PEReader(moduleStream);
      foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
      {
        if (entry.Type == DebugDirectoryEntryType.CodeView)
        {
          CodeViewDebugDirectoryData codeViewData = peReader.ReadCodeViewDebugDirectoryData(entry);
          string modulePdbFileName = $"{Path.GetFileNameWithoutExtension(module)}.pdb";
          if (_sourceRootTranslator.ResolveFilePath(codeViewData.Path) == modulePdbFileName)
          {
            // PDB is embedded
            embedded = true;
            return true;
          }

          if (_fileSystem.Exists(_sourceRootTranslator.ResolveFilePath(codeViewData.Path)))
          {
            // local PDB is located within original build location
            embedded = false;
            return true;
          }

          string localPdbFileName = Path.Combine(Path.GetDirectoryName(module), modulePdbFileName);
          if (_fileSystem.Exists(localPdbFileName))
          {
            // local PDB is located within same folder as module
            embedded = false;

            // mapping need to be registered in _sourceRootTranslator to use that discovery
            _sourceRootTranslator.AddMappingInCache(codeViewData.Path, localPdbFileName);

            return true;
          }
        }
      }

      return false;
    }

    public bool EmbeddedPortablePdbHasLocalSource(string module, AssemblySearchType excludeAssembliesWithoutSources)
    {
      using Stream moduleStream = _fileSystem.OpenRead(module);
      using var peReader = new PEReader(moduleStream);
      foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
      {
        if (entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
        {
          using MetadataReaderProvider embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
          MetadataReader metadataReader = embeddedMetadataProvider.GetMetadataReader();

          if (!MatchDocumentsWithSources(module, excludeAssembliesWithoutSources, metadataReader))
          {
            return false;
          }
        }
      }

      // If we don't have EmbeddedPortablePdb entry return true, for instance empty dll
      // We should call this method only on embedded pdb module
      return true;
    }

    public bool PortablePdbHasLocalSource(string module, AssemblySearchType excludeAssembliesWithoutSources)
    {
      using Stream moduleStream = _fileSystem.OpenRead(module);
      using var peReader = new PEReader(moduleStream);
      foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
      {
        if (entry.Type == DebugDirectoryEntryType.CodeView)
        {
          CodeViewDebugDirectoryData codeViewData = peReader.ReadCodeViewDebugDirectoryData(entry);
          using Stream pdbStream = _fileSystem.OpenRead(_sourceRootTranslator.ResolveFilePath(codeViewData.Path));
          using var metadataReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
          MetadataReader metadataReader = null;
          try
          {
            metadataReader = metadataReaderProvider.GetMetadataReader();
          }
          catch (BadImageFormatException)
          {
            _logger.LogWarning($"{nameof(BadImageFormatException)} during MetadataReaderProvider.FromPortablePdbStream in InstrumentationHelper.PortablePdbHasLocalSource, unable to check if module has got local source.");
            return true;
          }

          if (!MatchDocumentsWithSources(module, excludeAssembliesWithoutSources, metadataReader))
          {
            return false;
          }
        }
      }

      return true;
    }

    private bool MatchDocumentsWithSources(string module, AssemblySearchType excludeAssembliesWithoutSources,
        MetadataReader metadataReader)
    {
      if (excludeAssembliesWithoutSources.Equals(AssemblySearchType.MissingAll))
      {
        bool anyDocumentMatches = MatchDocumentsWithSourcesMissingAll(metadataReader);
        if (!anyDocumentMatches)
        {
          _logger.LogVerbose($"Excluding module from instrumentation: {module}, pdb without any local source files");
          return false;
        }
      }

      if (excludeAssembliesWithoutSources.Equals(AssemblySearchType.MissingAny))
      {
        (bool allDocumentsMatch, string notFoundDocument) = MatchDocumentsWithSourcesMissingAny(metadataReader);

        if (!allDocumentsMatch)
        {
          _logger.LogVerbose(
              $"Excluding module from instrumentation: {module}, pdb without local source files, [{FileSystem.EscapeFileName(notFoundDocument)}]");
          return false;
        }
      }

      return true;
    }

    private IEnumerable<(string documentName, bool documentExists)> DocumentSourceMap(MetadataReader metadataReader)
    {
      return metadataReader.Documents.Select(docHandle =>
      {
        Document document = metadataReader.GetDocument(docHandle);
        string docName = _sourceRootTranslator.ResolveFilePath(metadataReader.GetString(document.Name));
        return (docName, _fileSystem.Exists(docName));
      });
    }

    private bool MatchDocumentsWithSourcesMissingAll(MetadataReader metadataReader)
    {
      return DocumentSourceMap(metadataReader).Any(x => x.documentExists);
    }

    private (bool allDocumentsMatch, string notFoundDocument) MatchDocumentsWithSourcesMissingAny(
        MetadataReader metadataReader)
    {
      var documentSourceMap = DocumentSourceMap(metadataReader).ToList();

      if (documentSourceMap.Any(x => !x.documentExists))
        return (false, documentSourceMap.FirstOrDefault(x => !x.documentExists).documentName);

      return (true, string.Empty);
    }

    public void BackupOriginalModule(string module, string identifier)
    {
      string backupPath = GetBackupPath(module, identifier);
      string backupSymbolPath = Path.ChangeExtension(backupPath, ".pdb");
      _fileSystem.Copy(module, backupPath, true);
      if (!_backupList.TryAdd(module, backupPath))
      {
        throw new ArgumentException($"Key already added '{module}'");
      }

      string symbolFile = Path.ChangeExtension(module, ".pdb");
      if (_fileSystem.Exists(symbolFile))
      {
        _fileSystem.Copy(symbolFile, backupSymbolPath, true);
        if (!_backupList.TryAdd(symbolFile, backupSymbolPath))
        {
          throw new ArgumentException($"Key already added '{module}'");
        }
      }
    }

    public virtual void RestoreOriginalModule(string module, string identifier)
    {
      string backupPath = GetBackupPath(module, identifier);
      string backupSymbolPath = Path.ChangeExtension(backupPath, ".pdb");

      // Restore the original module - retry up to 10 times, since the destination file could be locked
      // See: https://github.com/tonerdo/coverlet/issues/25
      Func<TimeSpan> retryStrategy = CreateRetryStrategy();

      _retryHelper.Retry(() =>
      {
        _fileSystem.Copy(backupPath, module, true);
        _fileSystem.Delete(backupPath);
        _backupList.TryRemove(module, out string _);
      }, retryStrategy, RetryAttempts);

      _retryHelper.Retry(() =>
      {
        if (_fileSystem.Exists(backupSymbolPath))
        {
          string symbolFile = Path.ChangeExtension(module, ".pdb");
          _fileSystem.Copy(backupSymbolPath, symbolFile, true);
          _fileSystem.Delete(backupSymbolPath);
          _backupList.TryRemove(symbolFile, out string _);
        }
      }, retryStrategy, RetryAttempts);
    }

    public virtual void RestoreOriginalModules()
    {
      // Restore the original module - retry up to 10 times, since the destination file could be locked
      // See: https://github.com/tonerdo/coverlet/issues/25
      Func<TimeSpan> retryStrategy = CreateRetryStrategy();

      foreach (string key in _backupList.Keys.ToList())
      {
        string backupPath = _backupList[key];
        _retryHelper.Retry(() =>
        {
          _fileSystem.Copy(backupPath, key, true);
          _fileSystem.Delete(backupPath);
          _backupList.TryRemove(key, out string _);
        }, retryStrategy, RetryAttempts);
      }
    }

    public void DeleteHitsFile(string path)
    {
      Func<TimeSpan> retryStrategy = CreateRetryStrategy();
      _retryHelper.Retry(() => _fileSystem.Delete(path), retryStrategy, RetryAttempts);
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

      if (new Regex(@"[^\w*]", s_regexOptions, TimeSpan.FromSeconds(10)).IsMatch(filter.Replace(".", "").Replace("?", "").Replace("[", "").Replace("]", "")))
        return false;

      return true;
    }

    public IEnumerable<string> SelectModules(IEnumerable<string> modules, string[] includeFilters, string[] excludeFilters)
    {
      const char escapeSymbol = '!';
      ILookup<string, string> modulesLookup = modules.Where(x => x != null)
          .ToLookup(x => $"{escapeSymbol}{Path.GetFileNameWithoutExtension(x)}{escapeSymbol}");

      string moduleKeys = string.Join(Environment.NewLine, modulesLookup.Select(x => x.Key));
      string includedModuleKeys = GetModuleKeysForIncludeFilters(includeFilters, escapeSymbol, moduleKeys);
      string excludedModuleKeys = GetModuleKeysForExcludeFilters(excludeFilters, escapeSymbol, includedModuleKeys);

      IEnumerable<string> moduleKeysToInclude = includedModuleKeys
          .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
          .Except(excludedModuleKeys.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

       return moduleKeysToInclude.SelectMany(x => modulesLookup[x]);
    }

    private string GetModuleKeysForIncludeFilters(IEnumerable<string> filters, char escapeSymbol, string moduleKeys)
    {
      string[] validFilters = GetValidFilters(filters);

      return !validFilters.Any() ? moduleKeys : GetModuleKeysForValidFilters(escapeSymbol, moduleKeys, validFilters);
    }

    private string GetModuleKeysForExcludeFilters(IEnumerable<string> filters, char escapeSymbol, string moduleKeys)
    {
      string[] validFilters = GetValidFilters(filters);

      return !validFilters.Any() ? string.Empty : GetModuleKeysForValidFilters(escapeSymbol, moduleKeys, validFilters);
    }

    private static string GetModuleKeysForValidFilters(char escapeSymbol, string moduleKeys, string[] validFilters)
    {
      string pattern = CreateRegexPattern(validFilters, escapeSymbol);
      IEnumerable<Match> matches = Regex.Matches(moduleKeys, pattern, RegexOptions.IgnoreCase).Cast<Match>();

      return string.Join(
        Environment.NewLine,
        matches.Where(x => x.Success).Select(x => x.Groups[0].Value));
    }

    private string[] GetValidFilters(IEnumerable<string> filters)
    {
      return (filters ?? Array.Empty<string>())
          .Where(IsValidFilterExpression)
          .Where(x => x.EndsWith("*"))
          .ToArray();
    }

    private static string CreateRegexPattern(IEnumerable<string> filters, char escapeSymbol)
    {
      IEnumerable<string> regexPatterns = filters.Select(x =>
          $"{escapeSymbol}{WildcardToRegex(x.Substring(1, x.IndexOf(']') - 1)).Trim('^', '$')}{escapeSymbol}");
      return string.Join("|", regexPatterns);
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
        => Regex.IsMatch(method, WildcardToRegex("<*>*__*|*"));

    public void SetLogger(ILogger logger)
    {
      _logger = logger;
    }

    private static bool IsTypeFilterMatch(string module, string type, string[] filters)
    {
      Debug.Assert(module != null);
      Debug.Assert(filters != null);

      foreach (string filter in filters)
      {
#pragma warning disable IDE0057 // Use range operator
        string typePattern = filter.Substring(filter.IndexOf(']') + 1);
        string modulePattern = filter.Substring(1, filter.IndexOf(']') - 1);
#pragma warning restore IDE0057 // Use range operator

        typePattern = WildcardToRegex(typePattern);
        modulePattern = WildcardToRegex(modulePattern);

        if (Regex.IsMatch(type, typePattern) && Regex.IsMatch(module, modulePattern))
          return true;
      }

      return false;
    }

    private static string GetBackupPath(string module, string identifier)
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

    private static string WildcardToRegex(string pattern)
    {
      return "^" + Regex.Escape(pattern).
      Replace("\\*", ".*").
      Replace("\\?", "?") + "$";
    }

    private static bool IsAssembly(string filePath)
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
