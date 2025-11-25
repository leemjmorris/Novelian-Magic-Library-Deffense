using UnityEngine;
using UnityEditor;

public class CreateProjectileTemplatePrefab
{
    [MenuItem("Tools/Create ProjectileTemplate Prefab")]
    public static void CreatePrefab()
    {
        // Find ProjectileTemplate in scene
        GameObject obj = GameObject.Find("ProjectileTemplate");

        if (obj == null)
        {
            Debug.LogError("ProjectileTemplate GameObject not found in scene!");
            return;
        }

        // Save as prefab
        string prefabPath = "Assets/Prefabs/Skill/ProjectileTemplate.prefab";
        bool success;
        PrefabUtility.SaveAsPrefabAsset(obj, prefabPath, out success);

        if (success)
        {
            Debug.Log($"ProjectileTemplate prefab created at {prefabPath}");

            // Delete from scene
            Object.DestroyImmediate(obj);
            Debug.Log("ProjectileTemplate removed from scene");
        }
        else
        {
            Debug.LogError("Failed to create ProjectileTemplate prefab");
        }
    }
}
