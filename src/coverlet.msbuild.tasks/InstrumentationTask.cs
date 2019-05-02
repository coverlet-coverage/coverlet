using System;
using System.IO;
using Coverlet.Core;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;

namespace Coverlet.MSbuild.Tasks
{
    public class InstrumentationTask : Task
    {
        private readonly MSBuildLogger _logger;

        [Output]
        public ITaskItem InstrumenterState { get; set; }

        [Required]
        public string Path { get; set; }

        public string Include { get; set; }

        public string IncludeDirectory { get; set; }

        public string Exclude { get; set; }

        public string ExcludeByFile { get; set; }

        public string ExcludeByAttribute { get; set; }

        public bool IncludeTestAssembly { get; set; }

        public bool SingleHit { get; set; }

        public string MergeWith { get; set; }

        public bool UseSourceLink { get; set; }

        public InstrumentationTask() => _logger = new MSBuildLogger(Log);

        public override bool Execute()
        {
            try
            {
                var includeFilters = Include?.Split(',');
                var includeDirectories = IncludeDirectory?.Split(',');
                var excludeFilters = Exclude?.Split(',');
                var excludedSourceFiles = ExcludeByFile?.Split(',');
                var excludeAttributes = ExcludeByAttribute?.Split(',');

                IInstrumenter instrumenter = new Instrumenter(Path, includeFilters, includeDirectories, excludeFilters, excludedSourceFiles, excludeAttributes, IncludeTestAssembly, SingleHit, MergeWith, UseSourceLink, _logger);
                InstrumenterState instrumenterState = instrumenter.PrepareModules();

                // We pass instrumenter state throught msbuild output parameter
                IInstrumentStateSerializer serializer = new JsonInstrumentStateSerializer();
                InstrumenterState = new TaskItem(System.IO.Path.GetTempFileName());
                using (var instrumentedStateFile = new FileStream(InstrumenterState.ItemSpec, FileMode.Open, FileAccess.Write))
                {
                    using (var serializedState = serializer.Serialize(instrumenterState))
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
