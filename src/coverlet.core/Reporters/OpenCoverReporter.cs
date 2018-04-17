using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Coverlet.Core.Reporters
{
    public class OpenCoverReporter : IReporter
    {
        public string Format => "opencover";

        public string Extension => "xml";

        public string Report(CoverageResult result)
        {
            CoverageSummary summary = new CoverageSummary();
            XDocument xml = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            XElement coverage = new XElement("CoverageSession");
            XElement coverageSummary = new XElement("Summary");
            XElement modules = new XElement("Modules");

            int numSequencePoints = 0, numBranchPoints = 0, numClasses = 0, numMethods = 0;
            int visitedSequencePoints = 0, visitedBranchPoints = 0, visitedClasses = 0, visitedMethods = 0;

            int i = 1;

            foreach (var mod in result.Modules)
            {
                XElement module = new XElement("Module");
                module.Add(new XAttribute("hash", Guid.NewGuid().ToString().ToUpper()));

                XElement path = new XElement("ModulePath", mod.Key);
                XElement time = new XElement("ModuleTime", DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ss"));
                XElement name = new XElement("ModuleName", Path.GetFileNameWithoutExtension(mod.Key));

                module.Add(path);
                module.Add(time);
                module.Add(name);

                XElement files = new XElement("Files");
                XElement classes = new XElement("Classes");

                foreach (var doc in mod.Value)
                {
                    XElement file = new XElement("File");
                    file.Add(new XAttribute("uid", i.ToString()));
                    file.Add(new XAttribute("fullPath", doc.Key));
                    files.Add(file);

                    foreach (var cls in doc.Value)
                    {
                        XElement @class = new XElement("Class");
                        XElement classSummary = new XElement("Summary");

                        XElement className = new XElement("FullName", cls.Key);

                        XElement methods = new XElement("Methods");
                        int j = 0;
                        var classVisited = false;

                        foreach (var meth in cls.Value)
                        {
                            XElement method = new XElement("Method");

                            method.Add(new XAttribute("cyclomaticComplexity", "0"));
                            method.Add(new XAttribute("nPathComplexity", "0"));
                            method.Add(new XAttribute("sequenceCoverage", summary.CalculateLineCoverage(meth.Value).ToString()));
                            method.Add(new XAttribute("branchCoverage", summary.CalculateBranchCoverage(meth.Value).ToString()));
                            method.Add(new XAttribute("isConstructor", meth.Key.Contains("ctor").ToString()));
                            method.Add(new XAttribute("isGetter", meth.Key.Contains("get_").ToString()));
                            method.Add(new XAttribute("isSetter", meth.Key.Contains("set_").ToString()));
                            method.Add(new XAttribute("isStatic", (!meth.Key.Contains("get_") || !meth.Key.Contains("set_")).ToString()));

                            XElement methodName = new XElement("Name", meth.Key);

                            XElement fileRef = new XElement("FileRef");
                            fileRef.Add(new XAttribute("uid", i.ToString()));

                            XElement methodPoint = new XElement("MethodPoint");
                            methodPoint.Add(new XAttribute("vc", meth.Value.Select(l => l.Value.Hits).Sum().ToString()));
                            methodPoint.Add(new XAttribute("upsid", "0"));
                            methodPoint.Add(new XAttribute(XName.Get("type", "xsi"), "SequencePoint"));
                            methodPoint.Add(new XAttribute("ordinal", j.ToString()));
                            methodPoint.Add(new XAttribute("offset", j.ToString()));
                            methodPoint.Add(new XAttribute("sc", "0"));
                            methodPoint.Add(new XAttribute("sl", meth.Value.First().Key.ToString()));
                            methodPoint.Add(new XAttribute("ec", "1"));
                            methodPoint.Add(new XAttribute("el", meth.Value.Last().Key.ToString()));
                            methodPoint.Add(new XAttribute("bec", "0"));
                            methodPoint.Add(new XAttribute("bev", "0"));
                            methodPoint.Add(new XAttribute("fileid", i.ToString()));

                            // They're really just lines
                            XElement sequencePoints = new XElement("SequencePoints");
                            XElement branchPoints = new XElement("BranchPoints");
                            XElement methodSummary = new XElement("Summary");
                            int k = 0;
                            int kBr = 0;
                            var methodVisited = false;

                            foreach (var lines in meth.Value)
                            {
                                XElement sequencePoint = new XElement("SequencePoint");
                                sequencePoint.Add(new XAttribute("vc", lines.Value.Hits.ToString()));
                                sequencePoint.Add(new XAttribute("upsid", lines.Key.ToString()));
                                sequencePoint.Add(new XAttribute("ordinal", k.ToString()));
                                sequencePoint.Add(new XAttribute("sl", lines.Key.ToString()));
                                sequencePoint.Add(new XAttribute("sc", "1"));
                                sequencePoint.Add(new XAttribute("el", lines.Key.ToString()));
                                sequencePoint.Add(new XAttribute("ec", "2"));
                                sequencePoint.Add(new XAttribute("bec", "0"));
                                sequencePoint.Add(new XAttribute("bev", "0"));
                                sequencePoint.Add(new XAttribute("fileid", i.ToString()));
                                sequencePoints.Add(sequencePoint);

                                if (lines.Value.IsBranchPoint)
                                {
                                    XElement branchPoint = new XElement("BranchPoint");
                                    branchPoint.Add(new XAttribute("vc", lines.Value.Hits.ToString()));
                                    branchPoint.Add(new XAttribute("upsid", lines.Key.ToString()));
                                    branchPoint.Add(new XAttribute("ordinal", kBr.ToString()));
                                    branchPoint.Add(new XAttribute("sl", lines.Key.ToString()));
                                    branchPoint.Add(new XAttribute("fileid", i.ToString()));
                                    branchPoints.Add(branchPoint);
                                    kBr++;
                                    numBranchPoints++;
                                }

                                numSequencePoints++;
                                if (lines.Value.Hits > 0)
                                {
                                    visitedSequencePoints++;
                                    classVisited = true;
                                    methodVisited = true;
                                    if (lines.Value.IsBranchPoint)
                                        visitedBranchPoints++;
                                }

                                k++;
                            }

                            numMethods++;
                            if (methodVisited)
                                visitedMethods++;

                            methodSummary.Add(new XAttribute("numSequencePoints", meth.Value.Count().ToString()));
                            methodSummary.Add(new XAttribute("visitedSequencePoints", meth.Value.Where(l => l.Value.Hits > 0).Count().ToString()));
                            methodSummary.Add(new XAttribute("numBranchPoints", meth.Value.Where(l => l.Value.IsBranchPoint).Count().ToString()));
                            methodSummary.Add(new XAttribute("visitedBranchPoints", meth.Value.Where(l => l.Value.IsBranchPoint && l.Value.Hits > 0).Count().ToString()));
                            methodSummary.Add(new XAttribute("sequenceCoverage", summary.CalculateLineCoverage(meth.Value).ToString()));
                            methodSummary.Add(new XAttribute("branchCoverage", summary.CalculateBranchCoverage(meth.Value).ToString()));
                            methodSummary.Add(new XAttribute("maxCyclomaticComplexity", "0"));
                            methodSummary.Add(new XAttribute("minCyclomaticComplexity", "0"));
                            methodSummary.Add(new XAttribute("visitedClasses", "0"));
                            methodSummary.Add(new XAttribute("numClasses", "0"));
                            methodSummary.Add(new XAttribute("visitedMethods", methodVisited ? "1" : "0"));
                            methodSummary.Add(new XAttribute("numMethods", "1"));

                            method.Add(methodSummary);
                            method.Add(new XElement("MetadataToken"));
                            method.Add(methodName);
                            method.Add(fileRef);
                            method.Add(sequencePoints);
                            method.Add(branchPoints);
                            method.Add(methodPoint);
                            methods.Add(method);
                            j++;
                        }

                        numClasses++;
                        if (classVisited)
                            visitedClasses++;

                        classSummary.Add(new XAttribute("numSequencePoints", cls.Value.Select(c => c.Value.Count).Sum().ToString()));
                        classSummary.Add(new XAttribute("visitedSequencePoints", cls.Value.Select(c => c.Value.Where(l => l.Value.Hits > 0).Count()).Sum().ToString()));
                        classSummary.Add(new XAttribute("numBranchPoints", cls.Value.Select(c => c.Value.Count(l => l.Value.IsBranchPoint)).Sum().ToString()));
                        classSummary.Add(new XAttribute("visitedBranchPoints", cls.Value.Select(c => c.Value.Where(l => l.Value.Hits > 0 && l.Value.IsBranchPoint).Count()).Sum().ToString()));
                        classSummary.Add(new XAttribute("sequenceCoverage", summary.CalculateLineCoverage(cls.Value).ToString()));
                        classSummary.Add(new XAttribute("branchCoverage", summary.CalculateBranchCoverage(cls.Value).ToString()));
                        classSummary.Add(new XAttribute("maxCyclomaticComplexity", "0"));
                        classSummary.Add(new XAttribute("minCyclomaticComplexity", "0"));
                        classSummary.Add(new XAttribute("visitedClasses", classVisited ? "1" : "0"));
                        classSummary.Add(new XAttribute("numClasses", "1"));
                        classSummary.Add(new XAttribute("visitedMethods", "0"));
                        classSummary.Add(new XAttribute("numMethods", cls.Value.Count.ToString()));

                        @class.Add(classSummary);
                        @class.Add(className);
                        @class.Add(methods);
                        classes.Add(@class);
                    }
                    i++;
                }

                module.Add(files);
                module.Add(classes);
                modules.Add(module);
            }

            coverageSummary.Add(new XAttribute("numSequencePoints", numSequencePoints.ToString()));
            coverageSummary.Add(new XAttribute("visitedSequencePoints", visitedSequencePoints.ToString()));
            coverageSummary.Add(new XAttribute("numBranchPoints", numBranchPoints.ToString()));
            coverageSummary.Add(new XAttribute("visitedBranchPoints", visitedBranchPoints.ToString()));
            coverageSummary.Add(new XAttribute("sequenceCoverage", summary.CalculateLineCoverage(result.Modules).ToString()));
            coverageSummary.Add(new XAttribute("branchCoverage", summary.CalculateLineCoverage(result.Modules).ToString()));
            coverageSummary.Add(new XAttribute("maxCyclomaticComplexity", "0"));
            coverageSummary.Add(new XAttribute("minCyclomaticComplexity", "0"));
            coverageSummary.Add(new XAttribute("visitedClasses", visitedClasses.ToString()));
            coverageSummary.Add(new XAttribute("numClasses", numClasses.ToString()));
            coverageSummary.Add(new XAttribute("visitedMethods", visitedMethods.ToString()));
            coverageSummary.Add(new XAttribute("numMethods", numMethods.ToString()));

            coverage.Add(coverageSummary);
            coverage.Add(modules);
            xml.Add(coverage);

            var stream = new MemoryStream();
            xml.Save(stream);

            return Encoding.UTF8.GetString(stream.GetBuffer());
        }
    }
}