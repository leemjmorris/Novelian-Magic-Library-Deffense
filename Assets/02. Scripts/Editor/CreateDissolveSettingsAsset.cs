using UnityEngine;
using UnityEditor;

/// <summary>
/// DissolveSettings ScriptableObject 에셋을 생성하는 에디터 유틸리티
/// </summary>
public static class CreateDissolveSettingsAsset
{
    [MenuItem("Tools/Create Dissolve Settings Asset")]
    public static void CreateAsset()
    {
        // DissolveSettings 에셋 생성
        DissolveSettings asset = ScriptableObject.CreateInstance<DissolveSettings>();

        // 저장 경로 설정
        string path = "Assets/04. Settings/DissolveSettings.asset";

        // 폴더가 없으면 생성
        if (!AssetDatabase.IsValidFolder("Assets/04. Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "04. Settings");
        }

        // 에셋 생성
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 생성된 에셋 선택
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        Debug.Log($"[CreateDissolveSettingsAsset] DissolveSettings 에셋이 생성되었습니다: {path}");
        EditorUtility.DisplayDialog("완료", $"DissolveSettings 에셋이 생성되었습니다.\n\n경로: {path}\n\n이제 인스펙터에서 Dissolve Shader와 Noise Texture를 설정해주세요.", "확인");
    }
}
