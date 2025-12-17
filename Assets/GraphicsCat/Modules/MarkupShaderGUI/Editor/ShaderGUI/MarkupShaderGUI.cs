#if UNITY_EDITOR

using GraphicsCat.MarkupShaderGUIInternal;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GraphicsCat
{
    public class MarkupShaderGUI : ShaderGUI
    {
        public class Context
        {
            public Shader shader;
            public MaterialEditor materialEditor;
            public Material[] materials;
            public MaterialProperty[] materialProperties;
            public MaterialProperty materialProperty;
            public string[] attributes;
            public string attribute;
        }

        private Context m_Context = new();

        private MarkupShaderGUIInternal.RenderSettings m_RenderSettingsTag = new();
        private Foldout m_FoldoutTag = new();
        private ShowIf m_ShowIfTag = new();
        private EnableIf m_EnableIfTag = new();
        private Separator m_SeparatorTag = new();
        private Label m_LabelTag = new();
        private MiniTextureWithColor m_MiniTextureWithColorTag = new();
        private LightmapEmissionFlags m_LightmapEmissionFlags = new();

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            Material material = materialEditor.target as Material;
            if (material == null)
                return;

            Shader shader = material.shader;
            if (shader == null)
                return;

            m_Context.shader = shader;
            m_Context.materialEditor = materialEditor;
            m_Context.materials = materialEditor.targets.Cast<Material>().ToArray();
            m_Context.materialProperties = materialProperties;

            GUILayout.Space(2f);

            bool hasRenderSettings = false;
            for (int propIndex = 0; propIndex < materialProperties.Length; propIndex++)
            {
                m_Context.materialProperty = materialProperties[propIndex];
                m_Context.attributes = shader.GetPropertyAttributes(propIndex);
                foreach (var attribute in m_Context.attributes)
                {
                    m_Context.attribute = attribute;

                    if (attribute.IndexOf("RenderSettings") != -1)
                    {
                        hasRenderSettings = true;
                        m_RenderSettingsTag.InitNewMaterials(m_Context);
                    }

                    m_FoldoutTag.Process(m_Context);
                    m_ShowIfTag.Process(m_Context);
                    m_EnableIfTag.Process(m_Context);
                    m_MiniTextureWithColorTag.Process(m_Context);

                    var hideInFoldout = m_FoldoutTag.inScope && !m_FoldoutTag.state;
                    var hideInShowIf = m_ShowIfTag.inScope && !m_ShowIfTag.state;
                    if (hideInFoldout || hideInShowIf)
                        continue;

                    if (m_EnableIfTag.inScope)
                        GUI.enabled = m_EnableIfTag.state;

                    m_RenderSettingsTag.Process(m_Context);
                    m_SeparatorTag.Process(m_Context);
                    m_LabelTag.Process(m_Context);
                    m_LightmapEmissionFlags.Process(m_Context);

                    GUI.enabled = true;
                }

                if (m_RenderSettingsTag.inScope)
                    continue;

                if (m_FoldoutTag.inScope && !m_FoldoutTag.state)
                    continue;

                if (m_ShowIfTag.inScope && !m_ShowIfTag.state)
                    continue;

                ShaderPropertyFlags propFlags = shader.GetPropertyFlags(propIndex);
                if ((propFlags & ShaderPropertyFlags.HideInInspector) != 0)
                    continue;

                if (m_EnableIfTag.inScope)
                    GUI.enabled = m_EnableIfTag.state;

                if (m_MiniTextureWithColorTag.inScope && m_MiniTextureWithColorTag.state)
                {
                    var isTextureType = (m_Context.materialProperty.type == MaterialProperty.PropType.Texture);
                    if (isTextureType)
                    {
                        if (propIndex < materialProperties.Length - 1)
                        {
                            var nextPropIndex = propIndex + 1;
                            var nextProp = materialProperties[nextPropIndex];
                            var nextPropIsColorType = (nextProp.type == MaterialProperty.PropType.Color);
                            if (nextPropIsColorType)
                                m_MiniTextureWithColorTag.Draw(m_Context.materialProperty, materialEditor);
                        }
                    }
                }
                else
                {
                    var guiContent = GUIUtils.TempContent(m_Context.materialProperty.displayName);
                    materialEditor.ShaderProperty(m_Context.materialProperty, guiContent);
                }
            }

            if (hasRenderSettings == false)
            {
                materialEditor.RenderQueueField();
                materialEditor.DoubleSidedGIField();
            }

            GUI.enabled = true;
        }
    }
}

#endif