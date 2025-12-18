using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스킬 이펙트 매퍼 에디터 윈도우
/// CSV 스킬 데이터와 SpecialSkillsEffectsPack 에셋을 연결하는 통합 관리 툴
/// </summary>
public class SkillEffectMapperWindow : EditorWindow
{
    // 탭
    private enum Tab
    {
        SkillMapping,
        SupportSkillMapping,
        EffectBrowser,
        AOEPreview,
        Settings
    }

    private Tab currentTab = Tab.SkillMapping;

    // 스크롤 위치
    private Vector2 skillScrollPos;
    private Vector2 supportScrollPos;
    private Vector2 effectScrollPos;
    private Vector2 previewScrollPos;

    // 데이터 캐시
    private List<MainSkillData> mainSkillDataList = new List<MainSkillData>();
    private List<SupportSkillData> supportSkillDataList = new List<SupportSkillData>();
    private List<EffectPrefabInfo> effectPrefabList = new List<EffectPrefabInfo>();

    // SkillEffectDatabase 참조
    private SkillEffectDatabase effectDatabase;

    // 필터
    private string searchFilter = "";
    private bool showOnlyUnmapped = false;
    private SkillAssetType filterSkillType = SkillAssetType.Projectile;
    private bool useTypeFilter = false;

    // 이펙트 브라우저 필터
    private string effectSearchFilter = "";
    private EffectSetType filterEffectSet = EffectSetType.All;

    // 선택된 아이템
    private int selectedSkillId = -1;
    private int selectedSupportId = -1;
    private EffectPrefabInfo selectedEffect = null;

    // 프리뷰
    private GameObject previewInstance;
    private Editor previewEditor;

    // 경로 상수
    private const string CSV_PATH = "Assets/Data/CSV";
    private const string EFFECT_ASSET_PATH = "Assets/SpecialSkillsEffectsPack/AllEffects";
    private const string DATABASE_PATH = "Assets/ScriptableObjects/Skills/SkillEffectDatabase.asset";

    // GUIStyle 캐싱
    private GUIStyle _okStyle;
    private GUIStyle _errorStyle;
    private GUIStyle _selectedStyle;

    private GUIStyle OkStyle => _okStyle ??= CreateColorStyle(Color.green);
    private GUIStyle ErrorStyle => _errorStyle ??= CreateColorStyle(Color.red);
    private GUIStyle SelectedStyle => _selectedStyle ??= CreateSelectedStyle();

    // AOE 미리보기 관련
    private int selectedAOESkillId = -1;
    private Vector2 aoeSkillScrollPos;
    private Vector3 aoePreviewPosition = Vector3.zero;
    private float aoePreviewRadius = 5f;
    private bool showAOEGizmo = true;
    private Color aoeGizmoColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    private GameObject aoeEffectPreviewInstance;
    private float aoeEffectPreviewScale = 1f;

    // 이펙트 스케일 미리보기 관련 (Main Skills 탭용)
    private GameObject scalePreviewInstance;
    private Vector3 scalePreviewPosition = Vector3.zero;
    private enum EffectPreviewType { Main, Hit, Cast, Trail, Area }
    private EffectPreviewType currentPreviewType = EffectPreviewType.Main;

    private enum EffectSetType
    {
        All,
        Set1_NotScriptBased,
        Set2_ScriptBased
    }

    [Serializable]
    private class EffectPrefabInfo
    {
        public string name;
        public string path;
        public string category;
        public EffectSetType setType;
        public GameObject prefab;
        public bool hasObjectMove;
        public bool hasObjectMoveDestroy;
    }

