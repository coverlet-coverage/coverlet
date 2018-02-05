using System;
using System.IO;

using Coverlet.Core;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class CoverageResultTask : Task
    {
        private string _filename;

        [Required]
        public string Output
        {
            get { return _filename; }
            set { _filename = value; }
        }

        public override bool Execute()
        {
            try
            {
                var coverage = InstrumentationTask.Coverage;
                CoverageResult result = coverage.GetCoverageResult();
                File.WriteAllText(_filename, result.ToJson());
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
