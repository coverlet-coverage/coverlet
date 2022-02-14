// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Coverlet.Core.Helpers;

#nullable disable

namespace Coverlet.Core.Abstractions
{
    internal interface ISourceRootTranslator
    {
        string ResolveFilePath(string originalFileName);
        string ResolveDeterministicPath(string originalFileName);
        IReadOnlyList<SourceRootMapping> ResolvePathRoot(string pathRoot);
    }
}
