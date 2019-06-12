using System;
using System.IO;
using Coverlet.Core;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class InstrumentationTask : Task
    {
        private string _path;
        private string _include;
        private string _includeDirectory;
        private string _exclude;
        private string _excludeByFile;
        private string _excludeByAttribute;
        private bool _includeTestAssembly;
        private bool _singleHit;
        private string _mergeWith;
        private bool _useSourceLink;
        private ITaskItem _instrumenterState;
        private readonly MSBuildLogger _logger;

        [Required]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
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

        public bool IncludeTestAssembly
        {
            get { return _includeTestAssembly; }
            set { _includeTestAssembly = value; }
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

        [Output]
        public ITaskItem InstrumenterState
        {
            get { return _instrumenterState; }
            set { _instrumenterState = value; }
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

                // We add default exclusion filter if no specified
                if (excludeFilters is null || excludeFilters.Length == 0)
                {
                    excludeFilters = new string[] { "[xunit*]*" };
                }

                Coverage coverage = new Coverage(_path, includeFilters, includeDirectories, excludeFilters, excludedSourceFiles, excludeAttributes, _includeTestAssembly, _singleHit, _mergeWith, _useSourceLink, _logger);
                CoveragePrepareResult prepareResult = coverage.PrepareModules();
                InstrumenterState = new TaskItem(System.IO.Path.GetTempFileName());
                using (var instrumentedStateFile = new FileStream(InstrumenterState.ItemSpec, FileMode.Open, FileAccess.Write))
                {
                    using (Stream serializedState = CoveragePrepareResult.Serialize(prepareResult))
                    {
                        serializedState.CopyTo(instrumentedStateFile);
                    }
                }
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
