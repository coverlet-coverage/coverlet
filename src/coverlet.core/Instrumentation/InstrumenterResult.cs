// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Coverlet.Core.Instrumentation
{
  [DebuggerDisplay("Number = {Number} Hits = {Hits} Class = {Class} Method = {Method}")]
  [DataContract]
  internal class Line
  {
    [DataMember]
    public int Number;
    [DataMember]
    public string Class;
    [DataMember]
    public string Method;
    [DataMember]
    public int Hits;
  }

  [DebuggerDisplay("Line = {Number} Offset = {Offset} EndOffset = {EndOffset} Path = {Path} Ordinal = {Ordinal} Hits = {Hits}")]
  [DataContract]
  internal class Branch : Line
  {
    [DataMember]
    public int Offset;
    [DataMember]
    public int EndOffset;
    [DataMember]
    public int Path;
    [DataMember]
    public uint Ordinal;
  }

  [DebuggerDisplay("line = {Line} Ordinal = {Ordinal}")]
  // Implements IEquatable because is used by dictionary key https://docs.microsoft.com/en-us/dotnet/api/system.iequatable-1?view=netcore-2.2#remarks
  [DataContract]
  internal class BranchKey : IEquatable<BranchKey>
  {
    public BranchKey(int line, int ordinal) => (Line, Ordinal) = (line, ordinal);

    [DataMember]
    public int Line { get; set; }
    [DataMember]
    public int Ordinal { get; set; }

    public override bool Equals(object obj) => Equals(obj);

    public bool Equals(BranchKey other) => other is BranchKey branchKey && branchKey.Line == Line && branchKey.Ordinal == Ordinal;

    public override int GetHashCode()
    {
      return (Line, Ordinal).GetHashCode();
    }
  }

  [DataContract]
  internal class Document
  {
    public Document()
    {
      Lines = [];
      Branches = [];
    }

    [DataMember]
    public string Path;
    [DataMember]
    public int Index;
    [DataMember]
    public Dictionary<int, Line> Lines { get; private set; }
    [DataMember]
    public Dictionary<BranchKey, Branch> Branches { get; private set; }
  }

  [DebuggerDisplay("isBranch = {isBranch} docIndex = {docIndex} start = {start} end = {end}")]
  [DataContract]
  [SuppressMessage("Style", "IDE1006", Justification = "suppress casing error for API compatibility")]
  internal class HitCandidate
  {
    public HitCandidate(bool isBranch, int docIndex, int start, int end) => (this.isBranch, this.docIndex, this.start, this.end) = (isBranch, docIndex, start, end);

    [DataMember]
    public bool isBranch { get; set; }
    [DataMember]
    public int docIndex { get; set; }
    [DataMember]
    public int start { get; set; }
    [DataMember]
    public int end { get; set; }
    public HashSet<int> AccountedByNestedInstrumentation { get; set; }
  }

  [DataContract]
  internal class InstrumenterResult
  {
    public InstrumenterResult()
    {
      Documents = [];
      HitCandidates = [];
    }

    [DataMember]
    public string Module;
    [DataMember]
    public string[] BranchesInCompiledGeneratedClass;
    [DataMember]
    public string HitsFilePath;
    [DataMember]
    public string ModulePath;
    [DataMember]
    public string SourceLink;
    [DataMember]
    public Dictionary<string, Document> Documents { get; private set; }
    [DataMember]
    public List<HitCandidate> HitCandidates { get; private set; }
  }
}
