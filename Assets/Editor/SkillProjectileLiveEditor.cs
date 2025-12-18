// SkillProjectileLiveEditor.cs
// Play Mode에서 실시간으로 스킬 프리팹을 미리보고, CSV 값을 수정/저장할 수 있는 에디터 도구
// 확장: Scale 조정, AOE Gizmo, 프리팹 저장, 스킬 설정 UI

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Novelian.Combat;

public class SkillProjectileLiveEditor : EditorWindow
{
    // 탭
    private enum Tab { PrefabPreview, SkillConfigurator, CSVEditor, BatchOperations }
    private Tab currentTab = Tab.PrefabPreview;

    // 프리팹 미리보기
    private Vector2 prefabScrollPosition;
    private string prefabSearchFilter = "";
    private List<GameObject> skillProjectilePrefabs = new List<GameObject>();
    private GameObject selectedPrefab;
    private GameObject previewInstance;
    private bool autoRotate = false;
    private float rotationSpeed = 30f;

    // 실시간 조정 (Scale & AOE)
    private float previewScale = 1f;
    private float previewAoeRadius = 5f;
    private bool showAoeGizmo = true;
    private Color aoeGizmoColor = new Color(1f, 0f, 0f, 0.3f);
    private static SkillProjectileLiveEditor instance;

    // CSV 편집
    private Vector2 csvScrollPosition;
    private string csvSearchFilter = "";
    private List<MainSkillDataEditable> editableSkillData = new List<MainSkillDataEditable>();
    private new bool hasUnsavedChanges = false;
    private string csvPath = "Assets/Data/CSV/Skill/MainSkillTable.csv";

    // 스킬 설정 (새 탭)
    private Vector2 skillConfigScrollPosition;
    private List<SkillEffectConfig> skillEffectConfigs = new List<SkillEffectConfig>();
    private int selectedConfigIndex = -1;
    private string newSkillsCsvPath = "Assets/Data/CSV/Skill/NewEffectSkillTable.csv";

    // 실시간 테스트
    private Vector3 testSpawnPosition = Vector3.zero;
    private Vector3 testTargetPosition = new Vector3(0, 0, 10);
    private float testSpeed = 15f;
    private float testDamage = 100f;

    // Play Mode 상태
    private bool isPlayMode = false;

    // 스킬 타입 옵션
    private static readonly string[] skillTypeNames = new string[]
    {
        "Projectile (투사체)",
        "InstantSingle (즉발 단일)",
        "AOE (범위)",
        "DOT (지속 데미지)",
        "Buff (버프)",
        "Debuff (디버프)",
        "Channeling (채널링)",
        "Trap (함정)",
        "Mine (지뢰)"
    };
    private static readonly int[] skillTypeIds = new int[]
    {
        3000100, 3000201, 3000302, 3000403, 3000504, 3000605, 3000706, 3000807, 3000908
    };

    [MenuItem("Tools/Skill System/Skill Projectile Live Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillProjectileLiveEditor>("Skill Live Editor");
        window.minSize = new Vector2(900, 700);
        instance = window;
    }

    private void OnEnable()
    {
        instance = this;
        RefreshPrefabList();
        LoadCSVData();
        LoadSkillEffectConfigs();
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        CleanupPreview();
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        instance = null;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        isPlayMode = state == PlayModeStateChange.EnteredPlayMode;
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            CleanupPreview();
        }
    }

    private void OnGUI()
    {
        // 탭 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == Tab.PrefabPreview, "Prefab Preview", EditorStyles.toolbarButton))
            currentTab = Tab.PrefabPreview;
        if (GUILayout.Toggle(currentTab == Tab.SkillConfigurator, "Skill Configurator", EditorStyles.toolbarButton))
            currentTab = Tab.SkillConfigurator;
        if (GUILayout.Toggle(currentTab == Tab.CSVEditor, "CSV Editor", EditorStyles.toolbarButton))
            currentTab = Tab.CSVEditor;
        if (GUILayout.Toggle(currentTab == Tab.BatchOperations, "Batch Operations", EditorStyles.toolbarButton))
            currentTab = Tab.BatchOperations;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 현재 탭에 따른 내용 표시
        switch (currentTab)
        {
            case Tab.PrefabPreview:
                DrawPrefabPreviewTab();
                break;
            case Tab.SkillConfigurator:
                DrawSkillConfiguratorTab();
                break;
            case Tab.CSVEditor:
                DrawCSVEditorTab();
                break;
            case Tab.BatchOperations:
                DrawBatchOperationsTab();
                break;
        }
    }

