using System.Collections.Generic;

namespace Coverlet.Core
{
    public class HitInfo
    {
        public int Hits { get; set; }
    }

    public class Lines : SortedDictionary<int, HitInfo> { }
    public class Branches : SortedDictionary<(int Number, int Offset, int EndOffset, int Path, uint Ordinal), HitInfo> { }
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