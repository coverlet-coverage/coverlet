using System.Collections.Generic;
using System.IO;

using Coverlet.Core.Reporters;
using Jil;

namespace Coverlet.Core
{
    public class Lines : SortedDictionary<int, int> { }
    public class Methods : Dictionary<string, Lines> { }
    public class Classes : Dictionary<string, Methods> { }
    public class Documents : Dictionary<string, Classes> { }
    public class Modules : Dictionary<string, Documents> { }

    public class CoverageResult
    {
        public string Identifier;
        public Modules Modules;

        internal CoverageResult() { }

        public string Format(IReporter reporter)
            => reporter.Format(this);
    }
}