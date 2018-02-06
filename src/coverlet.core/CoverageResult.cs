using System.Collections.Generic;
using System.IO;

using Coverlet.Core.Formatters;
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

        public string Format(IFormatter formatter)
            => formatter.Format(this);
    }
}