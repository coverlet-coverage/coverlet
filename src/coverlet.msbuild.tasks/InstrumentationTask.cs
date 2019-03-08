using System;
using System.Collections.Immutable;
using Coverlet.Core;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class InstrumentationTask : Task
    {
        private static ImmutableDictionary<string, Coverage> _coverage;
        private string _path;
        private string _include;
        private string _includeDirectory;
        private string _exclude;
        private string _excludeByFile;
        private string _excludeByAttribute;
        private bool _singleHit;
        private string _mergeWith;
        private bool _useSourceLink;
        private readonly MSBuildLogger _logger;

        internal static ImmutableDictionary<string, Coverage> Coverage
        {
            get { return _coverage; }
        }

        [Required]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        [Required]
        public string Identifier
        {
            get;
            set;
        }

        public string Include
        {
            get { return _include; }
            set { _include = value; }
        }

        public string IncludeDirectory
        {
            get { return _includeDirectory; }
            set { _includeDirectory = value; }
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

        public string ExcludeByAttribute
        {
            get { return _excludeByAttribute; }
            set { _excludeByAttribute = value; }
        }

        public bool SingleHit
        {
            get { return _singleHit; }
            set { _singleHit = value; }
        }

        public string MergeWith
        {
            get { return _mergeWith; }
            set { _mergeWith = value; }
        }

        public bool UseSourceLink
        {
            get { return _useSourceLink; }
            set { _useSourceLink = value; }
        }

        public InstrumentationTask()
        {
            _logger = new MSBuildLogger(Log);
        }

        public override bool Execute()
        {
            try
            {
                var includeFilters = _include?.Split(',');
                var includeDirectories = _includeDirectory?.Split(',');
                var excludeFilters = _exclude?.Split(',');
                var excludedSourceFiles = _excludeByFile?.Split(',');
                var excludeAttributes = _excludeByAttribute?.Split(',');

                var coverage = new Coverage(_path, includeFilters, includeDirectories, excludeFilters, excludedSourceFiles, excludeAttributes, _singleHit, _mergeWith, _useSourceLink, _logger, Identifier);
                coverage.PrepareModules();

                ImmutableInterlocked.AddOrUpdate(ref _coverage, Identifier, coverage, (_1, _2) => coverage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return false;
            }

            return true;
        }
    }
}
