using Coverlet.Core.Abstractions;
using System;
using System.Linq;

namespace Coverlet.Core.Reporters
{
    internal class ReporterFactory
    {
        private string _format;
        private IReporter[] _reporters;
        private IFilePathHelper _filePathHelper;
       
        public ReporterFactory(string format, IFilePathHelper filePathHelper)
        {
            _format = format;
            _filePathHelper = filePathHelper;
            _reporters = new IReporter[] {
                new JsonReporter(), new LcovReporter(_filePathHelper),
                new OpenCoverReporter(), new CoberturaReporter(_filePathHelper),
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