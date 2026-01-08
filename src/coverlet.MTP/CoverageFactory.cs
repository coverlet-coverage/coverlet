// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core;
using Coverlet.Core.Abstractions;

namespace coverlet.Extension.Collector;

/// <summary>
/// Default factory for creating Coverage instances.
/// </summary>
internal sealed class CoverageFactory : ICoverageFactory
{
  private readonly ILogger _logger;
  private readonly IInstrumentationHelper _instrumentationHelper;
  private readonly IFileSystem _fileSystem;
  private readonly ISourceRootTranslator _sourceRootTranslator;
  private readonly ICecilSymbolHelper _cecilSymbolHelper;

  public CoverageFactory(
    ILogger logger,
    IInstrumentationHelper instrumentationHelper,
    IFileSystem fileSystem,
    ISourceRootTranslator sourceRootTranslator,
    ICecilSymbolHelper cecilSymbolHelper)
  {
    _logger = logger;
    _instrumentationHelper = instrumentationHelper;
    _fileSystem = fileSystem;
    _sourceRootTranslator = sourceRootTranslator;
    _cecilSymbolHelper = cecilSymbolHelper;
  }

  public ICoverage Create(string modulePath, CoverageParameters parameters)
  {
    return new Coverage(
      modulePath,
      parameters,
      _logger,
      _instrumentationHelper,
      _fileSystem,
      _sourceRootTranslator,
      _cecilSymbolHelper);
  }
}
