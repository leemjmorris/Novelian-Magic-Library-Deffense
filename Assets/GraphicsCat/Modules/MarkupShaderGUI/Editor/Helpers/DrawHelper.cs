#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class DrawHelper
    {
        static readonly Color k_LineColorLight = new Color(0.8f, 0.8f, 0.8f, 1);
        static readonly Color k_LineColorDark = new Color(0.8f, 0.8f, 0.8f, 1) * 2.7f;

        public static void DrawLine()
        {
            var previousColor = GUI.color;
            GUI.color = EditorGUIUtility.isProSkin ? k_LineColorDark : k_LineColorLight;
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = previousColor;
        }

        public static void DrawSeparator()
        {
            const int SpacePixelds = 3;
            GUILayout.Space(SpacePixelds);
            DrawHelper.DrawLine();
            GUILayout.Space(SpacePixelds);
        }

        public static float DrawFloatRange(string displayName, float val, float min, float max)
        {
            EditorGUILayout.BeginHorizontal();
            var guiConent = GUIUtils.TempContent(displayName);
            GUILayout.Label(guiConent, GUILayout.ExpandWidth(false));
            val = GUILayout.HorizontalSlider(val, min, max, GUILayout.ExpandWidth(true));
            val = EditorGUILayout.FloatField(val, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            return val;
        }

        public static void DrawLeftRight(string label, ref float left, ref float right, float min = 0, float max = 1, float strict = 1)
        {
            EditorGUILayout.BeginHorizontal();
            {
                var guiConent = GUIUtils.TempContent(label);
                GUILayout.Label(guiConent, GUILayout.MinWidth(103.0f));

                left = EditorGUILayout.FloatField(left);
                left = Mathf.Clamp(left, min, max);
                if (strict == 1)
                {
                    if (left > right)
                        left = right;
                }

                right = EditorGUILayout.FloatField(right);
                right = Mathf.Clamp(right, min, max);
                if (strict == 1)
                {
                    if (right < left)
                        right = left;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.MinMaxSlider(ref left, ref right, min, max);
        }
    }
}

#endif