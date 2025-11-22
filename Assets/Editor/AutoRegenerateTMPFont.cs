using UnityEngine;
using UnityEditor;
using TMPro;
using System.Linq;

public class AutoRegenerateTMPFont
{
    [MenuItem("Tools/Auto Fix GowunDodum Font (Add Missing Korean)")]
    public static void AutoFixFont()
    {
        // Find the font asset
        string fontAssetPath = "Assets/2D Pixel Quest Vol.3 - The UI-GUI/Font/GowunDodum-Regular SDF.asset";
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);

        if (fontAsset == null)
        {
            Debug.LogError($"Could not find font asset at: {fontAssetPath}");
            EditorUtility.DisplayDialog("Error", $"Could not find font asset at: {fontAssetPath}", "OK");
            return;
        }

        // Missing characters from error logs
        string missingChars = "관파견활동";

        if (!EditorUtility.DisplayDialog("Auto Fix Font",
            $"This will add missing Korean characters to the font:\n\n{missingChars}\n\n" +
            "The font atlas will be regenerated. Continue?",
            "Yes", "Cancel"))
        {
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Fixing Font", "Adding missing characters...", 0.5f);

            // Get existing characters from font asset
            var existingChars = fontAsset.characterTable.Select(c => (char)c.unicode).ToList();
            Debug.Log($"Existing character count: {existingChars.Count}");

            // Add missing characters
            foreach (char c in missingChars)
            {
                if (!existingChars.Contains(c))
                {
                    existingChars.Add(c);
                    Debug.Log($"Adding character: {c} (U+{((int)c):X4})");
                }
            }

            // Build character string
            string characterSet = new string(existingChars.ToArray());

            // Try to update font asset using TMP_FontAsset methods
            if (fontAsset.sourceFontFile != null)
            {
                Debug.Log($"Source font file: {fontAsset.sourceFontFile.name}");

                // Update atlas with new characters
                bool success = TryUpdateFontAtlas(fontAsset, characterSet);

                if (success)
                {
                    EditorUtility.SetDirty(fontAsset);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    EditorUtility.ClearProgressBar();
                    Debug.Log("[AutoRegenerateTMPFont] Font atlas updated successfully!");
                    EditorUtility.DisplayDialog("Success",
                        $"Added {missingChars.Length} missing Korean characters to the font!\n\n" +
                        "The font has been saved.",
                        "OK");
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                    ShowManualInstructions(missingChars);
                }
            }
            else
            {
                EditorUtility.ClearProgressBar();
                Debug.LogWarning("Source font file is missing. Cannot auto-regenerate.");
                ShowManualInstructions(missingChars);
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"Error: {e.Message}\n{e.StackTrace}");
            ShowManualInstructions(missingChars);
        }
    }

    private static bool TryUpdateFontAtlas(TMP_FontAsset fontAsset, string characterSet)
    {
        try
        {
            // Try to use TMP's internal method to update the atlas
            // This may not work in all Unity versions
            var method = typeof(TMP_FontAsset).GetMethod("TryAddCharacters",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            if (method != null)
            {
                // Add missing characters
                string missingChars = "관파견활동";
                uint[] unicodes = missingChars.Select(c => (uint)c).ToArray();

                object[] parameters = new object[] { unicodes, null };
                bool result = (bool)method.Invoke(fontAsset, parameters);

                Debug.Log($"TryAddCharacters result: {result}");
                return result;
            }
            else
            {
                Debug.LogWarning("TryAddCharacters method not found. Manual regeneration required.");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Auto-regeneration failed: {e.Message}");
            return false;
        }
    }

    private static void ShowManualInstructions(string missingChars)
    {
        // Copy to clipboard
        GUIUtility.systemCopyBuffer = missingChars;

        Debug.Log("=== MANUAL FIX REQUIRED ===");
        Debug.Log("Missing characters have been copied to clipboard: " + missingChars);
        Debug.Log("Follow these steps:");
        Debug.Log("1. Window → TextMeshPro → Font Asset Creator");
        Debug.Log("2. Select source font: GowunDodum-Regular");
        Debug.Log("3. Set 'Character Set' to 'Custom Characters'");
        Debug.Log("4. Paste missing characters: " + missingChars);
        Debug.Log("5. Set 'Atlas Resolution' to 2048x2048 or higher");
        Debug.Log("6. Click 'Generate Font Atlas'");
        Debug.Log("7. Click 'Save' and overwrite existing font asset");

        EditorUtility.DisplayDialog("Manual Fix Required",
            "Auto-regeneration is not supported in this Unity version.\n\n" +
            "Missing characters have been copied to clipboard:\n" +
            missingChars + "\n\n" +
            "Please follow these steps:\n" +
            "1. Window → TextMeshPro → Font Asset Creator\n" +
            "2. Source Font: GowunDodum-Regular\n" +
            "3. Character Set: Custom Characters\n" +
            "4. Paste the missing characters\n" +
            "5. Atlas Resolution: 2048x2048 or higher\n" +
            "6. Generate Font Atlas\n" +
            "7. Save and overwrite the existing font asset",
            "OK");
    }
}
