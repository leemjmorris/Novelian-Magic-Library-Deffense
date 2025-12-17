#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class Label : Tag
    {
        protected override string m_beginTag => "Label";

        GUIStyle m_LabelStyle;

        GUIStyle labelStyle => m_LabelStyle ??= new GUIStyle(EditorStyles.label);

        public override void OnBegin(MarkupShaderGUI.Context context)
        {
            var args = AttributeUtils.ExtractArgs(context.attribute);
            var argsCount = args.Count;

            var label = "";
            if (argsCount >= 1)
                label = args[0];
            var size = 10;
            if (args.Count >= 2)
                size = (int)AttributeUtils.ParseNumber(args[1]);

            if (argsCount == 1)
                DrawLabel(label);
            else if (argsCount == 2)
                DrawLabel(label, size);
            else if (argsCount == 3)
            {
                var fontStyle = args[2];
                DrawLabel(label, size, fontStyle);
            }
        }

        void DrawLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return;

            GUILayout.Label(label);
        }

        void DrawLabel(string label, int size)
        {
            if (string.IsNullOrEmpty(label))
                return;

            labelStyle.fontSize = size;
            labelStyle.fontStyle = FontStyle.Normal;
            GUILayout.Label(label, labelStyle);
        }

        void DrawLabel(string label, int size, string fontStyleStr)
        {
            if (string.IsNullOrEmpty(label))
                return;

            labelStyle.fontSize = size;
            labelStyle.fontStyle = (FontStyle)System.Enum.Parse(typeof(FontStyle), fontStyleStr);
            GUILayout.Label(label, labelStyle);
        }
    }
}

#endif