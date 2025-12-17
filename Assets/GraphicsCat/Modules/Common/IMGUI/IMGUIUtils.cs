using UnityEngine;

namespace GraphicsCat
{
    public partial class IMGUIUtils
    {
        public static float scaleWithScreenSize
        {
            get
            {
                if (Application.isMobilePlatform)
                {
                    // Mobile platform Legacy GUI needs scaling to be visible comfortably
                    // return (Screen.dpi / 165) * ScreenUtils.resolutionScale; // it works
                    return (ResolutionUtils.defaultLandscapeHeight / 1080) * ResolutionUtils.resolutionScale;
                }
                else
                {
                    // Editor cannot scale Screen, but will scale GameView
                    return 1.0f / GameViewUtils.GetGameViewScale(); // Use 1.4f/GameViewUtils.GetGameViewScale() to preview mobile
                }
            }
        }

        public static void BeginGUI()
        {
            IMGUICustomSkin.BeginGUI();
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scaleWithScreenSize);
        }

        public static void EndGUI()
        {
            IMGUICustomSkin.EndGUI();
            GUI.matrix = Matrix4x4.identity;
        }
    }

    public partial class IMGUIUtils
    {
        static Color s_LastButtonTextColor_Active;
        static Color s_LastButtonTextColor_Focused;
        static Color s_LastButtonTextColor_Hover;
        static Color s_LastButtonTextColor_Normal;

        public static void SetButtonTextColor(Color color)
        {
            s_LastButtonTextColor_Active = GUI.skin.button.active.textColor;
            s_LastButtonTextColor_Focused = GUI.skin.button.focused.textColor;
            s_LastButtonTextColor_Hover = GUI.skin.button.hover.textColor;
            s_LastButtonTextColor_Normal = GUI.skin.button.normal.textColor;

            GUI.skin.button.active.textColor = color;
            GUI.skin.button.focused.textColor = color;
            GUI.skin.button.hover.textColor = color;
            GUI.skin.button.normal.textColor = color;
        }

        public static void ResetButtonTextColor()
        {
            GUI.skin.button.active.textColor = s_LastButtonTextColor_Active;
            GUI.skin.button.focused.textColor = s_LastButtonTextColor_Focused;
            GUI.skin.button.hover.textColor = s_LastButtonTextColor_Hover;
            GUI.skin.button.normal.textColor = s_LastButtonTextColor_Normal;
        }
    }

    public partial class IMGUIUtils
    {
        static GUIContent s_TempContent = new GUIContent();

        public static GUIContent TempContent(string text, string tooltip = null)
        {
            s_TempContent.text = text;
            s_TempContent.image = null;
            s_TempContent.tooltip = tooltip;
            return s_TempContent;
        }
    }

    public partial class IMGUIUtils
    {
        public static int IntField(int value, params GUILayoutOption[] options)
        {
            string input = GUILayout.TextField(value.ToString(), options);
            if (int.TryParse(input, out int result))
            {
                return result;
            }
            return value; // Keep old value if parse fails
        }
    }
}

