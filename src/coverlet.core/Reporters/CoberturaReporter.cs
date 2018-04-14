using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Coverlet.Core.Reporters
{
    public class CoberturaReporter : IReporter
    {
        public string Format => "cobertura";

        public string Extension => "xml";

        public string Report(CoverageResult result)
        {
            CoverageSummary summary = new CoverageSummary();

            XmlDocument xml = new XmlDocument();
            XmlElement coverage = xml.CreateElement("coverage");
            coverage.SetAttribute("line-rate", summary.CalculateLineCoverage(result.Modules).ToString());
            coverage.SetAttribute("branch-rate", summary.CalculateBranchCoverage(result.Modules).ToString());
            coverage.SetAttribute("version", "1.9");
            coverage.SetAttribute("timestamp", "0");

            XmlElement sources = xml.CreateElement("sources");
            foreach (var src in GetSources(result.Modules))
            {
                XmlElement source = xml.CreateElement("source");
                source.AppendChild(xml.CreateTextNode(src));
                sources.AppendChild(source);
            }

            XmlElement packages = xml.CreateElement("packages");
            foreach (var module in result.Modules)
            {
                XmlElement package = xml.CreateElement("package");
                package.SetAttribute("line-rate", summary.CalculateLineCoverage(module.Value).ToString());
                package.SetAttribute("branch-rate", summary.CalculateBranchCoverage(module.Value).ToString());
                package.SetAttribute("complexity", "0");

                XmlElement classes = xml.CreateElement("classes");
                foreach (var document in module.Value)
                {
                    foreach (var cls in document.Value)
                    {
                        XmlElement @class = xml.CreateElement("class");
                        @class.SetAttribute("name", cls.Key);
                        @class.SetAttribute("filename", Path.GetFileName(document.Key));
                        @class.SetAttribute("line-rate", summary.CalculateLineCoverage(cls.Value).ToString());
                        @class.SetAttribute("branch-rate", summary.CalculateBranchCoverage(cls.Value).ToString());
                        @class.SetAttribute("complexity", "0");

                        XmlElement methods = xml.CreateElement("methods");
                        foreach (var meth in cls.Value)
                        {
                            XmlElement method = xml.CreateElement("method");
                            method.SetAttribute("name", meth.Key.Split(':')[2].Split('(')[0]);
                            method.SetAttribute("signature", meth.Key);
                            method.SetAttribute("line-rate", summary.CalculateLineCoverage(meth.Value).ToString());
                            method.SetAttribute("branch-rate", summary.CalculateBranchCoverage(meth.Value).ToString());

                            XmlElement lines = xml.CreateElement("lines");
                            foreach (var ln in meth.Value)
                            {
                                XmlElement line = xml.CreateElement("line");
                                line.SetAttribute("number", ln.Key.ToString());
                                line.SetAttribute("hits", ln.Value.Hits.ToString());
                                line.SetAttribute("branch", ln.Value.IsBranchPoint.ToString());

                                lines.AppendChild(line);
                            }

                            method.AppendChild(lines);
                            methods.AppendChild(method);
                        }

                        @class.AppendChild(methods);
                        classes.AppendChild(@class);
                    }
                }

                package.AppendChild(classes);
                packages.AppendChild(package);
            }

            coverage.AppendChild(sources);
            coverage.AppendChild(packages);
            xml.AppendChild(coverage);

            StringWriter writer = new StringWriter();
            xml.Save(writer);

            return writer.ToString();
        }

        private string[] GetSources(Modules modules)
        {
            List<string> sources = new List<string>();
            foreach (var module in modules)
            {
                sources.AddRange(
                    module.Value.Select(d => Path.GetDirectoryName(d.Key)));
            }

            return sources.Distinct().ToArray();
        }
    }
}