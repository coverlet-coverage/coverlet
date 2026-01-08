// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.Core.Abstractions;

/// <summary>
/// Abstraction for coverage instrumentation and result collection.
/// </summary>
internal interface ICoverage
{
  /// <summary>
  /// Prepares modules for instrumentation.
  /// </summary>
  /// <returns>The result of preparing modules for coverage.</returns>
  CoveragePrepareResult PrepareModules();

  /// <summary>
  /// Gets the coverage result after test execution.
  /// </summary>
  /// <returns>The coverage result.</returns>
  CoverageResult GetCoverageResult();
}
