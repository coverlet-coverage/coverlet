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
                JSON.Serialize(result.Modules, writer, Options.PrettyPrint);
                return writer.ToString();
            }
        }
    }
}