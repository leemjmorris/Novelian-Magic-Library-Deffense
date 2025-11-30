using UnityEngine;
using UnityEditor;
using Dispatch;

namespace DispatchEditor
{
    public class CreateDispatchSettings
    {
        [MenuItem("Tools/Dispatch/Create Time Settings")]
        public static void CreateTimeSettings()
        {
            DispatchTimeSettings settings = ScriptableObject.CreateInstance<DispatchTimeSettings>();

            string path = "Assets/Data/Dispatch";
            if (!AssetDatabase.IsValidFolder(path))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Data"))
                {
                    AssetDatabase.CreateFolder("Assets", "Data");
                }
                AssetDatabase.CreateFolder("Assets/Data", "Dispatch");
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/DispatchTimeSettings.asset");
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = settings;

            Debug.Log($"<color=green>[DispatchSettings] ScriptableObject 생성 완료: {assetPath}</color>");
        }
    }
}
