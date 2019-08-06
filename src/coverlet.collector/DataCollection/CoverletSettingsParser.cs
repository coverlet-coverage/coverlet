using System.Collections.Generic;
using System.Linq;
using System.Xml;
using coverlet.collector.Resources;
using Coverlet.Collector.Utilities;

namespace Coverlet.Collector.DataCollection
{
    /// <summary>
    /// Coverlet settings parser
    /// </summary>
    internal class CoverletSettingsParser
    {
        private readonly TestPlatformEqtTrace _eqtTrace;

        public CoverletSettingsParser(TestPlatformEqtTrace eqtTrace)
        {
            _eqtTrace = eqtTrace;
        }

        /// <summary>
        /// Parser coverlet settings
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <param name="testModules">Test modules</param>
        /// <returns>Coverlet settings</returns>
        public CoverletSettings Parse(XmlElement configurationElement, IEnumerable<string> testModules)
        {
            var coverletSettings = new CoverletSettings
            {
                TestModule = this.ParseTestModule(testModules)
            };

            if (configurationElement != null)
            {
                coverletSettings.IncludeFilters = this.ParseIncludeFilters(configurationElement);
                coverletSettings.IncludeDirectories = this.ParseIncludeDirectories(configurationElement);
                coverletSettings.ExcludeAttributes = this.ParseExcludeAttributes(configurationElement);
                coverletSettings.ExcludeSourceFiles = this.ParseExcludeSourceFiles(configurationElement);
                coverletSettings.MergeWith = this.ParseMergeWith(configurationElement);
                coverletSettings.UseSourceLink = this.ParseUseSourceLink(configurationElement);
                coverletSettings.SingleHit = this.ParseSingleHit(configurationElement);
                coverletSettings.IncludeTestAssembly = this.ParseIncludeTestAssembly(configurationElement);
            }

            coverletSettings.ReportFormat = this.ParseReportFormat(configurationElement);
            coverletSettings.ExcludeFilters = this.ParseExcludeFilters(configurationElement);

            if (_eqtTrace.IsVerboseEnabled)
            {
                _eqtTrace.Verbose("{0}: Initializing coverlet process with settings: \"{1}\"", CoverletConstants.DataCollectorName, coverletSettings.ToString());
            }

            return coverletSettings;
        }

        /// <summary>
        /// Parses test module
        /// </summary>
        /// <param name="testModules">Test modules</param>
        /// <returns>Test module</returns>
        private string ParseTestModule(IEnumerable<string> testModules)
        {
            // Validate if at least one source present.
            if (testModules == null || !testModules.Any())
            {
                string errorMessage = string.Format(Resources.NoTestModulesFound, CoverletConstants.DataCollectorName);
                throw new CoverletDataCollectorException(errorMessage);
            }

            // Note:
            // 1) .NET core test run supports one testModule per run. Coverlet also supports one testModule per run. So, we are using first testSource only and ignoring others.
            // 2) If and when .NET full is supported with coverlet OR .NET core starts supporting multiple testModules, revisit this code to use other testModules as well.
            return testModules.FirstOrDefault();
        }

        /// <summary>
        /// Parse report format
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Report format</returns>
        private string ParseReportFormat(XmlElement configurationElement)
        {
            string format = string.Empty;
            if (configurationElement != null)
            {
                XmlElement reportFormatElement = configurationElement[CoverletConstants.ReportFormatElementName];
                format = reportFormatElement?.InnerText?.Split(',').FirstOrDefault();
            }
            return string.IsNullOrEmpty(format) ? CoverletConstants.DefaultReportFormat : format;
        }

        /// <summary>
        /// Parse filters to include
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Filters to include</returns>
        private string[] ParseIncludeFilters(XmlElement configurationElement)
        {
            XmlElement includeFiltersElement = configurationElement[CoverletConstants.IncludeFiltersElementName];
            return includeFiltersElement?.InnerText?.Split(',');
        }

        /// <summary>
        /// Parse directories to include
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Directories to include</returns>
        private string[] ParseIncludeDirectories(XmlElement configurationElement)
        {
            XmlElement includeDirectoriesElement = configurationElement[CoverletConstants.IncludeDirectoriesElementName];
            return includeDirectoriesElement?.InnerText?.Split(',');
        }

        /// <summary>
        /// Parse filters to exclude
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Filters to exclude</returns>
        private string[] ParseExcludeFilters(XmlElement configurationElement)
        {
            List<string> excludeFilters = new List<string> { CoverletConstants.DefaultExcludeFilter };

            if (configurationElement != null)
            {
                XmlElement excludeFiltersElement = configurationElement[CoverletConstants.ExcludeFiltersElementName];
                string[] filters = excludeFiltersElement?.InnerText?.Split(',');
                if (filters != null)
                {
                    excludeFilters.AddRange(filters);
                }
            }

            // if we've only one element mean that we only added CoverletConstants.DefaultExcludeFilter
            // so add default exclusions
            if (excludeFilters.Count == 1)
            {
                excludeFilters.Add("[xunit*]*");
            }

            return excludeFilters.ToArray();
        }

        /// <summary>
        /// Parse source files to exclude
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Source files to exclude</returns>
        private string[] ParseExcludeSourceFiles(XmlElement configurationElement)
        {
            XmlElement excludeSourceFilesElement = configurationElement[CoverletConstants.ExcludeSourceFilesElementName];
            return excludeSourceFilesElement?.InnerText?.Split(',');
        }

        /// <summary>
        /// Parse attributes to exclude
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Attributes to exclude</returns>
        private string[] ParseExcludeAttributes(XmlElement configurationElement)
        {
            XmlElement excludeAttributesElement = configurationElement[CoverletConstants.ExcludeAttributesElementName];
            return excludeAttributesElement?.InnerText?.Split(',');
        }

        /// <summary>
        /// Parse merge with attribute
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Merge with attribute</returns>
        private string ParseMergeWith(XmlElement configurationElement)
        {
            XmlElement mergeWithElement = configurationElement[CoverletConstants.MergeWithElementName];
            return mergeWithElement?.InnerText;
        }

        /// <summary>
        /// Parse use source link flag
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Use source link flag</returns>
        private bool ParseUseSourceLink(XmlElement configurationElement)
        {
            XmlElement useSourceLinkElement = configurationElement[CoverletConstants.UseSourceLinkElementName];
            bool.TryParse(useSourceLinkElement?.InnerText, out bool useSourceLink);
            return useSourceLink;
        }

        /// <summary>
        /// Parse single hit flag
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Single hit flag</returns>
        private bool ParseSingleHit(XmlElement configurationElement)
        {
            XmlElement singleHitElement = configurationElement[CoverletConstants.SingleHitElementName];
            bool.TryParse(singleHitElement?.InnerText, out bool singleHit);
            return singleHit;
        }

        /// <summary>
        /// Parse include test assembly flag
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Include Test Assembly Flag</returns>
        private bool ParseIncludeTestAssembly(XmlElement configurationElement)
        {
            XmlElement includeTestAssemblyElement = configurationElement[CoverletConstants.IncludeTestAssemblyElementName];
            bool.TryParse(includeTestAssemblyElement?.InnerText, out bool includeTestAssembly);
            return includeTestAssembly;
        }
    }
}
