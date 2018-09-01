using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;

namespace Coverlet.Core
{
    public class Coverage
    {
        private string _module;
        private string _identifier;
        private string[] _excludeFilters;
        private string[] _includeFilters;
        private string[] _excludedSourceFiles;
        private List<InstrumenterResult> _results;

        public string Identifier
        {
            get { return _identifier; }
        }

        public Coverage(string module, string[] excludeFilters, string[] includeFilters, string[] excludedSourceFiles)
        {
            _module = module;
            _excludeFilters = excludeFilters;
            _includeFilters = includeFilters;
            _excludedSourceFiles = excludedSourceFiles;
            _identifier = Guid.NewGuid().ToString();
            _results = new List<InstrumenterResult>();
        }

        public void PrepareModules()
        {
            string[] modules = InstrumentationHelper.GetCoverableModules(_module);
            string[] excludes =  InstrumentationHelper.GetExcludedFiles(_excludedSourceFiles);
            _excludeFilters = _excludeFilters?.Where(f => InstrumentationHelper.IsValidFilterExpression(f)).ToArray();
            _includeFilters = _includeFilters?.Where(f => InstrumentationHelper.IsValidFilterExpression(f)).ToArray();

            foreach (var module in modules)
            {
                if (InstrumentationHelper.IsModuleExcluded(module, _excludeFilters)
                    || !InstrumentationHelper.IsModuleIncluded(module, _includeFilters))
                    continue;

                var instrumenter = new Instrumenter(module, _identifier, _excludeFilters, _includeFilters, excludes);
                if (instrumenter.CanInstrument())
                {
                    InstrumentationHelper.BackupOriginalModule(module, _identifier);
                    var result = instrumenter.Instrument();
                    _results.Add(result);
                }
            }
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

                modules.Add(result.ModulePath, documents);
                InstrumentationHelper.RestoreOriginalModule(result.ModulePath, _identifier);
            }

            return new CoverageResult
            {
                Identifier = _identifier,
                Modules = modules
            };
        }

        private void CalculateCoverage()
        {
            foreach (var result in _results)
            {
                if (!File.Exists(result.HitsFilePath))
                {
                    // File not instrumented, or nothing in it called.  Warn about this?
                    continue;
                }

                List<Document> documents = result.Documents.Values.ToList();

                using (var fs = new FileStream(result.HitsFilePath, FileMode.Open))
                using (var br = new BinaryReader(fs))
                {
                    int hitCandidatesCount = br.ReadInt32();

                    // TODO: hitCandidatesCount should be verified against result.HitCandidates.Count

                    var documentsList = result.Documents.Values.ToList();

                    for (int i = 0; i < hitCandidatesCount; ++i)
                    {
                        var hitLocation = result.HitCandidates[i];

                        var document = documentsList[hitLocation.docIndex];

                        int hits = br.ReadInt32();

                        if (hitLocation.isBranch)
                        {
                            var branch = document.Branches[(hitLocation.start, hitLocation.end)];
                            branch.Hits += hits;
                        }
                        else
                        {
                            for (int j = hitLocation.start; j <= hitLocation.end; j++)
                            {
                                var line = document.Lines[j];
                                line.Hits += hits;
                            }
                        }
                    }
                }

                InstrumentationHelper.DeleteHitsFile(result.HitsFilePath);
            }
        }
    }
}
