using System.Collections.Generic;

namespace Coverlet.Core.Instrumentation
{
    internal class Document
    {
        public string Path { get; set; }

        public List<Line> Lines { get; } = new List<Line>();
    }
}