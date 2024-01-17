// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.Core.Abstractions
{
  public interface IReporter
  {
    ReporterOutputType OutputType { get; }
    string Format { get; }
    string Extension { get; }
    string Report(CoverageResult result, ISourceRootTranslator sourceRootTranslator);
  }

  public enum ReporterOutputType
  {
    File,
    Console,
  }
}
