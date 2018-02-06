using System.IO;
using Jil;

namespace Coverlet.Core.Formatters
{
    public class JsonFormatter : IFormatter
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