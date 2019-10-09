using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Coverlet.Core.Instrumentation
{
    [DebuggerDisplay("Number = {Number} Hits = {Hits} Class = {Class} Method = {Method}")]
    [DataContract]
    public class Line
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
    public class Branch : Line
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
    public class BranchKey : IEquatable<BranchKey>
    {
        public BranchKey(int line, int ordinal) => (Line, Ordinal) = (line, ordinal);

        [DataMember]
        public int Line { get; set; }
        [DataMember]
        public int Ordinal { get; set; }

        public override bool Equals(object obj) => Equals(obj);

        public bool Equals(BranchKey other) => other is BranchKey branchKey && branchKey.Line == this.Line && branchKey.Ordinal == this.Ordinal;

        public override int GetHashCode()
        {
            return (this.Line, this.Ordinal).GetHashCode();
        }
    }

    [DataContract]
    public class Document
    {
        public Document()
        {
            Lines = new Dictionary<int, Line>();
            Branches = new Dictionary<BranchKey, Branch>();
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
    public class HitCandidate
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
    }

    [DataContract]
    public class InstrumenterResult
    {
        public InstrumenterResult()
        {
            Documents = new Dictionary<string, Document>();
            HitCandidates = new List<HitCandidate>();
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
