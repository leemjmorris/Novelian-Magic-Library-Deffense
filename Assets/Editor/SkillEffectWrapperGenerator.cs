using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스킬 이펙트 래퍼 프리팹 생성기
/// SkillEffectDatabase의 에셋 프리팹을 래핑하여 풀링 가능한 프리팹 생성
/// </summary>
public class SkillEffectWrapperGenerator : EditorWindow
{
    // 설정
    private SkillEffectDatabase effectDatabase;
    private string outputPath = "Assets/Prefabs/SkillEffects";
    private bool overwriteExisting = false;
    private bool addToAddressables = false;

    // 진행 상태
    private Vector2 scrollPos;
    private List<GenerationResult> results = new List<GenerationResult>();
    private bool isGenerating = false;

    // 결과 클래스
    private class GenerationResult
    {
        public int skillId;
        public string skillName;
        public bool success;
        public string message;
        public string prefabPath;
    }

    [MenuItem("Tools/Skills/Wrapper Prefab Generator", false, 102)]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillEffectWrapperGenerator>("Wrapper Generator");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    private void OnEnable()
    {
        LoadDatabase();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Skill Effect Wrapper Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // 데이터베이스 설정
        EditorGUILayout.LabelField("Database", EditorStyles.boldLabel);
        effectDatabase = (SkillEffectDatabase)EditorGUILayout.ObjectField(
            "Effect Database", effectDatabase, typeof(SkillEffectDatabase), false);

        EditorGUILayout.Space(10);

        // 출력 설정
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                // Assets 상대 경로로 변환
                if (selected.StartsWith(Application.dataPath))
                {
                    outputPath = "Assets" + selected.Substring(Application.dataPath.Length);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        addToAddressables = EditorGUILayout.Toggle("Add to Addressables", addToAddressables);

        EditorGUILayout.Space(20);

        // 생성 버튼
        EditorGUI.BeginDisabledGroup(effectDatabase == null || isGenerating);

        if (GUILayout.Button("Generate All Wrapper Prefabs", GUILayout.Height(30)))
        {
            GenerateAllWrappers();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Unmapped Only"))
        {
            GenerateUnmappedWrappers();
        }
        if (GUILayout.Button("Validate Existing"))
        {
            ValidateExistingPrefabs();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(20);

        // 결과 표시
        if (results.Count > 0)
        {
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

            // 요약
            int successCount = 0;
            int failCount = 0;
            foreach (var r in results)
            {
                if (r.success) successCount++;
                else failCount++;
            }
            EditorGUILayout.LabelField($"Success: {successCount} | Failed: {failCount}");

            EditorGUILayout.Space(5);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));

            foreach (var result in results)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // 상태 아이콘
                EditorGUILayout.LabelField(result.success ? "[OK]" : "[X]",
                    result.success ? GetOkStyle() : GetErrorStyle(), GUILayout.Width(40));

                // 스킬 정보
                EditorGUILayout.LabelField($"ID: {result.skillId}", GUILayout.Width(60));
                EditorGUILayout.LabelField(result.skillName, GUILayout.Width(100));

                // 메시지
                EditorGUILayout.LabelField(result.message);

                // 선택 버튼
                if (result.success && !string.IsNullOrEmpty(result.prefabPath))
                {
                    if (GUILayout.Button("Select", GUILayout.Width(50)))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(result.prefabPath);
                        Selection.activeObject = prefab;
                        EditorGUIUtility.PingObject(prefab);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear Results"))
            {
                results.Clear();
            }
        }
    }

    #region Generation

    private void GenerateAllWrappers()
    {
        if (effectDatabase == null) return;

        results.Clear();
        isGenerating = true;

        try
        {
            EnsureOutputFolder();

            int processed = 0;
            int total = effectDatabase.entries.Count;

            foreach (var entry in effectDatabase.entries)
            {
                EditorUtility.DisplayProgressBar("Generating Wrappers",
                    $"Processing {entry.skillName} ({processed}/{total})",
                    (float)processed / total);

                var result = GenerateWrapperForEntry(entry);
                results.Add(result);
                processed++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            isGenerating = false;
            AssetDatabase.Refresh();
        }
    }

    private void GenerateUnmappedWrappers()
    {
        if (effectDatabase == null) return;

        results.Clear();
        isGenerating = true;

        try
        {
            EnsureOutputFolder();

            int processed = 0;
            var unmappedEntries = new List<SkillEffectEntry>();

            foreach (var entry in effectDatabase.entries)
            {
                if (entry.HasMainEffect() && string.IsNullOrEmpty(entry.wrapperPrefabPath))
                {
                    unmappedEntries.Add(entry);
                }
            }

            int total = unmappedEntries.Count;

            foreach (var entry in unmappedEntries)
            {
                EditorUtility.DisplayProgressBar("Generating Wrappers",
                    $"Processing {entry.skillName} ({processed}/{total})",
                    (float)processed / total);

                var result = GenerateWrapperForEntry(entry);
                results.Add(result);
                processed++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            isGenerating = false;
            AssetDatabase.Refresh();
        }
    }

    private GenerationResult GenerateWrapperForEntry(SkillEffectEntry entry)
    {
        var result = new GenerationResult
        {
            skillId = entry.skillId,
            skillName = entry.skillName ?? $"Skill_{entry.skillId}"
        };

        // 메인 이펙트 확인
        if (entry.mainEffectPrefab == null)
        {
            result.success = false;
            result.message = "No main effect prefab assigned";
            return result;
        }

        // 출력 경로
        string prefabName = $"SkillEffectWrapper_{entry.skillId}.prefab";
        string prefabPath = Path.Combine(outputPath, prefabName).Replace("\\", "/");

        // 기존 파일 확인
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null && !overwriteExisting)
        {
            result.success = true;
            result.message = "Already exists (skipped)";
            result.prefabPath = prefabPath;
            entry.wrapperPrefabPath = prefabPath;
            return result;
        }

        try
        {
            // 래퍼 오브젝트 생성
            var wrapperObj = new GameObject($"SkillEffectWrapper_{entry.skillId}");

            // 래퍼 컴포넌트 추가
            var wrapper = wrapperObj.AddComponent<SkillEffectWrapper>();
            wrapper.skillId = entry.skillId;

            // 에셋 프리팹을 자식으로 추가
            var effectInstance = (GameObject)PrefabUtility.InstantiatePrefab(entry.mainEffectPrefab);
            effectInstance.transform.SetParent(wrapperObj.transform);
            effectInstance.transform.localPosition = Vector3.zero;
            effectInstance.transform.localRotation = Quaternion.identity;
            effectInstance.name = entry.mainEffectPrefab.name;

            // 프리팹 저장
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(wrapperObj, prefabPath);

            // 임시 오브젝트 삭제
            DestroyImmediate(wrapperObj);

            if (savedPrefab != null)
            {
                result.success = true;
                result.message = "Created successfully";
                result.prefabPath = prefabPath;

                // 데이터베이스에 경로 저장
                entry.wrapperPrefabPath = prefabPath;
                EditorUtility.SetDirty(effectDatabase);

                // Addressables 등록 (옵션)
                if (addToAddressables)
                {
                    AddToAddressables(savedPrefab, entry.skillId);
                }
            }
            else
            {
                result.success = false;
                result.message = "Failed to save prefab";
            }
        }
        catch (System.Exception ex)
        {
            result.success = false;
            result.message = $"Error: {ex.Message}";
            Debug.LogError($"[WrapperGenerator] Error generating wrapper for skill {entry.skillId}: {ex}");
        }

        return result;
    }

    private void ValidateExistingPrefabs()
    {
        if (effectDatabase == null) return;

        results.Clear();

        foreach (var entry in effectDatabase.entries)
        {
            var result = new GenerationResult
            {
                skillId = entry.skillId,
                skillName = entry.skillName ?? $"Skill_{entry.skillId}"
            };

            if (string.IsNullOrEmpty(entry.wrapperPrefabPath))
            {
                result.success = false;
                result.message = "No wrapper path set";
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.wrapperPrefabPath);
                if (prefab == null)
                {
                    result.success = false;
                    result.message = "Prefab not found at path";
                    entry.wrapperPrefabPath = null;
                    EditorUtility.SetDirty(effectDatabase);
                }
                else
                {
                    var wrapper = prefab.GetComponent<SkillEffectWrapper>();
                    if (wrapper == null)
                    {
                        result.success = false;
                        result.message = "Missing SkillEffectWrapper component";
                    }
                    else
                    {
                        result.success = true;
                        result.message = "Valid";
                        result.prefabPath = entry.wrapperPrefabPath;
                    }
                }
            }

            results.Add(result);
        }
    }

    #endregion

    #region Utility

    private void LoadDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:SkillEffectDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            effectDatabase = AssetDatabase.LoadAssetAtPath<SkillEffectDatabase>(path);
        }
    }

    private void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder(outputPath))
        {
            // 경로를 단계별로 생성
            string[] parts = outputPath.Split('/');
            string currentPath = parts[0]; // "Assets"

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
            }
        }
    }

    private void AddToAddressables(GameObject prefab, int skillId)
    {
#if ADDRESSABLES_ENABLED
        try
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var entry = settings.CreateOrMoveEntry(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)),
                settings.DefaultGroup);

            if (entry != null)
            {
                entry.address = $"SkillEffect_{skillId}";
                entry.SetLabel("SkillEffects", true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[WrapperGenerator] Failed to add to Addressables: {ex.Message}");
        }
#endif
    }

    private GUIStyle _okStyle;
    private GUIStyle _errorStyle;

    private GUIStyle GetOkStyle()
    {
        if (_okStyle == null)
        {
            _okStyle = new GUIStyle(EditorStyles.label);
            _okStyle.normal.textColor = Color.green;
        }
        return _okStyle;
    }

    private GUIStyle GetErrorStyle()
    {
        if (_errorStyle == null)
        {
            _errorStyle = new GUIStyle(EditorStyles.label);
            _errorStyle.normal.textColor = Color.red;
        }
        return _errorStyle;
    }

    #endregion
}
