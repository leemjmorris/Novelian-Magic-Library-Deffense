#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GraphicsCat.MarkupShaderGUIInternal.Experimental
{
    internal class IntEnum : MaterialPropertyDrawer
    {
        private readonly string[] m_Names;

        public IntEnum(params string[] names)
        {
            m_Names = names;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Float && prop.type != MaterialProperty.PropType.Int)
            {
                EditorGUI.LabelField(position, label, "Use with Float or Int only.");
                return;
            }

            // Preserve previous mixed value state
            EditorGUI.showMixedValue = prop.hasMixedValue;

            int currentValue = PropertyUtils.GetAsInt(prop);
            if (currentValue < 0 || currentValue >= m_Names.Length)
                currentValue = 0;

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.Popup(position, label, currentValue, m_Names);
            if (EditorGUI.EndChangeCheck())
            {
                // Apply to all selected materials
                PropertyUtils.SetAsInt(prop, newValue);
            }

            // Reset mixed value state
            EditorGUI.showMixedValue = false;
        }
    }
}

#endif