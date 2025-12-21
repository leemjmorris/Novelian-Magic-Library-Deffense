// EffectScaleEditor.cs
// 스킬 이펙트 프리팹의 루트/자식 스케일을 조절하는 에디터 윈도우
// - 루트 스케일: 전체 이펙트 크기 조절 (CSV aoe_radius 등 반영)
// - 자식 VFX 스케일: 항상 1,1,1이 기본 (기준점)

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class EffectScaleEditor : EditorWindow
{
    private float rootScale = 1f;
    private float childScale = 1f;
    private bool applyToRoot = true;
    private bool applyToChild = false;
    private Vector2 scrollPosition;
    private List<PrefabScaleInfo> prefabList = new List<PrefabScaleInfo>();
    private bool showPrefabList = false;

    private class PrefabScaleInfo
    {
        public string path;
        public string name;
        public Vector3 currentRootScale;
        public Vector3 currentChildScale;
        public bool selected;
    }

    [MenuItem("Tools/Skill System/Effect Scale Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<EffectScaleEditor>("Effect Scale Editor");
        window.minSize = new Vector2(400, 500);
        window.LoadPrefabList();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("스킬 이펙트 스케일 에디터", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "루트 스케일: 전체 이펙트 크기 조절 (CSV aoe_radius 반영)\n" +
            "자식 VFX 스케일: 항상 1,1,1이 기본 (기준점)",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 스케일 설정 섹션
        EditorGUILayout.LabelField("스케일 설정", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        {
            // 루트 스케일
            EditorGUILayout.BeginHorizontal();
            applyToRoot = EditorGUILayout.Toggle(applyToRoot, GUILayout.Width(20));
            EditorGUILayout.LabelField("루트 스케일", GUILayout.Width(80));
            rootScale = EditorGUILayout.FloatField(rootScale);
            if (GUILayout.Button("1.0", GUILayout.Width(40))) rootScale = 1f;
            if (GUILayout.Button("0.5", GUILayout.Width(40))) rootScale = 0.5f;
            if (GUILayout.Button("2.0", GUILayout.Width(40))) rootScale = 2f;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 자식 VFX 스케일
            EditorGUILayout.BeginHorizontal();
            applyToChild = EditorGUILayout.Toggle(applyToChild, GUILayout.Width(20));
            EditorGUILayout.LabelField("자식 VFX 스케일", GUILayout.Width(80));
            childScale = EditorGUILayout.FloatField(childScale);
            if (GUILayout.Button("1.0", GUILayout.Width(40))) childScale = 1f;
            if (GUILayout.Button("0.5", GUILayout.Width(40))) childScale = 0.5f;
            if (GUILayout.Button("2.0", GUILayout.Width(40))) childScale = 2f;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // 빠른 적용 버튼
        EditorGUILayout.LabelField("빠른 적용", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("루트 1.0 적용\n(모든 프리팹)", GUILayout.Height(40)))
            {
                ApplyRootScaleToAll(1f);
            }
            if (GUILayout.Button("자식 VFX 1.0 리셋\n(모든 프리팹)", GUILayout.Height(40)))
            {
                ApplyChildScaleToAll(1f);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 프리팹 리스트
        EditorGUILayout.BeginHorizontal();
        showPrefabList = EditorGUILayout.Foldout(showPrefabList, $"프리팹 목록 ({prefabList.Count}개)", true);
        if (GUILayout.Button("새로고침", GUILayout.Width(80)))
        {
            LoadPrefabList();
        }
        EditorGUILayout.EndHorizontal();

        if (showPrefabList)
        {
            EditorGUILayout.BeginVertical("box");
            {
                // 전체 선택/해제
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("전체 선택", GUILayout.Width(80)))
                {
                    for (int i = 0; i < prefabList.Count; i++)
                        prefabList[i].selected = true;
                }
                if (GUILayout.Button("전체 해제", GUILayout.Width(80)))
                {
                    for (int i = 0; i < prefabList.Count; i++)
                        prefabList[i].selected = false;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // 스크롤 리스트
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                {
                    for (int i = 0; i < prefabList.Count; i++)
                    {
                        var info = prefabList[i];
                        EditorGUILayout.BeginHorizontal();
                        info.selected = EditorGUILayout.Toggle(info.selected, GUILayout.Width(20));
                        EditorGUILayout.LabelField(info.name, GUILayout.Width(200));
                        EditorGUILayout.LabelField($"Root: {info.currentRootScale.x:F2}", GUILayout.Width(80));
                        EditorGUILayout.LabelField($"Child: {info.currentChildScale.x:F2}", GUILayout.Width(80));
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // 선택된 프리팹에 적용
        EditorGUILayout.BeginHorizontal();
        {
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("선택된 프리팹에 적용", GUILayout.Height(35)))
            {
                ApplyScaleToSelected();
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
            if (GUILayout.Button("모든 프리팹에 적용", GUILayout.Height(35)))
            {
                ApplyScaleToAll();
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void LoadPrefabList()
    {
        prefabList.Clear();

        string[] prefabPaths = new string[]
        {
            "Assets/03. Prefabs/SpecialSkillEffects/NotScriptBased",
            "Assets/03. Prefabs/SpecialSkillEffects/ScriptBased"
        };

        foreach (var folderPath in prefabPaths)
        {
            if (!Directory.Exists(folderPath)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null) continue;

                // LMJ: SkillProjectile 클래스 삭제됨 - 이름 기반으로 이펙트 프리팹 판별
                if (!prefab.name.StartsWith("Effect_")) continue;

                var info = new PrefabScaleInfo
                {
                    path = assetPath,
                    name = prefab.name,
                    currentRootScale = prefab.transform.localScale,
                    currentChildScale = Vector3.one,
                    selected = true
                };

                // 자식 VFX 스케일 확인
                if (prefab.transform.childCount > 0)
                {
                    info.currentChildScale = prefab.transform.GetChild(0).localScale;
                }

                prefabList.Add(info);
            }
        }

        Debug.Log($"[EffectScaleEditor] Loaded {prefabList.Count} prefabs");
    }

    private void ApplyScaleToSelected()
    {
        int count = 0;

        for (int i = 0; i < prefabList.Count; i++)
        {
            var info = prefabList[i];
            if (!info.selected) continue;

            if (ApplyScaleToPrefab(info.path, applyToRoot, rootScale, applyToChild, childScale))
            {
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        LoadPrefabList();

        EditorUtility.DisplayDialog("스케일 적용 완료",
            $"{count}개 프리팹에 스케일 적용 완료\n" +
            (applyToRoot ? $"루트: {rootScale}\n" : "") +
            (applyToChild ? $"자식 VFX: {childScale}" : ""),
            "OK");
    }

    private void ApplyScaleToAll()
    {
        int count = 0;

        for (int i = 0; i < prefabList.Count; i++)
        {
            var info = prefabList[i];
            if (ApplyScaleToPrefab(info.path, applyToRoot, rootScale, applyToChild, childScale))
            {
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        LoadPrefabList();

        EditorUtility.DisplayDialog("스케일 적용 완료",
            $"{count}개 프리팹에 스케일 적용 완료\n" +
            (applyToRoot ? $"루트: {rootScale}\n" : "") +
            (applyToChild ? $"자식 VFX: {childScale}" : ""),
            "OK");
    }

    private void ApplyRootScaleToAll(float scale)
    {
        int count = 0;

        for (int i = 0; i < prefabList.Count; i++)
        {
            var info = prefabList[i];
            if (ApplyScaleToPrefab(info.path, true, scale, false, 1f))
            {
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        LoadPrefabList();

        EditorUtility.DisplayDialog("루트 스케일 적용 완료",
            $"{count}개 프리팹에 루트 스케일 {scale} 적용 완료", "OK");
    }

    private void ApplyChildScaleToAll(float scale)
    {
        int count = 0;

        for (int i = 0; i < prefabList.Count; i++)
        {
            var info = prefabList[i];
            if (ApplyScaleToPrefab(info.path, false, 1f, true, scale))
            {
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        LoadPrefabList();

        EditorUtility.DisplayDialog("자식 VFX 스케일 적용 완료",
            $"{count}개 프리팹에 자식 VFX 스케일 {scale} 적용 완료", "OK");
    }

    private bool ApplyScaleToPrefab(string assetPath, bool setRoot, float rootVal, bool setChild, float childVal)
    {
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
        if (prefabContents == null) return false;

        bool applied = false;

        // 루트 스케일 적용
        if (setRoot)
        {
            prefabContents.transform.localScale = Vector3.one * rootVal;
            applied = true;
        }

        // 자식 VFX 스케일 적용
        // LMJ: SkillProjectile 클래스 삭제됨 - 첫 번째 자식에 직접 적용
        if (setChild)
        {
            if (prefabContents.transform.childCount > 0)
            {
                Transform firstChild = prefabContents.transform.GetChild(0);
                firstChild.localScale = Vector3.one * childVal;
                applied = true;
            }
        }

        if (applied)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
            Debug.Log($"[EffectScaleEditor] Applied scale to: {Path.GetFileName(assetPath)} " +
                      $"(Root: {(setRoot ? rootVal.ToString("F2") : "unchanged")}, " +
                      $"Child: {(setChild ? childVal.ToString("F2") : "unchanged")})");
        }

        PrefabUtility.UnloadPrefabContents(prefabContents);
        return applied;
    }
}
