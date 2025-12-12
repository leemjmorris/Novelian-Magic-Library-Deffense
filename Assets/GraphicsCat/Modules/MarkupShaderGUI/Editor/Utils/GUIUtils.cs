#if UNITY_EDITOR

using UnityEngine;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class GUIUtils
    {
        static GUIContent s_TempContent = new GUIContent();

        public static GUIContent TempContent(string displayName)
        {
            var guiContent = s_TempContent;
            if (string.IsNullOrEmpty(displayName))
            {
                guiContent.text = "";
                guiContent.tooltip = "";
                return guiContent;
            }

            int start = displayName.IndexOf('[');
            int end = displayName.IndexOf(']');

            if (start >= 0 && end > start)
            {
                // Text is everything before '['
                guiContent.text = displayName.Substring(0, start).Trim();
                // Tooltip is inside [ ... ]
                guiContent.tooltip = displayName.Substring(start + 1, end - start - 1).Trim();
            }
            else
            {
                // No brackets found ¡ú whole string is just text
                guiContent.text = displayName.Trim();
            }

            return guiContent;
        }
    }
}

#endif