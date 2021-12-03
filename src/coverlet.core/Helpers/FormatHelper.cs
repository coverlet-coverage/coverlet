using System;
using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Helpers
{
    public class FormatHelper : IFormatHelper
    {
        public string Invariant(FormattableString value)
        {
            return FormattableString.Invariant(value);
        }
    }
}
