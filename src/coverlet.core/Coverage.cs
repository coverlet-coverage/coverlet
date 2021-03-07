using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    }

    internal class Coverage
    {
        private string _moduleOrAppDirectory;
        private string _identifier;
        private ILogger _logger;
        private IInstrumentationHelper _instrumentationHelper;
        private IFileSystem _fileSystem;
        private ISourceRootTranslator _sourceRootTranslator;
        private ICecilSymbolHelper _cecilSymbolHelper;
        private List<InstrumenterResult> _results;
        private CoverageParameters _parameters;

        public string Identifier
        {
            get { return _identifier; }
        }

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
            _identifier = Guid.NewGuid().ToString();
            _results = new List<InstrumenterResult>();
        }

        public Coverage(CoveragePrepareResult prepareResult,
                        ILogger logger,
                        IInstrumentationHelper instrumentationHelper,
                        IFileSystem fileSystem,
                        ISourceRootTranslator sourceRootTranslator)
        {
            _identifier = prepareResult.Identifier;
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

            foreach (var module in modules)
            {
                if (_instrumentationHelper.IsModuleExcluded(module, _parameters.ExcludeFilters) ||
                    !_instrumentationHelper.IsModuleIncluded(module, _parameters.IncludeFilters))
                {
                    _logger.LogVerbose($"Excluded module: '{module}'");
                    continue;
                }

                var instrumenter = new Instrumenter(module,
                                                    _identifier,
                                                    _parameters,
                                                    _logger,
                                                    _instrumentationHelper,
                                                    _fileSystem,
                                                    _sourceRootTranslator,
                                                    _cecilSymbolHelper);

                if (instrumenter.CanInstrument())
                {
                    _instrumentationHelper.BackupOriginalModule(module, _identifier);

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
                        _logger.LogWarning($"Unable to instrument module: {module} because : {ex.Message}");
                        _instrumentationHelper.RestoreOriginalModule(module, _identifier);
                    }
                }
            }

            return new CoveragePrepareResult()
            {
                Identifier = _identifier,
                ModuleOrDirectory = _moduleOrAppDirectory,
                Parameters = _parameters,
                Results = _results.ToArray()
            };
        }

        public CoverageResult GetCoverageResult()
        {
            CalculateCoverage();

            Modules modules = new Modules();
            foreach (var result in _results)
            {
                Documents documents = new Documents();
                foreach (var doc in result.Documents.Values)
                {
                    // Construct Line Results
                    foreach (var line in doc.Lines.Values)
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
                    foreach (var branch in doc.Branches.Values)
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
                _instrumentationHelper.RestoreOriginalModule(result.ModulePath, _identifier);
            }

            // In case of anonymous delegate compiler generate a custom class and passes it as type.method delegate.
            // If in delegate method we've a branches we need to move these to "actual" class/method that use it.
            // We search "method" with same "Line" of closure class method and add missing branches to it,
            // in this way we correctly report missing branch inside compiled generated anonymous delegate.
            List<string> compileGeneratedClassToRemove = null;
            foreach (var module in modules)
            {
                foreach (var document in module.Value)
                {
                    foreach (var @class in document.Value)
                    {
                        // We fix only lamda generated class
                        // https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Portable/Symbols/Synthesized/GeneratedNameKind.cs#L18
                        if (!@class.Key.Contains("<>c"))
                        {
                            continue;
                        }

                        foreach (var method in @class.Value)
                        {
                            foreach (var branch in method.Value.Branches)
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
            if (!(compileGeneratedClassToRemove is null))
            {
                foreach (var module in modules)
                {
                    foreach (var document in module.Value)
                    {
                        foreach (var classToRemove in compileGeneratedClassToRemove)
                        {
                            document.Value.Remove(classToRemove);
                        }
                    }
                }
            }

            var coverageResult = new CoverageResult { Identifier = _identifier, Modules = modules, InstrumentedResults = _results, Parameters = _parameters };

            if (!string.IsNullOrEmpty(_parameters.MergeWith) && !string.IsNullOrWhiteSpace(_parameters.MergeWith) && _fileSystem.Exists(_parameters.MergeWith))
            {
                string json = _fileSystem.ReadAllText(_parameters.MergeWith);
                coverageResult.Merge(JsonConvert.DeserializeObject<Modules>(json));
            }

            return coverageResult;
        }

        private bool BranchInCompilerGeneratedClass(string methodName)
        {
            foreach (var instrumentedResult in _results)
            {
                if (instrumentedResult.BranchesInCompiledGeneratedClass.Contains(methodName))
                {
                    return true;
                }
            }
            return false;
        }

        private Method GetMethodWithSameLineInSameDocument(Classes documentClasses, string compilerGeneratedClassName, int branchLine)
        {
            foreach (var @class in documentClasses)
            {
                if (@class.Key == compilerGeneratedClassName)
                {
                    continue;
                }

                foreach (var method in @class.Value)
                {
                    foreach (var line in method.Value.Lines)
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
            foreach (var result in _results)
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

                List<Document> documents = result.Documents.Values.ToList();
                if (_parameters.UseSourceLink && result.SourceLink != null)
                {
                    var jObject = JObject.Parse(result.SourceLink)["documents"];
                    var sourceLinkDocuments = JsonConvert.DeserializeObject<Dictionary<string, string>>(jObject.ToString());
                    foreach (var document in documents)
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
                using (var fs = _fileSystem.NewFileStream(result.HitsFilePath, FileMode.Open))
                using (var br = new BinaryReader(fs))
                {
                    int hitCandidatesCount = br.ReadInt32();

                    // TODO: hitCandidatesCount should be verified against result.HitCandidates.Count

                    for (int i = 0; i < hitCandidatesCount; ++i)
                    {
                        var hitLocation = result.HitCandidates[i];
                        var document = documentsList[hitLocation.docIndex];
                        int hits = br.ReadInt32();

                        if (hitLocation.isBranch)
                        {
                            var branch = document.Branches[new BranchKey(hitLocation.start, hitLocation.end)];
                            branch.Hits += hits;
                        }
                        else
                        {
                            for (int j = hitLocation.start; j <= hitLocation.end; j++)
                            {
                                if (hitLocation.AccountedByNestedInstrumentation?.Contains(j) == true)
                                {
                                    continue;
                                }

                                var line = document.Lines[j];
                                line.Hits += hits;
                            }
                        }
                    }
                }

                _instrumentationHelper.DeleteHitsFile(result.HitsFilePath);
                _logger.LogVerbose($"Hit file '{result.HitsFilePath}' deleted");
            }
        }

        private string GetSourceLinkUrl(Dictionary<string, string> sourceLinkDocuments, string document)
        {
            if (sourceLinkDocuments.TryGetValue(document, out string url))
            {
                return url;
            }

            var keyWithBestMatch = string.Empty;
            var relativePathOfBestMatch = string.Empty;

            foreach (var sourceLinkDocument in sourceLinkDocuments)
            {
                string key = sourceLinkDocument.Key;
                if (Path.GetFileName(key) != "*") continue;

                IReadOnlyList<SourceRootMapping> rootMapping = _sourceRootTranslator.ResolvePathRoot(key.Substring(0, key.Length - 1));
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

                        relativePath = directoryDocument.Substring(sourceLinkRoot.Length + 1);
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

            url = sourceLinkDocuments[keyWithBestMatch];
            return url.Replace("*", replacement);
        }
    }
}
