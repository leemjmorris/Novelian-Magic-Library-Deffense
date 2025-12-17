#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class MiniTextureWithColor : Tag
    {
        protected override string m_beginTag => "BeginMiniTextureWithColor";
        protected override string m_EndTag => "EndMiniTextureWithColor";

        public override void OnBegin(MarkupShaderGUI.Context context)
        {
            state = true;
        }

        public override void OnEnd()
        {
            state = false;
        }

        public void Draw(MaterialProperty prop, MaterialEditor editor)
        {
            var material = editor.target as Material;
            var shader = material.shader;

            var texPropIndex = shader.FindPropertyIndex(prop.name);
            var texPropDisplayName = prop.displayName;

            // single line texture and color
            EditorGUILayout.BeginHorizontal();
            {
                var controlRect = EditorGUILayout.GetControlRect(true, 20f, EditorStyles.layerMaskField);
                editor.TexturePropertyMiniThumbnail(controlRect, prop, "", texPropDisplayName);

                var displayNameRect = controlRect;
                displayNameRect.x += 31;
                var labelGUIConent = GUIUtils.TempContent(prop.displayName);
                EditorGUI.LabelField(displayNameRect, labelGUIConent);

                var colorPropIndex = texPropIndex + 1;
                if (colorPropIndex < shader.GetPropertyCount())
                {
                    if (shader.GetPropertyType(colorPropIndex) == ShaderPropertyType.Color)
                    {
                        var offset = Mathf.Max(controlRect.width * 0.425f, 122f);
                        var colorRect = controlRect;
                        colorRect.x += offset;
                        colorRect.width -= offset;

                        string colorPropName = shader.GetPropertyName(colorPropIndex);
                        var colorProp = MaterialEditor.GetMaterialProperty(editor.targets, colorPropName);
                        editor.ColorProperty(colorRect, colorProp, GUIContent.none.text);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // texture uv scale offset
            ShaderPropertyFlags shaderPropFlags = shader.GetPropertyFlags(texPropIndex);
            if ((shaderPropFlags & ShaderPropertyFlags.NoScaleOffset) == 0)
            {
                // GUI.enabled = prop.textureValue != null;
                editor.TextureScaleOffsetProperty(prop);
                // GUI.enabled = true;
            }
        }
    }
}

#endif