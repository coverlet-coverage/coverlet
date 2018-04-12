using System.IO;
using Jil;

namespace Coverlet.Core.Reporters
{
    public class JsonReporter : IReporter
    {
        public string Report(CoverageResult result)
        {
            using (var writer = new StringWriter())
            {
                JSON.Serialize(result.Modules, writer, Options.PrettyPrint);
                return writer.ToString();
            }
        }
    }
}