using System.Collections.Generic;
using System.IO;

using Jil;

namespace Coverlet.Core
{
    public class LineInfo
    {
        public int Hits { get; set; }
        public bool IsBranchPoint { get; set; }
    }

    public class Lines : SortedDictionary<int, LineInfo> { }
    public class Methods : Dictionary<string, Lines> { }
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