using System;
using Coverlet.Core;
using Coverlet.Core.Helpers;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public class InstrumentationTask : Task
    {        
        internal static Coverage Coverage { get; private set; }

        [Required]
        public string Path { get; set; }
        
        [Required]
        public string[] ExclusionRules { get; set; }
        
        public string ExclusionParentDir { get; set; }
        
        public override bool Execute()
        {
            try
            {
                var excludedFiles =  InstrumentationHelper.GetExcludedFiles(
                    ExclusionRules, ExclusionParentDir);
                Coverage = new Coverage(Path, Guid.NewGuid().ToString(), excludedFiles);
                Coverage.PrepareModules();
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
