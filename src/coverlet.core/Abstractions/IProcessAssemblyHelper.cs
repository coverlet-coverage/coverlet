// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Coverlet.Core.Abstractions
{
  /// <summary>
  /// Provides access to assemblies loaded by the current process and to assembly
  /// names declared in the test module's deps.json, used to build dynamic exclude-filter defaults.
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

    /// <summary>
    /// Returns the simple assembly names of all non-project NuGet package entries
    /// declared in the <c>*.deps.json</c> files located next to the test module.
    /// These entries represent test-host infrastructure that is not yet loaded into
    /// the controller process when <see cref="GetLoadedAssemblyNames"/> is called.
    /// </summary>
    /// <param name="testModuleDirectory">Directory that contains the test module and its deps.json.</param>
    /// <param name="testAssemblyName">Simple name of the test assembly to exclude from the list.</param>
    IReadOnlyList<string> GetDepsJsonAssemblyNames(string testModuleDirectory, string testAssemblyName);
  }
}
