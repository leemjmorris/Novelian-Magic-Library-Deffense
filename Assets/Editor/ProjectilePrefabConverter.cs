using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// LMJ: Editor tool to convert 3D Projectile prefabs to 2D system
/// Removes 3D components (Rigidbody, SphereCollider, HS_ProjectileMover)
/// Adds 2D components (Rigidbody2D, CircleCollider2D, Projectile script)
/// Sets Layer to "Projectile" and Tag to "Projectile"
/// </summary>
public class ProjectilePrefabConverter : EditorWindow
{
    private string prefabFolder = "Assets/Prefabs/Skill";

    [MenuItem("Tools/Convert Projectile Prefabs to 2D")]
    public static void ShowWindow()
    {
        GetWindow<ProjectilePrefabConverter>("Projectile Converter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Projectile Prefab 2D Converter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "이 도구는 Hovl Studio 3D Projectile Prefab들을 2D 시스템으로 자동 변환합니다:\n\n" +
            "1. 3D 컴포넌트 제거 (Rigidbody, SphereCollider, HS_ProjectileMover)\n" +
            "2. 2D 컴포넌트 추가 (Rigidbody2D, CircleCollider2D)\n" +
            "3. Projectile.cs 스크립트 추가\n" +
            "4. Layer를 'Projectile', Tag를 'Projectile'으로 설정\n" +
            "5. Rigidbody2D 설정 (Gravity Scale=0, Interpolate=Interpolate)",
            MessageType.Info
        );

        GUILayout.Space(10);
        prefabFolder = EditorGUILayout.TextField("Prefab Folder:", prefabFolder);
        GUILayout.Space(10);

        if (GUILayout.Button("Convert All Projectile Prefabs", GUILayout.Height(40)))
        {
            ConvertAllPrefabs();
        }
    }

    private void ConvertAllPrefabs()
    {
        // Find all prefabs in the folder
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", $"No prefabs found in {prefabFolder}", "OK");
            return;
        }

        int converted = 0;
        int skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Only process Projectile prefabs
            if (!path.Contains("Projectile"))
            {
                skipped++;
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"[ProjectilePrefabConverter] Failed to load prefab at {path}");
                skipped++;
                continue;
            }

            // Load prefab for editing
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

            if (prefabInstance == null)
            {
                Debug.LogWarning($"[ProjectilePrefabConverter] Failed to load prefab contents for {path}");
                skipped++;
                continue;
            }

            bool modified = false;

            try
            {
                // Step 1: Remove 3D components
                Rigidbody rb = prefabInstance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    DestroyImmediate(rb);
                    modified = true;
                    Debug.Log($"[ProjectilePrefabConverter] Removed Rigidbody from {prefab.name}");
                }

                SphereCollider sphereCol = prefabInstance.GetComponent<SphereCollider>();
                if (sphereCol != null)
                {
                    DestroyImmediate(sphereCol);
                    modified = true;
                    Debug.Log($"[ProjectilePrefabConverter] Removed SphereCollider from {prefab.name}");
                }

                // Remove HS_ProjectileMover script
                MonoBehaviour[] scripts = prefabInstance.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    if (script != null && script.GetType().Name == "HS_ProjectileMover")
                    {
                        DestroyImmediate(script);
                        modified = true;
                        Debug.Log($"[ProjectilePrefabConverter] Removed HS_ProjectileMover from {prefab.name}");
                    }
                }

                // Step 2: Add 2D components
                Rigidbody2D rb2d = prefabInstance.GetComponent<Rigidbody2D>();
                if (rb2d == null)
                {
                    rb2d = prefabInstance.AddComponent<Rigidbody2D>();
                    rb2d.gravityScale = 0f;
                    rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
                    rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;
                    modified = true;
                    Debug.Log($"[ProjectilePrefabConverter] Added Rigidbody2D to {prefab.name}");
                }

                CircleCollider2D circleCol2d = prefabInstance.GetComponent<CircleCollider2D>();
                if (circleCol2d == null)
                {
                    circleCol2d = prefabInstance.AddComponent<CircleCollider2D>();
                    circleCol2d.radius = 0.15f; // Default radius
                    circleCol2d.isTrigger = true; // For trigger-based collision
                    modified = true;
                    Debug.Log($"[ProjectilePrefabConverter] Added CircleCollider2D to {prefab.name}");
                }

                // Step 3: Add Projectile script
                Projectile projectileScript = prefabInstance.GetComponent<Projectile>();
                if (projectileScript == null)
                {
                    projectileScript = prefabInstance.AddComponent<Projectile>();
                    modified = true;
                    Debug.Log($"[ProjectilePrefabConverter] Added Projectile script to {prefab.name}");
                }

                // Step 4: Set Layer and Tag
                int projectileLayer = LayerMask.NameToLayer("Projectile");
                if (projectileLayer != -1 && prefabInstance.layer != projectileLayer)
                {
                    prefabInstance.layer = projectileLayer;
                    modified = true;
                    Debug.Log($"[ProjectilePrefabConverter] Set layer to 'Projectile' for {prefab.name}");
                }

                // Note: Tags must exist in TagManager first
                try
                {
                    prefabInstance.tag = "Projectile";
                    modified = true;
                    Debug.Log($"[ProjectilePrefabConverter] Set tag to 'Projectile' for {prefab.name}");
                }
                catch (UnityException)
                {
                    Debug.LogWarning($"[ProjectilePrefabConverter] 'Projectile' tag not found. Please add it to TagManager.");
                }

                if (modified)
                {
                    // Save the modified prefab
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                    converted++;
                    Debug.Log($"[ProjectilePrefabConverter] ✅ Successfully converted {prefab.name}");
                }
                else
                {
                    skipped++;
                }
            }
            finally
            {
                // Always unload prefab contents
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Conversion Complete",
            $"Converted: {converted}\nSkipped: {skipped}\n\n" +
            "Note: If 'Projectile' tag warning appeared, please add the tag manually in TagManager.",
            "OK"
        );

        Debug.Log($"[ProjectilePrefabConverter] ✅ Conversion complete! Converted: {converted}, Skipped: {skipped}");
    }
}
