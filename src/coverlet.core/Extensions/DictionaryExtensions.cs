using System.Collections.Generic;
using Coverlet.Core.Attributes;

namespace Coverlet.Core.Extensions
{
    internal static class DictionaryExtensions
    {
        [ExcludeFromCoverage]
        public static bool TryAdd<T, U>(this Dictionary<T, U> dictionary, T key, U value)
        {
            if (dictionary.ContainsKey(key))
                return false;

            dictionary.Add(key, value);
            return true;
        }
    }
}