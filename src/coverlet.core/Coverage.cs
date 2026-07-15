// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;

namespace Coverlet.Core
{
  [DataContract]
  internal class CoverageParameters
  {
    [DataMember]
    public string Module { get; set; }
    [DataMember]
    public string[] IncludeFilters { get; set; }
    [DataMember]
    public string[] IncludeDirectories { get; set; }
    [DataMember]
    public string[] ExcludeFilters { get; set; }
    [DataMember]
    public string[] ExcludedSourceFiles { get; set; }
    [DataMember]
    public string[] ExcludeAttributes { get; set; }
    [DataMember]
    public bool IncludeTestAssembly { get; set; }
    [DataMember]
    public bool SingleHit { get; set; }
    [DataMember]
    public string MergeWith { get; set; }
    [DataMember]
    public bool UseSourceLink { get; set; }
    [DataMember]
    public string[] DoesNotReturnAttributes { get; set; }
    [DataMember]
    public bool SkipAutoProps { get; set; }
    [DataMember]
    public bool DeterministicReport { get; set; }
    [DataMember]
    public string ExcludeAssembliesWithoutSources { get; set; }
    [DataMember]
    public bool DisableManagedInstrumentationRestore { get; set; }
  }

  internal class Coverage : ICoverage
  {
    private readonly string _moduleOrAppDirectory;
    private readonly ILogger _logger;
    private readonly IInstrumentationHelper _instrumentationHelper;
    private readonly IFileSystem _fileSystem;
    private readonly ISourceRootTranslator _sourceRootTranslator;
    private readonly ICecilSymbolHelper _cecilSymbolHelper;
    private readonly List<InstrumenterResult> _results;
    private readonly CoverageParameters _parameters;

    public string Identifier { get; }

    readonly JsonSerializerOptions _options = new()
    {
      PropertyNameCaseInsensitive = true,
      IncludeFields = true,
      WriteIndented = true
    };

    public Coverage(string moduleOrDirectory,
        CoverageParameters parameters,
        ILogger logger,
        IInstrumentationHelper instrumentationHelper,
        IFileSystem fileSystem,
        ISourceRootTranslator sourceRootTranslator,
        ICecilSymbolHelper cecilSymbolHelper)
    {
      _moduleOrAppDirectory = moduleOrDirectory;
      parameters.IncludeDirectories ??= [];
      _logger = logger;
      _instrumentationHelper = instrumentationHelper;
      _parameters = parameters;
      _fileSystem = fileSystem;
      _sourceRootTranslator = sourceRootTranslator;
      _cecilSymbolHelper = cecilSymbolHelper;
      Identifier = Guid.NewGuid().ToString();
      _results = [];
    }

    public Coverage(CoveragePrepareResult prepareResult,
                    ILogger logger,
                    IInstrumentationHelper instrumentationHelper,
                    IFileSystem fileSystem,
                    ISourceRootTranslator sourceRootTranslator)
    {
      Identifier = prepareResult.Identifier;
      _moduleOrAppDirectory = prepareResult.ModuleOrDirectory;
      _parameters = prepareResult.Parameters;
      _results = [.. prepareResult.Results];
      _logger = logger;
      _instrumentationHelper = instrumentationHelper;
      _fileSystem = fileSystem;
      _sourceRootTranslator = sourceRootTranslator;
    }

