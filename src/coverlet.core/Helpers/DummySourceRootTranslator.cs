// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;

namespace coverlet.core.Helpers
{
  internal class DummySourceRootTranslator : ISourceRootTranslator
  {
    public bool AddMappingInCache(string originalFileName, string targetFileName)
    {
      throw new NotImplementedException();
    }

    public string ResolveFilePath(string originalFileName)
    {
      throw new NotImplementedException();
    }

    public string ResolveDeterministicPath(string originalFileName)
    {
      throw new NotImplementedException();
    }

    public IReadOnlyList<SourceRootMapping> ResolvePathRoot(string pathRoot)
    {
      throw new NotImplementedException();
    }
  }
}
