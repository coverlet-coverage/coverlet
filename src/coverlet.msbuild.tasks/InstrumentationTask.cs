using System;
using Coverlet.Core;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class InstrumentationTask : Task
    {
        private string _path;
        private static Coverage _coverage;

        internal static Coverage Coverage
        {
            get { return _coverage; }
            private set { _coverage = value; }
        }

        [Required]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        public override bool Execute()
        {
            try
            {
                _coverage = new Coverage(_path, Guid.NewGuid().ToString());
                _coverage.PrepareModules();
            }
            catch(Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }

            return true;
        }
    }
}
