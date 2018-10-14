using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Coverlet.Core
{
    public class BranchInfo
    {
        public int Line { get; set; }
        public int Offset { get; set; }
        public int EndOffset { get; set; }
        public int Path { get; set; }
        public uint Ordinal { get; set; }
        public int Hits { get; set; }
    }

    public class Lines : SortedDictionary<int, int> { }

    public class Branches : List<BranchInfo> { }

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

        internal void Merge(Modules modules)
        {
            foreach (var module in modules)
            {
                if (!this.Modules.Keys.Contains(module.Key))
                {
                    this.Modules.Add(module.Key, module.Value);
                }
                else
                {
                    foreach (var document in module.Value)
                    {
                        if (!this.Modules[module.Key].ContainsKey(document.Key))
                        {
                            this.Modules[module.Key].Add(document.Key, document.Value);
                        }
                        else
                        {
                            foreach (var @class in document.Value)
                            {
                                if (!this.Modules[module.Key][document.Key].ContainsKey(@class.Key))
                                {
                                    this.Modules[module.Key][document.Key].Add(@class.Key, @class.Value);
                                }
                                else
                                {
                                    foreach (var method in @class.Value)
                                    {
                                        if (!this.Modules[module.Key][document.Key][@class.Key].ContainsKey(method.Key))
                                        {
                                            this.Modules[module.Key][document.Key][@class.Key].Add(method.Key, method.Value);
                                        }
                                        else
                                        {
                                            foreach (var line in method.Value.Lines)
                                            {
                                                if (!this.Modules[module.Key][document.Key][@class.Key][method.Key].Lines.ContainsKey(line.Key))
                                                {
                                                    this.Modules[module.Key][document.Key][@class.Key][method.Key].Lines.Add(line.Key, line.Value);
                                                }
                                                else
                                                {
                                                    this.Modules[module.Key][document.Key][@class.Key][method.Key].Lines[line.Key] += line.Value;
                                                }
                                            }

                                            foreach (var branch in method.Value.Branches)
                                            {
                                                var branches = this.Modules[module.Key][document.Key][@class.Key][method.Key].Branches;
                                                var branchInfo = branches.FirstOrDefault(b => b.EndOffset == branch.EndOffset && b.Line == branch.Line && b.Offset == branch.Offset && b.Ordinal == branch.Ordinal && b.Path == branch.Path);
                                                if (branchInfo == null)
                                                    branches.Add(branch);
                                                else
                                                    branchInfo.Hits += branch.Hits;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}