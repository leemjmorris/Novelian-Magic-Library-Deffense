using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Monster 프리팹에 Death Effect 관련 필드를 자동으로 설정하는 에디터 도구
/// </summary>
public class MonsterDeathEffectSetup : EditorWindow
{
    private Vector2 scrollPosition;
    private List<string> logMessages = new List<string>();
    private DissolveSettings dissolveSettings;

    [MenuItem("Tools/Monster Death Effect Setup")]
    public static void ShowWindow()
    {
        GetWindow<MonsterDeathEffectSetup>("Monster Death Effect Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Monster Death Effect 자동 설정", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // DissolveSettings 필드
        EditorGUILayout.LabelField("Dissolve 설정 (선택사항)", EditorStyles.boldLabel);
        dissolveSettings = (DissolveSettings)EditorGUILayout.ObjectField(
            "Dissolve Settings",
            dissolveSettings,
            typeof(DissolveSettings),
            false);

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Monster 프리팹의 자식에서 SkinnedMeshRenderer/MeshRenderer를 자동으로 찾아 renderers 필드에 설정합니다.\n" +
            "DissolveSettings를 지정하면 함께 설정됩니다.\n\n" +
            "대상 폴더: Assets/03. Prefabs/Monster/Prefabs",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("선택된 프리팹에 Renderer 자동 설정", GUILayout.Height(30)))
        {
            SetupSelectedMonster();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("모든 Monster 프리팹에 Renderer 자동 설정", GUILayout.Height(40)))
        {
            SetupAllMonsterPrefabs();
        }

        GUILayout.Space(20);

        // 로그 표시
        if (logMessages.Count > 0)
        {
            GUILayout.Label("설정 결과:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (var msg in logMessages)
            {
                EditorGUILayout.LabelField(msg);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("로그 지우기"))
            {
                logMessages.Clear();
            }
        }
    }

    private void SetupSelectedMonster()
    {
        logMessages.Clear();

        Object[] selectedObjects = Selection.objects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("오류", "프리팹을 선택해주세요.", "확인");
            return;
        }

        int count = 0;
        foreach (var obj in selectedObjects)
        {
            GameObject prefab = obj as GameObject;
            if (prefab == null) continue;

            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path)) continue;

            Monster monster = prefab.GetComponent<Monster>();
            if (monster == null)
            {
                logMessages.Add($"[스킵] {prefab.name}: Monster 컴포넌트 없음");
                continue;
            }

            using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                Monster prefabMonster = editScope.prefabContentsRoot.GetComponent<Monster>();
                if (prefabMonster != null)
                {
                    int rendererCount = SetupMonsterRenderers(prefabMonster);
                    logMessages.Add($"[완료] {prefab.name}: {rendererCount}개 Renderer 설정");
                    count++;
                }
            }
        }

        EditorUtility.DisplayDialog("완료", $"{count}개의 프리팹 설정이 완료되었습니다.", "확인");
    }

    private void SetupAllMonsterPrefabs()
    {
        logMessages.Clear();

        // Prefabs 하위 폴더의 모든 프리팹 찾기
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/03. Prefabs/Monster/Prefabs" });
        int successCount = 0;
        int skipCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            Monster monster = prefab.GetComponent<Monster>();
            if (monster == null)
            {
                logMessages.Add($"[스킵] {prefab.name}: Monster 컴포넌트 없음");
                skipCount++;
                continue;
            }

            // 프리팹 편집 모드로 열기
            using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                Monster prefabMonster = editScope.prefabContentsRoot.GetComponent<Monster>();
                if (prefabMonster != null)
                {
                    int rendererCount = SetupMonsterRenderers(prefabMonster);
                    if (rendererCount > 0)
                    {
                        logMessages.Add($"[완료] {prefab.name}: {rendererCount}개 Renderer 설정");
                        successCount++;
                    }
                    else
                    {
                        logMessages.Add($"[경고] {prefab.name}: Renderer를 찾을 수 없음");
                    }
                }
            }
        }

        EditorUtility.DisplayDialog("완료",
            $"총 {guids.Length}개 프리팹 중\n" +
            $"- 성공: {successCount}개\n" +
            $"- 스킵: {skipCount}개", "확인");
    }

    private int SetupMonsterRenderers(Monster monster)
    {
        // SerializedObject를 사용하여 private 필드 접근
        SerializedObject so = new SerializedObject(monster);

        // renderers 필드 찾기
        SerializedProperty renderersProperty = so.FindProperty("renderers");

        if (renderersProperty == null)
        {
            Debug.LogWarning($"[MonsterDeathEffectSetup] {monster.name}: renderers 필드를 찾을 수 없습니다.");
            return 0;
        }

        // 모든 Renderer 컴포넌트 찾기 (자식 포함, SkinnedMeshRenderer와 MeshRenderer)
        List<Renderer> validRenderers = new List<Renderer>();
        Renderer[] allRenderers = monster.GetComponentsInChildren<Renderer>(true);

        foreach (var renderer in allRenderers)
        {
            // SkinnedMeshRenderer 또는 MeshRenderer만 추가
            if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
            {
                validRenderers.Add(renderer);
            }
        }

        if (validRenderers.Count == 0)
        {
            return 0;
        }

        // 배열 크기 설정
        renderersProperty.arraySize = validRenderers.Count;

        // 각 렌더러 할당
        for (int i = 0; i < validRenderers.Count; i++)
        {
            SerializedProperty element = renderersProperty.GetArrayElementAtIndex(i);
            element.objectReferenceValue = validRenderers[i];
        }

        // DissolveSettings 설정 (지정된 경우)
        if (dissolveSettings != null)
        {
            SerializedProperty dissolveSettingsProperty = so.FindProperty("dissolveSettings");
            if (dissolveSettingsProperty != null)
            {
                dissolveSettingsProperty.objectReferenceValue = dissolveSettings;
            }
        }

        // 변경사항 적용
        so.ApplyModifiedProperties();

        return validRenderers.Count;
    }
}
