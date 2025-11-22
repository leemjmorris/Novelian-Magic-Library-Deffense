using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class AutoChangeTMPFont
{
    [MenuItem("Tools/Auto Change All TMP to GowunDodum")]
    public static void ChangeAllToGowunDodum()
    {
        // Find the target font
        string[] fontGUIDs = AssetDatabase.FindAssets("GowunDodum-Regular SDF t:TMP_FontAsset", new[] { "Assets/Prefabs/Fonts" });

        if (fontGUIDs.Length == 0)
        {
            // Try searching in all Assets folder
            fontGUIDs = AssetDatabase.FindAssets("GowunDodum-Regular SDF t:TMP_FontAsset", new[] { "Assets" });
        }

        if (fontGUIDs.Length == 0)
        {
            Debug.LogError("Could not find GowunDodum-Regular SDF font!");
            EditorUtility.DisplayDialog("Error", "Could not find GowunDodum-Regular SDF font in Assets/Prefabs/Fonts folder!", "OK");
            return;
        }

        string fontPath = AssetDatabase.GUIDToAssetPath(fontGUIDs[0]);
        TMP_FontAsset targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);

        if (targetFont == null)
        {
            Debug.LogError($"Failed to load font from path: {fontPath}");
            EditorUtility.DisplayDialog("Error", $"Failed to load font from path: {fontPath}", "OK");
            return;
        }

        Debug.Log($"Found target font: {targetFont.name} at {fontPath}");

        int changedCount = 0;

        // Save current scene
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        string currentScenePath = SceneManager.GetActiveScene().path;

        try
        {
            // Change fonts in all scenes
            string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

            for (int i = 0; i < sceneGUIDs.Length; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUIDs[i]);

                // Skip demo/sample scenes
                if (scenePath.Contains("Demo") || scenePath.Contains("Sample") ||
                    scenePath.Contains("Feel") || scenePath.Contains("Hovl") ||
                    scenePath.Contains("LMHPOLY") || scenePath.Contains("RPG") ||
                    scenePath.Contains("Suriyun"))
                {
                    continue;
                }

                EditorUtility.DisplayProgressBar("Changing TMP Fonts", $"Processing scene: {scenePath}", (float)i / sceneGUIDs.Length);

                // Open scene
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                // Find all TMP components in the scene
                changedCount += ChangeTextsInScene(targetFont);

                // Save scene
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            // Change fonts in all prefabs
            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });

            for (int i = 0; i < prefabGUIDs.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUIDs[i]);

                // Skip demo/sample prefabs
                if (prefabPath.Contains("Demo") || prefabPath.Contains("Sample") ||
                    prefabPath.Contains("Feel") || prefabPath.Contains("Hovl") ||
                    prefabPath.Contains("LMHPOLY") || prefabPath.Contains("RPG"))
                {
                    continue;
                }

                EditorUtility.DisplayProgressBar("Changing TMP Fonts", $"Processing prefab: {prefabPath}", (float)i / prefabGUIDs.Length);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    changedCount += ChangeTextsInPrefab(prefabPath, targetFont);
                }
            }

            // Restore original scene
            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"Successfully changed {changedCount} TMP components to {targetFont.name}");
            EditorUtility.DisplayDialog("Success", $"Changed {changedCount} TMP components to {targetFont.name}", "OK");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"Error occurred: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Error", $"Error occurred: {e.Message}", "OK");
        }
    }

    private static int ChangeTextsInScene(TMP_FontAsset targetFont)
    {
        int count = 0;

        // Find all TextMeshProUGUI (UI texts) - Unity 2023+ version
        TextMeshProUGUI[] uiTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var text in uiTexts)
        {
            if (text != null && text.font != targetFont)
            {
                Undo.RecordObject(text, "Change TMP Font");
                text.font = targetFont;
                EditorUtility.SetDirty(text);
                count++;
            }
        }

        // Find all TextMeshPro (3D texts) - Unity 2023+ version
        TextMeshPro[] worldTexts = Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var text in worldTexts)
        {
            if (text != null && text.font != targetFont)
            {
                Undo.RecordObject(text, "Change TMP Font");
                text.font = targetFont;
                EditorUtility.SetDirty(text);
                count++;
            }
        }

        return count;
    }

    private static int ChangeTextsInPrefab(string prefabPath, TMP_FontAsset targetFont)
    {
        int count = 0;

        try
        {
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

            if (prefabContents == null)
            {
                return 0;
            }

            // Find all TextMeshProUGUI in prefab
            TextMeshProUGUI[] uiTexts = prefabContents.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in uiTexts)
            {
                if (text != null && text.font != targetFont)
                {
                    text.font = targetFont;
                    count++;
                }
            }

            // Find all TextMeshPro in prefab
            TextMeshPro[] worldTexts = prefabContents.GetComponentsInChildren<TextMeshPro>(true);
            foreach (var text in worldTexts)
            {
                if (text != null && text.font != targetFont)
                {
                    text.font = targetFont;
                    count++;
                }
            }

            if (count > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
        catch (System.Exception)
        {
            // Skip prefabs with errors (missing nested prefabs, etc.)
            // Silently continue to next prefab
        }

        return count;
    }
}
