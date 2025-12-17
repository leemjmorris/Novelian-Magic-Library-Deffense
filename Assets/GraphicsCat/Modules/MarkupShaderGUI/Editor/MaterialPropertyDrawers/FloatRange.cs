#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class FloatRange : DrawerBase
    {
        float m_Min;
        float m_Max;

        public FloatRange(float min, float max)
        {
            m_Min = min;
            m_Max = max;
        }

        public FloatRange(string min, string max)
        {
            m_Min = AttributeUtils.ParseNumber(min);
            m_Max = AttributeUtils.ParseNumber(max);
        }

        public FloatRange(float min, string max)
        {
            m_Min = min;
            m_Max = AttributeUtils.ParseNumber(max);
        }

        public FloatRange(string min, float max)
        {
            m_Min = AttributeUtils.ParseNumber(min);
            m_Max = max;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            EditorGUI.showMixedValue = prop.hasMixedValue;

            EditorGUI.BeginChangeCheck();
            var value = DrawHelper.DrawFloatRange(prop.displayName, prop.floatValue, m_Min, m_Max);
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = value;

            EditorGUI.showMixedValue = false;
        }
    }
}

#endif