    public CoveragePrepareResult PrepareModules()
    {
      string[] modules = _instrumentationHelper.GetCoverableModules(_moduleOrAppDirectory, _parameters.IncludeDirectories, _parameters.IncludeTestAssembly);

      Array.ForEach(_parameters.ExcludeFilters ?? [], filter => _logger.LogVerbose($"Excluded module filter '{filter}'"));
      Array.ForEach(_parameters.IncludeFilters ?? [], filter => _logger.LogVerbose($"Included module filter '{filter}'"));
      Array.ForEach(_parameters.ExcludedSourceFiles ?? [], filter => _logger.LogVerbose($"Excluded source files filter '{FileSystem.EscapeFileName(filter)}'"));

      _parameters.ExcludeFilters = _parameters.ExcludeFilters?.Where(f => _instrumentationHelper.IsValidFilterExpression(f)).ToArray();
      _parameters.IncludeFilters = _parameters.IncludeFilters?.Where(f => _instrumentationHelper.IsValidFilterExpression(f)).ToArray();

      IReadOnlyList<string> validModules = [.. _instrumentationHelper.SelectModules(modules, _parameters.IncludeFilters, _parameters.ExcludeFilters)];
      foreach (string excludedModule in modules.Except(validModules))
      {
        _logger.LogVerbose($"Excluded module: '{excludedModule}'");
      }

      foreach (string module in validModules)
      {
        var instrumenter = new Instrumenter(module,
                                            Identifier,
                                            _parameters,
                                            _logger,
                                            _instrumentationHelper,
                                            _fileSystem,
                                            _sourceRootTranslator,
                                            _cecilSymbolHelper);

        if (!instrumenter.CanInstrument())
        {
          continue;
        }

        InstrumentationPreflightResult preflightResult = instrumenter.Preflight();
        if (preflightResult.Status != InstrumentationPreflightStatus.Ready)
        {
          _logger.LogWarning($"Skipping module '{module}': {preflightResult.Status}. {preflightResult.Reason}");
          continue;
        }

        _instrumentationHelper.BackupOriginalModule(module, Identifier, _parameters.DisableManagedInstrumentationRestore);

        // Guard code path and restore if instrumentation fails.
        try
        {
          InstrumenterResult result = instrumenter.Instrument();
          if (!instrumenter.SkipModule)
          {
            _results.Add(result);
            _logger.LogVerbose($"Instrumented module: '{module}'");
          }
        }
        catch (Exception ex)
        {
          _logger.LogWarning($"Unable to instrument module: {module}\n{ex}");
          _instrumentationHelper.RestoreOriginalModule(module, Identifier);
        }
      }

      return new CoveragePrepareResult()
      {
        Identifier = Identifier,
        ModuleOrDirectory = _moduleOrAppDirectory,
        Parameters = _parameters,
        Results = [.. _results]
      };
    }

    public CoverageResult GetCoverageResult()
    {
      CalculateCoverage();

      var modules = new Modules();
      foreach (InstrumenterResult result in _results)
      {
        var documents = new Documents();
        foreach (Document doc in result.Documents.Values)
        {
          // Construct Line Results
          foreach (Line line in doc.Lines.Values)
          {
            if (documents.TryGetValue(doc.Path, out Classes classes))
            {
              if (classes.TryGetValue(line.Class, out Methods methods))
              {
                if (methods.TryGetValue(line.Method, out Method method))
                {
                  documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number, line.Hits);
                }
                else
                {
                  documents[doc.Path][line.Class].Add(line.Method, new Method());
                  documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number, line.Hits);
                }
              }
              else
              {
                documents[doc.Path].Add(line.Class, []);
                documents[doc.Path][line.Class].Add(line.Method, new Method());
                documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number, line.Hits);
              }
            }
            else
            {
              documents.Add(doc.Path, []);
              documents[doc.Path].Add(line.Class, []);
              documents[doc.Path][line.Class].Add(line.Method, new Method());
              documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number, line.Hits);
            }
          }

