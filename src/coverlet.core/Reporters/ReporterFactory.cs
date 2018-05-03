using System.Linq;
using System.Collections.Generic;

namespace Coverlet.Core.Reporters
{
    public class ReporterFactory
    {
        private IEnumerable<string> _formats;
        private IReporter[] _reporters;

        public ReporterFactory(string formats)
        {
            _formats = formats.Split(',');
            _reporters = new IReporter[] {
                new JsonReporter(), new LcovReporter(),
                new OpenCoverReporter(), new CoberturaReporter()
            };
        }

        public IEnumerable<IReporter> CreateReporters()
        {
            return _reporters.Where(r => _formats.Contains(r.Format));
        }
    }
}