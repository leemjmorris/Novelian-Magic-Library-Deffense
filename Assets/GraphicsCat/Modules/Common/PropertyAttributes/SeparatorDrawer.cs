#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GraphicsCat
{
    [CustomPropertyDrawer(typeof(SeparatorAttribute))]
    public class SeparatorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var separator = (SeparatorAttribute)attribute;
            var rect = new Rect(position.x, position.y + separator.Height / 2f, position.width, separator.Height);
            EditorGUI.DrawRect(rect, Color.gray);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var separator = (SeparatorAttribute)attribute;
            return separator.Height + 4f; 
        }
    }
}

#endif
