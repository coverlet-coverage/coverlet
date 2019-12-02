using System;
using System.Linq;

namespace Coverlet.Core.Abstracts
{
    internal class ReporterFactory : IReporterFactory
    {
        private string _format;
        private IReporter[] _reporters;

        public ReporterFactory()
        {
            _reporters = new IReporter[] {
                new JsonReporter(), new LcovReporter(),
                new OpenCoverReporter(), new CoberturaReporter(),
                new TeamCityReporter()
            };
        }

        public ReporterFactory(string format) : this()
        {
            _format = format;
        }

        public bool IsValidFormat()
        {
            return CreateReporter() != null;
        }

        public IReporter CreateReporter()
            => _reporters.FirstOrDefault(r => string.Equals(r.Format, _format, StringComparison.OrdinalIgnoreCase));

        public IReporter Create(string format)
        {
            ReporterFactory reporterFactory = new ReporterFactory(format);
            if (!reporterFactory.IsValidFormat())
            {
                throw new ArgumentException($"Format not supported '{format}'");
            }
            return reporterFactory.CreateReporter();
        }
    }
}