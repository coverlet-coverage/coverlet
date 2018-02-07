using System.IO;
using Jil;

namespace Coverlet.Core.Reporters
{
    public class JsonReporter : IReporter
    {
        public string Format(CoverageResult result)
        {
            using (var writer = new StringWriter())
            {
                JSON.Serialize(result.Data, writer, Options.PrettyPrint);
                return writer.ToString();
            }
        }
    }
}