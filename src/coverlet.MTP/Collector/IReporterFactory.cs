// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Abstractions;
using Coverlet.Core.Reporters;

namespace Coverlet.MTP.Collector;

/// <summary>
/// Factory abstraction for creating reporters. Enables testing.
/// </summary>
internal interface IReporterFactory
{
  IReporter? CreateReporter(string format);
}

/// <summary>
/// Default implementation that delegates to Coverlet's ReporterFactory.
/// </summary>
internal sealed class DefaultReporterFactory : IReporterFactory
{
  public IReporter? CreateReporter(string format)
  {
    return new ReporterFactory(format).CreateReporter();
  }
}
