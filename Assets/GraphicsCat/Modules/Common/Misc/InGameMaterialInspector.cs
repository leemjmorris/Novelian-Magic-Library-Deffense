using System.Collections.Generic;
using UnityEngine;

namespace GraphicsCat
{
    public class InGameMaterialInspector : MonoBehaviour, IMGUIDockable
    {
        Renderer targetRenderer;

        [Range(0.1f, 1.0f)] public float width = 0.3f;
        [Range(0.1f, 1.0f)] public float height = 0.5f;

        Material m_Material;
        Vector2 m_ScrollPosition;
        Dictionary<string, bool> m_ColorFoldoutStates = new Dictionary<string, bool>();

        void OnEnable()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            if (targetRenderer != null)
                m_Material = targetRenderer.material;

            IMGUIDock.topRight.DockGUI(this);
        }

        void OnDisable()
        {
            Destroy(m_Material);
        }

        public void OnDockGUI()
        {
            if (m_Material == null)
            {
                GUILayout.Label("No material found on the target renderer.");
                return;
            }

            float inspectorWidth = width * Screen.width / IMGUIUtils.scaleWithScreenSize;
            float inspectorHeight = height * Screen.height / IMGUIUtils.scaleWithScreenSize;

            GUILayout.BeginVertical("box", GUILayout.Width(inspectorWidth));

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, false, true, GUILayout.Height(inspectorHeight));

