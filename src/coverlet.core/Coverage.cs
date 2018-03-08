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
        private List<InstrumenterResult> _results;

        public Coverage(string module, string identifier)
        {
            _module = module;
            _identifier = identifier;
            _results = new List<InstrumenterResult>();
        }

        public void PrepareModules()
        {
            string[] modules = InstrumentationHelper.GetDependencies(_module);
            foreach (var module in modules)
            {
                var instrumenter = new Instrumenter(module, _identifier);
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
                foreach (var document in result.Documents)
                {
                    Lines lines = new Lines();
                    foreach (var line in document.Lines)
                        lines.Add(line.Number, line.Count);

                    documents.Add(document.Path, lines);
                }

                modules.Add(result.Module, documents);
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
                var lines = File.ReadAllLines(result.HitsFilePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    var info = lines[i].Split(',');
                    // Ignore malformed lines
                    if (info.Length != 3)
                        continue;

                    var document = result.Documents.FirstOrDefault(d => d.Path == info[0]);
                    if (document == null)
                        continue;

                    int start = int.Parse(info[1]);
                    int end = int.Parse(info[2]);

                    for (int j = start; j <= end; j++)
                    {
                        var line = document.Lines.First(l => l.Number == j);
                        line.Count = line.Count + 1;
                    }
                }
            }
        }
    }
}