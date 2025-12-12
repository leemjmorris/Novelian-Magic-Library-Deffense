using System.Collections.Generic;

namespace GraphicsCat
{
    public static class StringUtils
    {
        public static string GetLongestCommonPrefix(IList<string> values)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            string prefix = values[0];
            for (int i = 1; i < values.Count; i++)
            {
                string current = values[i];
                int j = 0;
                while (j < prefix.Length && j < current.Length && prefix[j] == current[j])
                    j++;

                prefix = prefix.Substring(0, j);
                if (prefix.Length == 0)
                    break;
            }

            return prefix;
        }
    }
}
