using System;
using coverlet.core.Logging;
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
        private string _excludeByFile;

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
            get { return _exclude; }
            set { _exclude = value; }
        }

        public string ExcludeByFile
        {
            get { return _excludeByFile; }
            set { _excludeByFile = value; }
        }

        public bool Verbose { get; set; }

        public override bool Execute()
        {
            try
            {
                var rules = _excludeByFile?.Split(',');
                var filters = _exclude?.Split(',');

                if (Verbose)
                    EnableLogging.Execute();

                _coverage = new Coverage(_path, Guid.NewGuid().ToString(), filters, rules);
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
