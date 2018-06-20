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

        public string Extension => "opencover.xml";

        public string Report(CoverageResult result)
        {
            CoverageSummary summary = new CoverageSummary();
            XDocument xml = new XDocument();
            XElement coverage = new XElement("CoverageSession");
            XElement coverageSummary = new XElement("Summary");
            XElement modules = new XElement("Modules");

            int numClasses = 0, numMethods = 0;
            int visitedClasses = 0, visitedMethods = 0;

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
                            // Skip all methods with no lines
                            if (meth.Value.Lines.Count == 0)
                                continue;

                            var methLineCoverage = summary.CalculateLineCoverage(meth.Value.Lines);
                            var methBranchCoverage = summary.CalculateBranchCoverage(meth.Value.Branches);
                            
                            XElement method = new XElement("Method");

                            method.Add(new XAttribute("cyclomaticComplexity", "0"));
                            method.Add(new XAttribute("nPathComplexity", "0"));
                            method.Add(new XAttribute("sequenceCoverage", methLineCoverage.Percent.ToString()));
                            method.Add(new XAttribute("branchCoverage", methBranchCoverage.Percent.ToString()));
                            method.Add(new XAttribute("isConstructor", meth.Key.Contains("ctor").ToString()));
                            method.Add(new XAttribute("isGetter", meth.Key.Contains("get_").ToString()));
                            method.Add(new XAttribute("isSetter", meth.Key.Contains("set_").ToString()));
                            method.Add(new XAttribute("isStatic", (!meth.Key.Contains("get_") || !meth.Key.Contains("set_")).ToString()));

                            XElement methodName = new XElement("Name", meth.Key);

                            XElement fileRef = new XElement("FileRef");
                            fileRef.Add(new XAttribute("uid", i.ToString()));

                            XElement methodPoint = new XElement("MethodPoint");
                            methodPoint.Add(new XAttribute("vc", methLineCoverage.Covered.ToString()));
                            methodPoint.Add(new XAttribute("upsid", "0"));
                            methodPoint.Add(new XAttribute(XName.Get("type", "xsi"), "SequencePoint"));
                            methodPoint.Add(new XAttribute("ordinal", j.ToString()));
                            methodPoint.Add(new XAttribute("offset", j.ToString()));
                            methodPoint.Add(new XAttribute("sc", "0"));
                            methodPoint.Add(new XAttribute("sl", meth.Value.Lines.First().Key.ToString()));
                            methodPoint.Add(new XAttribute("ec", "1"));
                            methodPoint.Add(new XAttribute("el", meth.Value.Lines.Last().Key.ToString()));
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

                            foreach (var lines in meth.Value.Lines)
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

                                if (lines.Value.Hits > 0)
                                {
                                    classVisited = true;
                                    methodVisited = true;
                                }

                                k++;
                            }

                            foreach (var branches in meth.Value.Branches)
                            {
                                XElement branchPoint = new XElement("BranchPoint");
                                branchPoint.Add(new XAttribute("vc", branches.Value.Hits.ToString()));
                                branchPoint.Add(new XAttribute("upsid", branches.Key.Number.ToString()));
                                branchPoint.Add(new XAttribute("ordinal", branches.Key.Ordinal.ToString()));
                                branchPoint.Add(new XAttribute("path", branches.Key.Path.ToString()));
                                branchPoint.Add(new XAttribute("offset", branches.Key.Offset.ToString()));
                                branchPoint.Add(new XAttribute("offsetend", branches.Key.EndOffset.ToString()));
                                branchPoint.Add(new XAttribute("sl", branches.Key.Number.ToString()));
                                branchPoint.Add(new XAttribute("fileid", i.ToString()));
                                branchPoints.Add(branchPoint);
                                kBr++;
                            }

                            numMethods++;
                            if (methodVisited)
                                visitedMethods++;

                            methodSummary.Add(new XAttribute("numSequencePoints", methLineCoverage.Total.ToString()));
                            methodSummary.Add(new XAttribute("visitedSequencePoints", methLineCoverage.Covered.ToString()));
                            methodSummary.Add(new XAttribute("numBranchPoints", methBranchCoverage.Total.ToString()));
                            methodSummary.Add(new XAttribute("visitedBranchPoints", methBranchCoverage.Covered.ToString()));
                            methodSummary.Add(new XAttribute("sequenceCoverage", methLineCoverage.Percent.ToString()));
                            methodSummary.Add(new XAttribute("branchCoverage", methBranchCoverage.Percent.ToString()));
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

                        var classLineCoverage = summary.CalculateLineCoverage(cls.Value);
                        var classBranchCoverage = summary.CalculateBranchCoverage(cls.Value);
                        var classMethodCoverage = summary.CalculateMethodCoverage(cls.Value);

                        classSummary.Add(new XAttribute("numSequencePoints", classLineCoverage.Total.ToString()));
                        classSummary.Add(new XAttribute("visitedSequencePoints", classLineCoverage.Covered.ToString()));
                        classSummary.Add(new XAttribute("numBranchPoints", classBranchCoverage.Total.ToString()));
                        classSummary.Add(new XAttribute("visitedBranchPoints", classBranchCoverage.Covered.ToString()));
                        classSummary.Add(new XAttribute("sequenceCoverage", classLineCoverage.Percent.ToString()));
                        classSummary.Add(new XAttribute("branchCoverage", classBranchCoverage.Percent.ToString()));
                        classSummary.Add(new XAttribute("maxCyclomaticComplexity", "0"));
                        classSummary.Add(new XAttribute("minCyclomaticComplexity", "0"));
                        classSummary.Add(new XAttribute("visitedClasses", classVisited ? "1" : "0"));
                        classSummary.Add(new XAttribute("numClasses", "1"));
                        classSummary.Add(new XAttribute("visitedMethods", classMethodCoverage.Covered.ToString()));
                        classSummary.Add(new XAttribute("numMethods", classMethodCoverage.Total.ToString()));

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

            var moduleLineCoverage = summary.CalculateLineCoverage(result.Modules);
            var moduleBranchCoverage = summary.CalculateLineCoverage(result.Modules);

            coverageSummary.Add(new XAttribute("numSequencePoints", moduleLineCoverage.Total.ToString()));
            coverageSummary.Add(new XAttribute("visitedSequencePoints", moduleLineCoverage.Covered.ToString()));
            coverageSummary.Add(new XAttribute("numBranchPoints", moduleBranchCoverage.Total.ToString()));
            coverageSummary.Add(new XAttribute("visitedBranchPoints", moduleBranchCoverage.Covered.ToString()));
            coverageSummary.Add(new XAttribute("sequenceCoverage", moduleLineCoverage.Percent.ToString()));
            coverageSummary.Add(new XAttribute("branchCoverage", moduleBranchCoverage.Percent.ToString()));
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

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public CoverageResult Read(string data)
        {
            throw new NotSupportedException("Not supported by this reporter.");
        }
    }
}