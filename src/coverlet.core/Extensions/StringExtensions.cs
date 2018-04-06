namespace Coverlet.Core.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveAttributeSuffix(this string attributeName)
        {
            return attributeName.Replace("Attribute", "");
        }
    }
}