using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Coverlet.Core.Reporters
{
    public class CoberturaReporter : IReporter
    {
        public string Format => "cobertura";

        public string Extension => "xml";

        public string Report(CoverageResult result)
        {
            var summary = new CoverageSummary();

            int totalLines = 0, coveredLines = 0, totalBranches = 0, coveredBranches = 0;

            var xml = new XDocument();
            var coverage = new XElement("coverage");
            coverage.Add(new XAttribute("line-rate", summary.CalculateLineCoverage(result.Modules).ToString()));
            coverage.Add(new XAttribute("branch-rate", summary.CalculateBranchCoverage(result.Modules).ToString()));
            coverage.Add(new XAttribute("version", "1.9"));
            coverage.Add(new XAttribute("timestamp", ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()));

            var sources = new XElement("sources");
            var basePath = GetBasePath(result.Modules);
            sources.Add(new XElement("source", basePath));

            var packages = new XElement("packages");
            foreach (var module in result.Modules)
            {
                var package = new XElement("package");
                package.Add(new XAttribute("name", Path.GetFileNameWithoutExtension(module.Key)));
                package.Add(new XAttribute("line-rate", summary.CalculateLineCoverage(module.Value).ToString()));
                package.Add(new XAttribute("branch-rate", summary.CalculateBranchCoverage(module.Value).ToString()));
                package.Add(new XAttribute("complexity", "0"));

                var classes = new XElement("classes");
                foreach (var document in module.Value)
                {
                    foreach (var classesDictionary in document.Value)
                    {
                        var @class = new XElement("class");
                        @class.Add(new XAttribute("name", classesDictionary.Key));
                        @class.Add(new XAttribute("filename", GetRelativePathFromBase(basePath, document.Key)));
                        @class.Add(new XAttribute("line-rate", summary.CalculateLineCoverage(classesDictionary.Value).ToString()));
                        @class.Add(new XAttribute("branch-rate", summary.CalculateBranchCoverage(classesDictionary.Value).ToString()));
                        @class.Add(new XAttribute("complexity", "0"));

                        var classLines = new XElement("lines");
                        var methods = new XElement("methods");

                        foreach (var methodsDictionary in classesDictionary.Value)
                        {
                            var method = new XElement("method");
                            method.Add(new XAttribute("name", methodsDictionary.Key.Split(':')[2].Split('(')[0]));
                            method.Add(new XAttribute("signature", "(" + methodsDictionary.Key.Split(':')[2].Split('(')[1]));
                            method.Add(new XAttribute("line-rate", summary.CalculateLineCoverage(methodsDictionary.Value).ToString()));
                            method.Add(new XAttribute("branch-rate", summary.CalculateBranchCoverage(methodsDictionary.Value).ToString()));

                            var lines = new XElement("lines");
                            foreach (var linesDictionary in methodsDictionary.Value)
                            {
                                var line = new XElement("line");
                                line.Add(new XAttribute("number", linesDictionary.Key.ToString()));
                                line.Add(new XAttribute("hits", linesDictionary.Value.Hits.ToString()));
                                line.Add(new XAttribute("branch", linesDictionary.Value.IsBranchPoint.ToString()));

                                totalLines++;
                                if (linesDictionary.Value.Hits > 0) coveredLines++;


                                if (linesDictionary.Value.IsBranchPoint)
                                {
                                    line.Add(new XAttribute("condition-coverage", "100% (1/1)"));
                                    var conditions = new XElement("conditions");
                                    var condition = new XElement("condition");
                                    condition.Add(new XAttribute("number", "0"));
                                    condition.Add(new XAttribute("type", "jump"));
                                    condition.Add(new XAttribute("coverage", "100%"));

                                    totalBranches++;
                                    if (linesDictionary.Value.Hits > 0) coveredBranches++;

                                    conditions.Add(condition);
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

            coverage.Add(new XAttribute("lines-covered", coveredLines.ToString()));
            coverage.Add(new XAttribute("lines-valid", totalLines.ToString()));
            coverage.Add(new XAttribute("branches-covered", coveredBranches.ToString()));
            coverage.Add(new XAttribute("branches-valid", totalBranches.ToString()));

            coverage.Add(sources);
            coverage.Add(packages);
            xml.Add(coverage);

            var stream = new MemoryStream();
            xml.Save(stream);

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private string GetBasePath(Modules modules)
        {
            var sources = new List<string>();
            var path = string.Empty;

            foreach (var module in modules)
            {
                sources.AddRange(
                    module.Value.Select(d => Path.GetDirectoryName(d.Key)));
            }

            sources = sources.Distinct().ToList();
            var segments = sources[0].Split(Path.DirectorySeparatorChar);

            foreach (var segment in segments)
            {
                var startsWith = sources.All(s => s.StartsWith(path + segment));
                if (!startsWith)
                    break;

                path += segment + Path.DirectorySeparatorChar;
            }

            return path;
        }

        private string GetRelativePathFromBase(string basePath, string path)
            => basePath == string.Empty ? path : path.Replace(basePath, string.Empty);
    }
}