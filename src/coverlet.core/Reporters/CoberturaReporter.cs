using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Coverlet.Core.Reporters
{
    public class CoberturaReporter : IReporter
    {
        public string Format => "cobertura";

        public string Extension => "xml";

        public string Report(CoverageResult result)
        {
            CoverageSummary summary = new CoverageSummary();

            int totalLines = 0, coveredLines = 0, totalBranches = 0, coveredBranches = 0;

            XDocument xml = new XDocument();
            XElement coverage = new XElement("coverage");
            coverage.Add(new XAttribute("line-rate", summary.CalculateLineCoverage(result.Modules).ToString()));
            coverage.Add(new XAttribute("branch-rate", summary.CalculateBranchCoverage(result.Modules).ToString()));
            coverage.Add(new XAttribute("version", "1.9"));
            coverage.Add(new XAttribute("timestamp", ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()));

            XElement sources = new XElement("sources");
            var basePath = GetBasePath(result.Modules);
            sources.Add(new XElement("source", basePath));

            XElement packages = new XElement("packages");
            foreach (var module in result.Modules)
            {
                XElement package = new XElement("package");
                package.Add(new XAttribute("name", Path.GetFileNameWithoutExtension(module.Key)));
                package.Add(new XAttribute("line-rate", summary.CalculateLineCoverage(module.Value).ToString()));
                package.Add(new XAttribute("branch-rate", summary.CalculateBranchCoverage(module.Value).ToString()));
                package.Add(new XAttribute("complexity", "0"));

                XElement classes = new XElement("classes");
                foreach (var document in module.Value)
                {
                    foreach (var cls in document.Value)
                    {
                        XElement @class = new XElement("class");
                        @class.Add(new XAttribute("name", cls.Key));
                        @class.Add(new XAttribute("filename", GetRelativePathFromBase(basePath, document.Key)));
                        @class.Add(new XAttribute("line-rate", summary.CalculateLineCoverage(cls.Value).ToString()));
                        @class.Add(new XAttribute("branch-rate", summary.CalculateBranchCoverage(cls.Value).ToString()));
                        @class.Add(new XAttribute("complexity", "0"));

                        XElement classLines = new XElement("lines");
                        XElement methods = new XElement("methods");

                        foreach (var meth in cls.Value)
                        {
                            XElement method = new XElement("method");
                            method.Add(new XAttribute("name", meth.Key.Split(':')[2].Split('(')[0]));
                            method.Add(new XAttribute("signature", "(" + meth.Key.Split(':')[2].Split('(')[1]));
                            method.Add(new XAttribute("line-rate", summary.CalculateLineCoverage(meth.Value).ToString()));
                            method.Add(new XAttribute("branch-rate", summary.CalculateBranchCoverage(meth.Value).ToString()));

                            XElement lines = new XElement("lines");
                            foreach (var ln in meth.Value)
                            {
                                XElement line = new XElement("line");
                                line.Add(new XAttribute("number", ln.Key.ToString()));
                                line.Add(new XAttribute("hits", ln.Value.Hits.ToString()));
                                line.Add(new XAttribute("branch", ln.Value.IsBranchPoint.ToString()));

                                totalLines++;
                                if (ln.Value.Hits > 0) coveredLines++;


                                if (ln.Value.IsBranchPoint)
                                {
                                    line.Add(new XAttribute("condition-coverage", "100% (1/1)"));
                                    XElement conditions = new XElement("conditions");
                                    XElement condition = new XElement("condition");
                                    condition.Add(new XAttribute("number", "0"));
                                    condition.Add(new XAttribute("type", "jump"));
                                    condition.Add(new XAttribute("coverage", "100%"));

                                    totalBranches++;
                                    if (ln.Value.Hits > 0) coveredBranches++;

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
            List<string> sources = new List<string>();
            string source = string.Empty;

            foreach (var module in modules)
            {
                sources.AddRange(
                    module.Value.Select(d => Path.GetDirectoryName(d.Key)));
            }

            sources = sources.Distinct().ToList();
            var segments = sources[0].Split(Path.DirectorySeparatorChar);

            foreach (var segment in segments)
            {
                var startsWith = sources.All(s => s.StartsWith(source + segment));
                if (!startsWith)
                    break;

                source += segment + Path.DirectorySeparatorChar;
            }

            return source;
        }

        private string GetRelativePathFromBase(string source, string path)
            => path.Replace(source, string.Empty);
    }
}