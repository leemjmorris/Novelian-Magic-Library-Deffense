using UnityEngine;
using UnityEditor;
using NovelianMagicLibraryDefense.Events;
using System.IO;

namespace NovelianMagicLibraryDefense.Editor
{
    /// <summary>
    /// LMJ: Editor utility to create EventChannel ScriptableObject assets
    /// </summary>
    public class CreateEventChannels : MonoBehaviour
    {
        [MenuItem("Tools/Create Event Channels")]
        public static void CreateAllEventChannels()
        {
            // Create folder if it doesn't exist
            string folderPath = "Assets/ScriptableObjects/Events";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Events");
            }

            // Create MonsterEvents
            CreateAsset<MonsterEvents>(folderPath, "MonsterEvents");

            // Create WallEvents
            CreateAsset<WallEvents>(folderPath, "WallEvents");

            // Create CharacterEvents
            CreateAsset<CharacterEvents>(folderPath, "CharacterEvents");

            // Create InputEvents
            CreateAsset<InputEvents>(folderPath, "InputEvents");

            // Create StageEvents
            CreateAsset<StageEvents>(folderPath, "StageEvents");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CreateEventChannels] All EventChannel assets created successfully!");
        }

        private static void CreateAsset<T>(string folderPath, string assetName) where T : ScriptableObject
        {
            string assetPath = $"{folderPath}/{assetName}.asset";

            // Check if asset already exists
            if (File.Exists(assetPath))
            {
                Debug.LogWarning($"[CreateEventChannels] {assetName} already exists. Skipping.");
                return;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            Debug.Log($"[CreateEventChannels] Created {assetName} at {assetPath}");
        }
    }
}
