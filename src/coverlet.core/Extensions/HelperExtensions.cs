// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Coverlet.Core.Attributes;

namespace Coverlet.Core.Extensions
{
    internal static class HelperExtensions
    {
        [ExcludeFromCoverage]
        public static TRet Maybe<T, TRet>(this T value, Func<T, TRet> action, TRet defValue = default)
            where T : class
        {
            return (value != null) ? action(value) : defValue;
        }
    }
}
