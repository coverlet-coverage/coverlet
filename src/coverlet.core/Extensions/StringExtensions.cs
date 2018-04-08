using System;

namespace Coverlet.Core.Extensions
{
    internal static class StringExtensions
    {
        public static string RemoveAttributeSuffix(this string attributeName)
        {
            var attributeSuffix = "Attribute";

            if (IsSuffixPlacedAtTheEndOfWord(attributeName, attributeSuffix) &&
                !IsWordContainsOnlySuffix(attributeName, attributeSuffix))
            {
                return attributeName.Replace(attributeSuffix, "");
            }
            return attributeName;
        }

        private static bool IsSuffixPlacedAtTheEndOfWord(string word, string suffix)
        {
            return word.LastIndexOf(suffix, StringComparison.Ordinal) == word.Length - suffix.Length;
        }

        private static bool IsWordContainsOnlySuffix(string word, string suffix)
        {
            return word == suffix;
        }
    }
}