// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Coverlet.Core.Symbols
{
  /// <summary>
  /// a branch point
  /// </summary>
  [DebuggerDisplay("StartLine = {StartLine}")]
  internal class BranchPoint
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

    /// <summary>
    /// Indicates whether this branch path is reachable through normal control flow.
    /// This property helps distinguish between:
    /// - Branches that are truly unreachable (e.g., dead code after unconditional return/throw)
    /// - Branches that are reachable but not covered in the current test run
    /// 
    /// Used to avoid false partial coverage reports for patterns like:
    ///   if (condition) { return value; }
    /// where the false branch should not be marked as unreachable.
    /// 
    /// Default value: true (branch is considered reachable unless proven otherwise)
    /// </summary>
    public bool IsReachable { get; set; } = true;
  }
}