          // Construct Branch Results
          foreach (Branch branch in doc.Branches.Values)
          {
            if (documents.TryGetValue(doc.Path, out Classes classes))
            {
              if (classes.TryGetValue(branch.Class, out Methods methods))
              {
                if (methods.TryGetValue(branch.Method, out Method method))
                {
                  method.Branches.Add(new BranchInfo
                  { Line = branch.Number, Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                  );
                }
                else
                {
                  documents[doc.Path][branch.Class].Add(branch.Method, new Method());
                  documents[doc.Path][branch.Class][branch.Method].Branches.Add(new BranchInfo
                  { Line = branch.Number, Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                  );
                }
              }
              else
              {
                documents[doc.Path].Add(branch.Class, []);
                documents[doc.Path][branch.Class].Add(branch.Method, new Method());
                documents[doc.Path][branch.Class][branch.Method].Branches.Add(new BranchInfo
                { Line = branch.Number, Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                );
              }
            }
            else
            {
              documents.Add(doc.Path, []);
              documents[doc.Path].Add(branch.Class, []);
              documents[doc.Path][branch.Class].Add(branch.Method, new Method());
              documents[doc.Path][branch.Class][branch.Method].Branches.Add(new BranchInfo
              { Line = branch.Number, Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
              );
            }
          }
        }

        modules.Add(Path.GetFileName(result.ModulePath), documents);
        _instrumentationHelper.RestoreOriginalModule(result.ModulePath, Identifier);
      }

      // In case of anonymous delegate compiler generate a custom class and passes it as type.method delegate.
      // If in delegate method we've a branches we need to move these to "actual" class/method that use it.
      // We search "method" with same "Line" of closure class method and add missing branches to it,
      // in this way we correctly report missing branch inside compiled generated anonymous delegate.
      List<string> compileGeneratedClassToRemove = null;
      foreach (KeyValuePair<string, Documents> module in modules)
      {
        foreach (KeyValuePair<string, Classes> document in module.Value)
        {
          foreach (KeyValuePair<string, Methods> @class in document.Value)
          {
            // We fix only lamda generated class
            // https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Portable/Symbols/Synthesized/GeneratedNameKind.cs#L18
            if (!@class.Key.Contains("<>c"))
            {
              continue;
            }

            foreach (KeyValuePair<string, Method> method in @class.Value)
            {
              foreach (BranchInfo branch in method.Value.Branches)
              {
                if (BranchInCompilerGeneratedClass(method.Key))
                {
                  Method actualMethod = GetMethodWithSameLineInSameDocument(document.Value, @class.Key, branch.Line);

                  if (actualMethod is null)
                  {
                    continue;
                  }

                  actualMethod.Branches.Add(branch);

                  compileGeneratedClassToRemove ??= [];

                  if (!compileGeneratedClassToRemove.Contains(@class.Key))
                  {
                    compileGeneratedClassToRemove.Add(@class.Key);
                  }
                }
              }
            }
          }
        }
      }

      // After method/branches analysis of compiled generated class we can remove noise from reports
      if (compileGeneratedClassToRemove is not null)
      {
        foreach (KeyValuePair<string, Documents> module in modules)
        {
          foreach (KeyValuePair<string, Classes> document in module.Value)
          {
            foreach (string classToRemove in compileGeneratedClassToRemove)
            {
              document.Value.Remove(classToRemove);
            }
          }
        }
      }

      var coverageResult = new CoverageResult { Identifier = Identifier, Modules = modules, InstrumentedResults = _results, Parameters = _parameters };

      if (!string.IsNullOrEmpty(_parameters.MergeWith) && !string.IsNullOrWhiteSpace(_parameters.MergeWith))
      {
        if (_fileSystem.Exists(_parameters.MergeWith))
        {
          _logger.LogInformation($"MergeWith: '{_parameters.MergeWith}'.");
          string json = _fileSystem.ReadAllText(_parameters.MergeWith);
          coverageResult.Merge(JsonSerializer.Deserialize<Modules>(json, _options));
        }
        else
        {
          _logger.LogInformation($"MergeWith: file '{_parameters.MergeWith}' does not exist.");
        }
      }

      return coverageResult;
    }

    private bool BranchInCompilerGeneratedClass(string methodName)
    {
      foreach (InstrumenterResult instrumentedResult in _results)
      {
        if (instrumentedResult.BranchesInCompiledGeneratedClass.Contains(methodName))
        {
          return true;
        }
      }
      return false;
    }

    private static Method GetMethodWithSameLineInSameDocument(Classes documentClasses, string compilerGeneratedClassName, int branchLine)
    {
      foreach (KeyValuePair<string, Methods> @class in documentClasses)
      {
        if (@class.Key == compilerGeneratedClassName)
        {
          continue;
        }

        foreach (KeyValuePair<string, Method> method in @class.Value)
        {
          foreach (KeyValuePair<int, int> line in method.Value.Lines)
          {
            if (line.Key == branchLine)
            {
              return method.Value;
            }
          }
        }
      }
      return null;
    }

    private void CalculateCoverage()
    {
      foreach (InstrumenterResult result in _results)
      {
        var documents = result.Documents.Values.ToList();
        if (_parameters.UseSourceLink && result.SourceLink != null)
        {
          JsonNode jObject = JsonNode.Parse(result.SourceLink)["documents"];
          Dictionary<string, string> sourceLinkDocuments = JsonSerializer.Deserialize<Dictionary<string, string>>(jObject.ToString());
          foreach (Document document in documents)
          {
            document.Path = GetSourceLinkUrl(sourceLinkDocuments, document.Path);
          }
        }

        string lockFilePath = result.HitsFilePath + ".lock";
        string tmpFilePath = result.HitsFilePath + ".tmp";

        // Hold a shared lock on the lock file while reading so the writer cannot slip in a new
        // write between our existence check and our read. The OS releases the writer's exclusive
        // lock on process death, so a crashed writer is also detected here.
        using (FileStream lockFileHandle = TryAcquireReaderLockOnFile(lockFilePath, result.Module, _logger))
        {
          if (!_fileSystem.Exists(result.HitsFilePath))
          {
            if (!_fileSystem.Exists(tmpFilePath))
            {
              _logger.LogVerbose($"Hits file: '{result.HitsFilePath}' not found for module: '{result.Module}'");
              continue;
            }

            // .tmp present, hits absent: the writer started but did not finish (crashed or killed).
            if (!_fileSystem.Exists(result.HitsFilePath))
            {
              _logger.LogWarning($"Hits file not found for module '{result.Module}': '{result.HitsFilePath}'. An incomplete write was detected; coverage data for this module is missing.");
              continue;
            }
          }
          else if (_fileSystem.Exists(tmpFilePath))
          {
            // Hits file exists but .tmp also present: merge write was killed mid-replace.
            // The file reflects the pre-merge state; coverage data from the merge is lost.
            _logger.LogWarning($"Hits file for module '{result.Module}' may be incomplete: a merge was in progress when the writer was killed. Coverage data from the merge is missing.");
          }

          // Calculate lines to skip for every hits start/end candidate
          // Nested ranges win on outermost one
          foreach (HitCandidate hitCandidate in result.HitCandidates)
          {
            if (hitCandidate.isBranch || hitCandidate.end == hitCandidate.start)
            {
              continue;
            }

            foreach (HitCandidate hitCandidateToCompare in result.HitCandidates.Where(x => x.docIndex.Equals(hitCandidate.docIndex)))
            {
              if (hitCandidate != hitCandidateToCompare && !hitCandidateToCompare.isBranch && hitCandidateToCompare.start > hitCandidate.start &&
                   hitCandidateToCompare.end < hitCandidate.end)
              {
                for (int i = hitCandidateToCompare.start;
                     i <= (hitCandidateToCompare.end == 0 ? hitCandidateToCompare.start : hitCandidateToCompare.end);
                     i++)
                {
                  (hitCandidate.AccountedByNestedInstrumentation ??= []).Add(i);
                }
              }
            }
          }

          var documentsList = result.Documents.Values.ToList();
          using (Stream fs = _fileSystem.NewFileStream(result.HitsFilePath, FileMode.Open, FileAccess.Read))
          using (var br = new BinaryReader(fs))
          {
            int hitCandidatesCount = br.ReadInt32();

            // TODO: hitCandidatesCount should be verified against result.HitCandidates.Count

            for (int i = 0; i < hitCandidatesCount; ++i)
            {
              HitCandidate hitLocation = result.HitCandidates[i];
              Document document = documentsList[hitLocation.docIndex];
              int hits = br.ReadInt32();

              if (hits == 0)
                continue;

              hits = hits < 0 ? int.MaxValue : hits;

              if (hitLocation.isBranch)
              {
                Branch branch = document.Branches[new BranchKey(hitLocation.start, hitLocation.end)];
                branch.Hits += hits;

                if (branch.Hits < 0)
                  branch.Hits = int.MaxValue;
              }
              else
              {
                for (int j = hitLocation.start; j <= hitLocation.end; j++)
                {
                  if (hitLocation.AccountedByNestedInstrumentation?.Contains(j) == true)
                  {
                    continue;
                  }

                  Line line = document.Lines[j];
                  line.Hits += hits;

                  if (line.Hits < 0)
                    line.Hits = int.MaxValue;
                }
              }
            }
          }
        } // shared lock released here; safe to delete the lock file below

        try
        {
          _instrumentationHelper.DeleteHitsFile(result.HitsFilePath);
          _logger.LogVerbose($"Hit file '{result.HitsFilePath}' deleted");
        }
        catch (Exception ex)
        {
          _logger.LogWarning($"Unable to remove hit file: {result.HitsFilePath} because : {ex.Message}");
        }

        TryDeleteFile(tmpFilePath);
        TryDeleteFile(lockFilePath);
      }
    }

    private static FileStream TryAcquireReaderLockOnFile(string lockFilePath, string moduleName, ILogger logger)
    {
      if (!File.Exists(lockFilePath))
        return null;

      TimeSpan elapsed = TimeSpan.Zero;
      TimeSpan interval = TimeSpan.FromMilliseconds(50);
      TimeSpan timeout = TimeSpan.FromSeconds(10);
      while (elapsed <= timeout)
      {
        try
        {
          return new FileStream(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (FileNotFoundException)
        {
          // Lock file was removed between File.Exists and FileStream open; no writer active.
          return null;
        }
        catch (IOException)
        {
          // Writer holds the lock file exclusively; wait for it to finish or die.
          Thread.Sleep(interval);
          elapsed += interval;
        }
      }

      logger.LogWarning($"Timed out waiting for write lock on hits file for module '{moduleName}'. Coverage data may be incomplete.");
      return null;
    }

    private static void TryDeleteFile(string path)
    {
      try
      {
        File.Delete(path);
      }
      catch
      {
      }
    }

    internal string GetSourceLinkUrl(Dictionary<string, string> sourceLinkDocuments, string document)
    {
      if (sourceLinkDocuments.TryGetValue(document, out string url))
      {
        return url;
      }

      string keyWithBestMatch = string.Empty;
      string relativePathOfBestMatch = string.Empty;

      foreach (KeyValuePair<string, string> sourceLinkDocument in sourceLinkDocuments)
      {
        string key = sourceLinkDocument.Key;
        if (Path.GetFileName(key) != "*") continue;
#pragma warning disable IDE0057
        IReadOnlyList<SourceRootMapping> rootMapping = _sourceRootTranslator.ResolvePathRoot(key.Substring(0, key.Length - 1));
#pragma warning restore IDE0057
        foreach (string keyMapping in rootMapping is null ? [key] : new List<string>(rootMapping.Select(m => m.OriginalPath)))
        {
          string directoryDocument = Path.GetDirectoryName(document);
          string sourceLinkRoot = Path.GetDirectoryName(keyMapping);
          string relativePath = "";

          // if document is on repo root we skip relative path calculation
          if (directoryDocument != sourceLinkRoot)
          {
            if (!directoryDocument.StartsWith(sourceLinkRoot + Path.DirectorySeparatorChar))
              continue;
#pragma warning disable IDE0057
            relativePath = directoryDocument.Substring(sourceLinkRoot.Length + 1);
#pragma warning restore IDE0057
          }

          if (relativePathOfBestMatch.Length == 0)
          {
            keyWithBestMatch = sourceLinkDocument.Key;
            relativePathOfBestMatch = relativePath;
          }

          if (relativePath.Length < relativePathOfBestMatch.Length)
          {
            keyWithBestMatch = sourceLinkDocument.Key;
            relativePathOfBestMatch = relativePath;
          }
        }
      }

      relativePathOfBestMatch = relativePathOfBestMatch == "." ? string.Empty : relativePathOfBestMatch;

      string replacement = Path.Combine(relativePathOfBestMatch, Path.GetFileName(document));
      replacement = replacement.Replace('\\', '/');

      if (sourceLinkDocuments.TryGetValue(keyWithBestMatch, out url))
      {
        return url.Replace("*", replacement);
      }

      return document;
    }
  }
}
