using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to set up Physics 2D Layer Collision Matrix
/// Run via: Tools > Physics Setup > Configure Layer Collision Matrix
/// </summary>
public class PhysicsLayerSetup : EditorWindow
{
    [MenuItem("Tools/Physics Setup/Configure Layer Collision Matrix")]
    public static void ConfigureLayerCollisionMatrix()
    {
        // Layer indices
        int wallLayer = LayerMask.NameToLayer("Wall");  // Layer 3
        int projectileLayer = LayerMask.NameToLayer("Projectile");  // Layer 6
        int monsterLayer = LayerMask.NameToLayer("Monster");  // Layer 7

        if (wallLayer == -1 || projectileLayer == -1 || monsterLayer == -1)
        {
            Debug.LogError("[PhysicsLayerSetup] Required layers not found! Make sure Wall (3), Projectile (6), and Monster (7) layers exist.");
            return;
        }

        // Configure collision matrix:
        // 1. Projectile should IGNORE Wall (pass through)
        Physics2D.IgnoreLayerCollision(projectileLayer, wallLayer, true);

        // 2. Projectile should COLLIDE with Monster (hit detection)
        Physics2D.IgnoreLayerCollision(projectileLayer, monsterLayer, false);

        // 3. Monster should COLLIDE with Wall (blocked by wall)
        Physics2D.IgnoreLayerCollision(monsterLayer, wallLayer, false);

        Debug.Log("[PhysicsLayerSetup] ✅ Layer collision matrix configured successfully!");
        Debug.Log($"  - Projectile (Layer {projectileLayer}) IGNORES Wall (Layer {wallLayer})");
        Debug.Log($"  - Projectile (Layer {projectileLayer}) COLLIDES with Monster (Layer {monsterLayer})");
        Debug.Log($"  - Monster (Layer {monsterLayer}) COLLIDES with Wall (Layer {wallLayer})");
    }

    [MenuItem("Tools/Physics Setup/Set Projectile Prefabs to Projectile Layer")]
    public static void SetProjectilePrefabsToLayer()
    {
        int projectileLayer = LayerMask.NameToLayer("Projectile");  // Layer 6

        if (projectileLayer == -1)
        {
            Debug.LogError("[PhysicsLayerSetup] Projectile layer (Layer 6) not found!");
            return;
        }

        // Find all projectile prefabs
        string[] prefabPaths = new string[]
        {
            "Assets/Prefabs/Skill"
        };

        int count = 0;

        foreach (string folderPath in prefabPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Only process Projectile prefabs
                if (!path.Contains("Projectile"))
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    // Check if it has a Projectile component
                    Projectile projectileComponent = prefab.GetComponent<Projectile>();

                    if (projectileComponent != null)
                    {
                        // Set layer
                        prefab.layer = projectileLayer;
                        EditorUtility.SetDirty(prefab);
                        count++;

                        Debug.Log($"[PhysicsLayerSetup] Set {prefab.name} to Layer {projectileLayer} (Projectile)");
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PhysicsLayerSetup] ✅ Updated {count} projectile prefabs to Layer {projectileLayer} (Projectile)");
    }

    [MenuItem("Tools/Physics Setup/Verify Physics Setup")]
    public static void VerifyPhysicsSetup()
    {
        int wallLayer = LayerMask.NameToLayer("Wall");
        int projectileLayer = LayerMask.NameToLayer("Projectile");
        int monsterLayer = LayerMask.NameToLayer("Monster");

        Debug.Log("===== Physics 2D Layer Setup Verification =====");
        Debug.Log($"Wall Layer: {wallLayer}");
        Debug.Log($"Projectile Layer: {projectileLayer}");
        Debug.Log($"Monster Layer: {monsterLayer}");
        Debug.Log("");
        Debug.Log($"Projectile-Wall Collision: {!Physics2D.GetIgnoreLayerCollision(projectileLayer, wallLayer)} (should be FALSE - ignored)");
        Debug.Log($"Projectile-Monster Collision: {!Physics2D.GetIgnoreLayerCollision(projectileLayer, monsterLayer)} (should be TRUE - collide)");
        Debug.Log($"Monster-Wall Collision: {!Physics2D.GetIgnoreLayerCollision(monsterLayer, wallLayer)} (should be TRUE - collide)");
    }
}
