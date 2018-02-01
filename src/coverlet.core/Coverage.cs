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
            string[] modules =  InstrumentationHelper.GetCoverableModules(_module);
            foreach (var module in modules)
            {
                Instrumenter instrumenter = new Instrumenter(module, _identifier);
                var result = instrumenter.Instrument();
                _results.Add(result);
            }
        }

        public CoverageResult GetCoverageResult()
        {
            CalculateCoverage();
            Data data = new Data();

            for (int i = 0; i < _results.Count; i++)
            {
                Documents documents = new Documents();
                var instrumenterResult = _results[i];

                foreach (var document in instrumenterResult.Documents)
                {
                    Lines lines = new Lines();
                    foreach (var line in document.Lines)
                        lines.Add(line.Number, line.Count);

                    documents.Add(document.Path, lines);
                }

                data.Add(instrumenterResult.Module, documents);
            }

            return new CoverageResult
            {
                Identifier = _identifier,
                Data = data
            };
        }

        private void CalculateCoverage()
        {
            foreach (var result in _results)
            {
                var lines = File.ReadAllLines(result.ReportPath);
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    var info = lines[i].Split(':');
                    // Ignore malformed lines
                    if (info.Length != 3)
                        continue;

                    var document = result.Documents.First(d => d.Path == info[0]);
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