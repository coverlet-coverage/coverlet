// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.Core.Abstractions;

/// <summary>
/// Factory for creating ICoverage instances.
/// </summary>
internal interface ICoverageFactory
{
  /// <summary>
  /// Creates a coverage instance for the specified module.
  /// </summary>
  /// <param name="modulePath">Path to the module to instrument.</param>
  /// <param name="parameters">Coverage configuration parameters.</param>
  /// <returns>An ICoverage instance.</returns>
  ICoverage Create(string modulePath, CoverageParameters parameters);
}
