#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class Foldout : Tag
    {
        protected override string m_beginTag => "BeginFoldout";
        protected override string m_EndTag => "EndFoldout";

        string m_FoldoutName = "";
        string m_FoldoutKey = "";

        public override void OnBegin(MarkupShaderGUI.Context context)
        {
            m_FoldoutName = ExtractFoldoutName(context.attribute);

            m_FoldoutKey = context.shader.name + m_FoldoutName;
            state = GetFoldoutState(m_FoldoutKey);

            DrawFoldout(m_FoldoutKey, context);

            if (state == true) // begin foldout space
                GUILayout.Space(4);
        }

        public override void OnEnd()
        {
            if (state == true) // end foldout space
                GUILayout.Space(6);
            else
                GUILayout.Space(2);

            m_FoldoutName = "";
            m_FoldoutKey = "";
            state = true;
        }

        void DrawFoldout(string foldoutKey, MarkupShaderGUI.Context context)
        {
            const float height = 21f;

            Rect foldoutRect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            foldoutRect.xMin -= 29;

            var style = new GUIStyle("ShurikenModuleTitle")
            {
                border = new RectOffset(15, 7, 4, 4),
                fixedHeight = height,
                contentOffset = new Vector2(19, -1), // text offset
                fontSize = 12 // Default is 10
            };

            var foldoutTitle = m_FoldoutName;
            var prop = context.materialProperty;
            var enumName = AttributeUtils.GetKeywordEnumName(context.attributes, prop);
            if (string.IsNullOrEmpty(enumName) == false)
            {
                if (prop.hasMixedValue)
                    foldoutTitle += " - *";
                else
                    foldoutTitle += " - " + enumName;
            }

            // Draw title
            GUI.Label(foldoutRect, foldoutTitle, style);

            // Draw arrow
            var arrowRect = new Rect(foldoutRect.x + 4, foldoutRect.y + foldoutRect.height / 2 - 7, 14f, 14f);
            Event e = Event.current;
            if (e.type == EventType.Repaint)
                EditorStyles.foldout.Draw(arrowRect, false, false, GetFoldoutState(foldoutKey), false);

            // Handle click to toggle foldout
            if (Event.current.type == EventType.MouseDown && foldoutRect.Contains(Event.current.mousePosition))
            {
                var previousState = GetFoldoutState(foldoutKey);
                SetFoldoutState(foldoutKey, !previousState);
                Event.current.Use();
            }
        }

        string ExtractFoldoutName(string attribute)
        {
            var args = AttributeUtils.ExtractArgs(attribute);
            if (args.Count > 0)
                return args[0];
            return "";
        }

        void SetFoldoutState(string foldoutKey, bool state)
        {
            var prefKey = GetPrefKey(foldoutKey);
            EditorPrefs.SetBool(prefKey, state);
        }

        bool GetFoldoutState(string foldoutKey)
        {
            var prefKey = GetPrefKey(foldoutKey);
            return EditorPrefs.GetBool(prefKey, false);
        }

        string GetPrefKey(string foldoutKey)
        {
            return $"{nameof(MarkupShaderGUIInternal)}.{foldoutKey}";
        }
    }
}

#endif