using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Coverlet.Collector.DataCollection;
using Coverlet.Collector.Utilities;
using Xunit;

namespace Coverlet.Collector.Tests
{
    public class CoverletSettingsParserTests
    {
        private CoverletSettingsParser _coverletSettingsParser;

        public CoverletSettingsParserTests()
        {
            _coverletSettingsParser = new CoverletSettingsParser(new TestPlatformEqtTrace());
        }

        [Fact]
        public void ParseShouldThrowCoverletDataCollectorExceptionIfTestModulesIsNull()
        {
            string message = Assert.Throws<CoverletDataCollectorException>(() => _coverletSettingsParser.Parse(null, null)).Message;

            Assert.Equal("CoverletCoverageDataCollector: No test modules found", message);
        }

        [Fact]
        public void ParseShouldThrowCoverletDataCollectorExceptionIfTestModulesIsEmpty()
        {
            string message = Assert.Throws<CoverletDataCollectorException>(() => _coverletSettingsParser.Parse(null, Enumerable.Empty<string>())).Message;

            Assert.Equal("CoverletCoverageDataCollector: No test modules found", message);
        }

        [Fact]
        public void ParseShouldSelectFirstTestModuleFromTestModulesList()
        {
            var testModules = new List<string> { "module1.dll", "module2.dll", "module3.dll" };

            CoverletSettings coverletSettings = _coverletSettingsParser.Parse(null, testModules);

            Assert.Equal("module1.dll", coverletSettings.TestModule);
        }

        [Theory]
        [InlineData("[*]*,[coverlet]*", "[coverlet.*.tests?]*,[coverlet.*.tests.*]*", @"E:\temp,/var/tmp", "module1.cs,module2.cs", "Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute")]
        [InlineData("[*]*, [coverlet]*", "[coverlet.*.tests?]*, [coverlet.*.tests.*]*", @"E:\temp, /var/tmp", "module1.cs, module2.cs", "Obsolete, GeneratedCodeAttribute, CompilerGeneratedAttribute")]
        [InlineData("[*]*,\r\n[coverlet]*", "[coverlet.*.tests?]*,\r\n[coverlet.*.tests.*]*", "E:\\temp,\r\n/var/tmp", "module1.cs,\r\nmodule2.cs", "Obsolete,\r\nGeneratedCodeAttribute,\r\nCompilerGeneratedAttribute")]
        public void ParseShouldCorrectlyParseConfigurationElement(string includeFilters,
            string excludeFilters,
            string includeDirectories,
            string excludeSourceFiles,
            string excludeAttributes)
        {
            var testModules = new List<string> { "abc.dll" };
            var doc = new XmlDocument();
            var configElement = doc.CreateElement("Configuration");
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.IncludeFiltersElementName, includeFilters);
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.ExcludeFiltersElementName, excludeFilters);
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.IncludeDirectoriesElementName, includeDirectories);
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.ExcludeSourceFilesElementName, excludeSourceFiles);
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.ExcludeAttributesElementName, excludeAttributes);
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.MergeWithElementName, "/path/to/result.json");
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.UseSourceLinkElementName, "false");
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.SingleHitElementName, "true");
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.IncludeTestAssemblyElementName, "true");

            CoverletSettings coverletSettings = _coverletSettingsParser.Parse(configElement, testModules);

            Assert.Equal("abc.dll", coverletSettings.TestModule);
            Assert.Equal("[*]*", coverletSettings.IncludeFilters[0]);
            Assert.Equal("[coverlet]*", coverletSettings.IncludeFilters[1]);
            Assert.Equal(@"E:\temp", coverletSettings.IncludeDirectories[0]);
            Assert.Equal("/var/tmp", coverletSettings.IncludeDirectories[1]);
            Assert.Equal("module1.cs", coverletSettings.ExcludeSourceFiles[0]);
            Assert.Equal("module2.cs", coverletSettings.ExcludeSourceFiles[1]);
            Assert.Equal("Obsolete", coverletSettings.ExcludeAttributes[0]);
            Assert.Equal("GeneratedCodeAttribute", coverletSettings.ExcludeAttributes[1]);
            Assert.Equal("CompilerGeneratedAttribute", coverletSettings.ExcludeAttributes[2]);
            Assert.Equal("/path/to/result.json", coverletSettings.MergeWith);
            Assert.Equal("[coverlet.*]*", coverletSettings.ExcludeFilters[0]);
            Assert.Equal("[coverlet.*.tests?]*", coverletSettings.ExcludeFilters[1]);
            Assert.Equal("[coverlet.*.tests.*]*", coverletSettings.ExcludeFilters[2]);
            
            Assert.False(coverletSettings.UseSourceLink);
            Assert.True(coverletSettings.SingleHit);
            Assert.True(coverletSettings.IncludeTestAssembly);
        }

        [Theory]
        [InlineData(" , json", 1, new[] { "json" })]
        [InlineData(" , json, ", 1, new[] { "json" })]
        [InlineData("json,cobertura", 2, new[] { "json", "cobertura" })]
        [InlineData("json,\r\ncobertura", 2, new[] { "json", "cobertura" })]
        [InlineData(" , json,, cobertura ", 2, new[] { "json", "cobertura" })]
        [InlineData(" , json, , cobertura ", 2, new[] { "json", "cobertura" })]
        public void ParseShouldCorrectlyParseMultipleFormats(string formats, int formatsCount, string[] expectedReportFormats)
        {
            var testModules = new List<string> { "abc.dll" };
            var doc = new XmlDocument();
            var configElement = doc.CreateElement("Configuration");
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.ReportFormatElementName, formats);

            CoverletSettings coverletSettings = _coverletSettingsParser.Parse(configElement, testModules);

            Assert.Equal(expectedReportFormats, coverletSettings.ReportFormats);
            Assert.Equal(formatsCount, coverletSettings.ReportFormats.Length);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseShouldUseDefaultFormatWhenNoFormatSpecified(string formats)
        {
            var testModules = new List<string> { "abc.dll" };
            var defaultFormat = CoverletConstants.DefaultReportFormat;
            var doc = new XmlDocument();
            var configElement = doc.CreateElement("Configuration");
            this.CreateCoverletNodes(doc, configElement, CoverletConstants.ReportFormatElementName, formats);

            CoverletSettings coverletSettings = _coverletSettingsParser.Parse(configElement, testModules);

            Assert.Equal(defaultFormat, coverletSettings.ReportFormats[0]);
        }

        private void CreateCoverletNodes(XmlDocument doc, XmlElement configElement, string nodeSetting, string nodeValue)
        {
            var node = doc.CreateNode("element", nodeSetting, string.Empty);
            node.InnerText = nodeValue;
            configElement.AppendChild(node);
        }
    }
}
