using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace Coverlet.Core
{
    public class JsonInstrumentStateSerializer : IInstrumentStateSerializer
    {
        public InstrumenterState Deserialize(Stream serializedInstrumentState)
        {
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(serializedInstrumentState))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<InstrumenterState>(jsonTextReader);
            }
        }

        public Stream Serialize(InstrumenterState instrumentState)
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
