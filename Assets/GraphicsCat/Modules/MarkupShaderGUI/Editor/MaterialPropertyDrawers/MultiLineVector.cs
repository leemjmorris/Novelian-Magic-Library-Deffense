#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class MultiLineVector : DrawerBase
    {
        readonly string[] k_DefaultNames = new string[] { "- X", "- Y", "- Z", "- W" };

        float m_Len;
        string[] m_Names = new string[] { "- X", "- Y", "- Z", "- W" };
        float[] m_Min = new float[] { short.MinValue, short.MinValue, short.MinValue, short.MinValue };
        float[] m_Max = new float[] { short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue };

        public MultiLineVector(float len)
        {
            m_Len = (int)len;
        }

        public MultiLineVector(float len, float min, float max)
        {
            m_Len = (int)len;
            m_Min = new float[4] { min, min, min, min };
            m_Max = new float[4] { max, max, max, max };
        }

        public MultiLineVector(float len, string minStr, string maxStr)
        {
            var min = AttributeUtils.ParseNumber(minStr);
            var max = AttributeUtils.ParseNumber(maxStr);

            m_Len = (int)len;
            m_Min = new float[4] { min, min, min, min };
            m_Max = new float[4] { max, max, max, max };
        }

        public MultiLineVector(float len, params object[] args)
        {
            m_Len = (int)len;

            for (int i = 0; i < args.Length && i < 4 * 3; i++)
            {
                var str = args[i].ToString();

                int channel = i / 3;
                int slot = i % 3;

                if (slot == 0)
                {
                    m_Names[channel] = "- " + str;
                }
                else if (slot == 1)
                {
                    str = str.Replace("n", "-").Replace("f", "");
                    m_Min[channel] = Convert.ToSingle(str);
                }
                else if (slot == 2)
                {
                    str = str.Replace("n", "-").Replace("f", "");
                    m_Max[channel] = Convert.ToSingle(str);
                }
            }
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            EditorGUI.LabelField(position, label);

            var hasMixedValueX = false;
            var hasMixedValueY = false;
            var hasMixedValueZ = false;
            var hasMixedValueW = false;
            for (int i = 0, len = editor.targets.Length; i < len - 1; i++)
            {
                var mat1 = editor.targets[i] as Material;
                var mat2 = editor.targets[i + 1] as Material;

                var value1 = mat1.GetVector(prop.name);
                var value2 = mat2.GetVector(prop.name);

                if (value1.x != value2.x)
                    hasMixedValueX = true;
                if (value1.y != value2.y)
                    hasMixedValueY = true;
                if (value1.z != value2.z)
                    hasMixedValueZ = true;
                if (value1.w != value2.w)
                    hasMixedValueW = true;
            }

            var propValue = prop.vectorValue;

            var xChanged = false;
            if (m_Len >= 1)
            {
                EditorGUI.showMixedValue = hasMixedValueX;
                EditorGUI.BeginChangeCheck();
                propValue.x = DrawHelper.DrawFloatRange(m_Names[0], propValue.x, m_Min[0], m_Max[0]);
                xChanged = EditorGUI.EndChangeCheck();
                EditorGUI.showMixedValue = false;
            }

            var yChanged = false;
            if (m_Len >= 2)
            {
                EditorGUI.showMixedValue = hasMixedValueY;
                EditorGUI.BeginChangeCheck();
                propValue.y = DrawHelper.DrawFloatRange(m_Names[1], propValue.y, m_Min[1], m_Max[1]);
                yChanged = EditorGUI.EndChangeCheck();
                EditorGUI.showMixedValue = false;
            }

            var zChanged = false;
            if (m_Len >= 3)
            {
                EditorGUI.showMixedValue = hasMixedValueZ;
                EditorGUI.BeginChangeCheck();
                propValue.z = DrawHelper.DrawFloatRange(m_Names[2], propValue.z, m_Min[2], m_Max[2]);
                zChanged = EditorGUI.EndChangeCheck();
                EditorGUI.showMixedValue = false;
            }

            var wChanged = false;
            if (m_Len >= 4)
            {
                EditorGUI.showMixedValue = hasMixedValueW;
                EditorGUI.BeginChangeCheck();
                propValue.w = DrawHelper.DrawFloatRange(m_Names[3], propValue.w, m_Min[3], m_Max[3]);
                wChanged = EditorGUI.EndChangeCheck();
                EditorGUI.showMixedValue = false;
            }

            foreach (var target in prop.targets)
            {
                var mat = target as Material;

                var newValue = mat.GetVector(prop.name);

                if (xChanged)
                    newValue.x = propValue.x;
                if (yChanged)
                    newValue.y = propValue.y;
                if (zChanged)
                    newValue.z = propValue.z;
                if (wChanged)
                    newValue.w = propValue.w;

                mat.SetVector(prop.name, newValue);
            }
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return 18;
        }
    }
}

#endif