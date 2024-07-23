// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using System.Linq;

namespace Coverlet.Collector.Utilities
{
  internal class ReportFormatParser
  {
    internal string[] ParseReportFormats(XmlElement configurationElement)
    {
      string[] formats = Array.Empty<string>();
      if (configurationElement != null)
      {
        XmlElement reportFormatElement = configurationElement[CoverletConstants.ReportFormatElementName];
        formats = SplitElement(reportFormatElement);
      }

      return formats is null || formats.Length == 0 ? new[] { CoverletConstants.DefaultReportFormat } : formats;
    }

    private static string[] SplitElement(XmlElement element)
    {
      return element?.InnerText?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray();
    }
  }
}
