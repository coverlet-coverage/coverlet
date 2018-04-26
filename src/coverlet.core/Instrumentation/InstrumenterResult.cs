using System.Collections.Generic;

namespace Coverlet.Core.Instrumentation
{
    internal class InstrumenterResult
    {
        public string Module { get; set; }

        public string HitsFilePath { get; set; }

        public string ModulePath { get; set; }

        public List<Document> Documents { get; } = new List<Document>();
    }
}