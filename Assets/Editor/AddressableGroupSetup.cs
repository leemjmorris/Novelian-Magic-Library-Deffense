using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// Automatically organizes Addressables into logical groups
/// Menu: Tools/Addressables/Setup Groups
/// </summary>
public class AddressableGroupSetup : EditorWindow
{
    [MenuItem("Tools/Addressables/Setup Groups")]
    public static void SetupGroups()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressableGroupSetup] Addressable Asset Settings not found!");
            return;
        }

        Debug.Log("[AddressableGroupSetup] Starting Addressable Groups setup...");

        // Create logical groups
        var csvGroup = CreateOrGetGroup(settings, "CSV_Data");
        var audioGroup = CreateOrGetGroup(settings, "Audio");
        var prefabGroup = CreateOrGetGroup(settings, "Prefabs");
        var uiGroup = CreateOrGetGroup(settings, "UI");

        // Assign assets to groups
        AssignCSVToGroup(settings, csvGroup);
        AssignAudioToGroup(settings, audioGroup);
        AssignPrefabsToGroup(settings, prefabGroup);
        AssignUIToGroup(settings, uiGroup);

        // Save settings
        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(settings);

        Debug.Log("[AddressableGroupSetup] ✓ Addressable Groups setup completed!");
        Debug.Log($"  - CSV_Data: {csvGroup.entries.Count} entries");
        Debug.Log($"  - Audio: {audioGroup.entries.Count} entries");
        Debug.Log($"  - Prefabs: {prefabGroup.entries.Count} entries");
        Debug.Log($"  - UI: {uiGroup.entries.Count} entries");
    }

    /// <summary>
    /// Create or get existing group
    /// </summary>
    private static AddressableAssetGroup CreateOrGetGroup(AddressableAssetSettings settings, string groupName)
    {
        var group = settings.FindGroup(groupName);

        if (group == null)
        {
            group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            Debug.Log($"[AddressableGroupSetup] Created new group: {groupName}");
        }
        else
        {
            Debug.Log($"[AddressableGroupSetup] Using existing group: {groupName}");
        }

        return group;
    }

    /// <summary>
    /// Assign CSV files to CSV_Data group
    /// </summary>
    private static void AssignCSVToGroup(AddressableAssetSettings settings, AddressableAssetGroup group)
    {
        string[] csvPaths = new string[]
        {
            "Assets/Resources/CSV/SkillTable.csv",
            "Assets/Resources/CSV/EffectTable.csv",
            "Assets/Scripts/Csv/Test.csv",
            "Assets/Scripts/Csv/Test2.csv"
        };

        string[] csvAddresses = new string[]
        {
            "SkillTable",
            "EffectTable",
            "Test",
            "Test2"
        };

        for (int i = 0; i < csvPaths.Length; i++)
        {
            string path = csvPaths[i];
            string address = csvAddresses[i];

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[AddressableGroupSetup] Asset not found: {path}");
                continue;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry != null)
            {
                entry.address = address;
                Debug.Log($"[AddressableGroupSetup] Added {address} to CSV_Data group");
            }
        }
    }

    /// <summary>
    /// Assign audio files to Audio group
    /// </summary>
    private static void AssignAudioToGroup(AddressableAssetSettings settings, AddressableAssetGroup group)
    {
        // Find all audio files in Resources
        string[] audioExtensions = new string[] { "*.mp3", "*.wav", "*.ogg" };

        foreach (string ext in audioExtensions)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Only add if in Resources folder
                if (path.Contains("/Resources/") || path.Contains("/Resources_moved/"))
                {
                    var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                    if (entry != null)
                    {
                        // Use filename without extension as address
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                        entry.address = fileName;
                        Debug.Log($"[AddressableGroupSetup] Added {fileName} to Audio group");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Assign prefabs to Prefabs group
    /// </summary>
    private static void AssignPrefabsToGroup(AddressableAssetSettings settings, AddressableAssetGroup group)
    {
        // First, add the default Projectile with specific address
        string defaultProjectilePath = "Assets/Prefabs/Skill/Projectile 1 magic.prefab";
        string defaultProjectileGuid = AssetDatabase.AssetPathToGUID(defaultProjectilePath);

        if (!string.IsNullOrEmpty(defaultProjectileGuid))
        {
            var entry = settings.CreateOrMoveEntry(defaultProjectileGuid, group, false, false);
            if (entry != null)
            {
                entry.address = "Projectile";  // Register as "Projectile" for pool
                Debug.Log($"[AddressableGroupSetup] Added Projectile (default) to Prefabs group");
            }
        }
        else
        {
            Debug.LogWarning($"[AddressableGroupSetup] Default projectile not found at {defaultProjectilePath}");
        }

        // Then add other prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Skip the default projectile (already added above)
            if (path == defaultProjectilePath)
                continue;

            // Only add if in Resources or Prefabs folder
            if (path.Contains("/Resources/") || path.Contains("/Resources_moved/") || path.Contains("/Prefabs/"))
            {
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry != null)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    entry.address = fileName;
                    Debug.Log($"[AddressableGroupSetup] Added {fileName} to Prefabs group");
                }
            }
        }
    }

    /// <summary>
    /// Assign UI assets to UI group
    /// </summary>
    private static void AssignUIToGroup(AddressableAssetSettings settings, AddressableAssetGroup group)
    {
        // Find all prefabs in UI folder
        string[] uiGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" });

        foreach (string guid in uiGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry != null)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                entry.address = $"UI/{fileName}";
                Debug.Log($"[AddressableGroupSetup] Added UI/{fileName} to UI group");
            }
        }
    }
}
