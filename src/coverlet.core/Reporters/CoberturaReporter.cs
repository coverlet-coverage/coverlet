using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Coverlet.Core.Reporters
{
    public class CoberturaReporter : IReporter
    {
        public string Report(CoverageResult result)
        {
            XmlDocument xml = new XmlDocument();
            XmlElement coverage = xml.CreateElement("coverage");
            coverage.SetAttribute("line-rate", "0");
            coverage.SetAttribute("branch-rate", "0");
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
                package.SetAttribute("line-rate", "0");
                package.SetAttribute("branch-rate", "0");
                package.SetAttribute("complexity", "0");

                XmlElement classes = xml.CreateElement("classes");
                foreach (var document in module.Value)
                {
                    foreach (var cls in document.Value)
                    {
                        XmlElement @class = xml.CreateElement("class");
                        @class.SetAttribute("name", cls.Key);
                        @class.SetAttribute("filename", Path.GetFileName(document.Key));
                        @class.SetAttribute("line-rate", "0");
                        @class.SetAttribute("branch-rate", "0");
                        @class.SetAttribute("complexity", "0");

                        XmlElement methods = xml.CreateElement("methods");
                        foreach (var meth in cls.Value)
                        {
                            XmlElement method = xml.CreateElement("method");
                            method.SetAttribute("name", meth.Key.Split(':')[2].Split('(')[0]);
                            method.SetAttribute("signature", meth.Key);
                            method.SetAttribute("line-rate", "0");
                            method.SetAttribute("branch-rate", "0");

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