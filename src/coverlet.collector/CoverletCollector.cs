using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Coverlet.Collector
{
    [DataCollectorFriendlyName("CoverletCollector")]
    [DataCollectorTypeUri("my://coverlet/collector")]
    public class CoverletCollector : DataCollector
    {
        private DataCollectionEnvironmentContext context;
        private DataCollectionLogger logger;
        private DataCollectionSink sink;

        public override void Initialize(
                System.Xml.XmlElement configuration,
                DataCollectionEvents events,
                DataCollectionSink sink,
                DataCollectionLogger logger,
                DataCollectionEnvironmentContext context)
        {
            this.logger = logger;
            this.sink = sink;
            this.context = context;

            events.SessionStart += this.SessionStarted_Handler;
            events.SessionEnd += this.SessionEnded_Handler;

            events.TestCaseStart += this.Events_TestCaseStart;
            events.TestCaseEnd += this.Events_TestCaseEnd;
        }

        private void SessionStarted_Handler(object sender, SessionStartEventArgs args)
        {

        }

        private void SessionEnded_Handler(object sender, SessionEndEventArgs args)
        {

        }

        private void Events_TestCaseStart(object sender, TestCaseStartEventArgs e)
        {

        }

        private void Events_TestCaseEnd(object sender, TestCaseEndEventArgs e)
        {

        }
    }
}
