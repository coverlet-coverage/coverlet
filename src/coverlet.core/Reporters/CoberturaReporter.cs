using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Coverlet.Core.Reporters
{
    internal class CoberturaReporter : IReporter
    {
        public ReporterOutputType OutputType => ReporterOutputType.File;

        public string Format => "cobertura";

        public string Extension => "cobertura.xml";

        public string Report(CoverageResult result)
        {
            CoverageSummary summary = new CoverageSummary();

            var lineCoverage = summary.CalculateLineCoverage(result.Modules);
            var branchCoverage = summary.CalculateBranchCoverage(result.Modules);

            XDocument xml = new XDocument();
            XElement coverage = new XElement("coverage");
            coverage.Add(new XAttribute("line-rate", (summary.CalculateLineCoverage(result.Modules).Percent / 100).ToString(CultureInfo.InvariantCulture)));
            coverage.Add(new XAttribute("branch-rate", (summary.CalculateBranchCoverage(result.Modules).Percent / 100).ToString(CultureInfo.InvariantCulture)));
            coverage.Add(new XAttribute("version", "1.9"));
            coverage.Add(new XAttribute("timestamp", (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds));

            XElement sources = new XElement("sources");
            var absolutePaths = GetBasePaths(result.Modules, result.UseSourceLink).ToList();
            absolutePaths.ForEach(x => sources.Add(new XElement("source", x)));

            XElement packages = new XElement("packages");
            foreach (var module in result.Modules)
            {
                XElement package = new XElement("package");
                package.Add(new XAttribute("name", Path.GetFileNameWithoutExtension(module.Key)));
                package.Add(new XAttribute("line-rate", (summary.CalculateLineCoverage(module.Value).Percent / 100).ToString(CultureInfo.InvariantCulture)));
                package.Add(new XAttribute("branch-rate", (summary.CalculateBranchCoverage(module.Value).Percent / 100).ToString(CultureInfo.InvariantCulture)));
                package.Add(new XAttribute("complexity", summary.CalculateCyclomaticComplexity(module.Value)));

                XElement classes = new XElement("classes");
                foreach (var document in module.Value)
                {
                    foreach (var cls in document.Value)
                    {
                        XElement @class = new XElement("class");
                        @class.Add(new XAttribute("name", cls.Key));
                        @class.Add(new XAttribute("filename", GetRelativePathFromBase(absolutePaths, document.Key, result.UseSourceLink)));
                        @class.Add(new XAttribute("line-rate", (summary.CalculateLineCoverage(cls.Value).Percent / 100).ToString(CultureInfo.InvariantCulture)));
                        @class.Add(new XAttribute("branch-rate", (summary.CalculateBranchCoverage(cls.Value).Percent / 100).ToString(CultureInfo.InvariantCulture)));
                        @class.Add(new XAttribute("complexity", summary.CalculateCyclomaticComplexity(cls.Value)));

                        XElement classLines = new XElement("lines");
                        XElement methods = new XElement("methods");

                        foreach (var meth in cls.Value)
                        {
                            // Skip all methods with no lines
                            if (meth.Value.Lines.Count == 0)
                                continue;

                            XElement method = new XElement("method");
                            method.Add(new XAttribute("name", meth.Key.Split(':').Last().Split('(').First()));
                            method.Add(new XAttribute("signature", "(" + meth.Key.Split(':').Last().Split('(').Last()));
                            method.Add(new XAttribute("line-rate", (summary.CalculateLineCoverage(meth.Value.Lines).Percent / 100).ToString(CultureInfo.InvariantCulture)));
                            method.Add(new XAttribute("branch-rate", (summary.CalculateBranchCoverage(meth.Value.Branches).Percent / 100).ToString(CultureInfo.InvariantCulture)));
                            method.Add(new XAttribute("complexity", summary.CalculateCyclomaticComplexity(meth.Value.Branches)));

                            XElement lines = new XElement("lines");
                            foreach (var ln in meth.Value.Lines)
                            {
                                bool isBranchPoint = meth.Value.Branches.Any(b => b.Line == ln.Key);
                                XElement line = new XElement("line");
                                line.Add(new XAttribute("number", ln.Key.ToString()));
                                line.Add(new XAttribute("hits", ln.Value.ToString()));
                                line.Add(new XAttribute("branch", isBranchPoint.ToString()));

                                if (isBranchPoint)
                                {
                                    var branches = meth.Value.Branches.Where(b => b.Line == ln.Key).ToList();
                                    var branchInfoCoverage = summary.CalculateBranchCoverage(branches);
                                    line.Add(new XAttribute("condition-coverage", $"{branchInfoCoverage.Percent.ToString(CultureInfo.InvariantCulture)}% ({branchInfoCoverage.Covered.ToString(CultureInfo.InvariantCulture)}/{branchInfoCoverage.Total.ToString(CultureInfo.InvariantCulture)})"));
                                    XElement conditions = new XElement("conditions");
                                    var byOffset = branches.GroupBy(b => b.Offset).ToDictionary(b => b.Key, b => b.ToList());
                                    foreach (var entry in byOffset)
                                    {
                                        XElement condition = new XElement("condition");
                                        condition.Add(new XAttribute("number", entry.Key));
                                        condition.Add(new XAttribute("type", entry.Value.Count() > 2 ? "switch" : "jump")); // Just guessing here
                                        condition.Add(new XAttribute("coverage", $"{summary.CalculateBranchCoverage(entry.Value).Percent.ToString(CultureInfo.InvariantCulture)}%"));
                                        conditions.Add(condition);
                                    }

                                    line.Add(conditions);
                                }


                                lines.Add(line);
                                classLines.Add(line);
                            }

                            method.Add(lines);
                            methods.Add(method);
                        }

                        @class.Add(methods);
                        @class.Add(classLines);
                        classes.Add(@class);
                    }
                }

                package.Add(classes);
                packages.Add(package);
            }

            coverage.Add(new XAttribute("lines-covered", lineCoverage.Covered.ToString(CultureInfo.InvariantCulture)));
            coverage.Add(new XAttribute("lines-valid", lineCoverage.Total.ToString(CultureInfo.InvariantCulture)));
            coverage.Add(new XAttribute("branches-covered", branchCoverage.Covered.ToString(CultureInfo.InvariantCulture)));
            coverage.Add(new XAttribute("branches-valid", branchCoverage.Total.ToString(CultureInfo.InvariantCulture)));

            coverage.Add(sources);
            coverage.Add(packages);
            xml.Add(coverage);

            var stream = new MemoryStream();
            xml.Save(stream);

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static IEnumerable<string> GetBasePaths(Modules modules, bool useSourceLink)
        {
            /*
                 Workflow

                 Path1 c:\dir1\dir2\file1.cs
                 Path2 c:\dir1\file2.cs
                 Path3 e:\dir1\file2.cs

                 1) Search for root dir 
                    c:\ ->	c:\dir1\dir2\file1.cs
                            c:\dir1\file2.cs
                    e:\ ->	e:\dir1\file2.cs

                 2) Split path on directory separator i.e. for record c:\ ordered ascending by fragment elements
                     Path1 = [c:|dir1|file2.cs]
                     Path2 = [c:|dir1|dir2|file1.cs]

                 3)  Find longest shared path comparing indexes		 
                     Path1[0]    = Path2[0], ..., PathY[0]     -> add to final fragment list
                     Path1[n]    = Path2[n], ..., PathY[n]     -> add to final fragment list
                     Path1[n+1] != Path2[n+1], ..., PathY[n+1] -> break, Path1[n] was last shared fragment 		 

                 4) Concat created fragment list
            */
            if (useSourceLink)
            {
                return new[] { string.Empty };
            }

            return modules.Values.SelectMany(k => k.Keys).GroupBy(Directory.GetDirectoryRoot).Select(group =>
            {
                var splittedPaths = group.Select(absolutePath => absolutePath.Split(Path.DirectorySeparatorChar))
                                         .OrderBy(absolutePath => absolutePath.Length).ToList();
                if (splittedPaths.Count == 1)
                {
                    return group.Key;
                }

                var basePathFragments = new List<string>();
                bool stopSearch = false;
                splittedPaths[0].Select((value, index) => (value, index)).ToList().ForEach(fragmentIndexPair =>
                {
                    if (stopSearch)
                    {
                        return;
                    }

                    if (splittedPaths.All(sp => fragmentIndexPair.value.Equals(sp[fragmentIndexPair.index])))
                    {
                        basePathFragments.Add(fragmentIndexPair.value);
                    }
                    else
                    {
                        stopSearch = true;
                    }
                });
                return string.Concat(string.Join(Path.DirectorySeparatorChar.ToString(), basePathFragments), Path.DirectorySeparatorChar);
            });
        }

        private static string GetRelativePathFromBase(IEnumerable<string> basePaths, string path, bool useSourceLink)
        {
            if (useSourceLink)
            {
                return path;
            }

            foreach (var basePath in basePaths)
            {
                if (path.StartsWith(basePath))
                {
                    return path.Substring(basePath.Length);
                }
            }

            Debug.Assert(false, "Unexpected, we should find at least one path starts with one pre-build roots list");

            return path;
        }
    }
}