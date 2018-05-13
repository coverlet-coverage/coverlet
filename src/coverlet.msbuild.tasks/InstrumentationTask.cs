using System;
using Coverlet.Core;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class InstrumentationTask : Task
    {
        private static Coverage _coverage;
        private string _path;
        private string _exclude;

        internal static Coverage Coverage
        {
            get { return _coverage; }
        }

        [Required]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        public string Exclude
        {
            get { return _path; }
            set { _path = value; }
        }

        public override bool Execute()
        {
            try
            {
                var excludeRules = _exclude?.Split(',');
                _coverage = new Coverage(_path, Guid.NewGuid().ToString(), excludeRules);
                _coverage.PrepareModules();
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }

            return true;
        }
    }
}
