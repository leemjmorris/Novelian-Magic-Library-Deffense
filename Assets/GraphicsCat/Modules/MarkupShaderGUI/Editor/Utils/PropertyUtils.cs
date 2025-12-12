#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public static class PropertyUtils
    {
        public static int GetAsInt(MaterialProperty prop)
        {
            return prop.type switch
            {
                MaterialProperty.PropType.Int => prop.intValue,
                MaterialProperty.PropType.Float => Mathf.RoundToInt(prop.floatValue),
                _ => 0
            };
        }

        public static void SetAsInt(MaterialProperty prop, int value)
        {
            switch (prop.type)
            {
                case MaterialProperty.PropType.Int:
                    prop.intValue = value;
                    break;
                case MaterialProperty.PropType.Float:
                    prop.floatValue = value;
                    break;
                default:
                    // Unsupported type, do nothing
                    break;
            }
        }
    }
}

#endif