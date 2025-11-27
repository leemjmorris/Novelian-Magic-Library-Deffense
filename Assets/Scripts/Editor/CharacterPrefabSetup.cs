using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to configure Collider and Rigidbody for all character prefabs
/// Auto-runs when Unity reloads scripts
/// </summary>
[InitializeOnLoad]
public class CharacterPrefabSetup : Editor
{
    private const string PREFS_KEY = "CharacterPrefabSetup_AlreadyRun";

    static CharacterPrefabSetup()
    {
        // Only run once automatically
        if (!EditorPrefs.GetBool(PREFS_KEY, false))
        {
            EditorApplication.delayCall += () =>
            {
                SetupAllCharacterPrefabs();
                EditorPrefs.SetBool(PREFS_KEY, true);
            };
        }
    }

    [MenuItem("Tools/Setup Character Prefabs")]
    public static void SetupAllCharacterPrefabs()
    {
        // Character prefab paths
        string[] prefabPaths = new string[]
        {
            "Assets/Prefabs/Player/Character_01.prefab",
            "Assets/Prefabs/Player/Character_02.prefab",
            "Assets/Prefabs/Player/Character_03.prefab",
            "Assets/Prefabs/Player/Character_04.prefab",
            "Assets/Prefabs/Player/Character_05.prefab"
        };

        int successCount = 0;
        int failCount = 0;

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[CharacterPrefabSetup] Failed to load prefab: {path}");
                failCount++;
                continue;
            }

            // Create a prefab instance for modification
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

            if (prefabInstance == null)
            {
                Debug.LogError($"[CharacterPrefabSetup] Failed to load prefab contents: {path}");
                failCount++;
                continue;
            }

            try
            {
                // Setup CapsuleCollider
                CapsuleCollider capsuleCollider = prefabInstance.GetComponent<CapsuleCollider>();
                if (capsuleCollider == null)
                {
                    capsuleCollider = prefabInstance.AddComponent<CapsuleCollider>();
                }

                capsuleCollider.isTrigger = true;
                capsuleCollider.radius = 0.4f;
                capsuleCollider.height = 1.8f;
                capsuleCollider.center = new Vector3(0f, 0.9f, 0f);
                capsuleCollider.direction = 1; // Y-Axis

                Debug.Log($"[CharacterPrefabSetup] CapsuleCollider configured: {prefab.name}");

                // Setup Rigidbody
                Rigidbody rb = prefabInstance.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = prefabInstance.AddComponent<Rigidbody>();
                }

                rb.mass = 1f;
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

                // Freeze all position and rotation constraints
                rb.constraints = RigidbodyConstraints.FreezePositionX |
                                RigidbodyConstraints.FreezePositionY |
                                RigidbodyConstraints.FreezePositionZ |
                                RigidbodyConstraints.FreezeRotationX |
                                RigidbodyConstraints.FreezeRotationY |
                                RigidbodyConstraints.FreezeRotationZ;

                Debug.Log($"[CharacterPrefabSetup] Rigidbody configured: {prefab.name}");

                // Save the prefab
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                successCount++;

                Debug.Log($"[CharacterPrefabSetup] Prefab saved successfully: {prefab.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CharacterPrefabSetup] Error configuring {prefab.name}: {e.Message}");
                failCount++;
            }
            finally
            {
                // Unload prefab instance
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
        }

        // Summary
        Debug.Log($"[CharacterPrefabSetup] ==================== SUMMARY ====================");
        Debug.Log($"[CharacterPrefabSetup] Success: {successCount} prefabs");
        Debug.Log($"[CharacterPrefabSetup] Failed: {failCount} prefabs");
        Debug.Log($"[CharacterPrefabSetup] ================================================");

        if (successCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CharacterPrefabSetup] Asset database refreshed.");
        }
    }
}