            for (int propIndex = 0; propIndex < m_Material.shader.GetPropertyCount(); propIndex++)
            {
                string propName = m_Material.shader.GetPropertyName(propIndex);
                string propDesc = m_Material.shader.GetPropertyDescription(propIndex);
                UnityEngine.Rendering.ShaderPropertyType propType = m_Material.shader.GetPropertyType(propIndex);

                string[] attributes = m_Material.shader.GetPropertyAttributes(propIndex);
                foreach (var attr in attributes)
                {
                    if (attr.StartsWith("BeginFoldout"))
                    {
                        string foldoutLabel = "# " + GetAttributeValue(attr, "BeginFoldout");
                        GUILayout.Label(foldoutLabel, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                    }
                    else if (attr.StartsWith("EndFoldout"))
                    {
                        GUILayout.Space(10);
                    }
                    else if (attr.StartsWith("Heade("))
                    {
                        string headerName = GetAttributeValue(attr, "Header");
                        if (!string.IsNullOrEmpty(headerName))
                        {
                            GUILayout.Space(10);
                            GUILayout.Label(headerName, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                        }
                    }
                    else if (attr.StartsWith("Space"))
                    {
                        string spaceValueStr = GetAttributeValue(attr, "Space");
                        if (float.TryParse(spaceValueStr, out float spaceValue))
                        {
                            GUILayout.Space(spaceValue);
                        }
                    }
                }

                var propFlags = m_Material.shader.GetPropertyFlags(propIndex);
                var hideInInspector = (propFlags & UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector) != 0;
                if (hideInInspector)
                    continue;

                string keywordName = GetKeywordFromToggleAttribute(m_Material.shader, propIndex, propName);
                bool isToggle = !string.IsNullOrEmpty(keywordName);

                if (isToggle)
                {
                    GUILayout.BeginHorizontal();
                    float originalValue = m_Material.GetFloat(propName);
                    bool originalBool = originalValue > 0.5f;
                    bool newBool = GUILayout.Toggle(originalBool, propDesc);
                    if (newBool != originalBool)
                    {
                        m_Material.SetFloat(propName, newBool ? 1.0f : 0.0f);
                        if (newBool)
                        {
                            m_Material.EnableKeyword(keywordName);
                        }
                        else
                        {
                            m_Material.DisableKeyword(keywordName);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    switch (propType)
                    {
                        case UnityEngine.Rendering.ShaderPropertyType.Color:
                            {
                                if (!m_ColorFoldoutStates.ContainsKey(propName))
                                    m_ColorFoldoutStates[propName] = false;

                                Color originalColor = m_Material.GetColor(propName);

                                GUILayout.BeginHorizontal();
                                {
                                    var colorR = originalColor.r.ToString("0.00");
                                    var colorG = originalColor.g.ToString("0.00");
                                    var colorB = originalColor.b.ToString("0.00");
                                    var colorA = originalColor.a.ToString("0.00");
                                    var colorRGBA = $"({colorR}, {colorG}, {colorB}, {colorA})";
                                    var colorLabel = propDesc + " " + colorRGBA;
                                    GUILayout.Label(colorLabel, GUILayout.ExpandWidth(false));

                                    // var previousColor = GUI.color;
                                    // GUI.color = originalColor;
                                    // if (GUILayout.Button(m_colorFoldoutStates[propertyName] ? "-" : "+"))
                                    // m_colorFoldoutStates[propertyName] = !m_colorFoldoutStates[propertyName];
                                    // GUI.color = previousColor;

                                    if (GUILayout.Button(""))
                                        m_ColorFoldoutStates[propName] = !m_ColorFoldoutStates[propName];

                                    Rect buttonRect = GUILayoutUtility.GetLastRect();
                                    buttonRect.x += 1;
                                    buttonRect.y += 1;
                                    buttonRect.width -= 2;
                                    buttonRect.height -= 2;
                                    var image = IMGUIColorTextureGenerator.GetTexture(originalColor);
                                    GUI.DrawTexture(buttonRect, image, ScaleMode.StretchToFill);
                                    // public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color color, float borderWidth, float borderRadius)
                                    // GUI.DrawTexture(buttonRect, image, ScaleMode.StretchToFill, false, 1, Color.white, 1, 0);

                                    if (GUILayout.Button(m_ColorFoldoutStates[propName] ? "-" : "+", GUILayout.Width(20)))
                                        m_ColorFoldoutStates[propName] = !m_ColorFoldoutStates[propName];
                                }
                                GUILayout.EndHorizontal();

                                if (m_ColorFoldoutStates[propName])
                                {
                                    GUILayout.BeginVertical();
                                    {
                                        const int indentSize = 15;

                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(indentSize);
                                        GUILayout.Label("- R ", GUILayout.ExpandWidth(false));
                                        originalColor.r = GUILayout.HorizontalSlider(originalColor.r, 0.0f, 1.0f);
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(indentSize);
                                        GUILayout.Label("- G ", GUILayout.ExpandWidth(false));
                                        originalColor.g = GUILayout.HorizontalSlider(originalColor.g, 0.0f, 1.0f);
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(indentSize);
                                        GUILayout.Label("- B ", GUILayout.ExpandWidth(false));
                                        originalColor.b = GUILayout.HorizontalSlider(originalColor.b, 0.0f, 1.0f);
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(indentSize);
                                        GUILayout.Label("- A ", GUILayout.ExpandWidth(false));
                                        originalColor.a = GUILayout.HorizontalSlider(originalColor.a, 0.0f, 1.0f);
                                        GUILayout.EndHorizontal();

                                        m_Material.SetColor(propName, originalColor);
                                    }
                                    GUILayout.EndVertical();
                                }
                                break;
                            }
                        case UnityEngine.Rendering.ShaderPropertyType.Float:
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(propDesc);
                                float originalValue = m_Material.GetFloat(propName);
                                float newValue = GetFloatField(originalValue);
                                if (Mathf.Abs(newValue - originalValue) > 0.001f)
                                {
                                    m_Material.SetFloat(propName, newValue);
                                }
                                GUILayout.EndHorizontal();
                                break;
                            }
                        case UnityEngine.Rendering.ShaderPropertyType.Range:
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(propDesc, GUILayout.ExpandWidth(false));
                                float originalValue = m_Material.GetFloat(propName);
                                Vector2 limits = m_Material.shader.GetPropertyRangeLimits(propIndex);
                                GUILayout.Label(originalValue.ToString("0.00"), GUILayout.ExpandWidth(false));
                                float newValue = GUILayout.HorizontalSlider(originalValue, limits.x, limits.y);
                                if (Mathf.Abs(newValue - originalValue) > 0.01f)
                                    m_Material.SetFloat(propName, newValue);
                                GUILayout.EndHorizontal();
                                break;
                            }
                        case UnityEngine.Rendering.ShaderPropertyType.Vector:
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(propDesc);
                                Vector4 originalVector = m_Material.GetVector(propName);
                                GUILayout.BeginVertical();
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("X", GUILayout.Width(20)); originalVector.x = GetFloatField(originalVector.x);
                                GUILayout.Label("Y", GUILayout.Width(20)); originalVector.y = GetFloatField(originalVector.y);
                                GUILayout.Label("Z", GUILayout.Width(20)); originalVector.z = GetFloatField(originalVector.z);
                                GUILayout.Label("W", GUILayout.Width(20)); originalVector.w = GetFloatField(originalVector.w);
                                GUILayout.EndHorizontal();

                                m_Material.SetVector(propName, originalVector);
                                GUILayout.EndVertical();
                                GUILayout.EndHorizontal();
                                break;
                            }
                        case UnityEngine.Rendering.ShaderPropertyType.Texture:
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(propDesc, GUILayout.ExpandWidth(true));
                                Texture tex = m_Material.GetTexture(propName);
                                if (tex != null)
                                {
                                    GUILayout.Box(tex, GUILayout.Width(16), GUILayout.Height(16));
                                }
                                else
                                {
                                    GUILayout.Label("None (Texture)");
                                }
                                GUILayout.EndHorizontal();
                                break;
                            }
                        default:
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(propDesc, GUILayout.Width(150));
                                GUILayout.Label("Unsupported Type");
                                GUILayout.EndHorizontal();
                                break;
                            }
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private string GetKeywordFromToggleAttribute(Shader shader, int propertyIndex, string propertyName)
        {
            string[] attributes = shader.GetPropertyAttributes(propertyIndex);
            foreach (var attr in attributes)
            {
                if (attr.StartsWith("Toggle(") || attr.StartsWith("MaterialToggle("))
                {
                    string keywordName = GetAttributeValue(attr, "Toggle") ?? GetAttributeValue(attr, "MaterialToggle");
                    return keywordName;
                }
                else if (attr.Equals("Toggle") || attr.Equals("MaterialToggle"))
                {
                    return propertyName.ToUpperInvariant() + "_ON";
                }
            }
            return null;
        }

        private string GetAttributeValue(string attributeString, string attributeName)
        {
            int start = attributeString.IndexOf(attributeName + "(") + attributeName.Length + 1;
            int end = attributeString.LastIndexOf(')');
            if (end > start)
            {
                string value = attributeString.Substring(start, end - start).Trim();
                if (value.StartsWith("\"") && value.EndsWith("\""))
                    return value.Substring(1, value.Length - 2);
                return value;
            }
            return null;
        }

        float GetFloatField(float value)
        {
            string strValue = GUILayout.TextField(value.ToString("0.00"), GUILayout.Width(50));
            if (float.TryParse(strValue, out float result))
                return result;
            return value;
        }
    }
}