// SkillProjectileWrapperGenerator.cs
// SpecialSkillsEffectsPack의 EffectsScene에 정의된 182개 이펙트를 SkillProjectile 래퍼 프리팹으로 변환하는 에디터 도구
// Updated: 2025-12-19

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Novelian.Combat;

public class SkillProjectileWrapperGenerator : EditorWindow
{
    // 에셋 경로
    private const string EFFECTS_SCENE_PATH = "Assets/SpecialSkillsEffectsPack/EffectsScenes/EffectsScene.unity";
    private const string SCRIPT_BASED_PATH = "Assets/SpecialSkillsEffectsPack/AllEffects/EffectsSet_2(ScriptBased)";
    private const string OUTPUT_PATH = "Assets/03. Prefabs/SpecialSkillEffects";

    // 설정
    private float defaultColliderRadius = 0.5f;
    private bool overwriteExisting = false;
    private Vector2 scrollPosition;

    // 생성 결과
    private List<string> generatedPrefabs = new List<string>();
    private List<string> skippedPrefabs = new List<string>();
    private List<string> errorPrefabs = new List<string>();

    // 182개 GUID 캐시
    private List<string> cachedGuids = null;

    [MenuItem("Tools/Skill System/Generate SkillProjectile Wrappers")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillProjectileWrapperGenerator>("SkillProjectile Generator");
        window.minSize = new Vector2(500, 650);
    }

