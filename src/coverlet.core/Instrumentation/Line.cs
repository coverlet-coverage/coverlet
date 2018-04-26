namespace Coverlet.Core.Instrumentation
{
    internal class Line
    {
        public int Number { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public bool IsBranchTarget { get; set; }
        public int Hits { get; set; }
    }
}