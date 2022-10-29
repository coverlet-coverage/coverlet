// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Coverlet.Core.Abstractions
{
    internal interface IReporter
    {
        ReporterOutputType OutputType { get; }
        string Format { get; }
        string Extension { get; }
        string Report(CoverageResult result, ISourceRootTranslator sourceRootTranslator);
    }

    internal enum ReporterOutputType
    {
        File,
        Console,
    }
}