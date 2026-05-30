// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Coverlet.Core.Abstractions;

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

    /// <summary>
    /// Converts a simple assembly name to a coverlet exclude-filter pattern.
    /// e.g. "xunit.runner.utility" → "[xunit.runner.utility]*"
    /// </summary>
    internal static string ToExcludeFilter(string assemblyName) => $"[{assemblyName}]*";
  }
}
