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
        private IEnumerable<string> _excludedFiles;
        private List<InstrumenterResult> _results;

        public Coverage(string module, string identifier, IEnumerable<string> excludedFiles)
        {
            _module = module;
            _identifier = identifier;
            _excludedFiles = excludedFiles;
            _results = new List<InstrumenterResult>();
        }

        public void PrepareModules()
        {
            string[] modules = InstrumentationHelper.GetDependencies(_module);
            foreach (var module in modules)
            {
                var instrumenter = new Instrumenter(module, _identifier, _excludedFiles);
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
                    foreach (var line in doc.Lines)
                    {
                        if (documents.TryGetValue(doc.Path, out Classes classes))
                        {
                            if (classes.TryGetValue(line.Class, out Methods methods))
                            {
                                if (methods.TryGetValue(line.Method, out Lines lines))
                                {
                                    documents[doc.Path][line.Class][line.Method].Add(line.Number, new LineInfo { Hits = line.Hits, IsBranchPoint = line.IsBranchTarget });
                                }
                                else
                                {
                                    documents[doc.Path][line.Class].Add(line.Method, new Lines());
                                    documents[doc.Path][line.Class][line.Method].Add(line.Number,  new LineInfo { Hits = line.Hits, IsBranchPoint = line.IsBranchTarget });
                                }
                            }
                            else
                            {
                                documents[doc.Path].Add(line.Class, new Methods());
                                documents[doc.Path][line.Class].Add(line.Method, new Lines());
                                documents[doc.Path][line.Class][line.Method].Add(line.Number,  new LineInfo { Hits = line.Hits, IsBranchPoint = line.IsBranchTarget });
                            }
                        }
                        else
                        {
                            documents.Add(doc.Path, new Classes());
                            documents[doc.Path].Add(line.Class, new Methods());
                            documents[doc.Path][line.Class].Add(line.Method, new Lines());
                            documents[doc.Path][line.Class][line.Method].Add(line.Number,  new LineInfo { Hits = line.Hits, IsBranchPoint = line.IsBranchTarget });
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
                foreach (var line in lines)
                {
                    var info = line.Split(',');
                    // Ignore malformed lines
                    if (info.Length != 4)
                        continue;

                    var document = result.Documents.FirstOrDefault(d => d.Path == info[0]);
                    if (document == null)
                        continue;

                    int start = int.Parse(info[1]);
                    int end = int.Parse(info[2]);
                    bool target = info[3] == "B";

                    for (int j = start; j <= end; j++)
                    {
                        var subLine = document.Lines.First(l => l.Number == j);
                        subLine.Hits = subLine.Hits + 1;

                        if (j == start)
                            subLine.IsBranchTarget = target;
                    }
                }

                InstrumentationHelper.DeleteHitsFile(result.HitsFilePath);
            }
        }
    }
}