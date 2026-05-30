// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Abstractions;
using Microsoft.Extensions.DependencyModel;

namespace Coverlet.Core.Helpers
{
  internal sealed class ProcessAssemblyHelper : IProcessAssemblyHelper
  {
    /// <inheritdoc/>
    public IReadOnlyList<string> GetLoadedAssemblyNames(string testAssemblyName)
    {
      if (string.IsNullOrWhiteSpace(testAssemblyName))
        throw new ArgumentException("Test assembly name must not be null or whitespace.", nameof(testAssemblyName));

      return AppDomain.CurrentDomain
        .GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .Select(a => a.GetName().Name)
        .Where(name => !string.IsNullOrWhiteSpace(name) &&
                       !name!.Equals(testAssemblyName, StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList()!;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetDepsJsonAssemblyNames(string testModuleDirectory, string testAssemblyName)
    {
      if (string.IsNullOrWhiteSpace(testModuleDirectory))
        throw new ArgumentException("Test module directory must not be null or whitespace.", nameof(testModuleDirectory));

      if (string.IsNullOrWhiteSpace(testAssemblyName))
        throw new ArgumentException("Test assembly name must not be null or whitespace.", nameof(testAssemblyName));

      var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      using var reader = new DependencyContextJsonReader();
      foreach (string depsFile in Directory.EnumerateFiles(testModuleDirectory, "*.deps.json"))
      {
        try
        {
          using FileStream stream = File.OpenRead(depsFile);
          DependencyContext context = reader.Read(stream);

          foreach (RuntimeLibrary lib in context.RuntimeLibraries)
          {
            // Skip the test project itself; we only want infrastructure packages.
            if (lib.Type.Equals("project", StringComparison.OrdinalIgnoreCase))
              continue;

            if (!string.IsNullOrWhiteSpace(lib.Name) &&
                !lib.Name.Equals(testAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
              names.Add(lib.Name);
            }
          }
        }
        catch (Exception)
        {
          // Best-effort; a corrupt or unreadable deps.json is not fatal.
        }
      }

      return [.. names];
    }

    /// <summary>
    /// Converts a simple assembly name to a coverlet exclude-filter pattern.
    /// For names with three or more dot-separated segments the third segment onwards is
    /// replaced by a wildcard so the filter is resilient to assembly names that differ
    /// from the NuGet package name (e.g. dashes vs. dots in the suffix).
    /// <list type="bullet">
    ///   <item><c>xunit.v3.mtp-v2</c>  → <c>[xunit.v3.*]*</c></item>
    ///   <item><c>Microsoft.Extensions.Configuration.Binder</c> → <c>[Microsoft.Extensions.*]*</c></item>
    ///   <item><c>coverlet.core</c>     → <c>[coverlet.core]*</c></item>
    ///   <item><c>xunit</c>             → <c>[xunit]*</c></item>
    /// </list>
    /// </summary>
    internal static string ToExcludeFilter(string assemblyName)
    {
      int first = assemblyName.IndexOf('.');
      if (first < 0)
        return $"[{assemblyName}]*";

      int second = assemblyName.IndexOf('.', first + 1);
      if (second < 0)
        return $"[{assemblyName}]*"; // exactly two segments — keep exact

      // Three or more segments: use first two as prefix and wildcard the rest.
      string prefix = assemblyName.Substring(0, second);
      return $"[{prefix}.*]*";
    }

    /// <summary>
    /// Removes exact-name filters from <paramref name="filters"/> that are already matched
    /// by a wildcard filter present in the same collection.
    /// For example <c>[coverlet.core]*</c> is redundant when <c>[coverlet.*]*</c> is present.
    /// </summary>
    internal static IEnumerable<string> PruneRedundantFilters(IEnumerable<string> filters)
    {
      // Collect wildcard filters of the form "[prefix.*]*"
      var wildcards = new HashSet<string>(
        filters.Where(f => f.EndsWith(".*]*", StringComparison.Ordinal)),
        StringComparer.OrdinalIgnoreCase);

      foreach (string filter in filters)
      {
        if (!filter.EndsWith(".*]*", StringComparison.Ordinal)
            && IsExactFilterCoveredByWildcard(filter, wildcards))
        {
          continue; // skip — already covered
        }

        yield return filter;
      }
    }

    // Checks whether an exact filter like "[coverlet.core]*" is already matched by
    // a wildcard like "[coverlet.*]*" in the wildcard set.
    private static bool IsExactFilterCoveredByWildcard(string filter, HashSet<string> wildcards)
    {
      // filter must be of the form "[AssemblyName]*"
      if (filter.Length < 4 || filter[0] != '[' || !filter.EndsWith("]*", StringComparison.Ordinal))
        return false;

      string name = filter.Substring(1, filter.Length - 3); // strip "[" and "]*"

      int dotIndex = name.LastIndexOf('.');
      while (dotIndex > 0)
      {
        string candidate = $"[{name.Substring(0, dotIndex)}.*]*";
        if (wildcards.Contains(candidate))
          return true;
        dotIndex = name.LastIndexOf('.', dotIndex - 1);
      }

      return false;
    }
  }
}
