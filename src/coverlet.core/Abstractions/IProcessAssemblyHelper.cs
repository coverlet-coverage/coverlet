// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Coverlet.Core.Abstractions
{
  /// <summary>
  /// Provides access to assemblies loaded by the current process,
  /// used to build dynamic exclude-filter defaults.
  /// </summary>
  internal interface IProcessAssemblyHelper
  {
    /// <summary>
    /// Returns the simple assembly names (no version, culture, or public key token)
    /// of assemblies directly loaded into the current process's default ALC,
    /// excluding the assembly under test itself.
    /// Transitive/indirect dependencies that are not yet loaded are not returned.
    /// </summary>
    /// <param name="testAssemblyName">Simple name of the test assembly to exclude from the list.</param>
    IReadOnlyList<string> GetLoadedAssemblyNames(string testAssemblyName);
  }
}
