using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Coverlet.Core.Reporters
{
    public class OpenCoverReporter : IReporter
    {
        public string Format(CoverageResult result)
        {
            XmlDocument xml = new XmlDocument();
            XmlElement coverage = xml.CreateElement("CoverageSession");
            coverage.AppendChild(xml.CreateElement("Summary"));

            XmlElement modules = xml.CreateElement("Modules");

            foreach (var mod in result.Modules)
            {
                XmlElement module = xml.CreateElement("Module");
                module.SetAttribute("hash", Guid.NewGuid().ToString().ToUpper());

                XmlElement path = xml.CreateElement("ModulePath");
                path.AppendChild(xml.CreateTextNode(mod.Key));

                XmlElement time = xml.CreateElement("ModuleTime");
                time.AppendChild(xml.CreateTextNode(DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ss")));

                XmlElement name = xml.CreateElement("ModuleName");
                name.AppendChild(xml.CreateTextNode(Path.GetFileNameWithoutExtension(mod.Key)));

                module.AppendChild(path);
                module.AppendChild(time);
                module.AppendChild(name);

                XmlElement files = xml.CreateElement("Files");
                XmlElement classes = xml.CreateElement("Classes");
                int i = 1;

                foreach (var doc in mod.Value)
                {
                    XmlElement file = xml.CreateElement("File");
                    file.SetAttribute("uid", i.ToString());
                    file.SetAttribute("fullPath", doc.Key);
                    files.AppendChild(file);

                    foreach (var cls in doc.Value)
                    {
                        XmlElement @class = xml.CreateElement("Class");

                        XmlElement className = xml.CreateElement("FullName");
                        className.AppendChild(xml.CreateTextNode(cls.Key));

                        @class.AppendChild(xml.CreateElement("Summary"));
                        @class.AppendChild(className);

                        XmlElement methods = xml.CreateElement("Methods");
                        int j = 0;
                        foreach (var meth in cls.Value)
                        {
                            XmlElement method = xml.CreateElement("Method");

                            XmlElement methodName = xml.CreateElement("Name");
                            methodName.AppendChild(xml.CreateTextNode(meth.Key));

                            XmlElement fileRef = xml.CreateElement("FileRef");
                            fileRef.SetAttribute("uid", i.ToString());

                            method.AppendChild(xml.CreateElement("Summary"));
                            method.AppendChild(xml.CreateElement("MetadataToken"));
                            method.AppendChild(methodName);
                            method.AppendChild(fileRef);

                            XmlElement methodPoint = xml.CreateElement("MethodPoint");
                            methodPoint.SetAttribute("type", "xsi", "SequencePoint");
                            methodPoint.SetAttribute("ordinal", j.ToString());
                            methodPoint.SetAttribute("sc", "0");
                            methodPoint.SetAttribute("sl", meth.Value.First().Key.ToString());
                            methodPoint.SetAttribute("ec", "1");
                            methodPoint.SetAttribute("el", meth.Value.Last().Key.ToString());
                            methodPoint.SetAttribute("bec", "0");
                            methodPoint.SetAttribute("bev", "0");
                            methodPoint.SetAttribute("fileid", i.ToString());

                            method.AppendChild(xml.CreateElement("SequencePoints"));
                            method.AppendChild(xml.CreateElement("BranchPoints"));
                            method.AppendChild(methodPoint);
                            methods.AppendChild(method);
                            j++;
                        }

                        @class.AppendChild(methods);
                        classes.AppendChild(@class);
                    }
                    i++;
                }

                module.AppendChild(files);
                module.AppendChild(classes);
                modules.AppendChild(module);
            }

            coverage.AppendChild(modules);
            xml.AppendChild(coverage);

            StringWriter writer = new StringWriter();
            xml.Save(writer);

            return writer.ToString();
        }
    }
}