    [MenuItem("Tools/Skills/Skill Effect Mapper", false, 101)]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillEffectMapperWindow>("Skill Effect Mapper");
        window.minSize = new Vector2(900, 600);
        window.Show();
    }

    private void OnEnable()
    {
        LoadSkillData();
        LoadSupportSkillData();
        LoadEffectDatabase();
        ScanEffectPrefabs();

        // Scene View에 Gizmo 그리기 등록
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        CleanupPreview();
        CleanupAOEPreview();
        CleanupScalePreview();

        // Scene View 콜백 해제
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.Space(5);

        // 탭 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == Tab.SkillMapping, "Main Skills", EditorStyles.toolbarButton))
            currentTab = Tab.SkillMapping;
        if (GUILayout.Toggle(currentTab == Tab.SupportSkillMapping, "Support Skills", EditorStyles.toolbarButton))
            currentTab = Tab.SupportSkillMapping;
        if (GUILayout.Toggle(currentTab == Tab.EffectBrowser, "Effect Browser", EditorStyles.toolbarButton))
            currentTab = Tab.EffectBrowser;
        if (GUILayout.Toggle(currentTab == Tab.AOEPreview, "AOE Preview", EditorStyles.toolbarButton))
            currentTab = Tab.AOEPreview;
        if (GUILayout.Toggle(currentTab == Tab.Settings, "Settings", EditorStyles.toolbarButton))
            currentTab = Tab.Settings;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 탭 내용
        switch (currentTab)
        {
            case Tab.SkillMapping:
                DrawSkillMappingTab();
                break;
            case Tab.SupportSkillMapping:
                DrawSupportSkillMappingTab();
                break;
            case Tab.EffectBrowser:
                DrawEffectBrowserTab();
                break;
            case Tab.AOEPreview:
                DrawAOEPreviewTab();
                break;
            case Tab.Settings:
                DrawSettingsTab();
                break;
        }
    }

    #region Toolbar

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Reload All", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            LoadSkillData();
            LoadEffectDatabase();
            ScanEffectPrefabs();
        }

        if (GUILayout.Button("Save Database", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            SaveEffectDatabase();
        }

        GUILayout.FlexibleSpace();

        // 통계 표시
        int mapped = effectDatabase != null ? effectDatabase.entries.Count(e => e.HasMainEffect()) : 0;
        int total = mainSkillDataList.Count;
        EditorGUILayout.LabelField($"Mapped: {mapped}/{total}", GUILayout.Width(100));

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Skill Mapping Tab

    private void DrawSkillMappingTab()
    {
        if (effectDatabase == null)
        {
            EditorGUILayout.HelpBox("SkillEffectDatabase not found. Create one in Settings tab.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // 왼쪽: 스킬 목록
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
        DrawSkillList();
        EditorGUILayout.EndVertical();

        // 중앙: 매핑 패널
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.35f));
        DrawMappingPanel();
        EditorGUILayout.EndVertical();

        // 오른쪽: 프리뷰
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.25f));
        DrawPreviewPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillList()
    {
        EditorGUILayout.LabelField("Skills", EditorStyles.boldLabel);

        // 필터 UI
        EditorGUILayout.BeginHorizontal();
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
        showOnlyUnmapped = GUILayout.Toggle(showOnlyUnmapped, "Unmapped", EditorStyles.toolbarButton, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        useTypeFilter = GUILayout.Toggle(useTypeFilter, "Type:", EditorStyles.toolbarButton, GUILayout.Width(50));
        if (useTypeFilter)
        {
            filterSkillType = (SkillAssetType)EditorGUILayout.EnumPopup(filterSkillType, GUILayout.Width(100));
        }
        EditorGUILayout.EndHorizontal();

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("ID", GUILayout.Width(40));
        EditorGUILayout.LabelField("Name", GUILayout.Width(100));
        EditorGUILayout.LabelField("Type", GUILayout.Width(80));
        EditorGUILayout.LabelField("Status", GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        skillScrollPos = EditorGUILayout.BeginScrollView(skillScrollPos);

        foreach (var skill in mainSkillDataList)
        {
            // 필터 적용
            if (!PassesSkillFilter(skill)) continue;

            var entry = effectDatabase.GetEntry(skill.skill_id);
            bool isMapped = entry != null && entry.HasMainEffect();

            if (showOnlyUnmapped && isMapped) continue;

            // 선택 상태에 따른 스타일
            bool isSelected = selectedSkillId == skill.skill_id;
            var style = isSelected ? SelectedStyle : EditorStyles.helpBox;

            EditorGUILayout.BeginHorizontal(style);

            // 클릭 가능한 영역
            if (GUILayout.Button("", GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                selectedSkillId = skill.skill_id;
                UpdatePreview();
            }

            // 덮어쓰기로 내용 표시
            var lastRect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(lastRect.x, lastRect.y, 40, lastRect.height), skill.skill_id.ToString());
            GUI.Label(new Rect(lastRect.x + 40, lastRect.y, 100, lastRect.height), skill.skill_name ?? "Unknown");
            GUI.Label(new Rect(lastRect.x + 140, lastRect.y, 80, lastRect.height), skill.GetSkillType().ToString());
            GUI.Label(new Rect(lastRect.x + 220, lastRect.y, 50, lastRect.height), isMapped ? "OK" : "X",
                isMapped ? OkStyle : ErrorStyle);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawMappingPanel()
    {
        EditorGUILayout.LabelField("Mapping", EditorStyles.boldLabel);

        if (selectedSkillId < 0)
        {
            EditorGUILayout.HelpBox("Select a skill from the list", MessageType.Info);
            return;
        }

        var skill = mainSkillDataList.FirstOrDefault(s => s.skill_id == selectedSkillId);
        if (skill == null) return;

        var entry = GetOrCreateEntry(selectedSkillId);
        entry.skillName = skill.skill_name ?? $"Skill_{selectedSkillId}";

        // 스킬 정보
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Skill ID: {selectedSkillId}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Name: {entry.skillName}");
        EditorGUILayout.LabelField($"Type: {skill.GetSkillType()}");
        EditorGUILayout.LabelField($"Damage: {skill.base_damage}");
        EditorGUILayout.LabelField($"Projectiles: {skill.projectile_count}");
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // 이펙트 타입 설정
        EditorGUILayout.LabelField("Effect Configuration", EditorStyles.boldLabel);

        entry.skillType = (SkillAssetType)EditorGUILayout.EnumPopup("Skill Type", entry.skillType);

        EditorGUILayout.Space(5);

        // 프리팹 필드
        EditorGUI.BeginChangeCheck();

        entry.mainEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Main Effect", entry.mainEffectPrefab, typeof(GameObject), false);

        entry.hitEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Hit Effect", entry.hitEffectPrefab, typeof(GameObject), false);

        entry.castEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Cast Effect", entry.castEffectPrefab, typeof(GameObject), false);

        entry.trailEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Trail Effect", entry.trailEffectPrefab, typeof(GameObject), false);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(effectDatabase);
            UpdatePreview();
        }

        EditorGUILayout.Space(10);

        // 이펙트 지속시간 설정
        EditorGUILayout.LabelField("Effect Duration (DOT용)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        entry.effectDuration = EditorGUILayout.FloatField("Duration (sec)", entry.effectDuration);
        if (GUILayout.Button("Measure", GUILayout.Width(70)))
        {
            float measured = MeasureEffectDuration(entry.mainEffectPrefab);
            if (measured > 0)
            {
                entry.effectDuration = measured;
                EditorUtility.SetDirty(effectDatabase);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("AOE 스킬의 DOT 지속시간으로 사용됩니다.\nMeasure 버튼으로 ParticleSystem Duration을 자동 측정합니다.", MessageType.Info);

        EditorGUILayout.Space(10);

        // 이펙트별 스케일 설정 (실시간 미리보기 지원)
        EditorGUILayout.LabelField("Effect Scale Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("-1 = 전역값 사용, 0 이상 = 개별값\n슬라이더로 조절 후 Scene에서 실시간 확인 가능", MessageType.Info);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Main Effect Scale
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Main Effect", GUILayout.Width(80));
        EditorGUI.BeginChangeCheck();
        entry.scaleOverride = EditorGUILayout.Slider(entry.scaleOverride, -1f, 5f);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(effectDatabase);
            if (scalePreviewInstance != null && currentPreviewType == EffectPreviewType.Main)
            {
                float scale = entry.scaleOverride >= 0f ? entry.scaleOverride : 1f;
                scalePreviewInstance.transform.localScale = Vector3.one * scale;
                SceneView.RepaintAll();
            }
        }
        if (GUILayout.Button("Preview", GUILayout.Width(60)))
        {
            SpawnScalePreview(entry, EffectPreviewType.Main);
        }
        EditorGUILayout.EndHorizontal();

        // Hit Effect Scale
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Hit Effect", GUILayout.Width(80));
        EditorGUI.BeginChangeCheck();
        entry.hitEffectScale = EditorGUILayout.Slider(entry.hitEffectScale, -1f, 5f);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(effectDatabase);
            if (scalePreviewInstance != null && currentPreviewType == EffectPreviewType.Hit)
            {
                float scale = entry.hitEffectScale >= 0f ? entry.hitEffectScale : 1f;
                scalePreviewInstance.transform.localScale = Vector3.one * scale;
                SceneView.RepaintAll();
            }
        }
        if (GUILayout.Button("Preview", GUILayout.Width(60)))
        {
            SpawnScalePreview(entry, EffectPreviewType.Hit);
        }
        EditorGUILayout.EndHorizontal();

        // Cast Effect Scale
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cast Effect", GUILayout.Width(80));
        EditorGUI.BeginChangeCheck();
        entry.castEffectScale = EditorGUILayout.Slider(entry.castEffectScale, -1f, 5f);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(effectDatabase);
            if (scalePreviewInstance != null && currentPreviewType == EffectPreviewType.Cast)
            {
                float scale = entry.castEffectScale >= 0f ? entry.castEffectScale : 1f;
                scalePreviewInstance.transform.localScale = Vector3.one * scale;
                SceneView.RepaintAll();
            }
        }
        if (GUILayout.Button("Preview", GUILayout.Width(60)))
        {
            SpawnScalePreview(entry, EffectPreviewType.Cast);
        }
        EditorGUILayout.EndHorizontal();

        // Trail Effect Scale
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Trail Effect", GUILayout.Width(80));
        EditorGUI.BeginChangeCheck();
        entry.trailEffectScale = EditorGUILayout.Slider(entry.trailEffectScale, -1f, 5f);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(effectDatabase);
            if (scalePreviewInstance != null && currentPreviewType == EffectPreviewType.Trail)
            {
                float scale = entry.trailEffectScale >= 0f ? entry.trailEffectScale : 1f;
                scalePreviewInstance.transform.localScale = Vector3.one * scale;
                SceneView.RepaintAll();
            }
        }
        if (GUILayout.Button("Preview", GUILayout.Width(60)))
        {
            SpawnScalePreview(entry, EffectPreviewType.Trail);
        }
        EditorGUILayout.EndHorizontal();

        // Area Effect Scale
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Area Effect", GUILayout.Width(80));
        EditorGUI.BeginChangeCheck();
        entry.areaEffectScale = EditorGUILayout.Slider(entry.areaEffectScale, -1f, 5f);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(effectDatabase);
            if (scalePreviewInstance != null && currentPreviewType == EffectPreviewType.Area)
            {
                float scale = entry.areaEffectScale >= 0f ? entry.areaEffectScale : 1f;
                scalePreviewInstance.transform.localScale = Vector3.one * scale;
                SceneView.RepaintAll();
            }
        }
        if (GUILayout.Button("Preview", GUILayout.Width(60)))
        {
            SpawnScalePreview(entry, EffectPreviewType.Area);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 저장 버튼 (Play Mode에서 바로 반영)
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply & Save", GUILayout.Height(25)))
        {
            EditorUtility.SetDirty(effectDatabase);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SkillEffectMapper] Scale settings saved for skill {entry.skillId}");
        }

        // 미리보기 컨트롤
        if (scalePreviewInstance != null)
        {
            if (GUILayout.Button("Clear Preview", GUILayout.Width(100)))
            {
                CleanupScalePreview();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (scalePreviewInstance != null)
        {
            EditorGUILayout.LabelField($"미리보기 중: {currentPreviewType}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // 추가 설정
        EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
        entry.useAssetMovement = EditorGUILayout.Toggle("Use Asset Movement", entry.useAssetMovement);
        entry.overrideSpeed = EditorGUILayout.Toggle("Override Speed", entry.overrideSpeed);

        EditorGUILayout.Space(10);

        // 빠른 할당 버튼
        if (selectedEffect != null)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Assign as Main"))
            {
                entry.mainEffectPrefab = selectedEffect.prefab;
                EditorUtility.SetDirty(effectDatabase);
                UpdatePreview();
            }
            if (GUILayout.Button("Assign as Hit"))
            {
                entry.hitEffectPrefab = selectedEffect.prefab;
                EditorUtility.SetDirty(effectDatabase);
            }
            EditorGUILayout.EndHorizontal();
        }

        // 클리어 버튼
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Clear All Effects"))
        {
            entry.mainEffectPrefab = null;
            entry.hitEffectPrefab = null;
            entry.castEffectPrefab = null;
            entry.trailEffectPrefab = null;
            EditorUtility.SetDirty(effectDatabase);
            UpdatePreview();
        }
    }

    private void DrawPreviewPanel()
    {
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos);

        if (previewEditor != null)
        {
            previewEditor.OnInteractivePreviewGUI(
                GUILayoutUtility.GetRect(200, 200),
                EditorStyles.helpBox);
        }
        else
        {
            EditorGUILayout.HelpBox("No preview available", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();

        // 선택된 이펙트 정보
        if (selectedEffect != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Selected Effect", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Name: {selectedEffect.name}");
            EditorGUILayout.LabelField($"Category: {selectedEffect.category}");
            EditorGUILayout.LabelField($"Set: {selectedEffect.setType}");
            EditorGUILayout.LabelField($"Has ObjectMove: {selectedEffect.hasObjectMove}");
            EditorGUILayout.LabelField($"Has ObjectMoveDestroy: {selectedEffect.hasObjectMoveDestroy}");
        }
    }

    #endregion

    #region Effect Browser Tab

    private void DrawEffectBrowserTab()
    {
        EditorGUILayout.LabelField("Effect Browser", EditorStyles.boldLabel);

        // 필터
        EditorGUILayout.BeginHorizontal();
        effectSearchFilter = EditorGUILayout.TextField("Search:", effectSearchFilter, GUILayout.Width(300));
        filterEffectSet = (EffectSetType)EditorGUILayout.EnumPopup("Set:", filterEffectSet, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 이펙트 그리드
        effectScrollPos = EditorGUILayout.BeginScrollView(effectScrollPos);

        int columns = Mathf.Max(1, (int)(position.width / 200));
        int column = 0;

        EditorGUILayout.BeginHorizontal();

        foreach (var effect in effectPrefabList)
        {
            // 필터 적용
            if (!PassesEffectFilter(effect)) continue;

            // 그리드 레이아웃
            if (column >= columns)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                column = 0;
            }

            bool isSelected = selectedEffect == effect;
            var style = isSelected ? SelectedStyle : EditorStyles.helpBox;

            EditorGUILayout.BeginVertical(style, GUILayout.Width(180), GUILayout.Height(100));

            // 썸네일
            var thumbnail = AssetPreview.GetAssetPreview(effect.prefab);
            if (thumbnail != null)
            {
                GUILayout.Label(thumbnail, GUILayout.Width(160), GUILayout.Height(60));
            }
            else
            {
                GUILayout.Label("No Preview", GUILayout.Width(160), GUILayout.Height(60));
            }

            // 이름
            EditorGUILayout.LabelField(effect.name, EditorStyles.miniLabel);

            // 선택 버튼
            if (GUILayout.Button("Select", EditorStyles.miniButton))
            {
                selectedEffect = effect;
                UpdatePreviewForEffect(effect);
            }

            EditorGUILayout.EndVertical();

            column++;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        // 통계
        EditorGUILayout.Space(5);
        int visibleCount = effectPrefabList.Count(e => PassesEffectFilter(e));
        EditorGUILayout.LabelField($"Showing {visibleCount} of {effectPrefabList.Count} effects");
    }

    #endregion

    #region Settings Tab

    private void DrawSettingsTab()
    {
        EditorGUILayout.LabelField("Database Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        effectDatabase = (SkillEffectDatabase)EditorGUILayout.ObjectField(
            "Effect Database", effectDatabase, typeof(SkillEffectDatabase), false);

        if (GUILayout.Button("Create New", GUILayout.Width(100)))
        {
            CreateEffectDatabase();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Paths", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"CSV Path: {CSV_PATH}");
        EditorGUILayout.LabelField($"Effect Asset Path: {EFFECT_ASSET_PATH}");
        EditorGUILayout.LabelField($"Database Path: {DATABASE_PATH}");

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Skills: {mainSkillDataList.Count}");
        EditorGUILayout.LabelField($"Total Effects: {effectPrefabList.Count}");
        EditorGUILayout.LabelField($"Script-Based Effects: {effectPrefabList.Count(e => e.hasObjectMove || e.hasObjectMoveDestroy)}");

        EditorGUILayout.Space(20);

        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Rescan Effect Prefabs"))
        {
            ScanEffectPrefabs();
        }

        if (GUILayout.Button("Sync Database with CSV"))
        {
            if (effectDatabase != null)
            {
                SyncDatabaseWithLoadedCSV();
                SaveEffectDatabase();
            }
        }

        if (GUILayout.Button("Auto-Map by Name Match"))
        {
            AutoMapByNameMatch();
        }

        if (GUILayout.Button("Auto-Map by CSV use_asset Column"))
        {
            AutoMapByCSVUseAsset();
        }

        if (GUILayout.Button("Measure All Effect Durations (AOE용)"))
        {
            MeasureAllEffectDurations();
        }

        if (GUILayout.Button("Measure & Save to CSV skill_lifetime"))
        {
            MeasureAndSaveToCSV();
        }

        EditorGUILayout.Space(10);

        // 중복 제거 버튼
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Remove Duplicate Entries (중복 제거)"))
        {
            RemoveDuplicateEntries();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);

        if (effectDatabase != null)
        {
            EditorGUI.BeginChangeCheck();
            effectDatabase.globalScaleFactor = EditorGUILayout.FloatField("Global Scale", effectDatabase.globalScaleFactor);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(effectDatabase);
            }
        }
    }

    #endregion

    #region Data Management

    private void LoadSkillData()
    {
        mainSkillDataList.Clear();

        string mainSkillPath = Path.Combine(CSV_PATH, "Skill/MainSkillTable.csv");
        if (File.Exists(mainSkillPath))
        {
            string csvText = File.ReadAllText(mainSkillPath);
            csvText = ProcessSkillCsvFormat(csvText);
            mainSkillDataList = CsvUtility.LoadCsvFromText<MainSkillData>(csvText);
            Debug.Log($"[SkillEffectMapper] Loaded {mainSkillDataList.Count} skills");
        }
    }

    private string ProcessSkillCsvFormat(string csvText)
    {
        var lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (lines.Length < 4) return csvText;

        var processedLines = new List<string>();
        processedLines.Add(lines[1]); // 영문 헤더

        for (int i = 3; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                processedLines.Add(lines[i]);
            }
        }

        return string.Join("\n", processedLines);
    }

    private void LoadEffectDatabase()
    {
        effectDatabase = AssetDatabase.LoadAssetAtPath<SkillEffectDatabase>(DATABASE_PATH);

        if (effectDatabase == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:SkillEffectDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                effectDatabase = AssetDatabase.LoadAssetAtPath<SkillEffectDatabase>(path);
            }
        }
    }

    private void CreateEffectDatabase()
    {
        // 폴더 확인/생성
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Skills"))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Skills");
        }

        effectDatabase = ScriptableObject.CreateInstance<SkillEffectDatabase>();
        AssetDatabase.CreateAsset(effectDatabase, DATABASE_PATH);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = effectDatabase;

        Debug.Log($"[SkillEffectMapper] Created SkillEffectDatabase at {DATABASE_PATH}");
    }

    private void SaveEffectDatabase()
    {
        if (effectDatabase != null)
        {
            EditorUtility.SetDirty(effectDatabase);
            AssetDatabase.SaveAssets();
            Debug.Log("[SkillEffectMapper] Database saved");
        }
    }

    /// <summary>
    /// 에디터 모드에서 직접 로드한 CSV 데이터로 데이터베이스 동기화
    /// (런타임 CSVLoader 없이 동작)
    /// </summary>
    private void SyncDatabaseWithLoadedCSV()
    {
        if (effectDatabase == null)
        {
            Debug.LogWarning("[SkillEffectMapper] Database not loaded");
            return;
        }

        if (mainSkillDataList == null || mainSkillDataList.Count == 0)
        {
            LoadSkillData();
        }

        if (mainSkillDataList.Count == 0)
        {
            Debug.LogWarning("[SkillEffectMapper] No skill data loaded from CSV");
            return;
        }

        // 기존 엔트리 업데이트
        foreach (var entry in effectDatabase.entries)
        {
            var skillData = mainSkillDataList.FirstOrDefault(s => s.skill_id == entry.skillId);
            if (skillData != null)
            {
                entry.skillName = skillData.skill_name;
                entry.skillType = skillData.GetSkillType();
            }
        }

        // CSV에 있지만 데이터베이스에 없는 스킬 추가 (빈 엔트리로)
        int addedCount = 0;
        foreach (var skillData in mainSkillDataList)
        {
            if (!effectDatabase.entries.Any(e => e.skillId == skillData.skill_id))
            {
                effectDatabase.entries.Add(new SkillEffectEntry
                {
                    skillId = skillData.skill_id,
                    skillName = skillData.skill_name,
                    skillType = skillData.GetSkillType()
                });
                addedCount++;
            }
        }

        // ID 순으로 정렬
        effectDatabase.entries = effectDatabase.entries.OrderBy(e => e.skillId).ToList();

        EditorUtility.SetDirty(effectDatabase);
        Debug.Log($"[SkillEffectMapper] Synced with CSV: {effectDatabase.entries.Count} entries total, {addedCount} new entries added");
    }

    private void ScanEffectPrefabs()
    {
        effectPrefabList.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EFFECT_ASSET_PATH });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            // 하위 컴포넌트가 아닌 메인 이펙트만 수집 (Base 제외)
            if (path.Contains("(Base)") || path.Contains("_Base") || path.Contains("_Parts"))
                continue;

            var info = new EffectPrefabInfo
            {
                name = prefab.name,
                path = path,
                prefab = prefab,
                hasObjectMove = prefab.GetComponentInChildren<ObjectMove>() != null,
                hasObjectMoveDestroy = prefab.GetComponentInChildren<ObjectMoveDestroy>() != null
            };

            // 카테고리/세트 타입 파싱
            if (path.Contains("EffectsSet_1"))
            {
                info.setType = EffectSetType.Set1_NotScriptBased;
            }
            else if (path.Contains("EffectsSet_2"))
            {
                info.setType = EffectSetType.Set2_ScriptBased;
            }

            // 카테고리 추출 (Effect_XX_Name 형식)
            var parts = prefab.name.Split('_');
            if (parts.Length >= 3)
            {
                info.category = parts[2];
            }

            effectPrefabList.Add(info);
        }

        effectPrefabList = effectPrefabList.OrderBy(e => e.name).ToList();
        Debug.Log($"[SkillEffectMapper] Found {effectPrefabList.Count} effect prefabs");
    }

    private SkillEffectEntry GetOrCreateEntry(int skillId)
    {
        var entry = effectDatabase.GetEntry(skillId);
        if (entry == null)
        {
            entry = new SkillEffectEntry { skillId = skillId };
            effectDatabase.entries.Add(entry);
            EditorUtility.SetDirty(effectDatabase);
        }
        return entry;
    }

    #endregion

    #region Filtering

    private bool PassesSkillFilter(MainSkillData skill)
    {
        // 텍스트 필터
        if (!string.IsNullOrEmpty(searchFilter))
        {
            bool matchesId = skill.skill_id.ToString().Contains(searchFilter);
            bool matchesName = skill.skill_name != null &&
                              skill.skill_name.ToLower().Contains(searchFilter.ToLower());
            if (!matchesId && !matchesName) return false;
        }

        // 타입 필터
        if (useTypeFilter)
        {
            var entry = effectDatabase?.GetEntry(skill.skill_id);
            if (entry != null && entry.skillType != filterSkillType)
                return false;
        }

        return true;
    }

    private bool PassesEffectFilter(EffectPrefabInfo effect)
    {
        // 텍스트 필터
        if (!string.IsNullOrEmpty(effectSearchFilter))
        {
            if (!effect.name.ToLower().Contains(effectSearchFilter.ToLower()))
                return false;
        }

        // 세트 필터
        if (filterEffectSet != EffectSetType.All)
        {
            if (effect.setType != filterEffectSet)
                return false;
        }

        return true;
    }

    #endregion

    #region Preview

    private void UpdatePreview()
    {
        CleanupPreview();

        if (selectedSkillId < 0 || effectDatabase == null) return;

        var entry = effectDatabase.GetEntry(selectedSkillId);
        if (entry?.mainEffectPrefab != null)
        {
            previewEditor = Editor.CreateEditor(entry.mainEffectPrefab);
        }
    }

    private void UpdatePreviewForEffect(EffectPrefabInfo effect)
    {
        CleanupPreview();

        if (effect?.prefab != null)
        {
            previewEditor = Editor.CreateEditor(effect.prefab);
        }
    }

    private void CleanupPreview()
    {
        if (previewEditor != null)
        {
            DestroyImmediate(previewEditor);
            previewEditor = null;
        }

        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    #endregion

    #region Auto Mapping

    private void AutoMapByNameMatch()
    {
        if (effectDatabase == null || mainSkillDataList.Count == 0)
        {
            Debug.LogWarning("[SkillEffectMapper] Cannot auto-map: missing data");
            return;
        }

        int mapped = 0;

        foreach (var skill in mainSkillDataList)
        {
            var entry = effectDatabase.GetEntry(skill.skill_id);
            if (entry != null && entry.HasMainEffect()) continue; // 이미 매핑됨

            // 스킬 이름으로 이펙트 찾기
            string skillName = skill.skill_name?.ToLower() ?? "";
            if (string.IsNullOrEmpty(skillName)) continue;

            // 이름 매칭 시도
            var matchingEffect = effectPrefabList.FirstOrDefault(e =>
                e.name.ToLower().Contains(skillName) ||
                skillName.Contains(e.category?.ToLower() ?? ""));

            if (matchingEffect != null)
            {
                entry = GetOrCreateEntry(skill.skill_id);
                entry.skillName = skill.skill_name;
                entry.mainEffectPrefab = matchingEffect.prefab;

                // 스크립트 기반이면 해당 타입 설정
                if (matchingEffect.hasObjectMoveDestroy)
                {
                    entry.skillType = SkillAssetType.Projectile;
                }
                else if (matchingEffect.hasObjectMove)
                {
                    entry.skillType = SkillAssetType.Projectile;
                }

                mapped++;
            }
        }

        SaveEffectDatabase();
        Debug.Log($"[SkillEffectMapper] Auto-mapped {mapped} skills");
    }

    /// <summary>
    /// CSV의 //use_asset 컬럼을 기반으로 자동 매핑
    /// </summary>
    private void AutoMapByCSVUseAsset()
    {
        if (effectDatabase == null || mainSkillDataList.Count == 0)
        {
            Debug.LogWarning("[SkillEffectMapper] Cannot auto-map: missing data");
            return;
        }

        int mapped = 0;
        int skipped = 0;
        int notFound = 0;
        var notFoundEffects = new List<string>();

        foreach (var skill in mainSkillDataList)
        {
            // 이미 매핑된 스킬은 스킵
            var entry = effectDatabase.GetEntry(skill.skill_id);
            if (entry != null && entry.HasMainEffect())
            {
                skipped++;
                continue;
            }

            // use_asset 컬럼 값 확인
            string useAsset = skill.use_asset;
            if (string.IsNullOrEmpty(useAsset))
            {
                continue;
            }

            // 이펙트 이름으로 프리팹 찾기
            var matchingEffect = FindEffectByName(useAsset);
            if (matchingEffect != null)
            {
                entry = GetOrCreateEntry(skill.skill_id);
                entry.skillName = skill.skill_name;
                entry.skillType = skill.GetSkillType();
                entry.mainEffectPrefab = matchingEffect.prefab;

                // ScriptBased 이펙트는 useAssetMovement = true
                entry.useAssetMovement = matchingEffect.hasObjectMove || matchingEffect.hasObjectMoveDestroy;

                // 히트 이펙트 자동 연결 시도
                var hitEffect = FindHitEffectFor(useAsset);
                if (hitEffect != null)
                {
                    entry.hitEffectPrefab = hitEffect.prefab;
                }

                mapped++;
                Debug.Log($"[SkillEffectMapper] Mapped {skill.skill_id} ({skill.skill_name}) -> {useAsset}");
            }
            else
            {
                notFound++;
                if (!notFoundEffects.Contains(useAsset))
                {
                    notFoundEffects.Add(useAsset);
                }
            }
        }

        SaveEffectDatabase();

        string message = $"Auto-mapping by CSV use_asset completed!\n\n" +
                         $"Mapped: {mapped}\n" +
                         $"Skipped (already mapped): {skipped}\n" +
                         $"Not Found: {notFound}";

        if (notFoundEffects.Count > 0)
        {
            message += $"\n\nNot found effects:\n" + string.Join("\n", notFoundEffects.Take(10));
            if (notFoundEffects.Count > 10)
            {
                message += $"\n... and {notFoundEffects.Count - 10} more";
            }
        }

        Debug.Log($"[SkillEffectMapper] {message}");
        EditorUtility.DisplayDialog("Auto-Map Complete", message, "OK");
    }

    /// <summary>
    /// 이펙트 이름으로 프리팹 찾기
    /// CSV의 use_asset 값 형식: "Effect_18_WindSlash", "Effect_32_FloatingArrow" 등
    /// </summary>
    private EffectPrefabInfo FindEffectByName(string effectName)
    {
        if (string.IsNullOrEmpty(effectName)) return null;

        // 1. 정확한 이름 매칭
        var exact = effectPrefabList.FirstOrDefault(e =>
            e.name.Equals(effectName, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        // 2. 프리팹 이름에 CSV 이름이 포함된 경우
        var containsName = effectPrefabList.FirstOrDefault(e =>
            e.name.IndexOf(effectName, StringComparison.OrdinalIgnoreCase) >= 0);
        if (containsName != null) return containsName;

        // 3. CSV 이름에서 번호 추출하여 같은 폴더의 메인 프리팹 찾기
        // Effect_18_WindSlash -> Effect_18_ 폴더의 메인 프리팹
        if (effectName.StartsWith("Effect_", StringComparison.OrdinalIgnoreCase))
        {
            var parts = effectName.Split('_');
            if (parts.Length >= 2)
            {
                string effectNumber = parts[1];
                string effectFolder = $"Effect_{effectNumber}_";

                // 해당 폴더의 메인 프리팹 찾기 (Base, Parts 폴더 제외됨)
                var folderMatch = effectPrefabList.FirstOrDefault(e =>
                    e.path.Contains(effectFolder) && !e.name.Contains("(Base)"));

                if (folderMatch != null) return folderMatch;
            }

            // 4. CSV의 이름에서 접미사 추출하여 매칭
            // Effect_18_WindSlash -> WindSlash로 검색
            if (parts.Length >= 3)
            {
                string suffix = string.Join("_", parts.Skip(2));

                // 접미사가 포함된 프리팹 찾기
                var suffixMatch = effectPrefabList.FirstOrDefault(e =>
                    e.name.IndexOf(suffix, StringComparison.OrdinalIgnoreCase) >= 0);

                if (suffixMatch != null) return suffixMatch;

                // 접미사가 없는 경우, 같은 번호의 폴더에서 비슷한 이름 찾기
                string effectNumber = parts[1];
                var similarMatch = effectPrefabList.FirstOrDefault(e =>
                    e.name.Contains($"Effect_{effectNumber}_"));

                if (similarMatch != null) return similarMatch;
            }
        }

        // 5. 특수 케이스 매핑 (CSV 이름과 에셋 이름이 다른 경우)
        var specialMapping = GetSpecialEffectMapping(effectName);
        if (!string.IsNullOrEmpty(specialMapping))
        {
            var special = effectPrefabList.FirstOrDefault(e =>
                e.name.IndexOf(specialMapping, StringComparison.OrdinalIgnoreCase) >= 0);
            if (special != null) return special;
        }

        return null;
    }

    /// <summary>
    /// CSV 이름과 에셋 이름이 다른 특수 케이스 매핑
    /// </summary>
    private string GetSpecialEffectMapping(string csvEffectName)
    {
        // CSV 이름 -> 에셋 이름 매핑
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // EffectsSet_2(ScriptBased)의 이펙트들
            { "Effect_03_ChargeFire", "Effect_03_ChargeFire" },
            { "Effect_03_FireCross", "Effect_03_FireCross" },
            { "Effect_04_ChargeShot", "Effect_04_ChargeAndRelease" },
            { "Effect_04_FlamePrison", "Effect_04_FlamePrison" },
            { "Effect_06_BloodFlood", "Effect_06_MassiveSlash" },
            { "Effect_07_OneHandSmash", "Effect_07_OneHandSmash" },
            { "Effect_09_GuardianShield", "Effect_09_GuardianShield" },
            { "Effect_09_GloryShield", "Effect_09_GloryShield" },
            { "Effect_11_ShiningSlashDance", "Effect_11_ShiningSlashDance" },
            { "Effect_11_LightInFullBloom", "Effect_11_LightInFullBloom" },
            { "Effect_13_DangerClose", "Effect_13_DangerClose" },
            { "Effect_14_RuinExplosion", "Effect_14_RuinExplosion" },
            { "Effect_15_MassiveCardRelease", "Effect_15_MassiveCardRelease" },
            { "Effect_15_PhantomShow", "Effect_15_PhantomShow" },
            { "Effect_16_SpaceWarpPortal", "Effect_16_SpaceWarpPortal" },
            { "Effect_18_WindSlash", "Effect_18_WindBlade" },
            { "Effect_18_TimeField", "Effect_18_TimeField" },
            { "Effect_20_RapidFire", "Effect_20_RapidFire" },
            { "Effect_22_WindCyclone", "Effect_22_WindCyclone" },
            { "Effect_26_IceFatalWheel", "Effect_26_IceFatalWheel" },
            { "Effect_28_PurifierBeam", "Effect_28_PurifierBeam" },
            { "Effect_29_LumenCrash", "Effect_29_LumenCrash" },
            { "Effect_30_SwordForce", "Effect_30_MagmaStrike" },
            { "Effect_31_LumenJudgement", "Effect_31_LumenJudgement" },
            { "Effect_32_FloatingArrow", "Effect_32_DevilEye" },
            { "Effect_32_DevilEye", "Effect_32_DevilEye" },
            { "Effect_33_DemonicSphere", "Effect_33_DeathRevolution" },
            { "Effect_34_WindTurbulance", "Effect_34_WindTurbulance" },
            { "Effect_34_SwordBoundary", "Effect_34_WindTurbulance" },
            { "Effect_34_SwordDance", "Effect_34_WindTurbulance" },
            { "Effect_38_PulseShot", "Effect_38_GloryBoundary" },
            { "Effect_38_ElectricExplosion", "Effect_38_GloryBoundary" },
            { "Effect_40_EMPAttack", "Effect_40_EMPAttack" },
            { "Effect_42_CyberStorm", "Effect_42_PlanetDesaster" },
            { "Effect_43_HolyRedemption", "Effect_43_HolyRedemption" },
            { "Effect_43_DarkDimensionAttack", "Effect_43_DarkChainSwamp" },
            { "Effect_44_PurifierWater", "Effect_44_PurifierWater" },
            { "Effect_44_PlanetCrash", "Effect_44_PurifierWater" },
            { "Effect_46_PoisonSmoke", "Effect_46_PoisonSmoke" },
            { "Effect_47_SunBurst", "Effect_47_PreciseShot" },
            { "Effect_47_PreciseShot", "Effect_47_PreciseShot" },
            { "Effect_47_StarFallen", "Effect_47_PreciseShot" },
            { "Effect_48_BondageChain", "Effect_48_BondageChain" },
            { "Effect_48_CriticalTumor", "Effect_48_CriticalTumor" },
            { "Effect_49_HugeTidalWave", "Effect_49_IceBlockCrash" },
            { "Effect_49_IceBlockCrash", "Effect_49_IceBlockCrash" },
            { "Effect_53_CurseOfSpider", "Effect_53_CurseOfSpider" },
            { "Effect_05_Nuke", "Effect_05_Nuke" },
            { "Effect_02_BlackHole", "Effect_02_BlackHole" },
            { "Effect_12_CosmicHorror", "Effect_12_CosmicHorror" },
        };

        if (mappings.TryGetValue(csvEffectName, out string assetName))
        {
            return assetName;
        }

        return null;
    }

    /// <summary>
    /// 메인 이펙트에 대한 히트 이펙트 찾기
    /// </summary>
    private EffectPrefabInfo FindHitEffectFor(string mainEffectName)
    {
        if (string.IsNullOrEmpty(mainEffectName)) return null;

        // 이펙트 번호 추출 (Effect_18_WindSlash -> 18)
        if (mainEffectName.StartsWith("Effect_", StringComparison.OrdinalIgnoreCase))
        {
            var parts = mainEffectName.Split('_');
            if (parts.Length >= 2)
            {
                string effectNumber = parts[1];

                // 같은 번호의 HitEffect 찾기
                var hitEffect = effectPrefabList.FirstOrDefault(e =>
                    e.name.Contains($"Effect_{effectNumber}_") &&
                    (e.name.Contains("Hit") || e.name.Contains("Explosion")));

                return hitEffect;
            }
        }

        return null;
    }

    /// <summary>
    /// 이펙트 지속시간을 측정하여 CSV의 skill_lifetime 컬럼에 저장
    /// </summary>
    private void MeasureAndSaveToCSV()
    {
        if (effectDatabase == null || mainSkillDataList.Count == 0)
        {
            Debug.LogWarning("[SkillEffectMapper] Cannot measure: missing database or CSV data");
            EditorUtility.DisplayDialog("Error", "Database or CSV data not loaded", "OK");
            return;
        }

        string csvPath = "Assets/Data/CSV/Skill/MainSkillTable.csv";
        if (!System.IO.File.Exists(csvPath))
        {
            Debug.LogWarning($"[SkillEffectMapper] CSV file not found: {csvPath}");
            EditorUtility.DisplayDialog("Error", $"CSV file not found: {csvPath}", "OK");
            return;
        }

        int measured = 0;
        int skipped = 0;
        int failed = 0;

        // CSV 파일 읽기
        var lines = new List<string>(System.IO.File.ReadAllLines(csvPath));
        if (lines.Count < 4) // 헤더 3줄 + 데이터 최소 1줄
        {
            Debug.LogWarning("[SkillEffectMapper] CSV file has invalid format");
            return;
        }

        // skill_lifetime 컬럼 인덱스 찾기
        string[] headers = lines[1].Split(',');
        int lifetimeIndex = -1;
        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i].Trim() == "skill_lifetime")
            {
                lifetimeIndex = i;
                break;
            }
        }

        if (lifetimeIndex == -1)
        {
            Debug.LogWarning("[SkillEffectMapper] skill_lifetime column not found in CSV");
            EditorUtility.DisplayDialog("Error", "skill_lifetime column not found in CSV", "OK");
            return;
        }

        // 각 AOE 스킬의 이펙트 지속시간 측정 및 CSV 업데이트
        for (int lineIndex = 3; lineIndex < lines.Count; lineIndex++)
        {
            string[] fields = lines[lineIndex].Split(',');
            if (fields.Length <= lifetimeIndex) continue;

            // skill_id 파싱
            if (!int.TryParse(fields[0], out int skillId)) continue;

            // 해당 스킬 데이터 찾기
            var skillData = mainSkillDataList.Find(s => s.skill_id == skillId);
            if (skillData == null) continue;

            // AOE 타입 스킬만 처리
            var skillType = skillData.GetSkillType();
            if (skillType != SkillAssetType.AOE)
            {
                skipped++;
                continue;
            }

            // 이펙트 프리팹 찾기
            var entry = effectDatabase.GetEntry(skillId);
            GameObject effectPrefab = entry?.mainEffectPrefab;

            if (effectPrefab == null)
            {
                // CSV의 use_asset으로 찾기 시도
                string useAsset = skillData.use_asset;
                if (!string.IsNullOrEmpty(useAsset))
                {
                    var effectInfo = FindEffectByName(useAsset);
                    effectPrefab = effectInfo?.prefab;
                }
            }

            if (effectPrefab == null)
            {
                failed++;
                continue;
            }

            // 이펙트 지속시간 측정
            float duration = MeasureEffectDuration(effectPrefab);
            if (duration > 0)
            {
                // CSV 필드 업데이트
                fields[lifetimeIndex] = duration.ToString("F1");
                lines[lineIndex] = string.Join(",", fields);
                measured++;

                Debug.Log($"[SkillEffectMapper] Measured {skillId} ({skillData.skill_name}): {duration:F1}s");
            }
            else
            {
                failed++;
            }
        }

        // CSV 파일 저장
        System.IO.File.WriteAllLines(csvPath, lines);
        AssetDatabase.Refresh();

        string message = $"Effect Duration -> CSV skill_lifetime Complete!\n\n" +
                         $"Measured & Saved: {measured}\n" +
                         $"Skipped (not AOE): {skipped}\n" +
                         $"Failed (no effect prefab): {failed}";

        Debug.Log($"[SkillEffectMapper] {message}");
        EditorUtility.DisplayDialog("Measurement Complete", message, "OK");

        // CSV 리로드
        LoadSkillData();
    }

    /// <summary>
    /// 모든 AOE 스킬의 이펙트 지속시간 일괄 측정
    /// </summary>
    private void MeasureAllEffectDurations()
    {
        if (effectDatabase == null)
        {
            Debug.LogWarning("[SkillEffectMapper] No database loaded");
            return;
        }

        int measured = 0;
        int skipped = 0;
        int failed = 0;

        foreach (var entry in effectDatabase.entries)
        {
            // AOE 타입 스킬만 처리
            if (entry.skillType != SkillAssetType.AOE &&
                entry.skillType != SkillAssetType.DOT &&
                entry.skillType != SkillAssetType.Trap)
            {
                skipped++;
                continue;
            }

            if (entry.mainEffectPrefab == null)
            {
                skipped++;
                continue;
            }

            // 이미 측정된 값이 있으면 스킵
            if (entry.effectDuration > 0)
            {
                skipped++;
                continue;
            }

            float duration = MeasureEffectDuration(entry.mainEffectPrefab);
            if (duration > 0)
            {
                entry.effectDuration = duration;
                measured++;
            }
            else
            {
                failed++;
            }
        }

        EditorUtility.SetDirty(effectDatabase);
        AssetDatabase.SaveAssets();

        string message = $"Effect Duration Measurement Complete!\n\n" +
                         $"Measured: {measured}\n" +
                         $"Skipped: {skipped}\n" +
                         $"Failed: {failed}";

        Debug.Log($"[SkillEffectMapper] {message}");
        EditorUtility.DisplayDialog("Measurement Complete", message, "OK");
    }

    /// <summary>
    /// 이펙트 프리팹의 ParticleSystem Duration 측정
    /// </summary>
    private float MeasureEffectDuration(GameObject prefab)
    {
        if (prefab == null) return 0f;

        float maxDuration = 0f;
        float maxLoopDuration = 0f;

        // 모든 ParticleSystem 검사
        var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            float duration = main.duration;

            if (!main.loop)
            {
                // 루프가 아닌 파티클: duration + startLifetime
                float totalDuration = duration + main.startLifetime.constantMax;
                if (totalDuration > maxDuration)
                {
                    maxDuration = totalDuration;
                }
            }
            else
            {
                // 루프 파티클: duration만 기록 (fallback용)
                if (duration > maxLoopDuration)
                {
                    maxLoopDuration = duration;
                }
            }
        }

        // 루프 파티클만 있는 경우: 자동 측정 불가, 수동 입력 필요
        // (루프 파티클은 무한 반복되므로 기획자가 직접 duration을 지정해야 함)

        // ObjectMoveDestroy의 time 값도 확인
        var objectMoveDestroy = prefab.GetComponent<ObjectMoveDestroy>();
        if (objectMoveDestroy != null)
        {
            var timeField = objectMoveDestroy.GetType().GetField("time");
            if (timeField != null)
            {
                float time = (float)timeField.GetValue(objectMoveDestroy);
                if (time > maxDuration)
                {
                    maxDuration = time;
                }
            }
        }

        // ObjectMove의 time 값도 확인
        var objectMove = prefab.GetComponent<ObjectMove>();
        if (objectMove != null)
        {
            var timeField = objectMove.GetType().GetField("time");
            if (timeField != null)
            {
                float time = (float)timeField.GetValue(objectMove);
                if (time > maxDuration)
                {
                    maxDuration = time;
                }
            }
        }

        if (maxDuration > 0)
        {
            Debug.Log($"[SkillEffectMapper] Measured duration for {prefab.name}: {maxDuration:F2}s");
        }
        else
        {
            Debug.LogWarning($"[SkillEffectMapper] Loop particle only - manual input required: {prefab.name} (CSV의 skill_lifetime에 직접 입력 필요)");
        }

        return maxDuration;
    }

    #endregion

    #region Utility

    private GUIStyle CreateColorStyle(Color color)
    {
        var style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = color;
        return style;
    }

    private GUIStyle CreateSelectedStyle()
    {
        var style = new GUIStyle(EditorStyles.helpBox);
        style.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.5f, 0.8f, 0.5f));
        return style;
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        var texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    #endregion

    #region Support Skill Tab

    private void DrawSupportSkillMappingTab()
    {
        if (effectDatabase == null)
        {
            EditorGUILayout.HelpBox("SkillEffectDatabase not found. Create one in Settings tab.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // 왼쪽: 서포트 스킬 목록
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
        DrawSupportSkillList();
        EditorGUILayout.EndVertical();

        // 오른쪽: 매핑 패널
        EditorGUILayout.BeginVertical();
        DrawSupportMappingPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSupportSkillList()
    {
        EditorGUILayout.LabelField("Support Skills", EditorStyles.boldLabel);

        supportScrollPos = EditorGUILayout.BeginScrollView(supportScrollPos);

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("ID", GUILayout.Width(60));
        GUILayout.Label("Name", GUILayout.Width(150));
        GUILayout.Label("Type", GUILayout.Width(100));
        GUILayout.Label("Status");
        EditorGUILayout.EndHorizontal();

        foreach (var support in supportSkillDataList)
        {
            bool isSelected = selectedSupportId == support.support_id;
            var entry = effectDatabase.supportEntries.FirstOrDefault(e => e.supportId == support.support_id);
            bool hasIcon = entry != null && entry.HasStatusIcon();

            EditorGUILayout.BeginHorizontal(isSelected ? SelectedStyle : GUIStyle.none);

            if (GUILayout.Button(support.support_id.ToString(), EditorStyles.label, GUILayout.Width(60)))
            {
                selectedSupportId = support.support_id;
            }

            if (GUILayout.Button(support.support_name ?? "-", EditorStyles.label, GUILayout.Width(150)))
            {
                selectedSupportId = support.support_id;
            }

            GUILayout.Label(support.GetStatusEffectType().ToString(), GUILayout.Width(100));
            GUILayout.Label(hasIcon ? "O" : "-", hasIcon ? OkStyle : ErrorStyle);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSupportMappingPanel()
    {
        EditorGUILayout.LabelField("Support Skill Mapping", EditorStyles.boldLabel);

        if (selectedSupportId < 0)
        {
            EditorGUILayout.HelpBox("Select a support skill from the list", MessageType.Info);
            return;
        }

        var supportData = supportSkillDataList.FirstOrDefault(s => s.support_id == selectedSupportId);
        if (supportData == null) return;

        // 서포트 스킬 정보 표시
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"ID: {supportData.support_id}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Name: {supportData.support_name ?? "N/A"}");
        EditorGUILayout.LabelField($"Effect Type: {supportData.GetStatusEffectType()}");
        EditorGUILayout.LabelField($"Category: {supportData.GetSupportCategory()}");

        EditorGUILayout.Space(10);

        // 엔트리 가져오기 또는 생성
        var entry = GetOrCreateSupportEntry(selectedSupportId);

        // 기본 정보 자동 채우기
        if (string.IsNullOrEmpty(entry.supportName))
        {
            entry.supportName = supportData.support_name;
            entry.effectType = supportData.GetStatusEffectType();
        }

        EditorGUILayout.LabelField("Icon Prefabs", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        // 상태이상 타입에 따른 UI
        switch (supportData.GetStatusEffectType())
        {
            case StatusEffectType.Mark:
                DrawMarkIconFields(entry);
                break;

            case StatusEffectType.CC:
                entry.stunIconPrefab = (GameObject)EditorGUILayout.ObjectField("Stun Icon", entry.stunIconPrefab, typeof(GameObject), false);
                entry.slowIconPrefab = (GameObject)EditorGUILayout.ObjectField("Slow Icon", entry.slowIconPrefab, typeof(GameObject), false);
                break;

            case StatusEffectType.DOT:
                entry.statusIconPrefab = (GameObject)EditorGUILayout.ObjectField("DOT Icon", entry.statusIconPrefab, typeof(GameObject), false);
                entry.dotTickEffectPrefab = (GameObject)EditorGUILayout.ObjectField("DOT Tick Effect", entry.dotTickEffectPrefab, typeof(GameObject), false);
                break;

            default:
                entry.statusIconPrefab = (GameObject)EditorGUILayout.ObjectField("Status Icon", entry.statusIconPrefab, typeof(GameObject), false);
                break;
        }

        EditorGUILayout.Space(5);
        entry.applyEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Apply Effect", entry.applyEffectPrefab, typeof(GameObject), false);
        entry.removeEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Remove Effect", entry.removeEffectPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Position Settings", EditorStyles.boldLabel);
        entry.iconYOffset = EditorGUILayout.FloatField("Y Offset", entry.iconYOffset);
        entry.iconScale = EditorGUILayout.FloatField("Scale", entry.iconScale);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(effectDatabase);
        }
    }

    private void DrawMarkIconFields(SupportSkillEffectEntry entry)
    {
        if (entry.markIcons == null)
        {
            entry.markIcons = new MarkIconSet();
        }

        EditorGUILayout.LabelField("Mark Type Icons", EditorStyles.miniLabel);
        entry.markIcons.romanceIcon = (GameObject)EditorGUILayout.ObjectField("  Romance", entry.markIcons.romanceIcon, typeof(GameObject), false);
        entry.markIcons.comedyIcon = (GameObject)EditorGUILayout.ObjectField("  Comedy", entry.markIcons.comedyIcon, typeof(GameObject), false);
        entry.markIcons.adventureIcon = (GameObject)EditorGUILayout.ObjectField("  Adventure", entry.markIcons.adventureIcon, typeof(GameObject), false);
        entry.markIcons.mysteryIcon = (GameObject)EditorGUILayout.ObjectField("  Mystery", entry.markIcons.mysteryIcon, typeof(GameObject), false);
        entry.markIcons.fearIcon = (GameObject)EditorGUILayout.ObjectField("  Fear", entry.markIcons.fearIcon, typeof(GameObject), false);
    }

    private SupportSkillEffectEntry GetOrCreateSupportEntry(int supportId)
    {
        var entry = effectDatabase.supportEntries.FirstOrDefault(e => e.supportId == supportId);
        if (entry == null)
        {
            entry = new SupportSkillEffectEntry { supportId = supportId };
            effectDatabase.supportEntries.Add(entry);
            EditorUtility.SetDirty(effectDatabase);
        }
        return entry;
    }

    private void LoadSupportSkillData()
    {
        supportSkillDataList.Clear();

        string supportSkillPath = Path.Combine(CSV_PATH, "Skill/SupportSkillTable.csv");
        if (File.Exists(supportSkillPath))
        {
            string csvText = File.ReadAllText(supportSkillPath);
            csvText = ProcessSkillCsvFormat(csvText);
            supportSkillDataList = CsvUtility.LoadCsvFromText<SupportSkillData>(csvText);
            Debug.Log($"[SkillEffectMapper] Loaded {supportSkillDataList.Count} support skills");
        }
        else
        {
            Debug.LogWarning($"[SkillEffectMapper] Support skill CSV not found: {supportSkillPath}");
        }
    }

    /// <summary>
    /// 중복 엔트리 제거
    /// </summary>
    private void RemoveDuplicateEntries()
    {
        if (effectDatabase == null)
        {
            Debug.LogWarning("[SkillEffectMapper] No database loaded");
            return;
        }

        int originalCount = effectDatabase.entries.Count;
        var uniqueEntries = new Dictionary<int, SkillEffectEntry>();

        // 중복 제거 (첫 번째 유효한 엔트리 유지)
        foreach (var entry in effectDatabase.entries)
        {
            if (!uniqueEntries.ContainsKey(entry.skillId))
            {
                uniqueEntries[entry.skillId] = entry;
            }
            else
            {
                // 기존 엔트리가 이펙트가 없고 새 엔트리에 있으면 교체
                var existing = uniqueEntries[entry.skillId];
                if (!existing.HasAnyEffect() && entry.HasAnyEffect())
                {
                    uniqueEntries[entry.skillId] = entry;
                }
            }
        }

        effectDatabase.entries = uniqueEntries.Values.ToList();
        effectDatabase.entries = effectDatabase.entries.OrderBy(e => e.skillId).ToList();

        int removedCount = originalCount - effectDatabase.entries.Count;

        EditorUtility.SetDirty(effectDatabase);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SkillEffectMapper] Removed {removedCount} duplicate entries. {effectDatabase.entries.Count} unique entries remain.");
        EditorUtility.DisplayDialog("중복 제거 완료",
            $"원래 엔트리 수: {originalCount}\n" +
            $"제거된 중복: {removedCount}\n" +
            $"남은 엔트리: {effectDatabase.entries.Count}",
            "OK");
    }

    #endregion

    #region AOE Preview Tab

    private void DrawAOEPreviewTab()
    {
        if (effectDatabase == null)
        {
            EditorGUILayout.HelpBox("SkillEffectDatabase not found. Create one in Settings tab.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // 왼쪽: AOE 스킬 목록
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.35f));
        DrawAOESkillList();
        EditorGUILayout.EndVertical();

        // 오른쪽: AOE 설정 및 미리보기
        EditorGUILayout.BeginVertical();
        DrawAOESettingsPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawAOESkillList()
    {
        EditorGUILayout.LabelField("AOE Skills", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("AOE 타입 스킬만 표시됩니다. 선택 후 Scene View에서 범위를 확인하세요.", MessageType.Info);

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("ID", GUILayout.Width(50));
        GUILayout.Label("Name", GUILayout.Width(120));
        GUILayout.Label("AOE Radius", GUILayout.Width(80));
        GUILayout.Label("Effect", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        aoeSkillScrollPos = EditorGUILayout.BeginScrollView(aoeSkillScrollPos);

        foreach (var skill in mainSkillDataList)
        {
            // AOE 스킬만 필터링 (aoe_radius > 0)
            if (skill.aoe_radius <= 0) continue;

            var entry = effectDatabase.GetEntry(skill.skill_id);
            bool hasEffect = entry != null && entry.HasMainEffect();
            bool isSelected = selectedAOESkillId == skill.skill_id;

            var style = isSelected ? SelectedStyle : EditorStyles.helpBox;

            EditorGUILayout.BeginHorizontal(style);

            if (GUILayout.Button("", GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                SelectAOESkill(skill.skill_id);
            }

            var lastRect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(lastRect.x, lastRect.y, 50, lastRect.height), skill.skill_id.ToString());
            GUI.Label(new Rect(lastRect.x + 50, lastRect.y, 120, lastRect.height), skill.skill_name ?? "Unknown");
            GUI.Label(new Rect(lastRect.x + 170, lastRect.y, 80, lastRect.height), skill.aoe_radius.ToString("F1"));
            GUI.Label(new Rect(lastRect.x + 250, lastRect.y, 60, lastRect.height), hasEffect ? "✓" : "-",
                hasEffect ? OkStyle : ErrorStyle);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAOESettingsPanel()
    {
        EditorGUILayout.LabelField("AOE Preview Settings", EditorStyles.boldLabel);

        if (selectedAOESkillId < 0)
        {
            EditorGUILayout.HelpBox("왼쪽 목록에서 AOE 스킬을 선택하세요.\n\n" +
                "워크플로우:\n" +
                "1. 스킬 선택 후 'Spawn Effect' 버튼 클릭\n" +
                "2. Gizmo = 데미지 범위 (aoe_radius) - 직접 수정 가능\n" +
                "3. Effect Scale 슬라이더로 이펙트 크기를 Gizmo에 맞춤\n" +
                "4. 'Save baseEffectRadius' 버튼으로 저장\n" +
                "→ baseEffectRadius = aoe_radius / effectScale", MessageType.Info);
            return;
        }

        var skill = mainSkillDataList.FirstOrDefault(s => s.skill_id == selectedAOESkillId);
        if (skill == null) return;

        var entry = effectDatabase.GetEntry(selectedAOESkillId);

        // 스킬 정보
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Skill ID: {selectedAOESkillId}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Name: {skill.skill_name}");
        EditorGUILayout.LabelField($"CSV aoe_radius: {skill.aoe_radius:F1} (데미지 범위)");
        if (entry != null)
        {
            EditorGUILayout.LabelField($"baseEffectRadius: {entry.baseEffectRadius:F1} (저장된 이펙트 기본 크기)");
            float runtimeScale = entry.baseEffectRadius > 0 ? skill.aoe_radius / entry.baseEffectRadius : 1f;
            EditorGUILayout.LabelField($"Runtime Scale: {runtimeScale:F2}");
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Gizmo 설정
        EditorGUILayout.LabelField("Gizmo Settings (= 데미지 범위)", EditorStyles.boldLabel);
        showAOEGizmo = EditorGUILayout.Toggle("Show Gizmo", showAOEGizmo);
        aoeGizmoColor = EditorGUILayout.ColorField("Gizmo Color", aoeGizmoColor);

        EditorGUILayout.Space(5);

        // 미리보기 위치
        aoePreviewPosition = EditorGUILayout.Vector3Field("Position", aoePreviewPosition);
        if (GUILayout.Button("Move to Scene Camera"))
        {
            if (SceneView.lastActiveSceneView != null)
            {
                var camera = SceneView.lastActiveSceneView.camera;
                aoePreviewPosition = camera.transform.position + camera.transform.forward * 10f;
                aoePreviewPosition.y = 0;
            }
        }

        EditorGUILayout.Space(10);

        // === Gizmo = aoe_radius 조절 (데미지 범위) ===
        EditorGUILayout.LabelField("Step 1: 데미지 범위 (Gizmo)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Gizmo 반경 = 실제 데미지가 적용되는 범위입니다.\n필요시 CSV에 저장할 수 있습니다.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        aoePreviewRadius = EditorGUILayout.Slider("AOE Radius (Gizmo)", aoePreviewRadius, 0.5f, 50f);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+0.5")) aoePreviewRadius += 0.5f;
        if (GUILayout.Button("-0.5")) aoePreviewRadius = Mathf.Max(0.5f, aoePreviewRadius - 0.5f);
        if (GUILayout.Button("+1")) aoePreviewRadius += 1f;
        if (GUILayout.Button("-1")) aoePreviewRadius = Mathf.Max(0.5f, aoePreviewRadius - 1f);
        if (GUILayout.Button("Reset")) aoePreviewRadius = skill.aoe_radius;
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

        // CSV 저장 버튼
        bool hasCSVChanges = Mathf.Abs(aoePreviewRadius - skill.aoe_radius) > 0.01f;
        if (hasCSVChanges)
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"Save aoe_radius to CSV ({skill.aoe_radius:F1} → {aoePreviewRadius:F1})"))
            {
                SaveAOERadiusToCSV(selectedAOESkillId, aoePreviewRadius);
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(10);

        // === Effect Scale 조절 ===
        EditorGUILayout.LabelField("Step 2: 이펙트 스케일 조절", EditorStyles.boldLabel);

        if (entry != null && entry.HasMainEffect())
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Effect"))
            {
                SpawnEffectForScaling(entry);
            }
            if (GUILayout.Button("Clear"))
            {
                CleanupAOEPreview();
            }
            EditorGUILayout.EndHorizontal();

            if (aoeEffectPreviewInstance != null)
            {
                EditorGUILayout.HelpBox(
                    "Effect Scale을 조절해서 이펙트 크기를 Gizmo(데미지 범위)에 맞추세요.\n" +
                    "이펙트가 Gizmo와 일치하면 'Save baseEffectRadius' 버튼을 클릭하세요.",
                    MessageType.Info);

                EditorGUI.BeginChangeCheck();
                aoeEffectPreviewScale = EditorGUILayout.Slider("Effect Scale", aoeEffectPreviewScale, 0.1f, 10f);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+0.1")) aoeEffectPreviewScale += 0.1f;
                if (GUILayout.Button("-0.1")) aoeEffectPreviewScale = Mathf.Max(0.1f, aoeEffectPreviewScale - 0.1f);
                if (GUILayout.Button("+0.5")) aoeEffectPreviewScale += 0.5f;
                if (GUILayout.Button("-0.5")) aoeEffectPreviewScale = Mathf.Max(0.1f, aoeEffectPreviewScale - 0.5f);
                if (GUILayout.Button("1.0")) aoeEffectPreviewScale = 1f;
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    // 이펙트 스케일 실시간 적용
                    aoeEffectPreviewInstance.transform.localScale = Vector3.one * aoeEffectPreviewScale;
                    SceneView.RepaintAll();
                }

                EditorGUILayout.Space(5);

                // baseEffectRadius 계산 및 저장
                float calculatedBaseRadius = aoePreviewRadius / aoeEffectPreviewScale;
                EditorGUILayout.LabelField($"계산된 baseEffectRadius: {calculatedBaseRadius:F2}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"(= aoe_radius {aoePreviewRadius:F1} / scale {aoeEffectPreviewScale:F2})", EditorStyles.miniLabel);

                bool baseChanged = Mathf.Abs(calculatedBaseRadius - entry.baseEffectRadius) > 0.01f;
                GUI.backgroundColor = baseChanged ? Color.cyan : Color.white;
                if (GUILayout.Button(baseChanged ? "★ Save baseEffectRadius ★" : "Save baseEffectRadius"))
                {
                    entry.baseEffectRadius = calculatedBaseRadius;
                    EditorUtility.SetDirty(effectDatabase);
                    AssetDatabase.SaveAssets();

                    EditorUtility.DisplayDialog("Success",
                        $"baseEffectRadius = {calculatedBaseRadius:F2} 저장됨!\n\n" +
                        $"계산식:\n" +
                        $"baseEffectRadius = aoe_radius / effectScale\n" +
                        $"= {aoePreviewRadius:F1} / {aoeEffectPreviewScale:F2}\n" +
                        $"= {calculatedBaseRadius:F2}\n\n" +
                        $"런타임에서:\n" +
                        $"scale = aoe_radius / baseEffectRadius\n" +
                        $"= {skill.aoe_radius:F1} / {calculatedBaseRadius:F2}\n" +
                        $"= {skill.aoe_radius / calculatedBaseRadius:F2}",
                        "OK");
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("'Spawn Effect' 버튼을 눌러 이펙트를 스폰하세요.", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("이펙트 프리팹이 매핑되지 않았습니다.\n'Main Skills' 탭에서 이펙트를 먼저 연결하세요.", MessageType.Warning);

            if (GUILayout.Button("Go to Main Skills Tab"))
            {
                currentTab = Tab.SkillMapping;
                selectedSkillId = selectedAOESkillId;
            }
        }

        EditorGUILayout.Space(10);

        // === Step 3: 런타임 미리보기 ===
        EditorGUILayout.LabelField("Step 3: 런타임 미리보기", EditorStyles.boldLabel);

        if (entry != null && entry.HasMainEffect() && entry.baseEffectRadius > 0)
        {
            float runtimeScale = skill.aoe_radius / entry.baseEffectRadius;
            EditorGUILayout.LabelField($"저장된 baseEffectRadius: {entry.baseEffectRadius:F2}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"계산된 Runtime Scale: {runtimeScale:F2}", EditorStyles.miniLabel);

            if (GUILayout.Button("Preview Runtime Scale"))
            {
                SpawnEffectAtRuntimeScale(entry, skill.aoe_radius);
                aoePreviewRadius = skill.aoe_radius;
            }
        }
        else if (entry != null && entry.baseEffectRadius <= 0)
        {
            EditorGUILayout.HelpBox("Step 2에서 baseEffectRadius를 먼저 설정하세요.", MessageType.Warning);
        }

        EditorGUILayout.Space(10);

        // Scene View 포커스
        if (GUILayout.Button("Focus Scene View"))
        {
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(aoePreviewPosition, Quaternion.Euler(45, 0, 0), aoePreviewRadius * 3f);
            }
        }
    }

    /// <summary>
    /// 스케일 조절용 이펙트 스폰
    /// baseEffectRadius가 이미 설정되어 있으면 해당 스케일로, 없으면 Scale=1로 시작
    /// </summary>
    private void SpawnEffectForScaling(SkillEffectEntry entry)
    {
        CleanupAOEPreview();

        if (entry?.mainEffectPrefab == null) return;

        // Instantiate 사용 (프리팹 연결 끊기) - NewMaterialChange 등의 스크립트가 자식 삭제 시 에러 방지
        aoeEffectPreviewInstance = UnityEngine.Object.Instantiate(entry.mainEffectPrefab);
        aoeEffectPreviewInstance.transform.position = aoePreviewPosition;
        aoeEffectPreviewInstance.hideFlags = HideFlags.DontSave;

        // 기존 baseEffectRadius가 있으면 해당 스케일로 시작, 없으면 1로 시작
        if (entry.baseEffectRadius > 0)
        {
            aoeEffectPreviewScale = aoePreviewRadius / entry.baseEffectRadius;
            aoeEffectPreviewInstance.name = $"[Preview] {entry.skillName} (Scale={aoeEffectPreviewScale:F2})";
        }
        else
        {
            aoeEffectPreviewScale = 1f;
            aoeEffectPreviewInstance.name = $"[Preview] {entry.skillName} (Scale=1)";
        }

        aoeEffectPreviewInstance.transform.localScale = Vector3.one * aoeEffectPreviewScale;

        Selection.activeGameObject = aoeEffectPreviewInstance;
        Debug.Log($"[AOE Preview] Spawned effect for scaling. Initial scale={aoeEffectPreviewScale:F2}");
    }

    /// <summary>
    /// 런타임 스케일로 이펙트 스폰
    /// </summary>
    private void SpawnEffectAtRuntimeScale(SkillEffectEntry entry, float aoeRadius)
    {
        CleanupAOEPreview();

        if (entry?.mainEffectPrefab == null || entry.baseEffectRadius <= 0) return;

        float runtimeScale = aoeRadius / entry.baseEffectRadius;

        // Instantiate 사용 (프리팹 연결 끊기) - NewMaterialChange 등의 스크립트가 자식 삭제 시 에러 방지
        aoeEffectPreviewInstance = UnityEngine.Object.Instantiate(entry.mainEffectPrefab);
        aoeEffectPreviewInstance.name = $"[Runtime] {entry.skillName} (Scale={runtimeScale:F2})";
        aoeEffectPreviewInstance.transform.position = aoePreviewPosition;
        aoeEffectPreviewInstance.transform.localScale = Vector3.one * runtimeScale;
        aoeEffectPreviewInstance.hideFlags = HideFlags.DontSave;

        aoeEffectPreviewScale = runtimeScale;

        Selection.activeGameObject = aoeEffectPreviewInstance;
        Debug.Log($"[AOE Preview] Spawned effect at runtime scale={runtimeScale:F2} (aoe={aoeRadius}/base={entry.baseEffectRadius})");
    }

    /// <summary>
    /// aoe_radius 값을 CSV 파일에 저장
    /// </summary>
    private void SaveAOERadiusToCSV(int skillId, float newRadius)
    {
        string csvPath = Path.Combine(CSV_PATH, "Skill/MainSkillTable.csv");

        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("Error", $"CSV file not found:\n{csvPath}", "OK");
            return;
        }

        try
        {
            // CSV 파일 읽기
            var lines = File.ReadAllLines(csvPath).ToList();

            if (lines.Count < 4)
            {
                EditorUtility.DisplayDialog("Error", "Invalid CSV format", "OK");
                return;
            }

            // 헤더에서 aoe_radius 컬럼 인덱스 찾기 (영문 헤더는 2번째 줄, index=1)
            string[] headers = lines[1].Split(',');
            int aoeRadiusIndex = -1;
            int skillIdIndex = -1;

            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i].Trim() == "aoe_radius") aoeRadiusIndex = i;
                if (headers[i].Trim() == "skill_id") skillIdIndex = i;
            }

            if (aoeRadiusIndex < 0 || skillIdIndex < 0)
            {
                EditorUtility.DisplayDialog("Error", "Required columns not found in CSV", "OK");
                return;
            }

            // 데이터 행에서 해당 skill_id 찾아서 수정 (데이터는 4번째 줄부터, index=3)
            bool found = false;
            for (int lineIndex = 3; lineIndex < lines.Count; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex])) continue;

                string[] fields = lines[lineIndex].Split(',');
                if (fields.Length <= Mathf.Max(aoeRadiusIndex, skillIdIndex)) continue;

                if (int.TryParse(fields[skillIdIndex].Trim(), out int parsedId) && parsedId == skillId)
                {
                    // aoe_radius 값 변경
                    fields[aoeRadiusIndex] = newRadius.ToString("F1");
                    lines[lineIndex] = string.Join(",", fields);
                    found = true;

                    Debug.Log($"[SkillEffectMapper] Updated skill_id {skillId}: aoe_radius = {newRadius:F1}");
                    break;
                }
            }

            if (!found)
            {
                EditorUtility.DisplayDialog("Error", $"skill_id {skillId} not found in CSV", "OK");
                return;
            }

            // CSV 파일 저장
            File.WriteAllLines(csvPath, lines);

            // 내부 데이터 리스트도 업데이트
            var skillData = mainSkillDataList.FirstOrDefault(s => s.skill_id == skillId);
            if (skillData != null)
            {
                // MainSkillData는 property라서 직접 수정 가능
                // reflection 사용하여 값 설정
                var property = typeof(MainSkillData).GetProperty("aoe_radius");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(skillData, newRadius);
                }
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"aoe_radius saved!\n\n" +
                $"Skill ID: {skillId}\n" +
                $"New Value: {newRadius:F1}\n\n" +
                $"이펙트 크기와 데미지 범위가 동기화됩니다.",
                "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to save CSV:\n{e.Message}", "OK");
            Debug.LogError($"[SkillEffectMapper] CSV save error: {e}");
        }
    }

    private void SelectAOESkill(int skillId)
    {
        selectedAOESkillId = skillId;

        var skill = mainSkillDataList.FirstOrDefault(s => s.skill_id == skillId);
        if (skill != null)
        {
            aoePreviewRadius = skill.aoe_radius;
        }

        // 기존 프리뷰 정리
        CleanupAOEPreview();

        SceneView.RepaintAll();
    }


    private void CleanupAOEPreview()
    {
        if (aoeEffectPreviewInstance != null)
        {
            // Selection 해제 (Inspector 에러 방지)
            if (Selection.activeGameObject == aoeEffectPreviewInstance)
            {
                Selection.activeGameObject = null;
            }

            DestroyImmediate(aoeEffectPreviewInstance);
            aoeEffectPreviewInstance = null;
        }
    }

    /// <summary>
    /// 이펙트 스케일 프리뷰 생성
    /// </summary>
    private void SpawnScalePreview(SkillEffectEntry entry, EffectPreviewType previewType)
    {
        CleanupScalePreview();

        if (entry == null) return;

        GameObject prefabToSpawn = null;
        float scale = 1f;

        switch (previewType)
        {
            case EffectPreviewType.Main:
                prefabToSpawn = entry.mainEffectPrefab;
                scale = entry.scaleOverride >= 0f ? entry.scaleOverride : 1f;
                break;
            case EffectPreviewType.Hit:
                prefabToSpawn = entry.hitEffectPrefab;
                scale = entry.hitEffectScale >= 0f ? entry.hitEffectScale : 1f;
                break;
            case EffectPreviewType.Cast:
                prefabToSpawn = entry.castEffectPrefab;
                scale = entry.castEffectScale >= 0f ? entry.castEffectScale : 1f;
                break;
            case EffectPreviewType.Trail:
                prefabToSpawn = entry.trailEffectPrefab;
                scale = entry.trailEffectScale >= 0f ? entry.trailEffectScale : 1f;
                break;
            case EffectPreviewType.Area:
                prefabToSpawn = entry.areaEffectPrefab;
                scale = entry.areaEffectScale >= 0f ? entry.areaEffectScale : 1f;
                break;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[SkillEffectMapper] No prefab assigned for {previewType} effect");
            return;
        }

        scalePreviewInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);
        if (scalePreviewInstance != null)
        {
            scalePreviewInstance.name = $"[SCALE PREVIEW] {prefabToSpawn.name}";
            scalePreviewInstance.transform.position = scalePreviewPosition;
            scalePreviewInstance.transform.localScale = Vector3.one * scale;
            scalePreviewInstance.hideFlags = HideFlags.DontSave;
            currentPreviewType = previewType;

            // Scene View 포커스
            SceneView.lastActiveSceneView?.LookAt(scalePreviewPosition);
            Selection.activeGameObject = scalePreviewInstance;

            Debug.Log($"[SkillEffectMapper] Spawned {previewType} effect preview with scale {scale}");
        }

        SceneView.RepaintAll();
    }

    /// <summary>
    /// 이펙트 스케일 프리뷰 정리
    /// </summary>
    private void CleanupScalePreview()
    {
        if (scalePreviewInstance != null)
        {
            // Selection 해제 (Inspector 에러 방지)
            if (Selection.activeGameObject == scalePreviewInstance)
            {
                Selection.activeGameObject = null;
            }

            DestroyImmediate(scalePreviewInstance);
            scalePreviewInstance = null;
        }
    }

    /// <summary>
    /// 스케일 프리뷰 인스턴스의 스케일을 실시간 업데이트
    /// </summary>
    private void UpdateScalePreviewScale(float newScale)
    {
        if (scalePreviewInstance != null)
        {
            scalePreviewInstance.transform.localScale = Vector3.one * newScale;
            SceneView.RepaintAll();
        }
    }

    /// <summary>
    /// Scene View에 AOE Gizmo 그리기
    /// </summary>
    private void OnSceneGUI(SceneView sceneView)
    {
        // AOE Preview 탭이 아니거나 Gizmo 비활성화 시 그리지 않음
        if (currentTab != Tab.AOEPreview || !showAOEGizmo || selectedAOESkillId < 0)
            return;

        Handles.color = aoeGizmoColor;

        // 바닥에 원형 Gizmo 그리기
        Handles.DrawWireDisc(aoePreviewPosition, Vector3.up, aoePreviewRadius);

        // 채워진 원 (반투명)
        Color fillColor = aoeGizmoColor;
        fillColor.a *= 0.3f;
        Handles.color = fillColor;
        Handles.DrawSolidDisc(aoePreviewPosition, Vector3.up, aoePreviewRadius);

        // 중앙 표시
        Handles.color = Color.white;
        float crossSize = 0.5f;
        Handles.DrawLine(aoePreviewPosition - Vector3.right * crossSize, aoePreviewPosition + Vector3.right * crossSize);
        Handles.DrawLine(aoePreviewPosition - Vector3.forward * crossSize, aoePreviewPosition + Vector3.forward * crossSize);

        // 반경 표시 라벨
        Handles.color = Color.white;
        Handles.Label(aoePreviewPosition + Vector3.right * aoePreviewRadius + Vector3.up * 0.5f,
            $"R: {aoePreviewRadius:F1}",
            EditorStyles.boldLabel);

        // 드래그로 위치 이동 가능하게
        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.PositionHandle(aoePreviewPosition, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            aoePreviewPosition = newPosition;
            aoePreviewPosition.y = 0; // 바닥에 고정

            // 이펙트 프리뷰도 같이 이동
            if (aoeEffectPreviewInstance != null)
            {
                aoeEffectPreviewInstance.transform.position = aoePreviewPosition;
            }

            Repaint();
        }

        // 이펙트 프리뷰 인스턴스의 스케일 업데이트
        if (aoeEffectPreviewInstance != null)
        {
            aoeEffectPreviewInstance.transform.localScale = Vector3.one * aoeEffectPreviewScale;
        }
    }

    #endregion
}
