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

    internal class Branch : Line
    {
        public int Offset;
        public int EndOffset;
        public int Path;
        public uint Ordinal;
    }

    internal class Document
    {
        public Document()
        {
            Lines = new Dictionary<int, Line>();
            Branches = new Dictionary<(int Line, int Ordinal), Branch>();
        }

        public string Path;
        public int Index;
        public Dictionary<int, Line> Lines { get; private set; }
        public Dictionary<(int Line, int Ordinal), Branch> Branches { get; private set; }
    }

    internal class InstrumenterResult
    {
        public InstrumenterResult()
        {
            Documents = new Dictionary<string, Document>();
            HitCandidates = new List<(bool isBranch, int docIndex, int start, int end)>();
        }

        public string Module;
        public string HitsFilePath;
        public string ModulePath;
        public Dictionary<string, Document> Documents { get; private set; }
        public List<(bool isBranch, int docIndex, int start, int end)> HitCandidates { get; private set; }
    }
}