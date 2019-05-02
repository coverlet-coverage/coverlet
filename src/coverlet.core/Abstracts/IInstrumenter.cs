using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace Coverlet.Core
{
    public class Line
    {
        public int Number;
        public string Class;
        public string Method;
        public int Hits;
    }

    public class Branch : Line
    {
        public int Offset;
        public int EndOffset;
        public int Path;
        public uint Ordinal;
    }

    public class BranchKeyConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return JsonConvert.DeserializeObject<BranchKey>(value.ToString());
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(BranchKey);
        }
    }

    [TypeConverter(typeof(BranchKeyConverter))]
    public class BranchKey : IEquatable<BranchKey>
    {
        public BranchKey(int line, int ordinal) => (Line, Ordinal) = (line, ordinal);

        public int Line { get; set; }
        public int Ordinal { get; set; }

        public override bool Equals(object obj) => Equals(obj);

        public bool Equals(BranchKey other) => other is BranchKey branchKey && branchKey.Line == this.Line && branchKey.Ordinal == this.Ordinal;

        public override int GetHashCode()
        {
            return (this.Line, this.Ordinal).GetHashCode();
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Document
    {
        public Document()
        {
            Lines = new Dictionary<int, Line>();
            Branches = new Dictionary<BranchKey, Branch>();
        }

        public string Path;
        public int Index;
        public Dictionary<int, Line> Lines { get; private set; }
        public Dictionary<BranchKey, Branch> Branches { get; private set; }
    }

    public class HitCandidate
    {
        public HitCandidate(bool isBranch, int docIndex, int start, int end) => (this.IsBranch, this.DocIndex, this.Start, this.End) = (isBranch, docIndex, start, end);

        public bool IsBranch { get; set; }
        public int DocIndex { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class InstrumenterResult
    {
        public InstrumenterResult()
        {
            Documents = new Dictionary<string, Document>();
            HitCandidates = new List<HitCandidate>();
        }

        public string Module;
        public string[] AsyncMachineStateMethod;
        public string HitsFilePath;
        public string ModulePath;
        public string SourceLink;
        public Dictionary<string, Document> Documents { get; private set; }
        public List<HitCandidate> HitCandidates { get; private set; }
    }

    public class InstrumenterState
    {
        public InstrumenterResult[] InstrumenterResults { get; set; }
        public bool UseSourceLink { get; set; }
        public string Identifier { get; set; }
        public string MergeWith { get; set; }
    }

    public interface IInstrumenter
    {
        InstrumenterState PrepareModules();
    }

    public interface IInstrumentStateSerializer
    {
        Stream Serialize(InstrumenterState instrumentState);
        InstrumenterState Deserialize(Stream serializedInstrumentState);
    }
}