    [MenuItem("Tools/Skill System/Delete All SkillProjectile Wrappers")]
    public static void DeleteAllWrappers()
    {
        if (!EditorUtility.DisplayDialog("Delete All Wrappers",
            "정말로 생성된 모든 SkillProjectile 래퍼 프리팹을 삭제하시겠습니까?\n\n" +
            $"경로: {OUTPUT_PATH}/NotScriptBased/*\n" +
            $"경로: {OUTPUT_PATH}/ScriptBased/*",
            "삭제", "취소"))
        {
            return;
        }

        int deletedCount = 0;

        string[] subFolders = { "NotScriptBased", "ScriptBased" };
        foreach (var folder in subFolders)
        {
            string folderPath = $"{OUTPUT_PATH}/{folder}";
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (var guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.DeleteAsset(prefabPath))
                {
                    deletedCount++;
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[SkillProjectileGenerator] Deleted {deletedCount} wrapper prefabs.");
        EditorUtility.DisplayDialog("Deletion Complete", $"{deletedCount}개의 래퍼 프리팹이 삭제되었습니다.", "OK");
    }

    [MenuItem("Tools/Skill System/Regenerate All Wrappers (Delete + Generate)")]
    public static void RegenerateAllWrappers()
    {
        if (!EditorUtility.DisplayDialog("Regenerate All Wrappers",
            "모든 래퍼 프리팹을 삭제하고 다시 생성합니다.\n계속하시겠습니까?",
            "재생성", "취소"))
        {
            return;
        }

        RegenerateAllWrappersNoConfirm();
    }

    /// <summary>
    /// 확인 대화상자 없이 바로 재생성 (MCP 등 자동화 용도)
    /// </summary>
    [MenuItem("Tools/Skill System/Force Regenerate All (No Confirm)")]
    public static void RegenerateAllWrappersNoConfirm()
    {
        // 삭제
        int deletedCount = 0;
        string[] subFolders = { "NotScriptBased", "ScriptBased" };
        foreach (var folder in subFolders)
        {
            string folderPath = $"{OUTPUT_PATH}/{folder}";
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (var guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.DeleteAsset(prefabPath))
                {
                    deletedCount++;
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[SkillProjectileGenerator] Deleted {deletedCount} wrapper prefabs.");

        // 창 열고 생성 실행
        var window = GetWindow<SkillProjectileWrapperGenerator>("SkillProjectile Generator");
        window.overwriteExisting = true;
        window.GenerateAllWrappers();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("SkillProjectile Wrapper Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "EffectsScene.unity의 m_effects 배열에 정의된 정확히 182개의 이펙트를\n" +
            "SkillProjectile 래퍼 프리팹으로 변환합니다.\n\n" +
            "각 프리팹은 Collider, Rigidbody, SkillProjectile 컴포넌트를 포함하며,\n" +
            "원본 이펙트는 VFX_Main 자식으로 배치됩니다.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 설정
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        defaultColliderRadius = EditorGUILayout.FloatField("Default Collider Radius", defaultColliderRadius);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);

        EditorGUILayout.Space(10);

        // 경로 정보
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("EffectsScene:", EFFECTS_SCENE_PATH);
        EditorGUILayout.LabelField("Output:", OUTPUT_PATH);

        EditorGUILayout.Space(20);

        // 버튼
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Preview Effects", GUILayout.Height(30)))
        {
            PreviewEffects();
        }

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Generate All Wrappers", GUILayout.Height(30)))
        {
            GenerateAllWrappers();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 결과 표시
        if (generatedPrefabs.Count > 0 || skippedPrefabs.Count > 0 || errorPrefabs.Count > 0)
        {
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

            if (generatedPrefabs.Count > 0)
            {
                EditorGUILayout.LabelField($"Generated ({generatedPrefabs.Count}):", EditorStyles.boldLabel);
                foreach (var prefab in generatedPrefabs)
                {
                    EditorGUILayout.LabelField("  ✓ " + prefab);
                }
            }

            if (skippedPrefabs.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Skipped ({skippedPrefabs.Count}):", EditorStyles.boldLabel);
                foreach (var prefab in skippedPrefabs)
                {
                    EditorGUILayout.LabelField("  - " + prefab);
                }
            }

            if (errorPrefabs.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Errors ({errorPrefabs.Count}):", EditorStyles.boldLabel);
                foreach (var prefab in errorPrefabs)
                {
                    EditorGUILayout.LabelField("  ✗ " + prefab);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void PreviewEffects()
    {
        generatedPrefabs.Clear();
        skippedPrefabs.Clear();
        errorPrefabs.Clear();

        // EffectsScene에서 182개 GUID 추출
        var guids = GetEffectsSceneGuids();
        Debug.Log($"[Preview] Found {guids.Count} GUIDs in EffectsScene.unity");

        int index = 0;
        foreach (var guid in guids)
        {
            index++;
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(prefabPath))
            {
                errorPrefabs.Add($"[{index:D3}] GUID not found: {guid}");
                continue;
            }

            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            // EffectsSet_2(ScriptBased) 폴더에 있으면 ScriptBased, 아니면 NotScriptBased
            // 주의: "NotScriptBased" 문자열에도 "ScriptBased"가 포함되므로 EffectsSet_2로 체크
            bool isScriptBased = prefabPath.Contains("EffectsSet_2");
            string category = isScriptBased ? "ScriptBased" : "NotScriptBased";

            generatedPrefabs.Add($"[{index:D3}] [{category}] {prefabName}");
        }

        Debug.Log($"[Preview] Total: {generatedPrefabs.Count} valid prefabs, {errorPrefabs.Count} errors");
    }

    private void GenerateAllWrappers()
    {
        generatedPrefabs.Clear();
        skippedPrefabs.Clear();
        errorPrefabs.Clear();

        // 출력 폴더 생성
        EnsureOutputFolders();

        int totalGenerated = 0;
        int totalSkipped = 0;
        int totalErrors = 0;

        // EffectsScene에서 182개 GUID 추출
        var guids = GetEffectsSceneGuids();
        Debug.Log($"[Generator] Processing {guids.Count} effects from EffectsScene.unity");

        int index = 0;
        foreach (var guid in guids)
        {
            index++;
            EditorUtility.DisplayProgressBar("Generating SkillProjectile Wrappers",
                $"Processing {index}/{guids.Count}...", (float)index / guids.Count);

            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(prefabPath))
            {
                errorPrefabs.Add($"[{index:D3}] GUID not found: {guid}");
                totalErrors++;
                continue;
            }

            GameObject originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (originalPrefab == null)
            {
                errorPrefabs.Add($"[{index:D3}] Failed to load: {prefabPath}");
                totalErrors++;
                continue;
            }

            // EffectsSet_2(ScriptBased) 폴더에 있으면 ScriptBased, 아니면 NotScriptBased
            // 주의: "NotScriptBased" 문자열에도 "ScriptBased"가 포함되므로 EffectsSet_2로 체크
            bool isScriptBased = prefabPath.Contains("EffectsSet_2");
            string category = isScriptBased ? "ScriptBased" : "NotScriptBased";

            var result = GenerateWrapperFromPrefab(originalPrefab, category, isScriptBased, index);
            switch (result)
            {
                case GenerationResult.Generated: totalGenerated++; break;
                case GenerationResult.Skipped: totalSkipped++; break;
                case GenerationResult.Error: totalErrors++; break;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SkillProjectileGenerator] Complete! Generated: {totalGenerated}, Skipped: {totalSkipped}, Errors: {totalErrors}");
        EditorUtility.DisplayDialog("Generation Complete",
            $"Total Effects: {guids.Count}\nGenerated: {totalGenerated}\nSkipped: {totalSkipped}\nErrors: {totalErrors}", "OK");
    }

    private enum GenerationResult { Generated, Skipped, Error }

    /// <summary>
    /// EffectsScene.unity에서 m_effects 배열의 182개 GUID 추출
    /// </summary>
    private List<string> GetEffectsSceneGuids()
    {
        if (cachedGuids != null)
        {
            return cachedGuids;
        }

        cachedGuids = new List<string>();

        string fullPath = Path.Combine(Application.dataPath, "..", EFFECTS_SCENE_PATH);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[SkillProjectileGenerator] EffectsScene not found: {fullPath}");
            return cachedGuids;
        }

        string sceneContent = File.ReadAllText(fullPath);

        // m_effects 배열에서 GUID 추출: guid: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
        // 패턴: - {fileID: xxx, guid: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx, type: 3}
        Regex guidRegex = new Regex(@"guid:\s*([a-f0-9]{32})", RegexOptions.IgnoreCase);

        // m_effects: 시작점 찾기
        int startIndex = sceneContent.IndexOf("m_effects:");
        if (startIndex < 0)
        {
            Debug.LogError("[SkillProjectileGenerator] m_effects array not found in EffectsScene.unity");
            return cachedGuids;
        }

        // m_effects 배열 끝점 찾기 (다음 속성까지)
        int endIndex = sceneContent.IndexOf("\n  scaleform:", startIndex);
        if (endIndex < 0)
        {
            endIndex = sceneContent.IndexOf("\n  m_destroyObjects:", startIndex);
        }
        if (endIndex < 0)
        {
            endIndex = sceneContent.Length;
        }

        string effectsSection = sceneContent.Substring(startIndex, endIndex - startIndex);

        // 모든 GUID 추출
        MatchCollection matches = guidRegex.Matches(effectsSection);
        foreach (Match match in matches)
        {
            string guid = match.Groups[1].Value;
            cachedGuids.Add(guid);
        }

        Debug.Log($"[SkillProjectileGenerator] Extracted {cachedGuids.Count} GUIDs from EffectsScene.unity");
        return cachedGuids;
    }

    /// <summary>
    /// 프리팹에서 직접 래퍼 생성
    /// </summary>
    private GenerationResult GenerateWrapperFromPrefab(GameObject originalPrefab, string category, bool hasScript, int effectIndex)
    {
        string prefabName = originalPrefab.name;
        string outputFolder = $"{OUTPUT_PATH}/{category}";
        string outputPath = $"{outputFolder}/{prefabName}.prefab";
        string indexStr = $"[{effectIndex:D3}]";

        // 이미 존재하는지 확인
        if (!overwriteExisting && File.Exists(outputPath))
        {
            skippedPrefabs.Add($"{indexStr} [{category}] {prefabName} (already exists)");
            return GenerationResult.Skipped;
        }

        try
        {
            // 래퍼 GameObject 생성
            GameObject wrapperObj = new GameObject(prefabName);

            // SkillProjectile 컴포넌트 추가
            SkillProjectile skillProjectile = wrapperObj.AddComponent<SkillProjectile>();

            // Rigidbody 추가
            Rigidbody rb = wrapperObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // SphereCollider 추가
            SphereCollider col = wrapperObj.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = defaultColliderRadius;

            // 레이어 설정
            int projectileLayer = LayerMask.NameToLayer("Projectile");
            if (projectileLayer >= 0)
            {
                wrapperObj.layer = projectileLayer;
            }

            // 원본 이펙트를 자식으로 인스턴스화
            GameObject effectInstance = (GameObject)PrefabUtility.InstantiatePrefab(originalPrefab);
            effectInstance.transform.SetParent(wrapperObj.transform);
            effectInstance.transform.localPosition = Vector3.zero;
            effectInstance.transform.localRotation = Quaternion.identity;
            effectInstance.name = "VFX_Main";

            // SkillProjectile 참조 설정 (SerializedObject 사용)
            SerializedObject serializedProjectile = new SerializedObject(skillProjectile);

            // ScriptBased 이펙트: 에셋 스크립트를 활성화 상태로 유지
            // ObjectMoveDestroy/ObjectMove는 이펙트의 이동 애니메이션을 담당하므로 비활성화하면 안됨
            if (hasScript)
            {
                // useAssetMovement 플래그 설정 (에셋 스크립트가 이동을 담당)
                var useAssetMovementField = serializedProjectile.FindProperty("useAssetMovement");
                if (useAssetMovementField != null) useAssetMovementField.boolValue = true;
            }

            var rbField = serializedProjectile.FindProperty("rb");
            if (rbField != null) rbField.objectReferenceValue = rb;

            var colField = serializedProjectile.FindProperty("col");
            if (colField != null) colField.objectReferenceValue = col;

            var vfxMainField = serializedProjectile.FindProperty("vfxMain");
            if (vfxMainField != null) vfxMainField.objectReferenceValue = effectInstance;

            // hitEffectPrefab 찾기 (같은 폴더에서 Hit/Impact/Explosion 포함된 프리팹)
            string originalPath = AssetDatabase.GetAssetPath(originalPrefab);
            string originalFolder = Path.GetDirectoryName(originalPath);
            GameObject hitEffect = FindHitEffectPrefab(originalFolder);
            if (hitEffect != null)
            {
                var hitEffectField = serializedProjectile.FindProperty("hitEffectPrefab");
                if (hitEffectField != null) hitEffectField.objectReferenceValue = hitEffect;
            }

            serializedProjectile.ApplyModifiedPropertiesWithoutUndo();

            // 프리팹으로 저장
            EnsureDirectory(outputFolder);
            PrefabUtility.SaveAsPrefabAsset(wrapperObj, outputPath);

            // 임시 객체 삭제
            DestroyImmediate(wrapperObj);

            generatedPrefabs.Add($"{indexStr} [{category}] {prefabName}");
            Debug.Log($"[SkillProjectileGenerator] {indexStr} Created: {outputPath}");

            return GenerationResult.Generated;
        }
        catch (System.Exception ex)
        {
            errorPrefabs.Add($"{indexStr} [{category}] {prefabName} ({ex.Message})");
            Debug.LogError($"[SkillProjectileGenerator] {indexStr} Error creating {prefabName}: {ex}");
            return GenerationResult.Error;
        }
    }

    /// <summary>
    /// 같은 폴더에서 Hit/Impact/Explosion 프리팹 찾기
    /// </summary>
    private GameObject FindHitEffectPrefab(string effectFolderPath)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { effectFolderPath });

        foreach (var guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            // Hit 이펙트 프리팹 찾기
            if (prefabName.Contains("Hit") || prefabName.Contains("Impact") || prefabName.Contains("Explosion"))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
        }

        return null;
    }

    private void EnsureOutputFolders()
    {
        EnsureDirectory(OUTPUT_PATH);
        EnsureDirectory($"{OUTPUT_PATH}/NotScriptBased");
        EnsureDirectory($"{OUTPUT_PATH}/ScriptBased");
    }

    private void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}