    #region Scene GUI (Gizmo)

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showAoeGizmo || previewInstance == null) return;

        // AOE 범위 Gizmo 그리기
        Handles.color = aoeGizmoColor;
        Vector3 center = previewInstance.transform.position;

        // 원형 디스크 (바닥)
        Handles.DrawSolidDisc(center, Vector3.up, previewAoeRadius);

        // 외곽선 (더 진하게)
        Handles.color = new Color(1f, 0f, 0f, 0.8f);
        Handles.DrawWireDisc(center, Vector3.up, previewAoeRadius);

        // 반지름 표시 라인
        Handles.DrawLine(center, center + Vector3.forward * previewAoeRadius);
        Handles.DrawLine(center, center + Vector3.right * previewAoeRadius);
        Handles.DrawLine(center, center + Vector3.back * previewAoeRadius);
        Handles.DrawLine(center, center + Vector3.left * previewAoeRadius);

        // 라벨
        Handles.Label(center + Vector3.up * 2f + Vector3.forward * previewAoeRadius,
            $"AOE Radius: {previewAoeRadius:F1}",
            EditorStyles.whiteBoldLabel);

        sceneView.Repaint();
    }

    #endregion

    #region Prefab Preview Tab

    private void DrawPrefabPreviewTab()
    {
        EditorGUILayout.LabelField("SkillProjectile Prefab Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "래퍼 프리팹을 미리보고 Scale/AOE를 조정합니다.\n" +
            "빨간 원 = 데미지 적용 범위 (AOE Radius)",
            MessageType.Info);

        EditorGUILayout.Space(5);

        // 검색 필터
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        string newFilter = EditorGUILayout.TextField(prefabSearchFilter);
        if (newFilter != prefabSearchFilter)
        {
            prefabSearchFilter = newFilter;
        }
        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            RefreshPrefabList();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 프리팹 리스트
        EditorGUILayout.BeginHorizontal();

        // 왼쪽: 프리팹 목록
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        EditorGUILayout.LabelField($"Prefabs ({skillProjectilePrefabs.Count}):", EditorStyles.boldLabel);

        prefabScrollPosition = EditorGUILayout.BeginScrollView(prefabScrollPosition, GUILayout.Height(250));

        foreach (var prefab in skillProjectilePrefabs)
        {
            if (prefab == null) continue;
            if (!string.IsNullOrEmpty(prefabSearchFilter) &&
                !prefab.name.ToLower().Contains(prefabSearchFilter.ToLower()))
                continue;

            bool isSelected = selectedPrefab == prefab;
            GUI.backgroundColor = isSelected ? Color.cyan : Color.white;

            if (GUILayout.Button(prefab.name, EditorStyles.miniButton))
            {
                selectedPrefab = prefab;
                Selection.activeObject = prefab;
                // 프리팹 선택 시 자동으로 Preview 생성
                SpawnEditorPreview();
            }
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // 오른쪽: 선택된 프리팹 정보 및 조정
        EditorGUILayout.BeginVertical();

        if (selectedPrefab != null)
        {
            EditorGUILayout.LabelField("Selected: " + selectedPrefab.name, EditorStyles.boldLabel);

            // 프리팹 정보
            var skillProjectile = selectedPrefab.GetComponent<SkillProjectile>();
            if (skillProjectile != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Components:", EditorStyles.boldLabel);

                var so = new SerializedObject(skillProjectile);
                var vfxMainProp = so.FindProperty("vfxMain");
                var hitEffectProp = so.FindProperty("hitEffectPrefab");

                EditorGUILayout.LabelField($"  VFX Main: {(vfxMainProp?.objectReferenceValue != null ? vfxMainProp.objectReferenceValue.name : "None")}");
                EditorGUILayout.LabelField($"  Hit Effect: {(hitEffectProp?.objectReferenceValue != null ? hitEffectProp.objectReferenceValue.name : "None")}");
            }

            EditorGUILayout.Space(10);

            // ========== 실시간 Scale 조정 ==========
            EditorGUILayout.LabelField("▶ Scale Adjustment", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            previewScale = EditorGUILayout.Slider("VFX Scale", previewScale, 0.1f, 5f);
            if (EditorGUI.EndChangeCheck() && previewInstance != null)
            {
                Transform vfxMain = previewInstance.transform.Find("VFX_Main");
                if (vfxMain != null)
                {
                    vfxMain.localScale = Vector3.one * previewScale;
                }
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(5);

            // ========== AOE Radius 조정 ==========
            EditorGUILayout.LabelField("▶ AOE Radius (Damage Range)", EditorStyles.boldLabel);

            showAoeGizmo = EditorGUILayout.Toggle("Show AOE Gizmo", showAoeGizmo);
            previewAoeRadius = EditorGUILayout.Slider("AOE Radius", previewAoeRadius, 0.5f, 20f);
            aoeGizmoColor = EditorGUILayout.ColorField("Gizmo Color", aoeGizmoColor);

            EditorGUILayout.Space(10);

            // ========== 저장 버튼 ==========
            EditorGUILayout.LabelField("▶ Save to Prefab", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save Scale to Prefab", GUILayout.Height(30)))
            {
                SaveScaleToPrefab();
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Reset Scale", GUILayout.Height(30)))
            {
                previewScale = 1f;
                if (previewInstance != null)
                {
                    Transform vfxMain = previewInstance.transform.Find("VFX_Main");
                    if (vfxMain != null)
                    {
                        vfxMain.localScale = Vector3.one;
                    }
                }
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // ========== Preview 컨트롤 ==========
            EditorGUILayout.LabelField("▶ Preview Control", EditorStyles.boldLabel);

            autoRotate = EditorGUILayout.Toggle("Auto Rotate", autoRotate);
            if (autoRotate)
            {
                rotationSpeed = EditorGUILayout.Slider("Rotation Speed", rotationSpeed, 0f, 100f);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Preview"))
            {
                SpawnEditorPreview();
            }
            if (GUILayout.Button("Clear Preview"))
            {
                CleanupPreview();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // ========== Play Mode 테스트 ==========
            EditorGUILayout.LabelField("▶ Live Test (Play Mode Only)", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                testSpawnPosition = EditorGUILayout.Vector3Field("Spawn Position", testSpawnPosition);
                testTargetPosition = EditorGUILayout.Vector3Field("Target Position", testTargetPosition);
                testSpeed = EditorGUILayout.FloatField("Speed", testSpeed);
                testDamage = EditorGUILayout.FloatField("Damage", testDamage);

                EditorGUILayout.Space(5);

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Fire Test Projectile", GUILayout.Height(25)))
                {
                    FireTestProjectile();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test projectile firing.", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.LabelField("Select a prefab from the list", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void SaveScaleToPrefab()
    {
        if (selectedPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "No prefab selected!", "OK");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selectedPrefab);
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(path);

        Transform vfxMain = prefabContents.transform.Find("VFX_Main");
        if (vfxMain != null)
        {
            vfxMain.localScale = Vector3.one * previewScale;
            PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
            Debug.Log($"[SkillProjectileLiveEditor] Saved scale {previewScale} to {selectedPrefab.name}");
            EditorUtility.DisplayDialog("Saved", $"Scale {previewScale:F2} saved to prefab!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "VFX_Main not found in prefab!", "OK");
        }

        PrefabUtility.UnloadPrefabContents(prefabContents);
        AssetDatabase.SaveAssets();
    }

    private void RefreshPrefabList()
    {
        skillProjectilePrefabs.Clear();

        string[] prefabPaths = new string[]
        {
            "Assets/03. Prefabs/SpecialSkillEffects/NotScriptBased",
            "Assets/03. Prefabs/SpecialSkillEffects/ScriptBased"
        };

        foreach (var path in prefabPaths)
        {
            if (!Directory.Exists(path)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null && prefab.GetComponent<SkillProjectile>() != null)
                {
                    skillProjectilePrefabs.Add(prefab);
                }
            }
        }

        // 이름순 정렬
        skillProjectilePrefabs.Sort((a, b) => a.name.CompareTo(b.name));

        Debug.Log($"[SkillProjectileLiveEditor] Found {skillProjectilePrefabs.Count} SkillProjectile prefabs");
    }

    private void FireTestProjectile()
    {
        if (selectedPrefab == null || !Application.isPlaying) return;

        GameObject instance = Object.Instantiate(selectedPrefab, testSpawnPosition, Quaternion.identity);
        var skillProjectile = instance.GetComponent<SkillProjectile>();

        if (skillProjectile != null)
        {
            skillProjectile.Launch(testSpawnPosition, testTargetPosition, testSpeed, 5f, testDamage, 0, 0, 0f, 1.5f, Genre.Horror);
            Debug.Log($"[SkillProjectileLiveEditor] Fired test projectile: {selectedPrefab.name}");
        }
    }

    private void SpawnEditorPreview()
    {
        CleanupPreview();

        if (selectedPrefab == null) return;

        previewInstance = Object.Instantiate(selectedPrefab);
        previewInstance.name = "[Preview] " + selectedPrefab.name;
        previewInstance.transform.position = Vector3.zero;

        // Rigidbody 비활성화
        var rb = previewInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Collider 비활성화
        var col = previewInstance.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 에셋 내장 이동 스크립트 비활성화
        var moveScripts = previewInstance.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var script in moveScripts)
        {
            if (script == null) continue;
            string typeName = script.GetType().Name;
            if (typeName.Contains("Move") || typeName.Contains("Destroy") ||
                typeName.Contains("Projectile") || typeName.Contains("Follow"))
            {
                script.enabled = false;
            }
        }

        // 현재 Scale 적용
        Transform vfxMain = previewInstance.transform.Find("VFX_Main");
        if (vfxMain != null)
        {
            previewScale = vfxMain.localScale.x; // 기존 스케일 로드
        }

        SceneView.RepaintAll();
    }

    private void CleanupPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    private void Update()
    {
        if (autoRotate && previewInstance != null)
        {
            previewInstance.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            SceneView.RepaintAll();
        }
    }

    #endregion

    #region Skill Configurator Tab (NEW)

    private void DrawSkillConfiguratorTab()
    {
        EditorGUILayout.LabelField("Skill Effect Configurator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "182개 이펙트에 대한 스킬 설정을 구성합니다.\n" +
            "스킬 타입, 한글 이름, 설명, AOE 반경 등을 설정하고 CSV로 내보냅니다.",
            MessageType.Info);

        EditorGUILayout.Space(5);

        // 상단 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Initialize from Prefabs", GUILayout.Height(25)))
        {
            InitializeSkillConfigsFromPrefabs();
        }
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Export to CSV", GUILayout.Height(25)))
        {
            ExportSkillConfigsToCSV();
        }
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Load from CSV", GUILayout.Height(25)))
        {
            LoadSkillEffectConfigs();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // CSV 경로
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Export Path:", GUILayout.Width(80));
        newSkillsCsvPath = EditorGUILayout.TextField(newSkillsCsvPath);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("ID", EditorStyles.toolbarButton, GUILayout.Width(50));
        GUILayout.Label("Effect Name", EditorStyles.toolbarButton, GUILayout.Width(150));
        GUILayout.Label("Korean Name", EditorStyles.toolbarButton, GUILayout.Width(120));
        GUILayout.Label("Skill Type", EditorStyles.toolbarButton, GUILayout.Width(130));
        GUILayout.Label("AOE", EditorStyles.toolbarButton, GUILayout.Width(50));
        GUILayout.Label("Speed", EditorStyles.toolbarButton, GUILayout.Width(50));
        GUILayout.Label("Duration", EditorStyles.toolbarButton, GUILayout.Width(60));
        GUILayout.Label("Preview", EditorStyles.toolbarButton, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        // 데이터 리스트
        skillConfigScrollPosition = EditorGUILayout.BeginScrollView(skillConfigScrollPosition);

        for (int i = 0; i < skillEffectConfigs.Count; i++)
        {
            var config = skillEffectConfigs[i];

            bool isSelected = (selectedConfigIndex == i);
            GUI.backgroundColor = isSelected ? Color.cyan : (i % 2 == 0 ? Color.white : new Color(0.95f, 0.95f, 0.95f));

            EditorGUILayout.BeginHorizontal();

            // ID
            EditorGUILayout.LabelField(config.skillId.ToString(), GUILayout.Width(50));

            // Effect Name (원본)
            EditorGUILayout.LabelField(config.effectName, GUILayout.Width(150));

            // Korean Name (편집 가능)
            config.koreanName = EditorGUILayout.TextField(config.koreanName, GUILayout.Width(120));

            // Skill Type (드롭다운)
            int typeIndex = System.Array.IndexOf(skillTypeIds, config.skillTypeId);
            if (typeIndex < 0) typeIndex = 0;
            int newTypeIndex = EditorGUILayout.Popup(typeIndex, skillTypeNames, GUILayout.Width(130));
            config.skillTypeId = skillTypeIds[newTypeIndex];

            // AOE Radius
            config.aoeRadius = EditorGUILayout.FloatField(config.aoeRadius, GUILayout.Width(50));

            // Projectile Speed
            config.projectileSpeed = EditorGUILayout.FloatField(config.projectileSpeed, GUILayout.Width(50));

            // Duration
            config.duration = EditorGUILayout.FloatField(config.duration, GUILayout.Width(60));

            // Preview 버튼
            if (GUILayout.Button("▶", GUILayout.Width(30)))
            {
                selectedConfigIndex = i;
                // 해당 프리팹 찾아서 Preview
                var prefab = skillProjectilePrefabs.Find(p => p.name == config.effectName);
                if (prefab != null)
                {
                    selectedPrefab = prefab;
                    previewAoeRadius = config.aoeRadius;
                    SpawnEditorPreview();
                }
            }

            // 설명 편집 버튼
            if (GUILayout.Button("...", GUILayout.Width(25)))
            {
                selectedConfigIndex = i;
                SkillDescriptionPopup.Show(config);
            }

            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndScrollView();

        // 선택된 스킬 상세 편집
        if (selectedConfigIndex >= 0 && selectedConfigIndex < skillEffectConfigs.Count)
        {
            EditorGUILayout.Space(10);
            DrawSelectedSkillDetail(skillEffectConfigs[selectedConfigIndex]);
        }
    }

    private void DrawSelectedSkillDetail(SkillEffectConfig config)
    {
        EditorGUILayout.LabelField("▶ Selected Skill Detail", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField($"Effect: {config.effectName}");

        EditorGUILayout.Space(5);

        config.koreanName = EditorGUILayout.TextField("한글 이름", config.koreanName);

        EditorGUILayout.LabelField("스킬 설명:");
        config.description = EditorGUILayout.TextArea(config.description, GUILayout.Height(60));

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        config.baseDamage = EditorGUILayout.FloatField("Base Damage", config.baseDamage);
        config.cooldown = EditorGUILayout.FloatField("Cooldown", config.cooldown);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        config.range = EditorGUILayout.FloatField("Range", config.range);
        config.projectileCount = EditorGUILayout.IntField("Projectile Count", config.projectileCount);
        EditorGUILayout.EndHorizontal();

        // DOT 설정
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("DOT Settings (if applicable):");
        EditorGUILayout.BeginHorizontal();
        config.dotDuration = EditorGUILayout.FloatField("DOT Duration", config.dotDuration);
        config.dotTickInterval = EditorGUILayout.FloatField("Tick Interval", config.dotTickInterval);
        config.dotDamagePerTick = EditorGUILayout.FloatField("Tick Damage", config.dotDamagePerTick);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void InitializeSkillConfigsFromPrefabs()
    {
        skillEffectConfigs.Clear();

        int startId = 50001; // 새 스킬 ID 시작점

        foreach (var prefab in skillProjectilePrefabs)
        {
            if (prefab == null) continue;

            var config = new SkillEffectConfig
            {
                skillId = startId++,
                effectName = prefab.name,
                koreanName = TranslateEffectName(prefab.name),
                description = GenerateDescription(prefab.name),
                skillTypeId = GuessSkillType(prefab),
                baseDamage = 50f,
                cooldown = 5f,
                range = 20f,
                projectileSpeed = 15f,
                duration = 3f,
                aoeRadius = 0f,
                projectileCount = 1
            };

            // AOE 타입이면 기본 반경 설정
            if (config.skillTypeId == 3000302 || config.skillTypeId == 3000403)
            {
                config.aoeRadius = 5f;
            }

            skillEffectConfigs.Add(config);
        }

        Debug.Log($"[SkillProjectileLiveEditor] Initialized {skillEffectConfigs.Count} skill configs from prefabs");
    }

    private string TranslateEffectName(string effectName)
    {
        // Effect_XX_ 접두사 제거
        string name = effectName;
        if (name.StartsWith("Effect_"))
        {
            int underscoreIndex = name.IndexOf('_', 7);
            if (underscoreIndex > 0)
            {
                name = name.Substring(underscoreIndex + 1);
            }
        }

        // 게임스러운 외래어 번역
        Dictionary<string, string> translations = new Dictionary<string, string>
        {
            {"StormTornado", "스톰 토네이도"},
            {"EnergyStrike", "에너지 스트라이크"},
            {"BlackHole", "블랙홀"},
            {"Meteor", "메테오"},
            {"FireBall", "파이어볼"},
            {"IceLance", "아이스 랜스"},
            {"ThunderBolt", "썬더볼트"},
            {"LightningStrike", "라이트닝 스트라이크"},
            {"DarkPulse", "다크 펄스"},
            {"HolyLight", "홀리 라이트"},
            {"ShadowBolt", "섀도우 볼트"},
            {"FrostNova", "프로스트 노바"},
            {"FlameWave", "플레임 웨이브"},
            {"ArcaneBlast", "아케인 블래스트"},
            {"VoidRift", "보이드 리프트"},
            {"SolarFlare", "솔라 플레어"},
            {"MoonBeam", "문 빔"},
            {"StarFall", "스타폴"},
            {"Earthquake", "어스퀘이크"},
            {"Tsunami", "쓰나미"},
            {"Blizzard", "블리자드"},
            {"Inferno", "인페르노"},
            {"Cyclone", "사이클론"},
            {"Explosion", "익스플로전"},
            {"Shockwave", "쇼크웨이브"},
            {"ChainLightning", "체인 라이트닝"},
            {"PoisonCloud", "포이즌 클라우드"},
            {"HealingAura", "힐링 오라"},
            {"Shield", "실드"},
            {"Barrier", "배리어"},
            {"Beam", "빔"},
            {"Ray", "레이"},
            {"Slash", "슬래시"},
            {"Cut", "컷"},
            {"Smash", "스매시"},
            {"Crash", "크래시"},
            {"Burst", "버스트"},
            {"Wave", "웨이브"},
            {"Storm", "스톰"},
            {"Wind", "윈드"},
            {"Fire", "파이어"},
            {"Ice", "아이스"},
            {"Thunder", "썬더"},
            {"Lightning", "라이트닝"},
            {"Dark", "다크"},
            {"Light", "라이트"},
            {"Holy", "홀리"},
            {"Shadow", "섀도우"},
            {"Void", "보이드"},
            {"Arcane", "아케인"},
            {"Magic", "매직"},
            {"Spell", "스펠"},
            {"Curse", "커스"},
            {"Hex", "헥스"},
            {"Blessing", "블레싱"},
            {"Aura", "오라"},
            {"Field", "필드"},
            {"Zone", "존"},
            {"Area", "에어리어"},
            {"Circle", "서클"},
            {"Ring", "링"},
            {"Sphere", "스피어"},
            {"Orb", "오브"},
            {"Ball", "볼"},
            {"Arrow", "애로우"},
            {"Bolt", "볼트"},
            {"Shot", "샷"},
            {"Bullet", "불릿"},
            {"Missile", "미사일"},
            {"Rocket", "로켓"},
            {"Bomb", "밤"},
            {"Mine", "마인"},
            {"Trap", "트랩"},
            {"Chain", "체인"},
            {"Bind", "바인드"},
            {"Seal", "씰"},
            {"Prison", "프리즌"},
            {"Cage", "케이지"}
        };

        // 단어 분리 (CamelCase)
        string result = "";
        foreach (char c in name)
        {
            if (char.IsUpper(c) && result.Length > 0)
            {
                result += " ";
            }
            result += c;
        }

        // 번역 매칭
        foreach (var kvp in translations)
        {
            if (name.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }

        // 매칭 안되면 원본을 한글로 음차
        return ConvertToKoreanPhonetic(result);
    }

    private string ConvertToKoreanPhonetic(string english)
    {
        // 간단한 음차 변환 (실제로는 더 정교한 로직 필요)
        return english
            .Replace("Storm", "스톰")
            .Replace("Tornado", "토네이도")
            .Replace("Energy", "에너지")
            .Replace("Strike", "스트라이크")
            .Replace("Black", "블랙")
            .Replace("Hole", "홀")
            .Replace("Fire", "파이어")
            .Replace("Ice", "아이스")
            .Replace("Wind", "윈드")
            .Replace("Thunder", "썬더")
            .Replace("Lightning", "라이트닝")
            .Replace("Dark", "다크")
            .Replace("Light", "라이트")
            .Replace("Magic", "매직")
            .Replace("Power", "파워")
            .Replace("Force", "포스")
            .Replace("Blast", "블래스트")
            .Replace("Wave", "웨이브")
            .Replace("Pulse", "펄스")
            .Replace("Beam", "빔")
            .Replace("Ray", "레이")
            .Replace("Shot", "샷")
            .Replace("Ball", "볼")
            .Replace("Sphere", "스피어")
            .Replace("Shield", "실드")
            .Replace("Barrier", "배리어")
            .Replace("Guard", "가드")
            .Replace("Attack", "어택")
            .Replace("Assault", "어썰트")
            .Replace("Charge", "차지")
            .Replace("Rush", "러시")
            .Replace("Rapid", "래피드")
            .Replace("Quick", "퀵")
            .Replace("Slow", "슬로우")
            .Replace("Fast", "패스트")
            .Replace("Heavy", "헤비")
            .Replace("Massive", "매시브")
            .Replace("Giant", "자이언트")
            .Replace("Huge", "휴즈")
            .Replace("Tiny", "타이니")
            .Replace("Small", "스몰")
            .Replace("Big", "빅")
            .Replace("Large", "라지")
            .Replace("Nova", "노바")
            .Replace("Burst", "버스트")
            .Replace("Explosion", "익스플로전")
            .Replace("Crash", "크래시")
            .Replace("Smash", "스매시")
            .Replace("Slash", "슬래시")
            .Replace("Cut", "컷")
            .Replace("Pierce", "피어스")
            .Replace("Stab", "스탭")
            .Replace("Spin", "스핀")
            .Replace("Rotate", "로테이트")
            .Replace("Circle", "서클")
            .Replace("Ring", "링")
            .Replace("Chain", "체인")
            .Replace("Link", "링크")
            .Replace("Connect", "커넥트")
            .Replace("Bounce", "바운스")
            .Replace("Reflect", "리플렉트")
            .Replace("Mirror", "미러")
            .Replace("Copy", "카피")
            .Replace("Clone", "클론")
            .Replace("Double", "더블")
            .Replace("Triple", "트리플")
            .Replace("Multi", "멀티")
            .Replace("Single", "싱글")
            .Replace("Dual", "듀얼")
            .Replace("Cross", "크로스")
            .Replace("Star", "스타")
            .Replace("Moon", "문")
            .Replace("Sun", "선")
            .Replace("Solar", "솔라")
            .Replace("Lunar", "루나")
            .Replace("Cosmic", "코스믹")
            .Replace("Galaxy", "갤럭시")
            .Replace("Space", "스페이스")
            .Replace("Void", "보이드")
            .Replace("Null", "널")
            .Replace("Zero", "제로")
            .Replace("Infinity", "인피니티")
            .Replace("Eternal", "이터널")
            .Replace("Ancient", "에인션트")
            .Replace("Divine", "디바인")
            .Replace("Holy", "홀리")
            .Replace("Sacred", "세이크리드")
            .Replace("Cursed", "커스드")
            .Replace("Demonic", "데모닉")
            .Replace("Devil", "데빌")
            .Replace("Angel", "엔젤")
            .Replace("Dragon", "드래곤")
            .Replace("Phoenix", "피닉스")
            .Replace("Titan", "타이탄")
            .Replace("Giant", "자이언트")
            .Replace("Golem", "골렘")
            .Replace("Spirit", "스피릿")
            .Replace("Soul", "소울")
            .Replace("Ghost", "고스트")
            .Replace("Phantom", "팬텀")
            .Replace("Specter", "스펙터")
            .Replace("Wraith", "레이스")
            .Replace("Undead", "언데드")
            .Replace("Zombie", "좀비")
            .Replace("Skeleton", "스켈레톤")
            .Replace("Bone", "본")
            .Replace("Blood", "블러드")
            .Replace("Poison", "포이즌")
            .Replace("Toxic", "톡식")
            .Replace("Venom", "베놈")
            .Replace("Acid", "애시드")
            .Replace("Corrosion", "코로전")
            .Replace("Decay", "디케이")
            .Replace("Rot", "로트")
            .Replace("Death", "데스")
            .Replace("Life", "라이프")
            .Replace("Heal", "힐")
            .Replace("Cure", "큐어")
            .Replace("Restore", "리스토어")
            .Replace("Recover", "리커버")
            .Replace("Regenerate", "리제너레이트")
            .Replace("Revive", "리바이브")
            .Replace("Resurrect", "레저렉트")
            .Replace(" ", " ");
    }

    private string GenerateDescription(string effectName)
    {
        // Effect_XX_ 접두사 제거
        string name = effectName;
        if (name.StartsWith("Effect_"))
        {
            int underscoreIndex = name.IndexOf('_', 7);
            if (underscoreIndex > 0)
            {
                name = name.Substring(underscoreIndex + 1);
            }
        }

        // 기본 설명 생성
        string koreanName = TranslateEffectName(effectName);
        return $"{koreanName}을(를) 발동하여 대상에게 피해를 입힌다.";
    }

    private int GuessSkillType(GameObject prefab)
    {
        string name = prefab.name.ToLower();

        // 이름 기반 추측
        if (name.Contains("trap") || name.Contains("chain") || name.Contains("bind"))
            return 3000807; // Trap
        if (name.Contains("mine") || name.Contains("bomb") || name.Contains("explosive"))
            return 3000908; // Mine
        if (name.Contains("heal") || name.Contains("buff") || name.Contains("shield") || name.Contains("aura"))
            return 3000504; // Buff
        if (name.Contains("curse") || name.Contains("debuff") || name.Contains("slow") || name.Contains("weak"))
            return 3000605; // Debuff
        if (name.Contains("beam") || name.Contains("ray") || name.Contains("laser") || name.Contains("channel"))
            return 3000706; // Channeling
        if (name.Contains("dot") || name.Contains("poison") || name.Contains("burn") || name.Contains("bleed"))
            return 3000403; // DOT
        if (name.Contains("aoe") || name.Contains("explosion") || name.Contains("nova") || name.Contains("wave") ||
            name.Contains("storm") || name.Contains("field") || name.Contains("area") || name.Contains("zone"))
            return 3000302; // AOE

        // ScriptBased (이동 스크립트 있음) = Projectile
        string path = AssetDatabase.GetAssetPath(prefab);
        if (path.Contains("ScriptBased"))
            return 3000100; // Projectile

        // 기본값
        return 3000302; // AOE
    }

    private void ExportSkillConfigsToCSV()
    {
        if (skillEffectConfigs.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No skill configs to export!", "OK");
            return;
        }

        // CSV 헤더 (MainSkillTable과 동일한 형식)
        var lines = new List<string>
        {
            "스킬ID,//스킬명,스킬타입ID,속성타입ID,기본데미지,버프타입,기본버프수치,디버프타입,기본디버프수치,CC타입,CC지속,스턴여부,슬로우량,DOT지속,DOT틱간격,DOT틱데미지,표식지속,표식데미지배율,쿨다운,시전시간,사거리,투사체속도,투사체수,지속시간,관통수,유도여부,범위반경,범위각도,채널링지속,채널링틱간격,중단가능여부,//설명,//use_asset,//note",
            "skill_id,//skill_name,skill_type_ID,element_type_ID,base_damage,buff_type,base_buff_value,debuff_type,base_debuff_value,cc_type,cc_duration,stun_use,cc_slow_amount,dot_duration,dot_tick_interval,dot_damage_per_tick,mark_duration,mark_damage_mult,cooldown,cast_time,range,projectile_speed,projectile_count,skill_lifetime,pierce_count,is_homing,aoe_radius,aoe_angle,channel_duration,channel_tick_interval,interruptible,//description,//use_asset,//note",
            "int,//string,int,int,float,int,float,int,float,int,float,bool,float,float,float,float,float,float,float,float,float,float,int,float,int,bool,float,float,float,float,bool,//string,//사용에셋,//참고사항"
        };

        foreach (var config in skillEffectConfigs)
        {
            string line = string.Join(",",
                config.skillId,
                config.koreanName,
                config.skillTypeId,
                3101000, // 기본 속성 (None)
                config.baseDamage,
                3604400, // 버프 타입 None
                0,
                3605000, // 디버프 타입 None
                0,
                3302100, // CC 타입 None
                0,
                0,
                0,
                config.dotDuration,
                config.dotTickInterval,
                config.dotDamagePerTick,
                0,
                0,
                config.cooldown,
                0,
                config.range,
                config.projectileSpeed,
                config.projectileCount,
                config.duration,
                0,
                0,
                config.aoeRadius,
                360,
                0,
                0,
                0,
                $"\"{config.description}\"",
                config.effectName,
                ""
            );
            lines.Add(line);
        }

        File.WriteAllLines(newSkillsCsvPath, lines);
        AssetDatabase.Refresh();

        Debug.Log($"[SkillProjectileLiveEditor] Exported {skillEffectConfigs.Count} skills to {newSkillsCsvPath}");
        EditorUtility.DisplayDialog("Exported", $"Exported {skillEffectConfigs.Count} skills to CSV!", "OK");
    }

    private void LoadSkillEffectConfigs()
    {
        skillEffectConfigs.Clear();

        if (!File.Exists(newSkillsCsvPath))
        {
            Debug.Log($"[SkillProjectileLiveEditor] Config CSV not found: {newSkillsCsvPath}");
            return;
        }

        string[] lines = File.ReadAllLines(newSkillsCsvPath);
        if (lines.Length < 4) return; // 헤더 3줄 + 데이터

        for (int i = 3; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 33) continue;

            var config = new SkillEffectConfig
            {
                skillId = int.TryParse(values[0], out int id) ? id : 0,
                koreanName = values[1],
                skillTypeId = int.TryParse(values[2], out int typeId) ? typeId : 3000100,
                baseDamage = float.TryParse(values[4], out float dmg) ? dmg : 50f,
                dotDuration = float.TryParse(values[13], out float dotDur) ? dotDur : 0f,
                dotTickInterval = float.TryParse(values[14], out float dotTick) ? dotTick : 0f,
                dotDamagePerTick = float.TryParse(values[15], out float dotDmg) ? dotDmg : 0f,
                cooldown = float.TryParse(values[18], out float cd) ? cd : 5f,
                range = float.TryParse(values[20], out float rng) ? rng : 20f,
                projectileSpeed = float.TryParse(values[21], out float spd) ? spd : 15f,
                projectileCount = int.TryParse(values[22], out int cnt) ? cnt : 1,
                duration = float.TryParse(values[23], out float dur) ? dur : 3f,
                aoeRadius = float.TryParse(values[26], out float aoe) ? aoe : 0f,
                description = values[31].Trim('"'),
                effectName = values.Length > 32 ? values[32] : ""
            };

            skillEffectConfigs.Add(config);
        }

        Debug.Log($"[SkillProjectileLiveEditor] Loaded {skillEffectConfigs.Count} skill configs from CSV");
    }

    #endregion

    #region CSV Editor Tab

    private void DrawCSVEditorTab()
    {
        EditorGUILayout.LabelField("MainSkillTable CSV Editor", EditorStyles.boldLabel);

        if (hasUnsavedChanges)
        {
            EditorGUILayout.HelpBox("You have unsaved changes!", MessageType.Warning);
        }

        EditorGUILayout.BeginHorizontal();
        csvPath = EditorGUILayout.TextField("CSV Path:", csvPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("Select CSV File", "Assets/Data/CSV", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                csvPath = "Assets" + path.Substring(Application.dataPath.Length);
                LoadCSVData();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload CSV"))
        {
            LoadCSVData();
        }
        GUI.backgroundColor = hasUnsavedChanges ? Color.yellow : Color.white;
        if (GUILayout.Button("Save CSV"))
        {
            SaveCSVData();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 검색 필터
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        csvSearchFilter = EditorGUILayout.TextField(csvSearchFilter);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // CSV 데이터 테이블
        EditorGUILayout.LabelField($"Skills ({editableSkillData.Count}):", EditorStyles.boldLabel);

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("ID", EditorStyles.toolbarButton, GUILayout.Width(50));
        GUILayout.Label("Name", EditorStyles.toolbarButton, GUILayout.Width(120));
        GUILayout.Label("Damage", EditorStyles.toolbarButton, GUILayout.Width(70));
        GUILayout.Label("Speed", EditorStyles.toolbarButton, GUILayout.Width(60));
        GUILayout.Label("Range", EditorStyles.toolbarButton, GUILayout.Width(60));
        GUILayout.Label("AOE Radius", EditorStyles.toolbarButton, GUILayout.Width(70));
        GUILayout.Label("Lifetime", EditorStyles.toolbarButton, GUILayout.Width(60));
        GUILayout.Label("Cooldown", EditorStyles.toolbarButton, GUILayout.Width(70));
        GUILayout.Label("Proj Count", EditorStyles.toolbarButton, GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();

        // 데이터 행
        csvScrollPosition = EditorGUILayout.BeginScrollView(csvScrollPosition, GUILayout.Height(350));

        foreach (var data in editableSkillData)
        {
            if (!string.IsNullOrEmpty(csvSearchFilter))
            {
                if (!data.skill_name.ToLower().Contains(csvSearchFilter.ToLower()) &&
                    !data.skill_id.ToString().Contains(csvSearchFilter))
                    continue;
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(data.skill_id.ToString(), GUILayout.Width(50));
            EditorGUILayout.LabelField(data.skill_name, GUILayout.Width(120));

            EditorGUI.BeginChangeCheck();

            data.base_damage = EditorGUILayout.FloatField(data.base_damage, GUILayout.Width(70));
            data.projectile_speed = EditorGUILayout.FloatField(data.projectile_speed, GUILayout.Width(60));
            data.range = EditorGUILayout.FloatField(data.range, GUILayout.Width(60));
            data.aoe_radius = EditorGUILayout.FloatField(data.aoe_radius, GUILayout.Width(70));
            data.skill_lifetime = EditorGUILayout.FloatField(data.skill_lifetime, GUILayout.Width(60));
            data.cooldown = EditorGUILayout.FloatField(data.cooldown, GUILayout.Width(70));
            data.projectile_count = EditorGUILayout.IntField(data.projectile_count, GUILayout.Width(70));

            if (EditorGUI.EndChangeCheck())
            {
                hasUnsavedChanges = true;
                data.isModified = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void LoadCSVData()
    {
        editableSkillData.Clear();
        hasUnsavedChanges = false;

        if (!File.Exists(csvPath))
        {
            Debug.LogWarning($"[SkillProjectileLiveEditor] CSV file not found: {csvPath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2) return;

        // 헤더 파싱
        string[] headers = lines[0].Split(',');
        Dictionary<string, int> headerIndex = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
        {
            headerIndex[headers[i].Trim()] = i;
        }

        // 데이터 파싱 (헤더 3줄 스킵)
        for (int i = 3; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < headers.Length) continue;

            var data = new MainSkillDataEditable
            {
                originalLine = lines[i],
                lineIndex = i
            };

            // 필드 파싱
            if (headerIndex.TryGetValue("skill_id", out int idIdx))
                int.TryParse(values[idIdx], out data.skill_id);
            if (headerIndex.TryGetValue("//skill_name", out int nameIdx))
                data.skill_name = values[nameIdx];
            if (headerIndex.TryGetValue("base_damage", out int dmgIdx))
                float.TryParse(values[dmgIdx], out data.base_damage);
            if (headerIndex.TryGetValue("projectile_speed", out int speedIdx))
                float.TryParse(values[speedIdx], out data.projectile_speed);
            if (headerIndex.TryGetValue("range", out int rangeIdx))
                float.TryParse(values[rangeIdx], out data.range);
            if (headerIndex.TryGetValue("aoe_radius", out int aoeIdx))
                float.TryParse(values[aoeIdx], out data.aoe_radius);
            if (headerIndex.TryGetValue("skill_lifetime", out int lifeIdx))
                float.TryParse(values[lifeIdx], out data.skill_lifetime);
            if (headerIndex.TryGetValue("cooldown", out int cdIdx))
                float.TryParse(values[cdIdx], out data.cooldown);
            if (headerIndex.TryGetValue("projectile_count", out int pcIdx))
                int.TryParse(values[pcIdx], out data.projectile_count);

            editableSkillData.Add(data);
        }

        Debug.Log($"[SkillProjectileLiveEditor] Loaded {editableSkillData.Count} skills from CSV");
    }

    private string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current.Trim());

        return result.ToArray();
    }

    private void SaveCSVData()
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[SkillProjectileLiveEditor] CSV file not found: {csvPath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);
        string[] headers = lines[0].Split(',');

        // 헤더 인덱스 매핑
        Dictionary<string, int> headerIndex = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
        {
            headerIndex[headers[i].Trim()] = i;
        }

        // 수정된 데이터 업데이트
        foreach (var data in editableSkillData)
        {
            if (!data.isModified) continue;

            string[] values = ParseCSVLine(lines[data.lineIndex]);

            // 값 업데이트
            if (headerIndex.TryGetValue("base_damage", out int dmgIdx))
                values[dmgIdx] = data.base_damage.ToString();
            if (headerIndex.TryGetValue("projectile_speed", out int speedIdx))
                values[speedIdx] = data.projectile_speed.ToString();
            if (headerIndex.TryGetValue("range", out int rangeIdx))
                values[rangeIdx] = data.range.ToString();
            if (headerIndex.TryGetValue("aoe_radius", out int aoeIdx))
                values[aoeIdx] = data.aoe_radius.ToString();
            if (headerIndex.TryGetValue("skill_lifetime", out int lifeIdx))
                values[lifeIdx] = data.skill_lifetime.ToString();
            if (headerIndex.TryGetValue("cooldown", out int cdIdx))
                values[cdIdx] = data.cooldown.ToString();
            if (headerIndex.TryGetValue("projectile_count", out int pcIdx))
                values[pcIdx] = data.projectile_count.ToString();

            lines[data.lineIndex] = string.Join(",", values);
            data.isModified = false;
        }

        // 파일 저장
        File.WriteAllLines(csvPath, lines);
        AssetDatabase.Refresh();

        hasUnsavedChanges = false;
        Debug.Log($"[SkillProjectileLiveEditor] CSV saved: {csvPath}");
        EditorUtility.DisplayDialog("Saved", "CSV file saved successfully!", "OK");
    }

    #endregion

    #region Batch Operations Tab

    private void DrawBatchOperationsTab()
    {
        EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "여러 프리팹에 대한 일괄 작업을 수행합니다.\n" +
            "주의: 일부 작업은 되돌릴 수 없습니다.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 일괄 스케일 조정
        EditorGUILayout.LabelField("Batch Scale Adjustment", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Scale Multiplier:", GUILayout.Width(120));
        float scaleMultiplier = EditorGUILayout.Slider(1f, 0.1f, 3f);
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Apply Scale to All VFX"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Apply scale to all SkillProjectile prefabs?", "Yes", "Cancel"))
            {
                ApplyScaleToAllPrefabs(scaleMultiplier);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(20);

        // 일괄 Collider 조정
        EditorGUILayout.LabelField("Batch Collider Adjustment", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Collider Radius:", GUILayout.Width(120));
        float colliderRadius = EditorGUILayout.Slider(0.5f, 0.1f, 3f);
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Apply Collider Radius to All"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Apply collider radius to all SkillProjectile prefabs?", "Yes", "Cancel"))
            {
                ApplyColliderRadiusToAllPrefabs(colliderRadius);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(20);

        // 프리팹 재생성
        EditorGUILayout.LabelField("Regenerate Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "SkillProjectileWrapperGenerator를 사용하여 모든 래퍼 프리팹을 다시 생성합니다.",
            MessageType.Info);

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Open SkillProjectile Generator"))
        {
            SkillProjectileWrapperGenerator.ShowWindow();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(20);

        // 통계
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total SkillProjectile Prefabs: {skillProjectilePrefabs.Count}");
        EditorGUILayout.LabelField($"Total Skills in CSV: {editableSkillData.Count}");
        EditorGUILayout.LabelField($"Total Skill Configs: {skillEffectConfigs.Count}");
    }

    private void ApplyScaleToAllPrefabs(float multiplier)
    {
        int count = 0;
        foreach (var prefab in skillProjectilePrefabs)
        {
            if (prefab == null) continue;

            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject instance = PrefabUtility.LoadPrefabContents(path);

            Transform vfxMain = instance.transform.Find("VFX_Main");
            if (vfxMain != null)
            {
                vfxMain.localScale *= multiplier;
                count++;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            PrefabUtility.UnloadPrefabContents(instance);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SkillProjectileLiveEditor] Applied scale to {count} prefabs");
        EditorUtility.DisplayDialog("Complete", $"Applied scale to {count} prefabs", "OK");
    }

    private void ApplyColliderRadiusToAllPrefabs(float radius)
    {
        int count = 0;
        foreach (var prefab in skillProjectilePrefabs)
        {
            if (prefab == null) continue;

            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject instance = PrefabUtility.LoadPrefabContents(path);

            var col = instance.GetComponent<SphereCollider>();
            if (col != null)
            {
                col.radius = radius;
                count++;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            PrefabUtility.UnloadPrefabContents(instance);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SkillProjectileLiveEditor] Applied collider radius to {count} prefabs");
        EditorUtility.DisplayDialog("Complete", $"Applied collider radius to {count} prefabs", "OK");
    }

    #endregion

    // 편집 가능한 스킬 데이터 클래스
    private class MainSkillDataEditable
    {
        public int skill_id;
        public string skill_name = "";
        public float base_damage;
        public float projectile_speed;
        public float range;
        public float aoe_radius;
        public float skill_lifetime;
        public float cooldown;
        public int projectile_count;

        public string originalLine;
        public int lineIndex;
        public bool isModified;
    }

    // 스킬 이펙트 설정 클래스
    private class SkillEffectConfig
    {
        public int skillId;
        public string effectName = "";
        public string koreanName = "";
        public string description = "";
        public int skillTypeId = 3000100;
        public float baseDamage = 50f;
        public float cooldown = 5f;
        public float range = 20f;
        public float projectileSpeed = 15f;
        public float duration = 3f;
        public float aoeRadius = 0f;
        public int projectileCount = 1;
        public float dotDuration = 0f;
        public float dotTickInterval = 0f;
        public float dotDamagePerTick = 0f;
    }
}

// 스킬 설명 편집 팝업
public class SkillDescriptionPopup : EditorWindow
{
    private static object targetConfig;
    private string description = "";

    public static void Show(object config)
    {
        targetConfig = config;
        var window = GetWindow<SkillDescriptionPopup>("Edit Description");
        window.minSize = new Vector2(400, 200);

        // Reflection으로 description 가져오기
        var descField = config.GetType().GetField("description");
        var nameField = config.GetType().GetField("koreanName");
        if (descField != null)
        {
            window.description = descField.GetValue(config) as string ?? "";
        }
        window.titleContent = new GUIContent($"Edit: {nameField?.GetValue(config)}");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("스킬 설명 편집", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        description = EditorGUILayout.TextArea(description, GUILayout.Height(100));

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Height(30)))
        {
            if (targetConfig != null)
            {
                var descField = targetConfig.GetType().GetField("description");
                if (descField != null)
                {
                    descField.SetValue(targetConfig, description);
                }
            }
            Close();
        }
        if (GUILayout.Button("Cancel", GUILayout.Height(30)))
        {
            Close();
        }
        EditorGUILayout.EndHorizontal();
    }
}
