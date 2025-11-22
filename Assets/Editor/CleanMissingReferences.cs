using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class CleanMissingReferences
{
    [MenuItem("Tools/Clean Missing References in All Scenes")]
    public static void CleanAllScenes()
    {
        if (!EditorUtility.DisplayDialog("Clean Missing References",
            "This will remove all missing prefab references from all scenes in the project.\n\n" +
            "Current scene will be saved first.\n\nContinue?",
            "Yes", "Cancel"))
        {
            return;
        }

        // Save current scene
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        string currentScenePath = SceneManager.GetActiveScene().path;

        int totalCleaned = 0;
        int scenesCleaned = 0;

        try
        {
            // Find all scenes
            string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

            for (int i = 0; i < sceneGUIDs.Length; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUIDs[i]);

                // Skip demo/sample scenes
                if (scenePath.Contains("Demo") || scenePath.Contains("Sample") ||
                    scenePath.Contains("Feel") || scenePath.Contains("Hovl") ||
                    scenePath.Contains("LMHPOLY") || scenePath.Contains("RPG") ||
                    scenePath.Contains("Suriyun") || scenePath.Contains("Multistory"))
                {
                    continue;
                }

                EditorUtility.DisplayProgressBar("Cleaning Missing References",
                    $"Processing scene: {scenePath}",
                    (float)i / sceneGUIDs.Length);

                // Open scene (Unity automatically cleans some missing references when loading)
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                // Clean missing references
                int cleaned = CleanSceneMissingReferences();

                // Always save the scene - Unity may have auto-cleaned references during load
                bool sceneModified = scene.isDirty || cleaned > 0;

                if (cleaned > 0)
                {
                    totalCleaned += cleaned;
                    scenesCleaned++;
                    Debug.Log($"[CleanMissingReferences] Cleaned {cleaned} missing references from: {scenePath}");
                }

                // Save scene to persist Unity's auto-cleanup
                if (sceneModified || true) // Always save to persist auto-cleanup
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[CleanMissingReferences] Saved scene: {scenePath}");
                }
            }

            // Restore original scene
            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            EditorUtility.ClearProgressBar();

            string message = totalCleaned > 0
                ? $"Successfully cleaned {totalCleaned} missing references from {scenesCleaned} scenes!"
                : "No missing references found!";

            Debug.Log($"[CleanMissingReferences] {message}");
            EditorUtility.DisplayDialog("Clean Complete", message, "OK");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[CleanMissingReferences] Error: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Error", $"Error occurred: {e.Message}", "OK");
        }
    }

    private static int CleanSceneMissingReferences()
    {
        int count = 0;

        // Get all GameObjects in the scene (including inactive)
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        // List to track objects to delete
        System.Collections.Generic.List<GameObject> objectsToDelete = new System.Collections.Generic.List<GameObject>();

        foreach (GameObject go in allObjects)
        {
            if (go == null) continue;

            // Check if this is a missing prefab instance by name
            if (go.name.Contains("Missing Prefab") ||
                go.name.Contains("Placeholder for referenced MonoBehaviour"))
            {
                objectsToDelete.Add(go);
                continue;
            }

            // Remove missing script components
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0)
            {
                count += removed;
                EditorUtility.SetDirty(go);
                Debug.Log($"[CleanMissingReferences] Removed {removed} missing components from: {go.name}");
            }
        }

        // Delete missing prefab instances
        foreach (GameObject go in objectsToDelete)
        {
            if (go != null)
            {
                Debug.Log($"[CleanMissingReferences] Deleting missing prefab instance: {go.name}");
                Object.DestroyImmediate(go);
                count++;
            }
        }

        return count;
    }

    [MenuItem("Tools/Clean Missing References in Current Scene")]
    public static void CleanCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        if (!EditorUtility.DisplayDialog("Clean Missing References",
            $"This will remove all missing references from current scene:\n{currentScene.name}\n\nContinue?",
            "Yes", "Cancel"))
        {
            return;
        }

        int cleaned = CleanSceneMissingReferences();

        if (cleaned > 0)
        {
            Debug.Log($"[CleanMissingReferences] Cleaned {cleaned} missing references from: {currentScene.name}");
            EditorSceneManager.SaveScene(currentScene);
            EditorUtility.DisplayDialog("Clean Complete", $"Cleaned {cleaned} missing references!", "OK");
        }
        else
        {
            Debug.Log($"[CleanMissingReferences] No missing references found in: {currentScene.name}");
            EditorUtility.DisplayDialog("Clean Complete", "No missing references found!", "OK");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
