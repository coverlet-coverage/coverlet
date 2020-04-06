using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Abstracts;
using Coverlet.Core.Instrumentation;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Coverlet.Core
{
    internal class Coverage
    {
        private string _module;
        private string _identifier;
        private string[] _includeFilters;
        private string[] _includeDirectories;
        private string[] _excludeFilters;
        private string[] _excludedSourceFiles;
        private string[] _excludeAttributes;
        private bool _includeTestAssembly;
        private bool _singleHit;
        private string _mergeWith;
        private bool _useSourceLink;
        private ILogger _logger;
        private IInstrumentationHelper _instrumentationHelper;
        private IFileSystem _fileSystem;
        private ISourceRootTranslator _sourceRootTranslator;
        private List<InstrumenterResult> _results;

        public string Identifier
        {
            get { return _identifier; }
        }

        public Coverage(string module,
            string[] includeFilters,
            string[] includeDirectories,
            string[] excludeFilters,
            string[] excludedSourceFiles,
            string[] excludeAttributes,
            bool includeTestAssembly,
            bool singleHit,
            string mergeWith,
            bool useSourceLink,
            ILogger logger,
            IInstrumentationHelper instrumentationHelper,
            IFileSystem fileSystem,
            ISourceRootTranslator sourceRootTranslator)
        {
            _module = module;
            _includeFilters = includeFilters;
            _includeDirectories = includeDirectories ?? Array.Empty<string>();
            _excludeFilters = excludeFilters;
            _excludedSourceFiles = excludedSourceFiles;
            _excludeAttributes = excludeAttributes;
            _includeTestAssembly = includeTestAssembly;
            _singleHit = singleHit;
            _mergeWith = mergeWith;
            _useSourceLink = useSourceLink;
            _logger = logger;
            _instrumentationHelper = instrumentationHelper;
            _fileSystem = fileSystem;
            _sourceRootTranslator = sourceRootTranslator;

            _identifier = Guid.NewGuid().ToString();
            _results = new List<InstrumenterResult>();
        }

        public Coverage(CoveragePrepareResult prepareResult, ILogger logger, IInstrumentationHelper instrumentationHelper, IFileSystem fileSystem)
        {
            _identifier = prepareResult.Identifier;
            _module = prepareResult.Module;
            _mergeWith = prepareResult.MergeWith;
            _useSourceLink = prepareResult.UseSourceLink;
            _results = new List<InstrumenterResult>(prepareResult.Results);
            _logger = logger;
            _instrumentationHelper = instrumentationHelper;
            _fileSystem = fileSystem;
        }

        public CoveragePrepareResult PrepareModules()
        {
            string[] modules = _instrumentationHelper.GetCoverableModules(_module, _includeDirectories, _includeTestAssembly);

            Array.ForEach(_excludeFilters ?? Array.Empty<string>(), filter => _logger.LogVerbose($"Excluded module filter '{filter}'"));
            Array.ForEach(_includeFilters ?? Array.Empty<string>(), filter => _logger.LogVerbose($"Included module filter '{filter}'"));
            Array.ForEach(_excludedSourceFiles ?? Array.Empty<string>(), filter => _logger.LogVerbose($"Excluded source files filter '{filter}'"));

            _excludeFilters = _excludeFilters?.Where(f => _instrumentationHelper.IsValidFilterExpression(f)).ToArray();
            _includeFilters = _includeFilters?.Where(f => _instrumentationHelper.IsValidFilterExpression(f)).ToArray();

            foreach (var module in modules)
            {
                if (_instrumentationHelper.IsModuleExcluded(module, _excludeFilters) ||
                    !_instrumentationHelper.IsModuleIncluded(module, _includeFilters))
                {
                    _logger.LogVerbose($"Excluded module: '{module}'");
                    continue;
                }

                var instrumenter = new Instrumenter(module, _identifier, _excludeFilters, _includeFilters, _excludedSourceFiles, _excludeAttributes, _singleHit, _logger, _instrumentationHelper, _fileSystem, _sourceRootTranslator);

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
                Module = _module,
                MergeWith = _mergeWith,
                UseSourceLink = _useSourceLink,
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

            var coverageResult = new CoverageResult { Identifier = _identifier, Modules = modules, InstrumentedResults = _results, UseSourceLink = _useSourceLink };

            if (!string.IsNullOrEmpty(_mergeWith) && !string.IsNullOrWhiteSpace(_mergeWith) && _fileSystem.Exists(_mergeWith))
            {
                string json = _fileSystem.ReadAllText(_mergeWith);
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
                if (_useSourceLink && result.SourceLink != null)
                {
                    var jObject = JObject.Parse(result.SourceLink)["documents"];
                    var sourceLinkDocuments = JsonConvert.DeserializeObject<Dictionary<string, string>>(jObject.ToString());
                    foreach (var document in documents)
                    {
                        document.Path = GetSourceLinkUrl(sourceLinkDocuments, document.Path);
                    }
                }

                List<(int docIndex, int line)> zeroHitsLines = new List<(int docIndex, int line)>();
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
                                var line = document.Lines[j];
                                line.Hits += hits;

                                // We register 0 hit lines for later cleanup false positive of nested lambda closures
                                if (hits == 0)
                                {
                                    zeroHitsLines.Add((hitLocation.docIndex, line.Number));
                                }
                            }
                        }
                    }
                }

                // Cleanup nested state machine false positive hits
                foreach (var (docIndex, line) in zeroHitsLines)
                {
                    foreach (var lineToCheck in documentsList[docIndex].Lines)
                    {
                        if (lineToCheck.Key == line)
                        {
                            lineToCheck.Value.Hits = 0;
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

                string directoryDocument = Path.GetDirectoryName(document);
                string sourceLinkRoot = Path.GetDirectoryName(key);
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

            relativePathOfBestMatch = relativePathOfBestMatch == "." ? string.Empty : relativePathOfBestMatch;

            string replacement = Path.Combine(relativePathOfBestMatch, Path.GetFileName(document));
            replacement = replacement.Replace('\\', '/');

            url = sourceLinkDocuments[keyWithBestMatch];
            return url.Replace("*", replacement);
        }
    }
}
