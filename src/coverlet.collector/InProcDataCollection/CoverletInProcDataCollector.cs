using System;
using System.Diagnostics;
using System.Reflection;

using coverlet.collector.Resources;
using Coverlet.Collector.Utilities;
using Coverlet.Core.Instrumentation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;

namespace Coverlet.Collector.DataCollection
{
    public class CoverletInProcDataCollector : InProcDataCollection
    {
        private TestPlatformEqtTrace _eqtTrace;

        private void AttachDebugger()
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("COVERLET_DATACOLLECTOR_INPROC_DEBUG"), out int result) && result == 1)
            {
                Debugger.Launch();
                Debugger.Break();
            }
        }

        public void Initialize(IDataCollectionSink dataCollectionSink)
        {

            AttachDebugger();

            _eqtTrace = new TestPlatformEqtTrace();
            _eqtTrace.Verbose("Initialize CoverletInProcDataCollector");
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
                {
                    continue;
                }

                try
                {
                    _eqtTrace.Verbose($"Calling ModuleTrackerTemplate.UnloadModule for '{injectedInstrumentationClass.Assembly.FullName}'");
                    var unloadModule = injectedInstrumentationClass.GetMethod(nameof(ModuleTrackerTemplate.UnloadModule), new[] { typeof(object), typeof(EventArgs) });
                    unloadModule.Invoke(null, new[] { null, EventArgs.Empty });
                    _eqtTrace.Verbose($"Called ModuleTrackerTemplate.UnloadModule for '{injectedInstrumentationClass.Assembly.FullName}'");
                }
                catch (Exception ex)
                {
                    _eqtTrace.Error("{0}: Failed to unload module with error: {1}", CoverletConstants.InProcDataCollectorName, ex);
                    string errorMessage = string.Format(Resources.FailedToUnloadModule, CoverletConstants.InProcDataCollectorName);
                    throw new CoverletDataCollectorException(errorMessage, ex);
                }
            }
        }

        public void TestSessionStart(TestSessionStartArgs testSessionStartArgs)
        {
        }

        private Type GetInstrumentationClass(Assembly assembly)
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
            catch (Exception ex)
            {
                _eqtTrace.Warning("{0}: Failed to get Instrumentation class with error: {1}", CoverletConstants.InProcDataCollectorName, ex);
                return null;
            }
        }
    }
}
