using UnityEditor;
using UnityEngine;

namespace GraphicsCat
{
    public class InspectorUtils
    {
#if UNITY_EDITOR
        public static void OpenLockedInspector(Object obj)
        {
            var lastActiveObject = Selection.activeObject;
            Selection.activeObject = obj;

            var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            var inspector = ScriptableObject.CreateInstance(inspectorType) as EditorWindow;
            inspector.Show();

            var isLockedProperty = inspectorType.GetProperty("isLocked");
            isLockedProperty.SetValue(inspector, true);

            Selection.activeObject = lastActiveObject;
        }
#endif
    }
}