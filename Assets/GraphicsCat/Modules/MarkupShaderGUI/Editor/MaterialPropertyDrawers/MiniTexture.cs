#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class MiniTexture : DrawerBase
    {
        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            var rect = EditorGUILayout.GetControlRect(true, 20f, EditorStyles.layerMaskField);
            editor.TexturePropertyMiniThumbnail(rect, prop, "", label);

            rect.x += 31;
            var guiContent = GUIUtils.TempContent(prop.displayName);
            EditorGUI.LabelField(rect, guiContent);

            var material = editor.target as Material;
            var shader = material.shader;
            var texturePropIndex = shader.FindPropertyIndex(prop.name);
            ShaderPropertyFlags shaderPropFlags = shader.GetPropertyFlags(texturePropIndex);
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