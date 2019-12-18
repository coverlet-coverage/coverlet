using System;
using System.Linq;

namespace Coverlet.Core.Reporters
{
    internal class ReporterFactory
    {
        private string _format;
        private IReporter[] _reporters;

        public ReporterFactory(string format)
        {
            _format = format;
            _reporters = new IReporter[] {
                new JsonReporter(), new LcovReporter(),
                new OpenCoverReporter(), new CoberturaReporter(),
                new TeamCityReporter()
            };
        }

        public bool IsValidFormat()
        {
            return CreateReporter() != null;
        }

        public IReporter CreateReporter()
            => _reporters.FirstOrDefault(r => string.Equals(r.Format, _format, StringComparison.OrdinalIgnoreCase));
    }
}