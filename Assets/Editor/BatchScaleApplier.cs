// BatchScaleApplier.cs
// 모든 SkillProjectile 래퍼 프리팹의 VFX 자식 Scale을 일괄 변경
// 자식 VFX는 항상 1,1,1 스케일이 기본이어야 루트 스케일 조정이 의미있음

using UnityEngine;
using UnityEditor;
using System.IO;

public static class BatchScaleApplier
{
    [MenuItem("Tools/Skill System/Reset All Child VFX Scale to 1,1,1")]
    public static void ResetAllChildVFXScale()
    {
        ApplyScaleToEffects(1.0f);
    }

    [MenuItem("Tools/Skill System/Apply Scale 0.5 to Child VFX (Legacy)")]
    public static void ApplyScaleToAllEffects()
    {
        ApplyScaleToEffects(0.5f);
    }

    private static void ApplyScaleToEffects(float scale)
    {
        string[] prefabPaths = new string[]
        {
            "Assets/03. Prefabs/SpecialSkillEffects/NotScriptBased",
            "Assets/03. Prefabs/SpecialSkillEffects/ScriptBased"
        };

        int count = 0;
        int skipped = 0;

        foreach (var folderPath in prefabPaths)
        {
            if (!Directory.Exists(folderPath)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null) continue;

                // LMJ: SkillProjectile 클래스 삭제됨 - 프리팹 이름 기반으로 처리
                // 프리팹 내용 로드
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);

                bool applied = false;

                // 첫 번째 자식에 스케일 적용
                if (prefabContents.transform.childCount > 0)
                {
                    Transform firstChild = prefabContents.transform.GetChild(0);
                    firstChild.localScale = Vector3.one * scale;
                    applied = true;
                }

                if (applied)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
                    count++;
                    Debug.Log($"[BatchScale] Applied scale {scale} to: {prefab.name}");
                }
                else
                {
                    skipped++;
                    Debug.LogWarning($"[BatchScale] No VFX child found in: {prefab.name}");
                }

                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BatchScale] Complete! Applied: {count}, Skipped: {skipped}");
        EditorUtility.DisplayDialog("Batch Scale Applied",
            $"Scale {scale} applied to {count} prefabs.\nSkipped (no SkillProjectile or no child): {skipped}", "OK");
    }
}
