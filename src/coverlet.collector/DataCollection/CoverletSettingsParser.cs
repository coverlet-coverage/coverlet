using System;
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
                TestModule = ParseTestModule(testModules)
            };

            if (configurationElement != null)
            {
                coverletSettings.IncludeFilters = ParseIncludeFilters(configurationElement);
                coverletSettings.IncludeDirectories = ParseIncludeDirectories(configurationElement);
                coverletSettings.ExcludeAttributes = ParseExcludeAttributes(configurationElement);
                coverletSettings.ExcludeSourceFiles = ParseExcludeSourceFiles(configurationElement);
                coverletSettings.MergeWith = ParseMergeWith(configurationElement);
                coverletSettings.UseSourceLink = ParseUseSourceLink(configurationElement);
                coverletSettings.SingleHit = ParseSingleHit(configurationElement);
                coverletSettings.IncludeTestAssembly = ParseIncludeTestAssembly(configurationElement);
                coverletSettings.SkipAutoProps = ParseSkipAutoProps(configurationElement);
                coverletSettings.DoesNotReturnAttributes = ParseDoesNotReturnAttributes(configurationElement);
            }

            coverletSettings.ReportFormats = ParseReportFormats(configurationElement);
            coverletSettings.ExcludeFilters = ParseExcludeFilters(configurationElement);

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
        /// Parse report formats
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Report formats</returns>
        private string[] ParseReportFormats(XmlElement configurationElement)
        {
            string[] formats = Array.Empty<string>();
            if (configurationElement != null)
            {
                XmlElement reportFormatElement = configurationElement[CoverletConstants.ReportFormatElementName];
                formats = this.SplitElement(reportFormatElement);
            }

            return formats is null || formats.Length == 0 ? new[] { CoverletConstants.DefaultReportFormat } : formats;
        }

        /// <summary>
        /// Parse filters to include
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Filters to include</returns>
        private string[] ParseIncludeFilters(XmlElement configurationElement)
        {
            XmlElement includeFiltersElement = configurationElement[CoverletConstants.IncludeFiltersElementName];
            return this.SplitElement(includeFiltersElement);
        }

        /// <summary>
        /// Parse directories to include
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Directories to include</returns>
        private string[] ParseIncludeDirectories(XmlElement configurationElement)
        {
            XmlElement includeDirectoriesElement = configurationElement[CoverletConstants.IncludeDirectoriesElementName];
            return this.SplitElement(includeDirectoriesElement);
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
                string[] filters = this.SplitElement(excludeFiltersElement);
                if (filters != null)
                {
                    excludeFilters.AddRange(filters);
                }
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
            return this.SplitElement(excludeSourceFilesElement);
        }

        /// <summary>
        /// Parse attributes to exclude
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Attributes to exclude</returns>
        private string[] ParseExcludeAttributes(XmlElement configurationElement)
        {
            XmlElement excludeAttributesElement = configurationElement[CoverletConstants.ExcludeAttributesElementName];
            return this.SplitElement(excludeAttributesElement);
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

        /// <summary>
        /// Parse skipautoprops flag
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>Include Test Assembly Flag</returns>
        private bool ParseSkipAutoProps(XmlElement configurationElement)
        {
            XmlElement skipAutoPropsElement = configurationElement[CoverletConstants.SkipAutoProps];
            bool.TryParse(skipAutoPropsElement?.InnerText, out bool skipAutoProps);
            return skipAutoProps;
        }

        /// <summary>
        /// Parse attributes that mark methods that do not return.
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <returns>DoesNotReturn attributes</returns>
        private string[] ParseDoesNotReturnAttributes(XmlElement configurationElement)
        {
            XmlElement doesNotReturnAttributesElement = configurationElement[CoverletConstants.DoesNotReturnAttributesElementName];
            return this.SplitElement(doesNotReturnAttributesElement);
        }

        /// <summary>
        /// Splits a comma separated elements into an array
        /// </summary>
        /// <param name="element">The element to split</param>
        /// <returns>An array of the values in the element</returns>
        private string[] SplitElement(XmlElement element)
        {
            return element?.InnerText?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray();
        }
    }
}
