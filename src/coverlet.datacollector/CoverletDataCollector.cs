using Coverlet.Core.Instrumentation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;
using System;
using System.Reflection;

namespace Coverlet.DataCollector
{
    public class CoverletDataCollector : InProcDataCollection
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
                catch
                {
                    // Ignore exceptions and continue with the unload
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
            catch
            {
                // Avoid crashing if reflection fails
                return null;
            }
        }
    }
}
