using System.Linq;

namespace Coverlet.Core.Reporters
{
    public class ReporterFactory
    {
        private readonly IReporter[] _reporters;

        public ReporterFactory()
        {
            _reporters = new IReporter[] {
                new JsonReporter(), new LcovReporter(),
                new OpenCoverReporter(), new CoberturaReporter()
            };
        }

        public IReporter CreateReporter(string format)
            => _reporters.FirstOrDefault(r => r.Format == format);
    }
}