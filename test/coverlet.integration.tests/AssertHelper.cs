// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core;
using Xunit.Sdk;

namespace Coverlet.Integration.Tests
{
  internal static class AssertHelper
  {
    public static Classes Document(this Modules modules, string docName)
    {
      if (modules is null)
      {
        throw new ArgumentNullException(nameof(modules));
      }

      if (docName is null)
      {
        throw new ArgumentNullException(nameof(docName));
      }

      var availableModules = new List<string>();
      var availableDocuments = new List<string>();

      foreach (KeyValuePair<string, Documents> module in modules)
      {
        availableModules.Add(module.Key);

        foreach (KeyValuePair<string, Classes> document in module.Value)
        {
          string documentFileName = Path.GetFileName(document.Key);
          availableDocuments.Add(documentFileName);
          if (documentFileName == docName)
          {
            return document.Value;
          }
        }
      }

      string details = modules.Count == 0
        ? "Coverage report contains no modules."
        : $"Available modules: {FormatLookupValues(availableModules)}. Available documents: {FormatLookupValues(availableDocuments)}.";

      throw new XunitException($"Coverage document '{docName}' was not found. {details}");
    }

    public static Methods Class(this Classes classes, string className)
    {
      if (classes is null)
      {
        throw new ArgumentNullException(nameof(classes));
      }

      if (className is null)
      {
        throw new ArgumentNullException(nameof(className));
      }

      foreach (KeyValuePair<string, Methods> @class in classes)
      {
        if (@class.Key == className)
        {
          return @class.Value;
        }
      }

      throw new XunitException($"Coverage class '{className}' was not found. Available classes: {FormatLookupValues(classes.Keys)}.");
    }

    public static Method Method(this Methods methods, string methodName)
    {
      if (methods is null)
      {
        throw new ArgumentNullException(nameof(methods));
      }

      if (methodName is null)
      {
        throw new ArgumentNullException(nameof(methodName));
      }

      foreach (KeyValuePair<string, Method> method in methods)
      {
        if (method.Key.Contains(methodName))
        {
          return method.Value;
        }
      }

      throw new XunitException($"Coverage method containing '{methodName}' was not found. Available methods: {FormatLookupValues(methods.Keys)}.");
    }

    public static void AssertLinesCovered(this Method method, params (int line, int hits)[] lines)
    {
      if (lines is null)
      {
        throw new ArgumentNullException(nameof(lines));
      }

      var linesToCover = new List<int>(lines.Select(l => l.line));

      foreach (KeyValuePair<int, int> line in method.Lines)
      {
        foreach ((int lineToCheck, int expectedHits) in lines)
        {
          if (line.Key == lineToCheck)
          {
            linesToCover.Remove(line.Key);
            if (line.Value != expectedHits)
            {
              throw new XunitException($"Unexpected hits expected line: {lineToCheck} hits: {expectedHits} actual hits: {line.Value}");
            }
          }
        }
      }

      if (linesToCover.Count != 0)
      {
        throw new XunitException($"Not all requested line found, {linesToCover.Select(l => l.ToString()).Aggregate((a, b) => $"{a}, {b}")}");
      }
    }

    private static string FormatLookupValues(IEnumerable<string> values)
    {
      if (values is null)
      {
        throw new ArgumentNullException(nameof(values));
      }

      string[] uniqueValues = values
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(v => v, StringComparer.Ordinal)
        .ToArray();

      return uniqueValues.Length == 0 ? "<none>" : string.Join(", ", uniqueValues);
    }
  }
}
