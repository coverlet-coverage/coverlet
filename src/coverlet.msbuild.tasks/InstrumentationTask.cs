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
        private string _include;
        private string _excludeByFile;
        private string _mergeWith;
        private string _excludeAttributes;

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

        public string Include
        {
            get { return _include; }
            set { _include = value; }
        }

        public string ExcludeByFile
        {
            get { return _excludeByFile; }
            set { _excludeByFile = value; }
        }

        public string MergeWith
        {
            get { return _mergeWith; }
            set { _mergeWith = value; }
        }

        public string ExcludeByAttribute
        {
            get { return _excludeAttributes; }
            set { _excludeAttributes = value; }
        }

        public override bool Execute()
        {
            try
            {
                var excludedSourceFiles = _excludeByFile?.Split(',');
                var excludeFilters = _exclude?.Split(',');
                var includeFilters = _include?.Split(',');
                var excludeAttributes = _excludeAttributes?.Split(',');

                _coverage = new Coverage(_path, excludeFilters, includeFilters, excludedSourceFiles, _mergeWith, excludeAttributes);
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
