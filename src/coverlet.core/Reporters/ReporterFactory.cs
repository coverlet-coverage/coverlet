// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Coverlet.Core.Abstractions;

#nullable disable

namespace Coverlet.Core.Reporters
{
    internal class ReporterFactory
    {
        private readonly string _format;
        private readonly IReporter[] _reporters;

        public ReporterFactory(string format)
        {
            _format = format;
            _reporters = new IReporter[] {
                new JsonReporter(), new LcovReporter(),
                new OpenCoverReporter(), new CoberturaReporter(),
                new TeamCityReporter()
            };
        }

        public bool IsValidFormat()
        {
            return CreateReporter() != null;
        }

        public IReporter CreateReporter()
            => _reporters.FirstOrDefault(r => string.Equals(r.Format, _format, StringComparison.OrdinalIgnoreCase));
    }
}
