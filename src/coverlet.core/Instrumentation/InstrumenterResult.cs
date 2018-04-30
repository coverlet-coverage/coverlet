using System.Collections.Generic;

namespace Coverlet.Core.Instrumentation
{
    internal class Line
    {
        public int Number;
        public string Class;
        public string Method;
        public bool IsBranchTarget;
        public int Hits;
    }

    internal class Branch : Line
    {
        public int Offset;
        public int Path;
        public uint Ordinal;
    }

    internal class Document
    {
        public Document()
        {
            Lines = new List<Line>();
            Branches = new List<Branch>();
        }

        public string Path;
        public List<Line> Lines { get; private set; }
        public List<Branch> Branches { get; private set; }
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