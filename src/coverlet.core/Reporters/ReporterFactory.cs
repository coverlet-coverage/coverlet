using System.Linq;

namespace Coverlet.Core.Reporters
{
    public class ReporterFactory
    {
        private string _format;
        private IReporter[] _reporters;

        public ReporterFactory(string format)
        {
            _format = format;
            _reporters = new IReporter[] {
                new JsonReporter(), new LcovReporter(),
                new OpenCoverReporter(), new CoberturaReporter()
            };
        }

        public IReporter CreateReporter()
            => _reporters.FirstOrDefault(r => r.Format == _format);
    }
}