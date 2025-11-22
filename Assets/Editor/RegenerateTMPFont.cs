using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

public class RegenerateTMPFont
{
    [MenuItem("Tools/Regenerate GowunDodum Font with Full Korean")]
    public static void RegenerateFontWithKorean()
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

        // Find the source font file
        string[] fontGUIDs = AssetDatabase.FindAssets("GowunDodum-Regular t:Font", new[] { "Assets" });

        if (fontGUIDs.Length == 0)
        {
            Debug.LogError("Could not find GowunDodum-Regular.ttf source font!");
            EditorUtility.DisplayDialog("Error", "Could not find GowunDodum-Regular.ttf source font file!", "OK");
            return;
        }

        string sourceFontPath = AssetDatabase.GUIDToAssetPath(fontGUIDs[0]);
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);

        if (sourceFont == null)
        {
            Debug.LogError($"Could not load source font from: {sourceFontPath}");
            EditorUtility.DisplayDialog("Error", $"Could not load source font from: {sourceFontPath}", "OK");
            return;
        }

        Debug.Log($"Found source font: {sourceFont.name} at {sourceFontPath}");
        Debug.Log($"Current font asset: {fontAsset.name}");

        // Build character set with full Korean support
        string characters = BuildKoreanCharacterSet();

        Debug.Log($"Character set size: {characters.Length} characters");
        Debug.Log($"Including Korean range: AC00-D7A3 (Hangul Syllables)");

        if (!EditorUtility.DisplayDialog("Regenerate Font",
            $"This will regenerate the font asset with {characters.Length} characters including full Korean support.\n\n" +
            "This may take a few minutes. Continue?",
            "Yes", "Cancel"))
        {
            return;
        }

        // Log instructions
        Debug.Log("=== HOW TO ADD KOREAN CHARACTERS ===");
        Debug.Log("1. Select the font asset in Project window:");
        Debug.Log($"   {fontAssetPath}");
        Debug.Log("2. In Inspector, click 'Font Asset Creator' button");
        Debug.Log("3. In the Font Asset Creator window:");
        Debug.Log("   - Set 'Character Set' to 'Unicode Range (Hex)'");
        Debug.Log("   - Enter this range: AC00-D7A3");
        Debug.Log("   - OR set 'Character Set' to 'Custom Characters'");
        Debug.Log("   - And paste missing characters: 관파견활동");
        Debug.Log("4. Click 'Generate Font Atlas'");
        Debug.Log("5. Click 'Save' to update the font asset");

        // Copy missing characters to clipboard
        string missingCharsOnly = "관파견활동";
        GUIUtility.systemCopyBuffer = missingCharsOnly;

        EditorUtility.DisplayDialog("Font Regeneration Instructions",
            "Missing Korean characters have been copied to clipboard!\n\n" +
            "To fix the font:\n" +
            "1. Window → TextMeshPro → Font Asset Creator\n" +
            "2. Select source font: GowunDodum-Regular\n" +
            "3. Set 'Character Set' to 'Custom Characters'\n" +
            "4. Paste the missing characters (관파견활동)\n" +
            "5. Click 'Generate Font Atlas'\n" +
            "6. Save as the existing font asset\n\n" +
            "OR\n\n" +
            "For FULL Korean support:\n" +
            "3. Set 'Character Set' to 'Unicode Range (Hex)'\n" +
            "4. Enter range: AC00-D7A3\n" +
            "5. Click 'Generate Font Atlas'\n" +
            "6. Save as the existing font asset",
            "OK");

        // Select the font asset in project
        Selection.activeObject = fontAsset;
        EditorGUIUtility.PingObject(fontAsset);
    }

    [MenuItem("Tools/Add Missing Korean Characters to GowunDodum")]
    public static void AddMissingCharacters()
    {
        // Missing characters from error logs
        char[] missingChars = new char[]
        {
            '\uAD00', // 관
            '\uD30C', // 파
            '\uACAC', // 견
            '\uD65C', // 활
            '\uB3D9'  // 동
        };

        string characters = new string(missingChars);

        Debug.Log("Missing Korean characters that need to be added:");
        foreach (char c in missingChars)
        {
            Debug.Log($"U+{((int)c):X4} = {c}");
        }

        EditorUtility.DisplayDialog("Missing Characters",
            $"These characters are missing from the font:\n\n{characters}\n\n" +
            "Copy this from the console and add it to your font atlas.\n" +
            "Or use 'Regenerate GowunDodum Font with Full Korean' to add all Korean characters.",
            "OK");
    }

    private static string BuildKoreanCharacterSet()
    {
        List<char> characters = new List<char>();

        // Add basic ASCII (space to ~)
        for (char c = ' '; c <= '~'; c++)
        {
            characters.Add(c);
        }

        // Add full Korean Hangul Syllables range (AC00-D7A3)
        // This covers all 11,172 modern Korean syllables
        for (int i = 0xAC00; i <= 0xD7A3; i++)
        {
            characters.Add((char)i);
        }

        // Add common punctuation and symbols
        string commonSymbols = "€£¥©®™°±×÷≠≤≥∞√∫≈→←↑↓⇒⇐⇑⇓•·‥…※";
        characters.AddRange(commonSymbols.ToCharArray());

        return new string(characters.ToArray());
    }
}
