#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GraphicsCat
{
    [CustomPropertyDrawer(typeof(InspectorDisplayNameAttribute))]
    public class InspectorDisplayNameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (InspectorDisplayNameAttribute)attribute;
            var content = string.IsNullOrEmpty(attr.tooltip)
                ? new GUIContent(attr.displayName)
                : new GUIContent(attr.displayName, attr.tooltip);

            EditorGUI.BeginProperty(position, content, property);
            EditorGUI.PropertyField(position, property, content, true); // 'true' allows drawing children
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}

#endif
