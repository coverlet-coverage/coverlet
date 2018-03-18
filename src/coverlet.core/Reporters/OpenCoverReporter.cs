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
            XmlElement coverageSummary = xml.CreateElement("Summary");

            XmlElement modules = xml.CreateElement("Modules");

            int numSequencePoints = 0, numClasses = 0, numMethods = 0;
            int visitedSequencePoints = 0, visitedClasses = 0, visitedMethods = 0;

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
                        XmlElement classSummary = xml.CreateElement("Summary");

                        XmlElement className = xml.CreateElement("FullName");
                        className.AppendChild(xml.CreateTextNode(cls.Key));

                        XmlElement methods = xml.CreateElement("Methods");
                        int j = 0;
                        var classVisited = false;

                        foreach (var meth in cls.Value)
                        {
                            XmlElement method = xml.CreateElement("Method");

                            method.SetAttribute("cyclomaticComplexity", "0");
                            method.SetAttribute("nPathComplexity", "0");
                            method.SetAttribute("sequenceCoverage", "0");
                            method.SetAttribute("branchCoverage", "0");
                            method.SetAttribute("isConstructor", meth.Key.Contains("ctor").ToString());
                            method.SetAttribute("isGetter", meth.Key.Contains("get_").ToString());
                            method.SetAttribute("isSetter", meth.Key.Contains("set_").ToString());
                            method.SetAttribute("isStatic", (!meth.Key.Contains("get_") || !meth.Key.Contains("set_")).ToString());

                            XmlElement methodName = xml.CreateElement("Name");
                            methodName.AppendChild(xml.CreateTextNode(meth.Key));

                            XmlElement fileRef = xml.CreateElement("FileRef");
                            fileRef.SetAttribute("uid", i.ToString());

                            XmlElement methodPoint = xml.CreateElement("MethodPoint");
                            methodPoint.SetAttribute("vc", "0");
                            methodPoint.SetAttribute("upsid", "0");
                            methodPoint.SetAttribute("type", "xsi", "SequencePoint");
                            methodPoint.SetAttribute("ordinal", j.ToString());
                            methodPoint.SetAttribute("offset", j.ToString());
                            methodPoint.SetAttribute("sc", "0");
                            methodPoint.SetAttribute("sl", meth.Value.First().Key.ToString());
                            methodPoint.SetAttribute("ec", "1");
                            methodPoint.SetAttribute("el", meth.Value.Last().Key.ToString());
                            methodPoint.SetAttribute("bec", "0");
                            methodPoint.SetAttribute("bev", "0");
                            methodPoint.SetAttribute("fileid", i.ToString());

                            // They're really just lines
                            XmlElement sequencePoints = xml.CreateElement("SequencePoints");
                            XmlElement methodSummary = xml.CreateElement("Summary");
                            int k = 0;
                            var methodVisited = false;

                            foreach (var lines in meth.Value)
                            {
                                XmlElement sequencePoint = xml.CreateElement("SequencePoint");
                                sequencePoint.SetAttribute("vc", "0");
                                sequencePoint.SetAttribute("upsid", lines.Key.ToString());
                                sequencePoint.SetAttribute("ordinal", k.ToString());
                                sequencePoint.SetAttribute("sl", lines.Key.ToString());
                                sequencePoint.SetAttribute("sc", "1");
                                sequencePoint.SetAttribute("el", lines.Key.ToString());
                                sequencePoint.SetAttribute("ec", "2");
                                sequencePoint.SetAttribute("bec", "0");
                                sequencePoint.SetAttribute("bev", "0");
                                sequencePoint.SetAttribute("fileid", i.ToString());
                                sequencePoints.AppendChild(sequencePoint);

                                numSequencePoints++;
                                if (lines.Value > 0)
                                {
                                    visitedSequencePoints++;
                                    classVisited = true;
                                    methodVisited = true;
                                }

                                k++;
                            }

                            numMethods++;
                            if (methodVisited)
                                visitedMethods++;

                            methodSummary.SetAttribute("numSequencePoints", meth.Value.Count().ToString());
                            methodSummary.SetAttribute("visitedSequencePoints", meth.Value.Where(l => l.Value > 0).Count().ToString());
                            methodSummary.SetAttribute("numBranchPoints", "0");
                            methodSummary.SetAttribute("visitedBranchPoints", "0");
                            methodSummary.SetAttribute("sequenceCoverage", "0");
                            methodSummary.SetAttribute("branchCoverage", "0");
                            methodSummary.SetAttribute("maxCyclomaticComplexity", "0");
                            methodSummary.SetAttribute("minCyclomaticComplexity", "0");
                            methodSummary.SetAttribute("visitedClasses", "0");
                            methodSummary.SetAttribute("numClasses", "0");
                            methodSummary.SetAttribute("visitedMethods", methodVisited ? "1" : "0");
                            methodSummary.SetAttribute("numMethods", "1");

                            method.AppendChild(methodSummary);
                            method.AppendChild(xml.CreateElement("MetadataToken"));
                            method.AppendChild(methodName);
                            method.AppendChild(fileRef);
                            method.AppendChild(sequencePoints);
                            method.AppendChild(xml.CreateElement("BranchPoints"));
                            method.AppendChild(methodPoint);
                            methods.AppendChild(method);
                            j++;
                        }

                        numClasses++;
                        if (classVisited)
                            visitedClasses++;

                        classSummary.SetAttribute("numSequencePoints", cls.Value.Select(c => c.Value.Count).Sum().ToString());
                        classSummary.SetAttribute("visitedSequencePoints", cls.Value.Select(c => c.Value.Where(l => l.Value > 0).Count()).Sum().ToString());
                        classSummary.SetAttribute("numBranchPoints", "0");
                        classSummary.SetAttribute("visitedBranchPoints", "0");
                        classSummary.SetAttribute("sequenceCoverage", "0");
                        classSummary.SetAttribute("branchCoverage", "0");
                        classSummary.SetAttribute("maxCyclomaticComplexity", "0");
                        classSummary.SetAttribute("minCyclomaticComplexity", "0");
                        classSummary.SetAttribute("visitedClasses", classVisited ? "1" : "0");
                        classSummary.SetAttribute("numClasses", "1");
                        classSummary.SetAttribute("visitedMethods", "0");
                        classSummary.SetAttribute("numMethods", cls.Value.Count.ToString());

                        @class.AppendChild(classSummary);
                        @class.AppendChild(className);
                        @class.AppendChild(methods);
                        classes.AppendChild(@class);
                    }
                    i++;
                }

                module.AppendChild(files);
                module.AppendChild(classes);
                modules.AppendChild(module);
            }

            coverageSummary.SetAttribute("numSequencePoints", numSequencePoints.ToString());
            coverageSummary.SetAttribute("visitedSequencePoints", visitedSequencePoints.ToString());
            coverageSummary.SetAttribute("numBranchPoints", "0");
            coverageSummary.SetAttribute("visitedBranchPoints", "0");
            coverageSummary.SetAttribute("sequenceCoverage", "0");
            coverageSummary.SetAttribute("branchCoverage", "0");
            coverageSummary.SetAttribute("maxCyclomaticComplexity", "0");
            coverageSummary.SetAttribute("minCyclomaticComplexity", "0");
            coverageSummary.SetAttribute("visitedClasses", visitedClasses.ToString());
            coverageSummary.SetAttribute("numClasses", numClasses.ToString());
            coverageSummary.SetAttribute("visitedMethods", visitedMethods.ToString());
            coverageSummary.SetAttribute("numMethods", numMethods.ToString());

            coverage.AppendChild(coverageSummary);
            coverage.AppendChild(modules);
            xml.AppendChild(coverage);

            StringWriter writer = new StringWriter();
            xml.Save(writer);

            return writer.ToString();
        }
    }
}