using System.Collections.Generic;
using System.IO;

using Jil;

namespace Coverlet.Core
{
    public class LineInfo
    {
        public int Hits { get; set; }
    }

    public class BranchInfo : LineInfo
    {
        public int Offset { get; set; }
        public int EndOffset { get; set; }
        public int Path { get; set; }
        public uint Ordinal { get; set; }
    }

    public class Lines : SortedDictionary<int, LineInfo> { }
    public class Branches : SortedDictionary<int, List<BranchInfo>> { }
    public class Method
    {
        internal Method()
        {
            Lines = new Lines();
            Branches = new Branches();
        }
        public Lines Lines;
        public Branches Branches;
    }
    public class Methods : Dictionary<string, Method> { }
    public class Classes : Dictionary<string, Methods> { }
    public class Documents : Dictionary<string, Classes> { }
    public class Modules : Dictionary<string, Documents> { }

    public class CoverageResult
    {
        public string Identifier;
        public Modules Modules;

        internal CoverageResult() { }
    }
}