using System.Globalization;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Coverlet.Core;
using Coverlet.Core.Reporters;

namespace Coverlet.Core.Reporters
{
    public class TeamCityReporter : IReporter
    {
        public ReporterOutputType OutputType => ReporterOutputType.File;

        public string Format => "teamcity";

        public string Extension => "cov.xml";

        public string Report(CoverageResult result)
        {
            // Calculate coverage
            var summary = new CoverageSummary();
            var overallLineCoverage = summary.CalculateLineCoverage(result.Modules);
            var overallBranchCoverage = summary.CalculateBranchCoverage(result.Modules);
            var overallMethodCoverage = summary.CalculateMethodCoverage(result.Modules);

            // Report coverage
            var stringBuilder = new StringBuilder();
            OutputLineCoverage(overallLineCoverage, stringBuilder);
            OutputBranchCoverage(overallBranchCoverage, stringBuilder);
            OutputMethodCoverage(overallMethodCoverage, stringBuilder);
            System.Console.WriteLine(stringBuilder);

            XDocument xml = new XDocument();
            XElement modules = new XElement("Root");
            modules.Add(new XAttribute("ReportType","DetailedXml"));
            modules.Add(new XAttribute("CoveredStatements", overallBranchCoverage.Covered));
            modules.Add(new XAttribute("TotalStatements", overallBranchCoverage.Total));
            modules.Add(new XAttribute("CoveragePercent", overallBranchCoverage.Percent));
            modules.Add(new XAttribute("DotCoverVersion","2018.2.3"));

            XElement files = new XElement("FileIndices");
            modules.Add(files);
            int fileId = 1;

            foreach (var mod in result.Modules)
            {
                XElement module = new XElement("Assembly");
                //module.Add(new XAttribute("hash", Guid.NewGuid().ToString().ToUpper()));

                // XElement time = new XElement("ModuleTime", DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ss"));
                module.Add(new XAttribute("Name", Path.GetFileNameWithoutExtension(mod.Key)));

                var moduleTotalStatements = 0;
                var moduleCoveredStatements = 0;

                XElement nameSpace = new XElement("Namespace");

                foreach (var doc in mod.Value)
                {
                    var nameSpaceTotalStatements = 0;
                    var nameSpaceCoveredStatements = 0;

                    XElement file = new XElement("File");
                    file.Add(new XAttribute("Index", fileId.ToString()));
                    file.Add(new XAttribute("Name", doc.Key));
                    files.Add(file);

                    foreach (var cls in doc.Value)
                    {
                        XElement @class = new XElement("Type");

                        int index = cls.Key.LastIndexOf('.');
                        string nameSpaceName;
                        string typeName;
                        if (index > 0) {
                            nameSpaceName = cls.Key.Substring(0, index);
                            typeName = cls.Key.Substring(index + 1);
                        } else {
                            typeName = cls.Key;
                            nameSpaceName = "";
                        }
                        @class.Add(new XAttribute("Name", typeName));

                        var typeTotalStatements = 0;
                        var typeCoveredStatements = 0;

                        foreach (var meth in cls.Value)
                        {
                            // Skip all methods with no lines
                            if (meth.Value.Lines.Count == 0)
                                continue;

                            var methBranchCoverage = summary.CalculateBranchCoverage(meth.Value.Branches);
                            
                            XElement method = new XElement("Method");
                            method.Add(new XAttribute("Name", meth.Key));
                            method.Add(new XAttribute("CoveredStatements", methBranchCoverage.Covered));
                            method.Add(new XAttribute("TotalStatements", methBranchCoverage.Total));
                            method.Add(new XAttribute("Percent", methBranchCoverage.Percent));
                            typeTotalStatements += methBranchCoverage.Total;
                            typeCoveredStatements += (int) methBranchCoverage.Covered;//todo check cast

                            foreach (var line in meth.Value.Branches)
                            {
                                XElement methodPoint = new XElement("Statement");
                                methodPoint.Add(new XAttribute("FileIndex", fileId.ToString()));
                                methodPoint.Add(new XAttribute("Column", line.Offset));
                                methodPoint.Add(new XAttribute("Line", line.Path.ToString()));
                                methodPoint.Add(new XAttribute("EndColumn", line.EndOffset));
                                methodPoint.Add(new XAttribute("EndLine", line.Ordinal));
                                methodPoint.Add(new XAttribute("Covered", line.Hits > 0? "True" : "False"));

                                method.Add(methodPoint);
                            }
                            @class.Add(method);
                        }

                        nameSpaceTotalStatements += typeTotalStatements;
                        nameSpaceCoveredStatements += typeCoveredStatements;
                        @class.Add(new XAttribute("CoveredStatements", typeCoveredStatements));
                        @class.Add(new XAttribute("TotalStatements", typeTotalStatements));
                        @class.Add(new XAttribute("CoveragePercent", Percent(typeCoveredStatements, typeTotalStatements)));
                        nameSpace.Add(@class);
                    }
                    fileId++;
                    
                    nameSpace.Add(new XAttribute("CoveredStatements", nameSpaceCoveredStatements));
                    nameSpace.Add(new XAttribute("TotalStatements", nameSpaceTotalStatements));
                    nameSpace.Add(new XAttribute("CoveragePercent", Percent(nameSpaceCoveredStatements, nameSpaceTotalStatements)));

                    moduleCoveredStatements += nameSpaceCoveredStatements;
                    moduleTotalStatements += nameSpaceTotalStatements;
                }

                module.Add(nameSpace);
                module.Add(new XAttribute("CoveredStatements", moduleCoveredStatements));
                module.Add(new XAttribute("TotalStatements", moduleTotalStatements));
                module.Add(new XAttribute("CoveragePercent", Percent(moduleCoveredStatements, moduleTotalStatements)));
                modules.Add(module);
            }

            xml.Add(modules);

            var stream = new MemoryStream();
            xml.Save(stream);

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private string Percent(double covered, double total) {
            if (total > 0)
            {
                var str = (covered / total).ToString("P2");
                return str.Substring(0, str.Length - 1);
            } else {
                return "0.00";
            }
        }

        private void OutputLineCoverage(CoverageDetails coverageDetails, StringBuilder builder)
        {
            // The number of covered lines
            OutputTeamCityServiceMessage("CodeCoverageAbsLCovered", coverageDetails.Covered, builder);

            // Line-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsLTotal", coverageDetails.Total, builder);
        }

        private void OutputBranchCoverage(CoverageDetails coverageDetails, StringBuilder builder)
        {
            // The number of covered branches
            OutputTeamCityServiceMessage("CodeCoverageAbsBCovered", coverageDetails.Covered, builder);

            // Branch-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsBTotal", coverageDetails.Total, builder);
        }

        private void OutputMethodCoverage(CoverageDetails coverageDetails, StringBuilder builder)
        {
            // The number of covered methods
            OutputTeamCityServiceMessage("CodeCoverageAbsMCovered", coverageDetails.Covered, builder);

            // Method-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsMTotal", coverageDetails.Total, builder);
        }

        private void OutputTeamCityServiceMessage(string key, double value, StringBuilder builder)
        {
            builder.AppendLine($"##teamcity[buildStatisticValue key='{key}' value='{value.ToString("0.##", new CultureInfo("en-US"))}']");
        }
    }
}
