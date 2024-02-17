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

      CoverageSummary summary = new();
      XDocument xml = new();
      XElement coverage = new("CoverageSession");
      XElement coverageSummary = new("Summary");
      XElement modules = new("Modules");

      int numClasses = 0, numMethods = 0;
      int visitedClasses = 0, visitedMethods = 0;

      int i = 1;

      foreach (System.Collections.Generic.KeyValuePair<string, Documents> mod in result.Modules)
      {
        XElement module = new("Module");
        module.Add(new XAttribute("hash", Guid.NewGuid().ToString().ToUpper()));

        XElement path = new("ModulePath", mod.Key);
        XElement time = new("ModuleTime", DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ss"));
        XElement name = new("ModuleName", Path.GetFileNameWithoutExtension(mod.Key));

        module.Add(path);
        module.Add(time);
        module.Add(name);

        XElement files = new("Files");
        XElement classes = new("Classes");

        foreach (System.Collections.Generic.KeyValuePair<string, Classes> doc in mod.Value)
        {
          XElement file = new("File");
          file.Add(new XAttribute("uid", i.ToString()));
          file.Add(new XAttribute("fullPath", doc.Key));
          files.Add(file);

          foreach (System.Collections.Generic.KeyValuePair<string, Methods> cls in doc.Value)
          {
            XElement @class = new("Class");
            XElement classSummary = new("Summary");

            XElement className = new("FullName", cls.Key);

            XElement methods = new("Methods");
            int j = 0;
            bool classVisited = false;

            foreach (System.Collections.Generic.KeyValuePair<string, Method> meth in cls.Value)
            {
              // Skip all methods with no lines
              if (meth.Value.Lines.Count == 0)
                continue;

              CoverageDetails methLineCoverage = summary.CalculateLineCoverage(meth.Value.Lines);
              CoverageDetails methBranchCoverage = summary.CalculateBranchCoverage(meth.Value.Branches);
              int methCyclomaticComplexity = summary.CalculateCyclomaticComplexity(meth.Value.Branches);
              int methNpathComplexity = summary.CalculateNpathComplexity(meth.Value.Branches);

              XElement method = new("Method");

              method.Add(new XAttribute("cyclomaticComplexity", methCyclomaticComplexity.ToString()));
              method.Add(new XAttribute("nPathComplexity", methCyclomaticComplexity.ToString()));
              method.Add(new XAttribute("sequenceCoverage", methLineCoverage.Percent.ToString("G", CultureInfo.InvariantCulture)));
              method.Add(new XAttribute("branchCoverage", methBranchCoverage.Percent.ToString("G", CultureInfo.InvariantCulture)));
              method.Add(new XAttribute("isConstructor", meth.Key.Contains("ctor").ToString()));
              method.Add(new XAttribute("isGetter", meth.Key.Contains("get_").ToString()));
              method.Add(new XAttribute("isSetter", meth.Key.Contains("set_").ToString()));
              method.Add(new XAttribute("isStatic", (!meth.Key.Contains("get_") || !meth.Key.Contains("set_")).ToString()));

              XElement methodName = new("Name", meth.Key);

              XElement fileRef = new("FileRef");
              fileRef.Add(new XAttribute("uid", i.ToString()));

              XElement methodPoint = new("MethodPoint");
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
              XElement sequencePoints = new("SequencePoints");
              XElement branchPoints = new("BranchPoints");
              XElement methodSummary = new("Summary");
              int k = 0;
              int kBr = 0;
              bool methodVisited = false;

              foreach (System.Collections.Generic.KeyValuePair<int, int> lines in meth.Value.Lines)
              {
                BranchInfo[] lineBranches = meth.Value.Branches.Where(branchInfo => branchInfo.Line == lines.Key).ToArray();
                CoverageDetails branchCoverage = summary.CalculateBranchCoverage(lineBranches);

                XElement sequencePoint = new("SequencePoint");
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
                XElement branchPoint = new("BranchPoint");
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

            CoverageDetails classLineCoverage = summary.CalculateLineCoverage(cls.Value);
            CoverageDetails classBranchCoverage = summary.CalculateBranchCoverage(cls.Value);
            CoverageDetails classMethodCoverage = summary.CalculateMethodCoverage(cls.Value);
            int classMaxCyclomaticComplexity = summary.CalculateMaxCyclomaticComplexity(cls.Value);
            int classMinCyclomaticComplexity = summary.CalculateMinCyclomaticComplexity(cls.Value);

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

      CoverageDetails moduleLineCoverage = summary.CalculateLineCoverage(result.Modules);
      CoverageDetails moduleBranchCoverage = summary.CalculateBranchCoverage(result.Modules);
      int moduleMaxCyclomaticComplexity = summary.CalculateMaxCyclomaticComplexity(result.Modules);
      int moduleMinCyclomaticComplexity = summary.CalculateMinCyclomaticComplexity(result.Modules);

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

      using MemoryStream stream = new();
      using StreamWriter streamWriter = new(stream, new UTF8Encoding(false));
      xml.Save(streamWriter);

      return Encoding.UTF8.GetString(stream.ToArray());
    }
  }
}
