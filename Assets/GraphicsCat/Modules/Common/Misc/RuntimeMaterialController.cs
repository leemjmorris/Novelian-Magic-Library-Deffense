using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace GraphicsCat
{
    public class RuntimeMaterialController : MonoBehaviour, IMGUIDockable
    {
        [System.Serializable]
        public class ControlledProperty
        {
            public string propertyName;
        }

        public List<ControlledProperty> controlledProperties = new List<ControlledProperty>();

        private readonly Dictionary<string, List<Material>> _materialsByProperty = new ();
        private readonly Dictionary<string, ShaderPropertyType> _shaderPropertyTypes = new ();
        private bool _initialized;

        public void OnDockGUI()
        {
            if (_materialsByProperty.Count == 0)
                return;

            foreach (var kv in _materialsByProperty)
            {
                string prop = kv.Key;
                List<Material> mats = kv.Value;
                if (mats == null || mats.Count == 0)
                    continue;

                ShaderPropertyType type = _shaderPropertyTypes[prop];
                Material refMat = mats[0];

                GUILayout.Space(6);
                GUILayout.Label(prop, EditorLike.boldLabel);

                switch (type)
                {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        DrawFloatOrSlider(prop, mats, refMat, type);
                        break;
                    case ShaderPropertyType.Vector:
                        DrawVector(prop, mats, refMat);
                        break;
                    case ShaderPropertyType.Color:
                        DrawColor(prop, mats, refMat);
                        break;
                    case ShaderPropertyType.Texture:
                        GUILayout.Label("Texture control not supported at runtime.");
                        break;
                }
            }
        }

        private void Start()
        {
            IMGUIDock.topRight.DockGUI(this);
            RefreshMaterials();
            _initialized = true;
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _initialized)
                RefreshMaterials();
        }

        private void RefreshMaterials()
        {
            _materialsByProperty.Clear();
            _shaderPropertyTypes.Clear();

            Renderer[] renderers = GameObjectUtils.FindObjectsByType<Renderer>();
            HashSet<Material> allMats = new HashSet<Material>();

            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat != null)
                        allMats.Add(mat);
                }
            }

            foreach (var cp in controlledProperties)
            {
                string propName = cp.propertyName;
                List<Material> matsWithProp = new List<Material>();
                ShaderPropertyType? detectedType = null;

                foreach (var mat in allMats)
                {
                    Shader shader = mat.shader;
                    if (shader == null)
                        continue;

                    if (!ShaderHasProperty(shader, propName))
                        continue;

                    matsWithProp.Add(mat);

                    if (!detectedType.HasValue)
                    {
                        detectedType = GetShaderPropertyType(shader, propName);
                    }
                }

                if (matsWithProp.Count > 0)
                {
                    _materialsByProperty[propName] = matsWithProp;
                    _shaderPropertyTypes[propName] = detectedType ?? ShaderPropertyType.Float;
                }
            }

            Debug.Log($"[RuntimeMaterialController] Found {_materialsByProperty.Count} valid controlled properties.");
        }

        private static bool ShaderHasProperty(Shader shader, string propertyName)
        {
            int count = shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                if (shader.GetPropertyName(i) == propertyName)
                    return true;
            }
            return false;
        }

        private static ShaderPropertyType GetShaderPropertyType(Shader shader, string propertyName)
        {
            int count = shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                if (shader.GetPropertyName(i) == propertyName)
                    return shader.GetPropertyType(i);
            }
            return ShaderPropertyType.Float;
        }

        private void DrawFloatOrSlider(string name, List<Material> mats, Material refMat, ShaderPropertyType type)
        {
            float value = refMat.GetFloat(name);
            float newValue;

            if (type == ShaderPropertyType.Range)
            {
                var propIndex = refMat.shader.FindPropertyIndex(name);
                Vector2 minMax = refMat.shader.GetPropertyRangeLimits(propIndex);
                newValue = GUILayout.HorizontalSlider(value, minMax.x, minMax.y);
                GUILayout.Label(newValue.ToString("0.##"));
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Value", GUILayout.Width(60));
                string str = GUILayout.TextField(value.ToString("0.###"), GUILayout.Width(80));
                GUILayout.EndHorizontal();
                newValue = float.TryParse(str, out float f) ? f : value;
            }

            if (!Mathf.Approximately(newValue, value))
            {
                foreach (var m in mats)
                {
                    if (m && m.shader && ShaderHasProperty(m.shader, name))
                        m.SetFloat(name, newValue);
                }
            }
        }

        private void DrawVector(string name, List<Material> mats, Material refMat)
        {
            Vector4 v = refMat.GetVector(name);
            float x = FloatFieldRow("X", v.x);
            float y = FloatFieldRow("Y", v.y);
            float z = FloatFieldRow("Z", v.z);
            Vector4 newV = new Vector4(x, y, z, v.w);

            if (newV != v)
            {
                foreach (var m in mats)
                {
                    if (m && m.shader && ShaderHasProperty(m.shader, name))
                        m.SetVector(name, newV);
                }
            }
        }

        private void DrawColor(string name, List<Material> mats, Material refMat)
        {
            Color c = refMat.GetColor(name);
            float r = SliderRow("R", c.r);
            float g = SliderRow("G", c.g);
            float b = SliderRow("B", c.b);
            float a = SliderRow("A", c.a);
            Color newC = new Color(r, g, b, a);

            if (newC != c)
            {
                foreach (var m in mats)
                {
                    if (m && m.shader && ShaderHasProperty(m.shader, name))
                        m.SetColor(name, newC);
                }
            }
        }

        private static float FloatFieldRow(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(20));
            string str = GUILayout.TextField(value.ToString("0.###"), GUILayout.Width(60));
            GUILayout.EndHorizontal();
            return float.TryParse(str, out float f) ? f : value;
        }

        private static float SliderRow(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(20));
            float newValue = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.Label(newValue.ToString("0.##"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            return newValue;
        }

        private static class EditorLike
        {
            public static readonly GUIStyle boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        }
    }
}
