using System;

namespace Coverlet.Core.Abstractions
{
    internal interface IFormatHelper
    {
        string Invariant(FormattableString value);
    }
}
