using System.Collections.Generic;

namespace Coverlet.Core.Extensions
{
    internal static class DictionaryExtensions
    {
        public static bool TryAdd<T, U>(this Dictionary<T, U> dictionary, T key, U value)
        {
            if (dictionary.ContainsKey(key))
                return false;

            dictionary.Add(key, value);
            return true;
        }
    }
}