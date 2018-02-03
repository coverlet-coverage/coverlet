using System;
using Coverlet.Core;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class InstrumentationTask : Task
    {
        private Coverage _coverage;
        private string _path;

        [Required]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        [Output]
        public Coverage Coverage
        {
            get { return _coverage; }
            set { _coverage = value; }
        }

        public override bool Execute()
        {
            try
            {
                _coverage = new Coverage(_path, Guid.NewGuid().ToString());
                _coverage.PrepareModules();
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
