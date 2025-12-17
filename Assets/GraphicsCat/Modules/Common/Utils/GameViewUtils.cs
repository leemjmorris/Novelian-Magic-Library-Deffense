using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace GraphicsCat
{
    public class GameViewUtils
    {
#if UNITY_EDITOR
        // Don't retrieve it every time, otherwise the Editor will also be slow
        static Type s_GameViewType;
        static EditorWindow s_GameViewWindow;
        static FieldInfo s_GameViewZoomAreaField;

        public static float GetGameViewScale()
        {
            if (s_GameViewType == null)
            {
                s_GameViewType = GetGameViewType();
                s_GameViewZoomAreaField = s_GameViewType.GetField("m_ZoomArea", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            if (s_GameViewWindow == null)
                s_GameViewWindow = GetGameViewWindow(s_GameViewType);

            if (s_GameViewWindow != null)
            {

                var areaObj = s_GameViewZoomAreaField.GetValue(s_GameViewWindow);
                var scaleField = areaObj.GetType().GetField("m_Scale", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var scale = (Vector2)scaleField.GetValue(areaObj);
                return scale.x;
            }

            return 1;
        }

        static Type GetGameViewType()
        {
            Assembly unityEditorAssembly = typeof(EditorWindow).Assembly;
            Type gameViewType = unityEditorAssembly.GetType("UnityEditor.GameView");
            return gameViewType;
        }

        static EditorWindow GetGameViewWindow(Type gameViewType)
        {
            UnityEngine.Object[] obj = Resources.FindObjectsOfTypeAll(gameViewType);
            if (obj.Length > 0)
                return obj[0] as EditorWindow;
            return null;
        }
#else
        public static float GetGameViewScale()
        {
            return 1;
        }
#endif
    }
}
