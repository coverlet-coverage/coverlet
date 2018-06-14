using Coverlet.Core;

namespace coverlet.core.Extensions
{
    public static class CoverageResultHelper
    {
        public static void Merge(this CoverageResult result, CoverageResult other)
        {
            MergeModules(result.Modules, other.Modules);
        }

        private static void MergeModules(Modules result, Modules other)
        {
            foreach (var keyValuePair in other)
            {
                if (!result.ContainsKey(keyValuePair.Key))
                {
                    result[keyValuePair.Key] = keyValuePair.Value;
                }
                else
                {
                    MergeDocuments(result[keyValuePair.Key], keyValuePair.Value);
                }
            }
        }

        private static void MergeDocuments(Documents result, Documents other)
        {
            foreach (var keyValuePair in other)
            {
                if (!result.ContainsKey(keyValuePair.Key))
                {
                    result[keyValuePair.Key] = keyValuePair.Value;
                }
                else
                {
                    MergeClasses(result[keyValuePair.Key], keyValuePair.Value);
                }
            }
        }

        private static void MergeClasses(Classes result, Classes other)
        {
            foreach (var keyValuePair in other)
            {
                if (!result.ContainsKey(keyValuePair.Key))
                {
                    result[keyValuePair.Key] = keyValuePair.Value;
                }
                else
                {
                    MergeMethods(result[keyValuePair.Key], keyValuePair.Value);
                }
            }
        }

        private static void MergeMethods(Methods result, Methods other)
        {
            foreach (var keyValuePair in other)
            {
                if (!result.ContainsKey(keyValuePair.Key))
                {
                    result[keyValuePair.Key] = keyValuePair.Value;
                }
                else
                {
                    MergeMethod(result[keyValuePair.Key], keyValuePair.Value);
                }
            }
        }

        private static void MergeMethod(Method result, Method other)
        {
            MergeLines(result.Lines, other.Lines);
            MergeBranches(result.Branches, other.Branches);
        }

        private static void MergeBranches(Branches result, Branches other)
        {
            foreach (var keyValuePair in other)
            {
                if (!result.ContainsKey(keyValuePair.Key))
                {
                    result[keyValuePair.Key] = keyValuePair.Value;
                }
                else
                {
                    result[keyValuePair.Key].Hits += keyValuePair.Value.Hits;
                }
            }
        }

        private static void MergeLines(Lines result, Lines other)
        {
            foreach (var keyValuePair in other)
            {
                if (!result.ContainsKey(keyValuePair.Key))
                {
                    result[keyValuePair.Key] = keyValuePair.Value;
                }
                else
                {
                    result[keyValuePair.Key].Hits += keyValuePair.Value.Hits;
                }
            }
        }
    }
}
