using System;
using System.IO;

using Coverlet.Core;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;

namespace Coverlet.MSbuild.Tasks
{
    public class CoverageResultTask : Task
    {
        private Coverage _coverage;
        private string _filename;

        [Required]
        public Coverage Coverage
        {
            get { return _coverage; }
            set { _coverage = value; }
        }

        [Required]
        public string CoverageOutput
        {
            get { return _filename; }
            set { _filename = value; }
        }

        public override bool Execute()
        {
            try
            {
                CoverageResult result = _coverage.GetCoverageResult();
                File.WriteAllText(_filename, JsonConvert.SerializeObject(result.Data, Formatting.Indented));
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
