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
        private IEnumerable<string> _excludeRules;
        private List<InstrumenterResult> _results;

        public Coverage(string module, string identifier, IEnumerable<string> excludeRules = null)
        {
            _module = module;
            _identifier = identifier;
            _excludeRules = excludeRules;
            _results = new List<InstrumenterResult>();
        }

        public void PrepareModules()
        {
            string[] modules = InstrumentationHelper.GetDependencies(_module);
            var excludedFiles =  InstrumentationHelper.GetExcludedFiles(_excludeRules);
            foreach (var module in modules)
            {
                var instrumenter = new Instrumenter(module, _identifier, excludedFiles);
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
                foreach (var doc in result.Documents)
                {
                    // Construct Line Results
                    foreach (var line in doc.Lines)
                    {
                        if (documents.TryGetValue(doc.Path, out Classes classes))
                        {
                            if (classes.TryGetValue(line.Class, out Methods methods))
                            {
                                if (methods.TryGetValue(line.Method, out Method method))
                                {
                                    documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number, new LineInfo { Hits = line.Hits });
                                }
                                else
                                {
                                    documents[doc.Path][line.Class].Add(line.Method, new Method());
                                    documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number,  new LineInfo { Hits = line.Hits });
                                }
                            }
                            else
                            {
                                documents[doc.Path].Add(line.Class, new Methods());
                                documents[doc.Path][line.Class].Add(line.Method, new Method());
                                documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number,  new LineInfo { Hits = line.Hits });
                            }
                        }
                        else
                        {
                            documents.Add(doc.Path, new Classes());
                            documents[doc.Path].Add(line.Class, new Methods());
                            documents[doc.Path][line.Class].Add(line.Method, new Method());
                            documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number,  new LineInfo { Hits = line.Hits });
                        }
                    }

                    // Construct Branch Results
                    foreach (var branch in doc.Branches)
                    {
                        if (documents.TryGetValue(doc.Path, out Classes classes))
                        {
                            if (classes.TryGetValue(branch.Class, out Methods methods))
                            {
                                if (methods.TryGetValue(branch.Method, out Method method))
                                {
                                    if (method.Branches.TryGetValue(branch.Number, out List<BranchInfo> branchInfo))
                                    {
                                        documents[doc.Path][branch.Class][branch.Method].Branches[branch.Number].Add(new BranchInfo
                                            { Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                                        );
                                    }
                                    else
                                    {
                                        documents[doc.Path][branch.Class][branch.Method].Branches.Add(branch.Number, new List<BranchInfo>());
                                        documents[doc.Path][branch.Class][branch.Method].Branches[branch.Number].Add(new BranchInfo
                                            { Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                                        );
                                    }
                                }
                                else
                                {
                                    documents[doc.Path][branch.Class].Add(branch.Method, new Method());
                                    documents[doc.Path][branch.Class][branch.Method].Branches.Add(branch.Number, new List<BranchInfo>());
                                    documents[doc.Path][branch.Class][branch.Method].Branches[branch.Number].Add(new BranchInfo
                                        { Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                                    );
                                }
                            }
                            else
                            {
                                documents[doc.Path].Add(branch.Class, new Methods());
                                documents[doc.Path][branch.Class].Add(branch.Method, new Method());
                                documents[doc.Path][branch.Class][branch.Method].Branches.Add(branch.Number, new List<BranchInfo>());
                                documents[doc.Path][branch.Class][branch.Method].Branches[branch.Number].Add(new BranchInfo
                                    { Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                                );
                            }
                        }
                        else
                        {
                            documents.Add(doc.Path, new Classes());
                            documents[doc.Path].Add(branch.Class, new Methods());
                            documents[doc.Path][branch.Class].Add(branch.Method, new Method());
                            documents[doc.Path][branch.Class][branch.Method].Branches.Add(branch.Number, new List<BranchInfo>());
                            documents[doc.Path][branch.Class][branch.Method].Branches[branch.Number].Add(new BranchInfo
                                { Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
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
                if (!File.Exists(result.HitsFilePath)) { continue; }
                var lines = InstrumentationHelper.ReadHitsFile(result.HitsFilePath);
                foreach (var row in lines)
                {
                    var info = row.Split(',');
                    // Ignore malformed lines
                    if (info.Length != 4)
                        continue;

                    bool isBranch = info[0] == "B";

                    var document = result.Documents.FirstOrDefault(d => d.Path == info[1]);
                    if (document == null)
                        continue;

                    int start = int.Parse(info[2]);

                    if (isBranch)
                    {
                        uint ordinal = uint.Parse(info[3]);
                        var branch = document.Branches.First(b => b.Number == start && b.Ordinal == ordinal);
                        if (branch.Hits != int.MaxValue)
                            branch.Hits += branch.Hits + 1;
                    }
                    else
                    {
                        int end = int.Parse(info[3]);
                        for (int j = start; j <= end; j++)
                        {
                            var line = document.Lines.First(l => l.Number == j);
                            if (line.Hits != int.MaxValue)
                                line.Hits = line.Hits + 1;
                        }
                    }
                }

                InstrumentationHelper.DeleteHitsFile(result.HitsFilePath);
            }
        }
    }
}