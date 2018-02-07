using System;
using System.IO;

using Coverlet.Core;
using Coverlet.Core.Reporters;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class CoverageResultTask : Task
    {
        private string _filename;
        private string _format;

        [Required]
        public string Output
        {
            get { return _filename; }
            set { _filename = value; }
        }

        [Required]
        public string OutputFormat
        {
            get { return _format; }
            set { _format = value; }
        }

        public override bool Execute()
        {
            try
            {
                var coverage = InstrumentationTask.Coverage;
                CoverageResult result = coverage.GetCoverageResult();

                IReporter reporter = default(IReporter);
                if (_format == "lcov")
                    reporter = new LcovReporter();
                else
                    reporter = new JsonReporter();

                File.WriteAllText(_filename, result.Format(reporter));
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
