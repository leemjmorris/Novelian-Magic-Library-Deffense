using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressablesBuildHelper
{
    [MenuItem("Tools/Addressables/Build for Current Platform")]
    public static void BuildAddressablesForCurrentPlatform()
    {
        Debug.Log("[AddressablesBuildHelper] Starting Addressables build for current platform...");
        
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null)
        {
            Debug.LogError("[AddressablesBuildHelper] Addressables settings not found!");
            return;
        }

        // Build Addressables content
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError($"[AddressablesBuildHelper] Build failed: {result.Error}");
        }
        else
        {
            Debug.Log($"[AddressablesBuildHelper] Build completed successfully! Duration: {result.Duration}");
        }
    }

    [MenuItem("Tools/Addressables/Switch to Fast Mode (Asset Database)")]
    public static void SwitchToFastMode()
    {
        Debug.Log("[AddressablesBuildHelper] Switching to Fast Mode (Use Asset Database)...");
        
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null)
        {
            Debug.LogError("[AddressablesBuildHelper] Addressables settings not found!");
            return;
        }

        // Set to fast mode
        settings.ActivePlayModeDataBuilderIndex = 0; // 0 is typically Fast Mode
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        
        Debug.Log("[AddressablesBuildHelper] Switched to Fast Mode successfully!");
    }

    [MenuItem("Tools/Addressables/Switch to Virtual Mode")]
    public static void SwitchToVirtualMode()
    {
        Debug.Log("[AddressablesBuildHelper] Switching to Virtual Mode...");
        
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null)
        {
            Debug.LogError("[AddressablesBuildHelper] Addressables settings not found!");
            return;
        }

        // Set to virtual mode
        settings.ActivePlayModeDataBuilderIndex = 1; // 1 is typically Virtual Mode
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        
        Debug.Log("[AddressablesBuildHelper] Switched to Virtual Mode successfully!");
    }

    [MenuItem("Tools/Addressables/Switch to Packed Mode")]
    public static void SwitchToPackedMode()
    {
        Debug.Log("[AddressablesBuildHelper] Switching to Packed Mode...");
        
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null)
        {
            Debug.LogError("[AddressablesBuildHelper] Addressables settings not found!");
            return;
        }

        // Set to packed mode
        settings.ActivePlayModeDataBuilderIndex = 2; // 2 is typically Packed Mode
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        
        Debug.Log("[AddressablesBuildHelper] Switched to Packed Mode successfully!");
    }

    [MenuItem("Tools/Addressables/Clean Build Cache")]
    public static void CleanBuildCache()
    {
        Debug.Log("[AddressablesBuildHelper] Cleaning Addressables build cache...");
        
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null)
        {
            Debug.LogError("[AddressablesBuildHelper] Addressables settings not found!");
            return;
        }

        AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
        
        Debug.Log("[AddressablesBuildHelper] Build cache cleaned successfully!");
    }
}
