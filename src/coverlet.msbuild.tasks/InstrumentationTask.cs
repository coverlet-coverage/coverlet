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
        
        public string Exclude { get; set; }
                
        public override bool Execute()
        {
            try
            {
                var excludeRules = Exclude?.Split(',');
                Coverage = new Coverage(Path, Guid.NewGuid().ToString(), excludeRules);
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
