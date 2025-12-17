#if UNITY_EDITOR

using UnityEditor;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class DrawerBase : MaterialPropertyDrawer
    {
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return -2;
        }
    }
}

#endif