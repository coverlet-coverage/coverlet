using System.Collections.Generic;

namespace Coverlet.Core.Instrumentation
{
    internal class Line
    {
        public int Number;
        public int Count;
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
        public string ReportPath;
        public string OriginalModulePath;
        public string OriginalModuleTempPath;
        public List<Document> Documents { get; private set; }
    }
}