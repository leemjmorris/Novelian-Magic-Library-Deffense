#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 에셋팩에서 누락된 이펙트를 찾아 프리팹으로 생성하는 에디터 툴
/// </summary>
public class MissingEffectPrefabGenerator : EditorWindow
{
    // 경로 상수
    private const string ASSET_PACK_NOTSCRIPT_PATH = "Assets/SpecialSkillsEffectsPack/AllEffects/EffectsSet_1(NotScriptBased)/Effects";
    private const string ASSET_PACK_SCRIPT_PATH = "Assets/SpecialSkillsEffectsPack/AllEffects/EffectsSet_2(ScriptBased)/Effects";
    private const string OUTPUT_NOTSCRIPT_PATH = "Assets/03. Prefabs/SpecialSkillEffects/NotScriptBased";
    private const string OUTPUT_SCRIPT_PATH = "Assets/03. Prefabs/SpecialSkillEffects/ScriptBased";
    private const string DEMO_SCENE_PATH = "Assets/SpecialSkillsEffectsPack/EffectsScenes/EffectsScene.unity";

    // UI 상태
    private Vector2 scrollPos;
    private List<EffectInfo> notScriptBasedEffects = new List<EffectInfo>();
    private List<EffectInfo> scriptBasedEffects = new List<EffectInfo>();
    private List<EffectInfo> allPrefabEffects = new List<EffectInfo>(); // 에셋팩 전체 프리팹
    private bool showNotScriptBased = true;
    private bool showScriptBased = true;
    private bool showAllPrefabs = false;
    private int scanMode = 0; // 0=폴더스캔, 1=전체프리팹스캔

    private class EffectInfo
    {
        public string Name;
        public string SourcePath;
        public string TargetPath;
        public bool Exists;
        public bool Selected;
        public bool IsScriptBased;
        public List<string> SubEffects = new List<string>(); // 하위 이펙트 (예: LightningTornado 안에 StormTornado)
    }

    [MenuItem("Tools/Skills/Missing Effect Prefab Generator", false, 200)]
    public static void ShowWindow()
    {
        var window = GetWindow<MissingEffectPrefabGenerator>("Missing Effect Generator");
        window.minSize = new Vector2(800, 600);
    }

