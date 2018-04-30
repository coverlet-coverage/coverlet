
using System;
using Coverlet.Core.Attributes;

namespace Coverlet.Core.Extensions
{
    internal static class HelperExtensions
    {
        [ExcludeFromCoverage]
        public static TRet Maybe<T, TRet>(this T value, Func<T, TRet> action, TRet defValue = default(TRet))
            where T : class
        {
            return (value != null) ? action(value) : defValue;
        }
    }
}