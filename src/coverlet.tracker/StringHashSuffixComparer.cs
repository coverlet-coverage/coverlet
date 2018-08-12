using System.Collections.Generic;

namespace Coverlet.Tracker
{
    internal class StringHashSuffixComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y) => string.Equals(x, y);

        public int GetHashCode(string s)
        {
            if (s == null || s.Length == 0)
                return 0;

            // Hash calculation based on the old implementation of NameTable used in System.Xml
            const int SuffixLength = 8;
            const int Seed = 1031880390;
            int hashCode;
            unchecked
            {
                hashCode = s.Length + Seed;
                int i = s.Length > SuffixLength ? s.Length - SuffixLength : 0;
                for (; i<s.Length; ++i)
                {
                    hashCode += (hashCode << 7) ^ s[i];
                }

                hashCode -= hashCode >> 17;
                hashCode -= hashCode >> 11;
                hashCode -= hashCode >> 5;
            }

            return hashCode;
        }
    }
}
