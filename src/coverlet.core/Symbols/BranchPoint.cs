using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Coverlet.Core.Symbols
{
    /// <summary>
    /// a branch point
    /// </summary>
    [DebuggerDisplay("StartLine = {StartLine}")]
    public class BranchPoint
    {
        /// <summary>
        /// Line of the branching instruction
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// A path that can be taken
        /// </summary>
        public int Path { get; set; }

        /// <summary>
        /// An order of the point within the method
        /// </summary>
        public UInt32 Ordinal { get; set; }

        /// <summary>
        /// List of OffsetPoints between Offset and EndOffset (exclusive)
        /// </summary>
        public System.Collections.Generic.List<int> OffsetPoints { get; set; }

        /// <summary>
        /// The IL offset of the point
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Last Offset == EndOffset.
        /// Can be same as Offset
        /// </summary>
        public int EndOffset { get; set; }

        /// <summary>
        /// The url to the document if an entry was not mapped to an id
        /// </summary>
        public string Document { get; set; }
    }
}