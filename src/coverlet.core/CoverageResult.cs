using System.Collections.Generic;
using System.IO;

using Jil;

namespace Coverlet.Core
{
    public class Lines : SortedDictionary<int, int> { }
    public class Documents : Dictionary<string, Lines> { }
    public class Data : Dictionary<string, Documents> { }

    public class CoverageResult
    {
        public string Identifier;
        public Data Data;

        public string ToJson()
        {
            using (var writer = new StringWriter())
            {
                JSON.Serialize(this.Data, writer, Options.PrettyPrint);
                return writer.ToString();
            }
        }
    }
}