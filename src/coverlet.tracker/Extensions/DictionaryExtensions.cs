using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Coverlet.Tracker.Extensions
{
    internal static class DictionaryExtensions
    {
        [ExcludeFromCodeCoverage]
        public static bool TryAdd<T, U>(this Dictionary<T, U> dictionary, T key, U value)
        {
            if (dictionary.ContainsKey(key))
                return false;

            dictionary.Add(key, value);
            return true;
        }
    }
}