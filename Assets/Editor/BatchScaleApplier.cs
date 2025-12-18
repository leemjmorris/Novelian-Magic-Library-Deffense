// BatchScaleApplier.cs
// 모든 SkillProjectile 래퍼 프리팹의 VFX_Main Scale을 일괄 변경

using UnityEngine;
using UnityEditor;
using System.IO;

public static class BatchScaleApplier
{
    [MenuItem("Tools/Skill System/Apply Scale 0.5 to All Effects")]
    public static void ApplyScaleToAllEffects()
    {
        string[] prefabPaths = new string[]
        {
            "Assets/03. Prefabs/SpecialSkillEffects/NotScriptBased",
            "Assets/03. Prefabs/SpecialSkillEffects/ScriptBased"
        };

        int count = 0;
        int failed = 0;

        foreach (var folderPath in prefabPaths)
        {
            if (!Directory.Exists(folderPath)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                
                if (prefab == null) continue;

                // SkillProjectile 컴포넌트 확인
                var skillProjectile = prefab.GetComponent<Novelian.Combat.SkillProjectile>();
                if (skillProjectile == null) continue;

                // 프리팹 내용 로드
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
                
                Transform vfxMain = prefabContents.transform.Find("VFX_Main");
                if (vfxMain != null)
                {
                    vfxMain.localScale = Vector3.one * 0.5f;
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
                    count++;
                    Debug.Log($"[BatchScale] Applied scale 0.5 to: {prefab.name}");
                }
                else
                {
                    failed++;
                    Debug.LogWarning($"[BatchScale] VFX_Main not found in: {prefab.name}");
                }

                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BatchScale] Complete! Applied: {count}, Failed: {failed}");
        EditorUtility.DisplayDialog("Batch Scale Applied", 
            $"Scale 0.5 applied to {count} prefabs.\nFailed: {failed}", "OK");
    }
}
