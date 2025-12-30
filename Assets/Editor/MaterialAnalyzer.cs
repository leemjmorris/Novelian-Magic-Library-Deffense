using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MaterialAnalyzer : EditorWindow
{
    private Vector2 scrollPosition;
    private List<MaterialInfo> usedMaterials = new List<MaterialInfo>();
    private bool analyzed = false;
    private bool showOnlyNonFlatKit = true;

    private class MaterialInfo
    {
        public Material material;
        public string shaderName;
        public List<string> usedByObjects = new List<string>();
        public bool isParticle;
        public bool isFlatKit;
        public bool hasTexture;
    }

    [MenuItem("Tools/FlatKit/Material Analyzer")]
    public static void ShowWindow()
    {
        GetWindow<MaterialAnalyzer>("Material Analyzer");
    }

    private void OnGUI()
    {
        GUILayout.Label("3D Object Material Analyzer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Analyze Current Scene", GUILayout.Height(30)))
        {
            AnalyzeScene();
        }

        if (GUILayout.Button("Analyze All Build Scenes", GUILayout.Height(30)))
        {
            AnalyzeAllScenes();
        }

        GUILayout.Space(10);

        if (analyzed)
        {
            showOnlyNonFlatKit = EditorGUILayout.Toggle("Show Only Non-FlatKit", showOnlyNonFlatKit);

            var displayList = showOnlyNonFlatKit
                ? usedMaterials.Where(m => !m.isFlatKit && !m.isParticle).ToList()
                : usedMaterials;

            GUILayout.Label($"Total Materials Found: {usedMaterials.Count}", EditorStyles.boldLabel);
            GUILayout.Label($"Non-FlatKit 3D Materials: {usedMaterials.Count(m => !m.isFlatKit && !m.isParticle)}", EditorStyles.label);
            GUILayout.Label($"Particle Materials: {usedMaterials.Count(m => m.isParticle)}", EditorStyles.label);
            GUILayout.Label($"Already FlatKit: {usedMaterials.Count(m => m.isFlatKit)}", EditorStyles.label);

            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var info in displayList)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(info.material, typeof(Material), false);

                string tag = "";
                if (info.isParticle) tag = "[Particle]";
                else if (info.isFlatKit) tag = "[FlatKit]";
                else if (info.hasTexture) tag = "[Has Texture]";

                if (!string.IsNullOrEmpty(tag))
                {
                    GUILayout.Label(tag, EditorStyles.miniLabel, GUILayout.Width(80));
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Shader:", info.shaderName, EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Used by:", string.Join(", ", info.usedByObjects.Take(3)) +
                    (info.usedByObjects.Count > 3 ? $" +{info.usedByObjects.Count - 3} more" : ""),
                    EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            if (GUILayout.Button("Export List to Console", GUILayout.Height(25)))
            {
                ExportToConsole();
            }
        }
    }

    private void AnalyzeScene()
    {
        usedMaterials.Clear();
        var materialDict = new Dictionary<Material, MaterialInfo>();

        // MeshRenderer
        foreach (var renderer in FindObjectsOfType<MeshRenderer>(true))
        {
            ProcessRenderer(renderer, materialDict);
        }

        // SkinnedMeshRenderer
        foreach (var renderer in FindObjectsOfType<SkinnedMeshRenderer>(true))
        {
            ProcessRenderer(renderer, materialDict);
        }

        usedMaterials = materialDict.Values
            .OrderBy(m => m.isParticle)
            .ThenBy(m => m.isFlatKit)
            .ThenBy(m => m.shaderName)
            .ToList();

        analyzed = true;
        Debug.Log($"[MaterialAnalyzer] Found {usedMaterials.Count} materials in current scene");
    }

    private void AnalyzeAllScenes()
    {
        usedMaterials.Clear();
        var materialDict = new Dictionary<Material, MaterialInfo>();

        // Get all scenes in build settings
        var scenePaths = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToList();

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

        foreach (var scenePath in scenePaths)
        {
            if (string.IsNullOrEmpty(scenePath)) continue;

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            foreach (var renderer in FindObjectsOfType<MeshRenderer>(true))
            {
                ProcessRenderer(renderer, materialDict);
            }

            foreach (var renderer in FindObjectsOfType<SkinnedMeshRenderer>(true))
            {
                ProcessRenderer(renderer, materialDict);
            }
        }

        // Return to original scene
        if (!string.IsNullOrEmpty(currentScene))
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(currentScene);
        }

        usedMaterials = materialDict.Values
            .OrderBy(m => m.isParticle)
            .ThenBy(m => m.isFlatKit)
            .ThenBy(m => m.shaderName)
            .ToList();

        analyzed = true;
        Debug.Log($"[MaterialAnalyzer] Found {usedMaterials.Count} materials across {scenePaths.Count} scenes");
    }

    private void ProcessRenderer(Renderer renderer, Dictionary<Material, MaterialInfo> dict)
    {
        if (renderer == null) return;

        foreach (var mat in renderer.sharedMaterials)
        {
            if (mat == null) continue;

            if (!dict.ContainsKey(mat))
            {
                var info = new MaterialInfo
                {
                    material = mat,
                    shaderName = mat.shader != null ? mat.shader.name : "None",
                    isParticle = IsParticleShader(mat.shader),
                    isFlatKit = IsFlatKitShader(mat.shader),
                    hasTexture = HasMainTexture(mat)
                };
                dict[mat] = info;
            }

            string objPath = GetGameObjectPath(renderer.gameObject);
            if (!dict[mat].usedByObjects.Contains(objPath))
            {
                dict[mat].usedByObjects.Add(objPath);
            }
        }
    }

    private bool IsParticleShader(Shader shader)
    {
        if (shader == null) return false;
        string name = shader.name.ToLower();
        return name.Contains("particle") || name.Contains("vfx") || name.Contains("effect");
    }

    private bool IsFlatKitShader(Shader shader)
    {
        if (shader == null) return false;
        return shader.name.StartsWith("FlatKit");
    }

    private bool HasMainTexture(Material mat)
    {
        if (mat == null) return false;

        string[] textureProps = { "_MainTex", "_BaseMap", "_Albedo", "_Diffuse" };
        foreach (var prop in textureProps)
        {
            if (mat.HasProperty(prop) && mat.GetTexture(prop) != null)
            {
                return true;
            }
        }
        return false;
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        int depth = 0;
        while (parent != null && depth < 2)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
            depth++;
        }
        return path;
    }

    private void ExportToConsole()
    {
        Debug.Log("=== Material Analysis Report ===");
        Debug.Log($"Total: {usedMaterials.Count} | Non-FlatKit 3D: {usedMaterials.Count(m => !m.isFlatKit && !m.isParticle)} | Particle: {usedMaterials.Count(m => m.isParticle)} | FlatKit: {usedMaterials.Count(m => m.isFlatKit)}");
        Debug.Log("");
        Debug.Log("--- Non-FlatKit 3D Materials (Conversion Targets) ---");

        foreach (var info in usedMaterials.Where(m => !m.isFlatKit && !m.isParticle))
        {
            string texInfo = info.hasTexture ? " [Has Texture]" : "";
            Debug.Log($"  {info.material.name} | Shader: {info.shaderName}{texInfo}");
        }

        Debug.Log("");
        Debug.Log("--- Particle Materials (Keep As-Is) ---");
        foreach (var info in usedMaterials.Where(m => m.isParticle))
        {
            Debug.Log($"  {info.material.name} | Shader: {info.shaderName}");
        }
    }
}
