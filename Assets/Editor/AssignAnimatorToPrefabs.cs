using UnityEngine;
using UnityEditor;

public class AssignAnimatorToPrefabs
{
    [MenuItem("Tools/Assign Animator Controller to Character Prefabs")]
    public static void AssignAnimatorController()
    {
        // 애니메이터 컨트롤러 로드
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/CharacterAnimatorController.controller");
        
        if (controller == null)
        {
            Debug.LogError("[AssignAnimator] CharacterAnimatorController not found!");
            return;
        }

        // Character Prefabs 폴더의 모든 프리팹 찾기
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Character Prefabs" });
        
        int successCount = 0;
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;

            // 프리팹 수정 모드로 열기
            using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject prefabRoot = editScope.prefabContentsRoot;
                Animator animator = prefabRoot.GetComponent<Animator>();
                
                if (animator != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log($"[AssignAnimator] Assigned controller to: {prefab.name}");
                    successCount++;
                }
                else
                {
                    Debug.LogWarning($"[AssignAnimator] No Animator found on: {prefab.name}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[AssignAnimator] Completed! {successCount}/{prefabGuids.Length} prefabs updated.");
    }
}
