using System;
using Coverlet.Core;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class InstrumentationTask : Task
    {
        internal static Coverage Coverage { get; private set; }

        [Required]
        public string Path { get; set; }

        public string Include { get; set; }

        public string IncludeDirectory { get; set; }

        public string Exclude { get; set; }

        public string ExcludeByFile { get; set; }

        public string ExcludeByAttribute { get; set; }

        public string MergeWith { get; set; }

        public bool UseSourceLink { get; set; }

        public string SourceLinkFilter { get; set; }

        public override bool Execute()
        {
            try
            {
                var includeFilters = Include?.Split(',');
                var includeDirectories = IncludeDirectory?.Split(',');
                var excludeFilters = Exclude?.Split(',');
                var excludedSourceFiles = ExcludeByFile?.Split(',');
                var excludeAttributes = ExcludeByAttribute?.Split(',');
                var sourceLinkFilter = SourceLinkFilter?.Split(',');

                Coverage = new Coverage(Path, includeFilters, includeDirectories, excludeFilters, excludedSourceFiles, excludeAttributes, MergeWith, UseSourceLink, sourceLinkFilter);
                Coverage.PrepareModules();
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
