using System.Collections.Generic;

namespace Coverlet.Core.Instrumentation
{
    internal class Line
    {
        public int Number;
        public string Class;
        public string Method;
        public int Hits;
    }

    internal class Document
    {
        public Document() => Lines = new List<Line>();

        public string Path;
        public List<Line> Lines { get; private set; }
    }

    internal class InstrumenterResult
    {
        public InstrumenterResult() => Documents = new List<Document>();
        public string Module;
        public string HitsFilePath;
        public string ModulePath;
        public List<Document> Documents { get; private set; }
    }
}