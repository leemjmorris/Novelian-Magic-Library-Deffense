#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public static class AttributeUtils
    {
        public static List<string> ExtractArgs(string input)
        {
            int start = input.IndexOf('(');
            int end = input.LastIndexOf(')');

            if (start < 0 || end < 0 || end <= start)
                return new List<string>();

            string inside = input.Substring(start + 1, end - start - 1);
            var parts = inside.Split(',');
            var result = new List<string>();
            foreach (var part in parts)
            {
                string trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }

            return result;
        }

        public static float ParseNumber(string val)
        {
            val = val.Replace("n", "-").Replace("f", "");
            return Convert.ToSingle(val);
        }

        /// <summary>
        /// Parse Enum attribute strings and return the current enum name.
        /// Supports MaterialProperty of type Float and Int.
        /// </summary>
        public static string GetEnumName(string[] attributes, MaterialProperty prop)
        {
            if (attributes == null || attributes.Length == 0 || prop == null)
                return string.Empty;

            int propValue = PropertyUtils.GetAsInt(prop);

            foreach (var attr in attributes)
            {
                if (attr.StartsWith("Enum(", StringComparison.OrdinalIgnoreCase))
                {
                    string content = attr.Substring(5, attr.Length - 6).Trim();

                    // Case 1: real enum type
                    Type enumType = Type.GetType(content);
                    if (enumType != null && enumType.IsEnum)
                    {
                        string[] names = System.Enum.GetNames(enumType);
                        if (propValue >= 0 && propValue < names.Length)
                            return names[propValue];
                        return string.Empty;
                    }

                    // Case 2: manual pairs (Name,Value,...)
                    string[] tokens = content.Split(',');
                    if (tokens.Length % 2 == 0)
                    {
                        Dictionary<int, string> map = new Dictionary<int, string>();
                        for (int i = 0; i < tokens.Length; i += 2)
                        {
                            string name = tokens[i].Trim();
                            if (int.TryParse(tokens[i + 1], out int val))
                            {
                                map[val] = name;
                            }
                        }

                        if (map.TryGetValue(propValue, out string found))
                            return found;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Parse KeywordEnum attribute strings and return the current enum name.
        /// Supports MaterialProperty of type Float and Int.
        /// </summary>
        public static string GetKeywordEnumName(string[] attributes, MaterialProperty prop)
        {
            if (attributes == null || attributes.Length == 0 || prop == null)
                return string.Empty;

            int propValue = PropertyUtils.GetAsInt(prop);

            foreach (var attr in attributes)
            {
                if (attr.StartsWith("KeywordEnum(", StringComparison.OrdinalIgnoreCase))
                {
                    string content = attr.Substring(12, attr.Length - 13).Trim();
                    string[] tokens = content.Split(',');

                    List<string> names = new List<string>();
                    for (int i = 0; i < tokens.Length; i++)
                        names.Add(tokens[i].Trim());

                    if (propValue >= 0 && propValue < names.Count)
                        return names[propValue];
                }
            }

            return string.Empty;
        }
    }
}

#endif