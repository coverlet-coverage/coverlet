// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Reporters
{
    internal class OpenCoverReporter : IReporter
    {
        public ReporterOutputType OutputType => ReporterOutputType.File;

        public string Format => "opencover";

        public string Extension => "opencover.xml";

        public string Report(CoverageResult result, ISourceRootTranslator sourceRootTranslator)
        {
            if (result.Parameters.DeterministicReport)
            {
                throw new NotSupportedException("Deterministic report not supported by openCover reporter");
            }

            var xml = new XDocument();
            var coverage = new XElement("CoverageSession");
            var coverageSummary = new XElement("Summary");
            var modules = new XElement("Modules");

            int numClasses = 0, numMethods = 0;
            int visitedClasses = 0, visitedMethods = 0;

            int i = 1;

            foreach (System.Collections.Generic.KeyValuePair<string, Documents> mod in result.Modules)
            {
                var module = new XElement("Module");
                module.Add(new XAttribute("hash", Guid.NewGuid().ToString().ToUpper()));

                var path = new XElement("ModulePath", mod.Key);
                var time = new XElement("ModuleTime", DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ss"));
                var name = new XElement("ModuleName", Path.GetFileNameWithoutExtension(mod.Key));

                module.Add(path);
                module.Add(time);
                module.Add(name);

                var files = new XElement("Files");
                var classes = new XElement("Classes");

                foreach (System.Collections.Generic.KeyValuePair<string, Classes> doc in mod.Value)
                {
                    var file = new XElement("File");
                    file.Add(new XAttribute("uid", i.ToString()));
                    file.Add(new XAttribute("fullPath", doc.Key));
                    files.Add(file);

                    foreach (System.Collections.Generic.KeyValuePair<string, Methods> cls in doc.Value)
                    {
                        var @class = new XElement("Class");
                        var classSummary = new XElement("Summary");

                        var className = new XElement("FullName", cls.Key);

                        var methods = new XElement("Methods");
                        int j = 0;
                        bool classVisited = false;

                        foreach (System.Collections.Generic.KeyValuePair<string, Method> meth in cls.Value)
                        {
                            // Skip all methods with no lines
                            if (meth.Value.Lines.Count == 0)
                                continue;

                            CoverageDetails methLineCoverage = CoverageSummary.CalculateLineCoverage(meth.Value.Lines);
                            CoverageDetails methBranchCoverage = CoverageSummary.CalculateBranchCoverage(meth.Value.Branches);
                            int methCyclomaticComplexity = CoverageSummary.CalculateCyclomaticComplexity(meth.Value.Branches);
                            int methNpathComplexity = CoverageSummary.CalculateNpathComplexity(meth.Value.Branches);

                            var method = new XElement("Method");

                            method.Add(new XAttribute("cyclomaticComplexity", methCyclomaticComplexity.ToString()));
                            method.Add(new XAttribute("nPathComplexity", methCyclomaticComplexity.ToString()));
                            method.Add(new XAttribute("sequenceCoverage", methLineCoverage.Percent.ToString("G", CultureInfo.InvariantCulture)));
                            method.Add(new XAttribute("branchCoverage", methBranchCoverage.Percent.ToString("G", CultureInfo.InvariantCulture)));
                            method.Add(new XAttribute("isConstructor", meth.Key.Contains("ctor").ToString()));
                            method.Add(new XAttribute("isGetter", meth.Key.Contains("get_").ToString()));
                            method.Add(new XAttribute("isSetter", meth.Key.Contains("set_").ToString()));
                            method.Add(new XAttribute("isStatic", (!meth.Key.Contains("get_") || !meth.Key.Contains("set_")).ToString()));

                            var methodName = new XElement("Name", meth.Key);

                            var fileRef = new XElement("FileRef");
                            fileRef.Add(new XAttribute("uid", i.ToString()));

                            var methodPoint = new XElement("MethodPoint");
                            methodPoint.Add(new XAttribute("vc", methLineCoverage.Covered.ToString()));
                            methodPoint.Add(new XAttribute("uspid", "0"));
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
                            var sequencePoints = new XElement("SequencePoints");
                            var branchPoints = new XElement("BranchPoints");
                            var methodSummary = new XElement("Summary");
                            int k = 0;
                            int kBr = 0;
                            bool methodVisited = false;

                            foreach (System.Collections.Generic.KeyValuePair<int, int> lines in meth.Value.Lines)
                            {
                                BranchInfo[] lineBranches = meth.Value.Branches.Where(branchInfo => branchInfo.Line == lines.Key).ToArray();
                                CoverageDetails branchCoverage = CoverageSummary.CalculateBranchCoverage(lineBranches);

                                var sequencePoint = new XElement("SequencePoint");
                                sequencePoint.Add(new XAttribute("vc", lines.Value.ToString()));
                                sequencePoint.Add(new XAttribute("uspid", lines.Key.ToString()));
                                sequencePoint.Add(new XAttribute("ordinal", k.ToString()));
                                sequencePoint.Add(new XAttribute("sl", lines.Key.ToString()));
                                sequencePoint.Add(new XAttribute("sc", "1"));
                                sequencePoint.Add(new XAttribute("el", lines.Key.ToString()));
                                sequencePoint.Add(new XAttribute("ec", "2"));
                                sequencePoint.Add(new XAttribute("bec", branchCoverage.Total));
                                sequencePoint.Add(new XAttribute("bev", branchCoverage.Covered));
                                sequencePoint.Add(new XAttribute("fileid", i.ToString()));
                                sequencePoints.Add(sequencePoint);

                                if (lines.Value > 0)
                                {
                                    classVisited = true;
                                    methodVisited = true;
                                }

                                k++;
                            }

                            foreach (BranchInfo branche in meth.Value.Branches)
                            {
                                var branchPoint = new XElement("BranchPoint");
                                branchPoint.Add(new XAttribute("vc", branche.Hits.ToString()));
                                branchPoint.Add(new XAttribute("uspid", branche.Line.ToString()));
                                branchPoint.Add(new XAttribute("ordinal", branche.Ordinal.ToString()));
                                branchPoint.Add(new XAttribute("path", branche.Path.ToString()));
                                branchPoint.Add(new XAttribute("offset", branche.Offset.ToString()));
                                branchPoint.Add(new XAttribute("offsetend", branche.EndOffset.ToString()));
                                branchPoint.Add(new XAttribute("sl", branche.Line.ToString()));
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
                            methodSummary.Add(new XAttribute("sequenceCoverage", methLineCoverage.Percent.ToString("G", CultureInfo.InvariantCulture)));
                            methodSummary.Add(new XAttribute("branchCoverage", methBranchCoverage.Percent.ToString("G", CultureInfo.InvariantCulture)));
                            methodSummary.Add(new XAttribute("maxCyclomaticComplexity", methCyclomaticComplexity.ToString()));
                            methodSummary.Add(new XAttribute("minCyclomaticComplexity", methCyclomaticComplexity.ToString()));
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

                        CoverageDetails classLineCoverage = CoverageSummary.CalculateLineCoverage(cls.Value);
                        CoverageDetails classBranchCoverage = CoverageSummary.CalculateBranchCoverage(cls.Value);
                        CoverageDetails classMethodCoverage = CoverageSummary.CalculateMethodCoverage(cls.Value);
                        int classMaxCyclomaticComplexity = CoverageSummary.CalculateMaxCyclomaticComplexity(cls.Value);
                        int classMinCyclomaticComplexity = CoverageSummary.CalculateMinCyclomaticComplexity(cls.Value);

                        classSummary.Add(new XAttribute("numSequencePoints", classLineCoverage.Total.ToString()));
                        classSummary.Add(new XAttribute("visitedSequencePoints", classLineCoverage.Covered.ToString()));
                        classSummary.Add(new XAttribute("numBranchPoints", classBranchCoverage.Total.ToString()));
                        classSummary.Add(new XAttribute("visitedBranchPoints", classBranchCoverage.Covered.ToString()));
                        classSummary.Add(new XAttribute("sequenceCoverage", classLineCoverage.Percent.ToString("G", CultureInfo.InvariantCulture)));
                        classSummary.Add(new XAttribute("branchCoverage", classBranchCoverage.Percent.ToString("G", CultureInfo.InvariantCulture)));
                        classSummary.Add(new XAttribute("maxCyclomaticComplexity", classMaxCyclomaticComplexity.ToString()));
                        classSummary.Add(new XAttribute("minCyclomaticComplexity", classMinCyclomaticComplexity.ToString()));
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

            CoverageDetails moduleLineCoverage = CoverageSummary.CalculateLineCoverage(result.Modules);
            CoverageDetails moduleBranchCoverage = CoverageSummary.CalculateBranchCoverage(result.Modules);
            int moduleMaxCyclomaticComplexity = CoverageSummary.CalculateMaxCyclomaticComplexity(result.Modules);
            int moduleMinCyclomaticComplexity = CoverageSummary.CalculateMinCyclomaticComplexity(result.Modules);

            coverageSummary.Add(new XAttribute("numSequencePoints", moduleLineCoverage.Total.ToString()));
            coverageSummary.Add(new XAttribute("visitedSequencePoints", moduleLineCoverage.Covered.ToString()));
            coverageSummary.Add(new XAttribute("numBranchPoints", moduleBranchCoverage.Total.ToString()));
            coverageSummary.Add(new XAttribute("visitedBranchPoints", moduleBranchCoverage.Covered.ToString()));
            coverageSummary.Add(new XAttribute("sequenceCoverage", moduleLineCoverage.Percent.ToString("G", CultureInfo.InvariantCulture)));
            coverageSummary.Add(new XAttribute("branchCoverage", moduleBranchCoverage.Percent.ToString("G", CultureInfo.InvariantCulture)));
            coverageSummary.Add(new XAttribute("maxCyclomaticComplexity", moduleMaxCyclomaticComplexity.ToString()));
            coverageSummary.Add(new XAttribute("minCyclomaticComplexity", moduleMinCyclomaticComplexity.ToString()));
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
    }
}
