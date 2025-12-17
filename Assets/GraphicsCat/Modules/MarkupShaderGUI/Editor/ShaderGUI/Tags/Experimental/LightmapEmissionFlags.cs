#if UNITY_EDITOR

using UnityEditor;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class LightmapEmissionFlags : Tag
    {
        protected override string m_beginTag => "LightmapEmissionFlags";

        public override void OnBegin(MarkupShaderGUI.Context context)
        {
            // Change the GI emission flag and fix it up with emissive as black if necessary.
            context.materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel - 2, true);
        }
    }
}

#endif