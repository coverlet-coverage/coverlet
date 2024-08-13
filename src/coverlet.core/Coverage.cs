// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
  }

  internal class Coverage
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

    public Coverage(string moduleOrDirectory,
        CoverageParameters parameters,
        ILogger logger,
        IInstrumentationHelper instrumentationHelper,
        IFileSystem fileSystem,
        ISourceRootTranslator sourceRootTranslator,
        ICecilSymbolHelper cecilSymbolHelper)
    {
      _moduleOrAppDirectory = moduleOrDirectory;
      parameters.IncludeDirectories ??= Array.Empty<string>();
      _logger = logger;
      _instrumentationHelper = instrumentationHelper;
      _parameters = parameters;
      _fileSystem = fileSystem;
      _sourceRootTranslator = sourceRootTranslator;
      _cecilSymbolHelper = cecilSymbolHelper;
      Identifier = Guid.NewGuid().ToString();
      _results = new List<InstrumenterResult>();
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
      _results = new List<InstrumenterResult>(prepareResult.Results);
      _logger = logger;
      _instrumentationHelper = instrumentationHelper;
      _fileSystem = fileSystem;
      _sourceRootTranslator = sourceRootTranslator;
    }

    public CoveragePrepareResult PrepareModules()
    {
      string[] modules = _instrumentationHelper.GetCoverableModules(_moduleOrAppDirectory, _parameters.IncludeDirectories, _parameters.IncludeTestAssembly);

      Array.ForEach(_parameters.ExcludeFilters ?? Array.Empty<string>(), filter => _logger.LogVerbose($"Excluded module filter '{filter}'"));
      Array.ForEach(_parameters.IncludeFilters ?? Array.Empty<string>(), filter => _logger.LogVerbose($"Included module filter '{filter}'"));
      Array.ForEach(_parameters.ExcludedSourceFiles ?? Array.Empty<string>(), filter => _logger.LogVerbose($"Excluded source files filter '{FileSystem.EscapeFileName(filter)}'"));

      _parameters.ExcludeFilters = _parameters.ExcludeFilters?.Where(f => _instrumentationHelper.IsValidFilterExpression(f)).ToArray();
      _parameters.IncludeFilters = _parameters.IncludeFilters?.Where(f => _instrumentationHelper.IsValidFilterExpression(f)).ToArray();

      IReadOnlyList<string> validModules = _instrumentationHelper.SelectModules(modules, _parameters.IncludeFilters, _parameters.ExcludeFilters).ToList();
      foreach (var excludedModule in modules.Except(validModules))
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

        if (instrumenter.CanInstrument())
        {
          _instrumentationHelper.BackupOriginalModule(module, Identifier);

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
      }

      return new CoveragePrepareResult()
      {
        Identifier = Identifier,
        ModuleOrDirectory = _moduleOrAppDirectory,
        Parameters = _parameters,
        Results = _results.ToArray()
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
                documents[doc.Path].Add(line.Class, new Methods());
                documents[doc.Path][line.Class].Add(line.Method, new Method());
                documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number, line.Hits);
              }
            }
            else
            {
              documents.Add(doc.Path, new Classes());
              documents[doc.Path].Add(line.Class, new Methods());
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
                documents[doc.Path].Add(branch.Class, new Methods());
                documents[doc.Path][branch.Class].Add(branch.Method, new Method());
                documents[doc.Path][branch.Class][branch.Method].Branches.Add(new BranchInfo
                { Line = branch.Number, Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                );
              }
            }
            else
            {
              documents.Add(doc.Path, new Classes());
              documents[doc.Path].Add(branch.Class, new Methods());
              documents[doc.Path][branch.Class].Add(branch.Method, new Method());
              documents[doc.Path][branch.Class][branch.Method].Branches.Add(new BranchInfo
              { Line = branch.Number, Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
              );
            }
          }
        }

        modules.Add(Path.GetFileName(result.ModulePath), documents);
        UnloadModule(result.ModulePath);
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

                  if (compileGeneratedClassToRemove is null)
                  {
                    compileGeneratedClassToRemove = new List<string>();
                  }

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
          coverageResult.Merge(JsonConvert.DeserializeObject<Modules>(json));
        } else
        {
          _logger.LogInformation($"MergeWith: file '{_parameters.MergeWith}' does not exist.");
        }
      }

      return coverageResult;
    }

    /// <summary>
    /// unloads all modules that were instrumented
    /// </summary>
    /// <returns> exit code of module unloading </returns>
    public int UnloadModule()
    {
      string[] modules = _instrumentationHelper.GetCoverableModules(_moduleOrAppDirectory,
        _parameters.IncludeDirectories, _parameters.IncludeTestAssembly);

      IReadOnlyList<string> validModules = _instrumentationHelper
        .SelectModules(modules, _parameters.IncludeFilters, _parameters.ExcludeFilters).ToList();
      foreach (string modulePath in validModules) {
        try
        {
          _instrumentationHelper.RestoreOriginalModule(modulePath, Identifier);
        }
        catch (Exception e)
        {
          _logger.LogVerbose($"{e.InnerException} occured, module unloading aborted.");
          return -1;
        }
      }

      return 0;
  }

    /// <summary>
    /// Invoke the unloading of modules and restoration of the original assembly files, made public to allow unloading
    /// of instrumentation in large scale testing utilising parallelization
    /// </summary>
    /// <param name="modulePath"></param>
    /// <returns> exist code of unloading modules </returns>
    public void UnloadModule(string modulePath)
    {
      try
      {
        _instrumentationHelper.RestoreOriginalModule(modulePath, Identifier);
      }
      catch (Exception e)
      {
        _logger.LogVerbose($"{e.InnerException} occured, module unloading aborted.");
      }
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
        if (!_fileSystem.Exists(result.HitsFilePath))
        {
          // Hits file could be missed mainly for two reason
          // 1) Issue during module Unload()
          // 2) Instrumented module is never loaded or used so we don't have any hit to register and
          //    module tracker is never used
          _logger.LogVerbose($"Hits file:'{result.HitsFilePath}' not found for module: '{result.Module}'");
          continue;
        }

        var documents = result.Documents.Values.ToList();
        if (_parameters.UseSourceLink && result.SourceLink != null)
        {
          JToken jObject = JObject.Parse(result.SourceLink)["documents"];
          Dictionary<string, string> sourceLinkDocuments = JsonConvert.DeserializeObject<Dictionary<string, string>>(jObject.ToString());
          foreach (Document document in documents)
          {
            document.Path = GetSourceLinkUrl(sourceLinkDocuments, document.Path);
          }
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
            if (hitCandidate != hitCandidateToCompare && !hitCandidateToCompare.isBranch)
            {
              if (hitCandidateToCompare.start > hitCandidate.start &&
                 hitCandidateToCompare.end < hitCandidate.end)
              {
                for (int i = hitCandidateToCompare.start;
                     i <= (hitCandidateToCompare.end == 0 ? hitCandidateToCompare.start : hitCandidateToCompare.end);
                     i++)
                {
                  (hitCandidate.AccountedByNestedInstrumentation ??= new HashSet<int>()).Add(i);
                }
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

        try
        {
          _instrumentationHelper.DeleteHitsFile(result.HitsFilePath);
          _logger.LogVerbose($"Hit file '{result.HitsFilePath}' deleted");
        }
        catch (Exception ex)
        {
          _logger.LogWarning($"Unable to remove hit file: {result.HitsFilePath} because : {ex.Message}");
        }
      }
    }

    private string GetSourceLinkUrl(Dictionary<string, string> sourceLinkDocuments, string document)
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

#pragma warning disable IDE0057 // Use range operator
        IReadOnlyList<SourceRootMapping> rootMapping = _sourceRootTranslator.ResolvePathRoot(key.Substring(0, key.Length - 1));
#pragma warning restore IDE0057 // Use range operator
        foreach (string keyMapping in rootMapping is null ? new List<string>() { key } : new List<string>(rootMapping.Select(m => m.OriginalPath)))
        {
          string directoryDocument = Path.GetDirectoryName(document);
          string sourceLinkRoot = Path.GetDirectoryName(keyMapping);
          string relativePath = "";

          // if document is on repo root we skip relative path calculation
          if (directoryDocument != sourceLinkRoot)
          {
            if (!directoryDocument.StartsWith(sourceLinkRoot + Path.DirectorySeparatorChar))
              continue;

#pragma warning disable IDE0057 // Use range operator
            relativePath = directoryDocument.Substring(sourceLinkRoot.Length + 1);
#pragma warning restore IDE0057 // Use range operator
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