    private void OnEnable()
    {
        ScanEffects();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Missing Effect Prefab Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "에셋팩의 이펙트와 현재 프리팹을 비교하여 누락된 이펙트를 식별합니다.\n" +
            "선택한 이펙트를 프리팹으로 생성할 수 있습니다.\n" +
            "전체 프리팹 스캔: 에셋팩의 모든 프리팹(388개)을 스캔합니다.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 스캔 모드 선택
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Scan Mode:", GUILayout.Width(80));
        scanMode = EditorGUILayout.Popup(scanMode, new[] { "Folder Scan (Main Effects)", "All Prefabs Scan (388)" }, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 버튼 바
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan Effects", GUILayout.Width(100)))
        {
            if (scanMode == 0)
                ScanEffects();
            else
                ScanAllPrefabs();
        }

        if (GUILayout.Button("Select Missing", GUILayout.Width(100)))
        {
            SelectAllMissing();
        }

        if (GUILayout.Button("Deselect All", GUILayout.Width(100)))
        {
            DeselectAll();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Generate Selected Prefabs", GUILayout.Width(180)))
        {
            GenerateSelectedPrefabs();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 통계
        int totalNotScript = notScriptBasedEffects.Count;
        int missingNotScript = 0;
        int totalScript = scriptBasedEffects.Count;
        int missingScript = 0;
        int totalAll = allPrefabEffects.Count;
        int missingAll = 0;

        foreach (var e in notScriptBasedEffects) if (!e.Exists) missingNotScript++;
        foreach (var e in scriptBasedEffects) if (!e.Exists) missingScript++;
        foreach (var e in allPrefabEffects) if (!e.Exists) missingAll++;

        if (scanMode == 0)
        {
            EditorGUILayout.LabelField($"NotScriptBased: {totalNotScript} total, {missingNotScript} missing | ScriptBased: {totalScript} total, {missingScript} missing");
        }
        else
        {
            EditorGUILayout.LabelField($"All Prefabs: {totalAll} total, {missingAll} missing");
        }

        EditorGUILayout.Space(5);

        // 이펙트 목록
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (scanMode == 0)
        {
            // NotScriptBased
            showNotScriptBased = EditorGUILayout.Foldout(showNotScriptBased, $"NotScriptBased Effects ({totalNotScript})");
            if (showNotScriptBased)
            {
                EditorGUI.indentLevel++;
                DrawEffectList(notScriptBasedEffects);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // ScriptBased
            showScriptBased = EditorGUILayout.Foldout(showScriptBased, $"ScriptBased Effects ({totalScript})");
            if (showScriptBased)
            {
                EditorGUI.indentLevel++;
                DrawEffectList(scriptBasedEffects);
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            // All Prefabs
            showAllPrefabs = EditorGUILayout.Foldout(showAllPrefabs, $"All Prefabs ({totalAll})");
            if (showAllPrefabs)
            {
                EditorGUI.indentLevel++;
                DrawEffectList(allPrefabEffects);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEffectList(List<EffectInfo> effects)
    {
        foreach (var effect in effects)
        {
            EditorGUILayout.BeginHorizontal();

            // 체크박스 (없는 것만 선택 가능)
            GUI.enabled = !effect.Exists;
            effect.Selected = EditorGUILayout.Toggle(effect.Selected, GUILayout.Width(20));
            GUI.enabled = true;

            // 상태 아이콘
            if (effect.Exists)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✓", GUILayout.Width(20));
            }
            else
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("✗", GUILayout.Width(20));
            }
            GUI.color = Color.white;

            // 이름
            EditorGUILayout.LabelField(effect.Name, GUILayout.Width(250));

            // 하위 이펙트 수
            if (effect.SubEffects.Count > 0)
            {
                EditorGUILayout.LabelField($"+{effect.SubEffects.Count} sub", EditorStyles.miniLabel, GUILayout.Width(60));
            }

            // 소스 경로 (축약)
            string shortPath = effect.SourcePath.Replace("Assets/SpecialSkillsEffectsPack/AllEffects/", "");
            EditorGUILayout.LabelField(shortPath, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();

            // 하위 이펙트 표시
            if (effect.SubEffects.Count > 0)
            {
                EditorGUI.indentLevel += 2;
                foreach (var sub in effect.SubEffects)
                {
                    EditorGUILayout.LabelField($"└ {sub}", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel -= 2;
            }
        }
    }

    private void ScanEffects()
    {
        notScriptBasedEffects.Clear();
        scriptBasedEffects.Clear();

        // NotScriptBased 스캔
        ScanEffectFolder(ASSET_PACK_NOTSCRIPT_PATH, OUTPUT_NOTSCRIPT_PATH, notScriptBasedEffects);

        // ScriptBased 스캔
        ScanEffectFolder(ASSET_PACK_SCRIPT_PATH, OUTPUT_SCRIPT_PATH, scriptBasedEffects);

        Debug.Log($"[MissingEffectGenerator] Scanned: NotScriptBased={notScriptBasedEffects.Count}, ScriptBased={scriptBasedEffects.Count}");
    }

    private void ScanEffectFolder(string sourcePath, string targetPath, List<EffectInfo> effectList)
    {
        if (!Directory.Exists(sourcePath))
        {
            Debug.LogWarning($"Source path not found: {sourcePath}");
            return;
        }

        string[] effectFolders = Directory.GetDirectories(sourcePath);

        foreach (string folder in effectFolders)
        {
            string folderName = Path.GetFileName(folder);

            // Effect_XX_ 패턴 확인
            if (!folderName.StartsWith("Effect_"))
                continue;

            var effectInfo = new EffectInfo
            {
                Name = folderName,
                SourcePath = folder.Replace("\\", "/"),
                TargetPath = $"{targetPath}/{folderName}.prefab"
            };

            // 타겟 프리팹 존재 여부 확인
            effectInfo.Exists = File.Exists(effectInfo.TargetPath);

            // 하위 이펙트 폴더 스캔 (예: Effect_01_StormTornado 안에 Effect_01_LightningTornado)
            string[] subFolders = Directory.GetDirectories(folder);
            foreach (string subFolder in subFolders)
            {
                string subName = Path.GetFileName(subFolder);
                if (subName.StartsWith("Effect_") && !subName.Contains("Parts") && !subName.Contains("Base"))
                {
                    effectInfo.SubEffects.Add(subName);

                    // 하위 이펙트도 별도 프리팹으로 체크
                    string subTargetPath = $"{targetPath}/{subName}.prefab";
                    if (!File.Exists(subTargetPath))
                    {
                        // 하위 이펙트를 별도 항목으로 추가
                        var subInfo = new EffectInfo
                        {
                            Name = subName,
                            SourcePath = subFolder.Replace("\\", "/"),
                            TargetPath = subTargetPath,
                            Exists = false
                        };
                        effectList.Add(subInfo);
                    }
                }
            }

            effectList.Add(effectInfo);
        }

        // 이름순 정렬
        effectList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
    }

    private void SelectAllMissing()
    {
        foreach (var e in notScriptBasedEffects)
            if (!e.Exists) e.Selected = true;
        foreach (var e in scriptBasedEffects)
            if (!e.Exists) e.Selected = true;
        foreach (var e in allPrefabEffects)
            if (!e.Exists) e.Selected = true;
    }

    private void DeselectAll()
    {
        foreach (var e in notScriptBasedEffects) e.Selected = false;
        foreach (var e in scriptBasedEffects) e.Selected = false;
        foreach (var e in allPrefabEffects) e.Selected = false;
    }

    /// <summary>
    /// 에셋팩의 모든 프리팹을 스캔 (388개)
    /// </summary>
    private void ScanAllPrefabs()
    {
        allPrefabEffects.Clear();

        // 현재 프로젝트에 있는 프리팹 목록 (이름 기준)
        HashSet<string> existingPrefabs = new HashSet<string>();

        string[] existingGuids = AssetDatabase.FindAssets("t:Prefab", new[] { OUTPUT_NOTSCRIPT_PATH, OUTPUT_SCRIPT_PATH });
        foreach (string guid in existingGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            existingPrefabs.Add(name);
        }

        // 에셋팩에서 모든 프리팹 스캔
        string[] assetPackPaths = new[]
        {
            "Assets/SpecialSkillsEffectsPack/AllEffects/EffectsSet_1(NotScriptBased)",
            "Assets/SpecialSkillsEffectsPack/AllEffects/EffectsSet_2(ScriptBased)"
        };

        foreach (string basePath in assetPackPaths)
        {
            bool isScriptBased = basePath.Contains("ScriptBased") && !basePath.Contains("NotScript");

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { basePath });

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);

                // Parts, Hit, Base 프리팹은 제외 (조합용)
                if (name.Contains("_Parts") || name.Contains("_Hit") || name.Contains("_Base") ||
                    name.EndsWith("Parts") || name.EndsWith("Hit") || name.EndsWith("Base"))
                    continue;

                // 메인 이펙트만 포함 (Effect_XX_ 패턴)
                if (!name.StartsWith("Effect_"))
                    continue;

                string outputPath = isScriptBased ? OUTPUT_SCRIPT_PATH : OUTPUT_NOTSCRIPT_PATH;

                var info = new EffectInfo
                {
                    Name = name,
                    SourcePath = path,
                    TargetPath = $"{outputPath}/{name}.prefab",
                    Exists = existingPrefabs.Contains(name),
                    IsScriptBased = isScriptBased
                };

                // 중복 방지
                bool isDuplicate = false;
                foreach (var existing in allPrefabEffects)
                {
                    if (existing.Name == name)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    allPrefabEffects.Add(info);
                }
            }
        }

        // 이름순 정렬
        allPrefabEffects.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        Debug.Log($"[MissingEffectGenerator] Scanned all prefabs: {allPrefabEffects.Count} total");
    }

    private void GenerateSelectedPrefabs()
    {
        int generatedCount = 0;

        if (scanMode == 0)
        {
            // NotScriptBased
            foreach (var effect in notScriptBasedEffects)
            {
                if (effect.Selected && !effect.Exists)
                {
                    if (GeneratePrefab(effect, OUTPUT_NOTSCRIPT_PATH))
                        generatedCount++;
                }
            }

            // ScriptBased
            foreach (var effect in scriptBasedEffects)
            {
                if (effect.Selected && !effect.Exists)
                {
                    if (GeneratePrefab(effect, OUTPUT_SCRIPT_PATH))
                        generatedCount++;
                }
            }
        }
        else
        {
            // All Prefabs 모드
            foreach (var effect in allPrefabEffects)
            {
                if (effect.Selected && !effect.Exists)
                {
                    string outputPath = effect.IsScriptBased ? OUTPUT_SCRIPT_PATH : OUTPUT_NOTSCRIPT_PATH;
                    if (GeneratePrefabDirect(effect, outputPath))
                        generatedCount++;
                }
            }
        }

        AssetDatabase.Refresh();

        // 재스캔
        if (scanMode == 0)
            ScanEffects();
        else
            ScanAllPrefabs();

        EditorUtility.DisplayDialog("Complete", $"Generated {generatedCount} prefabs.", "OK");
    }

    /// <summary>
    /// 소스 프리팹 경로가 직접 지정된 경우 (전체 스캔 모드용)
    /// </summary>
    private bool GeneratePrefabDirect(EffectInfo effect, string outputPath)
    {
        try
        {
            GameObject sourceObject = AssetDatabase.LoadAssetAtPath<GameObject>(effect.SourcePath);

            if (sourceObject == null)
            {
                Debug.LogWarning($"[MissingEffectGenerator] Source prefab not found: {effect.SourcePath}");
                return false;
            }

            // 출력 디렉토리 확인
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // 프리팹 복사/생성
            string targetPath = $"{outputPath}/{effect.Name}.prefab";

            // 기존 프리팹이 있으면 건너뛰기
            if (File.Exists(targetPath))
            {
                Debug.Log($"[MissingEffectGenerator] Prefab already exists: {targetPath}");
                return false;
            }

            // 인스턴스 생성 후 프리팹으로 저장
            GameObject instance = PrefabUtility.InstantiatePrefab(sourceObject) as GameObject;
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(sourceObject);
            }

            instance.name = effect.Name;

            // 프리팹으로 저장
            PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            DestroyImmediate(instance);

            Debug.Log($"[MissingEffectGenerator] Created prefab: {targetPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[MissingEffectGenerator] Failed to generate {effect.Name}: {e.Message}");
            return false;
        }
    }

    private bool GeneratePrefab(EffectInfo effect, string outputPath)
    {
        try
        {
            // 소스 폴더에서 메인 이펙트 게임오브젝트 찾기
            string effectName = effect.Name;

            // 소스 폴더 내의 프리팹이나 씬 오브젝트 찾기
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { effect.SourcePath });

            GameObject sourceObject = null;

            // 1. 먼저 동일 이름의 프리팹 찾기
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                // 정확히 같은 이름이거나, 메인 이펙트 이름인 경우
                if (fileName == effectName ||
                    fileName == effectName.Replace("Effect_", "") ||
                    path.Contains($"/{effectName}.prefab"))
                {
                    sourceObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    break;
                }
            }

            // 2. 못 찾으면 첫 번째 프리팹 사용 (Parts, Base 제외)
            if (sourceObject == null)
            {
                foreach (string guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileNameWithoutExtension(path);

                    if (!fileName.Contains("Parts") && !fileName.Contains("Base") && !fileName.Contains("Hit"))
                    {
                        sourceObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        break;
                    }
                }
            }

            if (sourceObject == null)
            {
                Debug.LogWarning($"[MissingEffectGenerator] No source prefab found for: {effectName}");
                return false;
            }

            // 출력 디렉토리 확인
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // 프리팹 복사/생성
            string targetPath = $"{outputPath}/{effectName}.prefab";

            // 기존 프리팹이 있으면 건너뛰기
            if (File.Exists(targetPath))
            {
                Debug.Log($"[MissingEffectGenerator] Prefab already exists: {targetPath}");
                return false;
            }

            // 인스턴스 생성 후 프리팹으로 저장
            GameObject instance = PrefabUtility.InstantiatePrefab(sourceObject) as GameObject;
            if (instance == null)
            {
                instance = Instantiate(sourceObject);
            }

            instance.name = effectName;

            // 프리팹으로 저장
            PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            DestroyImmediate(instance);

            Debug.Log($"[MissingEffectGenerator] Created prefab: {targetPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[MissingEffectGenerator] Failed to generate {effect.Name}: {e.Message}");
            return false;
        }
    }
}
#endif
