using System;
using System.Reflection;
using Coverlet.Collector.Utilities;
using Coverlet.Core.Instrumentation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;

namespace Coverlet.Collector.DataCollection
{
    public class CoverletInProcDataCollector : InProcDataCollection
    {
        public void Initialize(IDataCollectionSink dataCollectionSink)
        {
        }

        public void TestCaseEnd(TestCaseEndArgs testCaseEndArgs)
        {
        }

        public void TestCaseStart(TestCaseStartArgs testCaseStartArgs)
        {
        }

        public void TestSessionEnd(TestSessionEndArgs testSessionEndArgs)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type injectedInstrumentationClass = GetInstrumentationClass(assembly);
                if (injectedInstrumentationClass is null)
                    continue;

                var unloadModule = injectedInstrumentationClass.GetMethod(nameof(ModuleTrackerTemplate.UnloadModule), new[] { typeof(object), typeof(EventArgs) });
                if (unloadModule is null)
                    continue;

                try
                {
                    unloadModule.Invoke(null, new[] { null, EventArgs.Empty });
                }
                catch(Exception ex)
                {
                    // Ignore exceptions and continue with the unload
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning("{0}: Failed to unload module with error: {1}", CoverletConstants.InProcDataCollectorName, ex);
                    }
                }
            }
        }

        public void TestSessionStart(TestSessionStartArgs testSessionStartArgs)
        {
        }

        private static Type GetInstrumentationClass(Assembly assembly)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Namespace == "Coverlet.Core.Instrumentation.Tracker"
                        && type.Name.StartsWith(assembly.GetName().Name + "_"))
                    {
                        return type;
                    }
                }

                return null;
            }
            catch(Exception ex)
            {
                // Avoid crashing if reflection fails.
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("{0}: Failed to get Instrumentation class with error: {1}", CoverletConstants.InProcDataCollectorName, ex);
                }
                return null;
            }
        }
    }
}
