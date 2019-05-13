using System.IO;
using System.Text;

using Coverlet.Core.Instrumentation;
using Newtonsoft.Json;

namespace Coverlet.Core
{
    public class CoveragePrepareResult
    {
        public string Identifier { get; set; }
        public string Module { get; set; }
        public string MergeWith { get; set; }
        public bool UseSourceLink { get; set; }
        public InstrumenterResult[] Results { get; set; }

        public static CoveragePrepareResult Deserialize(Stream serializedInstrumentState)
        {
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(serializedInstrumentState))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<CoveragePrepareResult>(jsonTextReader);
            }
        }

        public static Stream Serialize(CoveragePrepareResult instrumentState)
        {
            var serializer = new JsonSerializer();
            MemoryStream ms = new MemoryStream();
            using (var sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
            {
                serializer.Serialize(sw, instrumentState);
                sw.Flush();
                ms.Position = 0;
                return ms;
            }
        }
    }
}
