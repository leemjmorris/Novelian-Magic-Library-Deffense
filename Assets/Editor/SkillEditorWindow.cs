using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Novelian.Combat;

/// <summary>
/// 스킬 에디터 통합 윈도우
/// - CSV ↔ VFXDatabase 동기화
/// - 스킬 프리뷰
/// - VFX 할당
/// - 테스트 씬 자동화
/// </summary>
public class SkillEditorWindow : EditorWindow
{
    #region Tab System
    private enum Tab
    {
        VFXDatabase,
        CSVSync,
        Preview,
        Test,
        ExternalAssets
    }
    private Tab currentTab = Tab.VFXDatabase;
    private readonly string[] tabNames = { "VFX Database", "CSV 동기화", "프리뷰", "테스트", "외부 에셋" };
    #endregion

    #region References
    private SkillVFXDatabase vfxDatabase;
    private Vector2 scrollPosition;
    private Vector2 csvScrollPosition;
    #endregion

    #region CSV Data
    private List<MainSkillCSVEntry> csvSkills = new List<MainSkillCSVEntry>();
    private string csvPath = "Assets/Data/CSV/Skill/MainSkillTable.csv";
    private bool csvLoaded = false;
    #endregion

    #region Filter & Search
    private string searchFilter = "";
    private string behaviorTypeFilter = "All";
    private readonly string[] behaviorTypes = {
        "All", "SingleProjectile", "ExplosiveProjectile", "FallingProjectile",
        "BeamRay", "TargetAOE", "LinearAOE", "GroundAOE", "MovingAOE",
        "Barrier", "Buff", "Debuff", "Trap", "Instant"
    };
    #endregion

    #region Preview
    private int selectedSkillId = -1;
    private GameObject previewInstance;
    private Editor gameObjectEditor;
    #endregion

    #region External Assets
    private Vector2 externalAssetsScrollPosition;
    private List<ExternalEffectInfo> externalEffects = new List<ExternalEffectInfo>();
    private bool externalAssetsLoaded = false;
    private string externalAssetPath = "Assets/SpecialSkillsEffectsPack/AllEffects";
    private int selectedExternalEffectIndex = -1;
    private bool showScriptBasedOnly = false;
    private string externalSearchFilter = "";

    // behavior_type 자동 매핑
    private static readonly Dictionary<string, string> EffectNameToBehaviorType = new Dictionary<string, string>
    {
        // AOE 타입
        { "tornado", "MovingAOE" },
        { "storm", "MovingAOE" },
        { "cyclone", "MovingAOE" },
        { "blackhole", "GroundAOE" },
        { "field", "GroundAOE" },
        { "swamp", "GroundAOE" },
        { "poison", "GroundAOE" },
        { "timeField", "GroundAOE" },
        { "nuke", "TargetAOE" },
        { "explosion", "TargetAOE" },
        { "boom", "TargetAOE" },
        { "blast", "TargetAOE" },

        // Falling 타입
        { "orbital", "FallingProjectile" },
        { "strike", "FallingProjectile" },
        { "meteor", "FallingProjectile" },
        { "airstrike", "FallingProjectile" },
        { "fleet", "FallingProjectile" },
        { "satelite", "FallingProjectile" },

        // Beam 타입
        { "beam", "BeamRay" },
        { "laser", "BeamRay" },
        { "breath", "BeamRay" },
        { "ray", "BeamRay" },

        // Projectile 타입
        { "shot", "SingleProjectile" },
        { "fire", "SingleProjectile" },
        { "ball", "SingleProjectile" },
        { "bullet", "SingleProjectile" },
        { "fist", "SingleProjectile" },

        // 기타
        { "shield", "Barrier" },
        { "guardian", "Barrier" },
        { "slash", "LinearAOE" },
        { "wave", "LinearAOE" },
        { "chain", "Debuff" }
    };
    #endregion

    #region Styles
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialized = false;
    #endregion

    [MenuItem("Tools/Novelian/스킬 에디터 %#k")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillEditorWindow>("스킬 에디터");
        window.minSize = new Vector2(600, 500);
        window.Show();
    }

    private void OnEnable()
    {
        FindVFXDatabase();
        LoadCSVData();
    }

    private void OnDisable()
    {
        CleanupPreview();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        boxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(10, 10, 10, 10)
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11
        };

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();

        // 탭 선택
        EditorGUILayout.Space(5);
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, tabNames);
        EditorGUILayout.Space(10);

        // 현재 탭 렌더링
        switch (currentTab)
        {
            case Tab.VFXDatabase:
                DrawVFXDatabaseTab();
                break;
            case Tab.CSVSync:
                DrawCSVSyncTab();
                break;
            case Tab.Preview:
                DrawPreviewTab();
                break;
            case Tab.Test:
                DrawTestTab();
                break;
            case Tab.ExternalAssets:
                DrawExternalAssetsTab();
                break;
        }
    }

    #region VFX Database Tab

    private void DrawVFXDatabaseTab()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        // 데이터베이스 선택
        EditorGUILayout.LabelField("VFX Database 관리", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUI.BeginChangeCheck();
        vfxDatabase = (SkillVFXDatabase)EditorGUILayout.ObjectField(
            "VFX Database", vfxDatabase, typeof(SkillVFXDatabase), false);
        if (EditorGUI.EndChangeCheck() && vfxDatabase != null)
        {
            EditorUtility.SetDirty(vfxDatabase);
        }

        if (vfxDatabase == null)
        {
            EditorGUILayout.HelpBox("SkillVFXDatabase를 선택하거나 새로 생성하세요.", MessageType.Warning);
            if (GUILayout.Button("새 VFXDatabase 생성", GUILayout.Height(30)))
            {
                CreateNewVFXDatabase();
            }
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.Space(10);

        // 필터
        DrawFilterSection();

        EditorGUILayout.Space(10);

        // Entry 목록
        DrawEntryList();

        EditorGUILayout.Space(10);

        // 액션 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CSV에서 누락된 Entry 추가", GUILayout.Height(30)))
        {
            AddMissingEntriesFromCSV();
        }
        if (GUILayout.Button("빈 VFX 자동 감지", GUILayout.Height(30)))
        {
            DetectEmptyVFX();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 초기화 섹션
        DrawVFXDatabaseResetSection();

        EditorGUILayout.EndVertical();
    }

    private void DrawFilterSection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("검색:", GUILayout.Width(40));
        searchFilter = EditorGUILayout.TextField(searchFilter);

        EditorGUILayout.LabelField("타입:", GUILayout.Width(35));
        int typeIndex = Array.IndexOf(behaviorTypes, behaviorTypeFilter);
        typeIndex = EditorGUILayout.Popup(typeIndex, behaviorTypes, GUILayout.Width(150));
        behaviorTypeFilter = behaviorTypes[Mathf.Max(0, typeIndex)];
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEntryList()
    {
        if (vfxDatabase == null) return;

        var entries = GetDatabaseEntries();
        if (entries == null || entries.Count == 0)
        {
            EditorGUILayout.HelpBox("등록된 Entry가 없습니다.", MessageType.Info);
            return;
        }

        // 필터링
        var filteredEntries = FilterEntries(entries);

        EditorGUILayout.LabelField($"Entry 목록 ({filteredEntries.Count}/{entries.Count})", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));

        foreach (var entry in filteredEntries)
        {
            DrawEntryRow(entry);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEntryRow(SkillVFXDatabase.Entry entry)
    {
        // CSV에서 스킬 정보 가져오기
        var csvEntry = csvSkills.Find(s => s.skill_id == entry.skillId);
        string skillName = csvEntry != null ? csvEntry.skill_name : "Unknown";
        string behaviorType = csvEntry != null ? csvEntry.behavior_type : "N/A";

        EditorGUILayout.BeginHorizontal("box");

        // ID & 이름
        EditorGUILayout.LabelField($"[{entry.skillId}] {skillName}", GUILayout.Width(150));
        EditorGUILayout.LabelField(behaviorType, GUILayout.Width(120));

        // VFX 상태 표시
        bool hasVFX = entry.vfxPrefab != null;
        bool hasHit = entry.hitPrefab != null;

        GUI.color = hasVFX ? Color.green : Color.red;
        EditorGUILayout.LabelField(hasVFX ? "●" : "○", GUILayout.Width(20));
        GUI.color = Color.white;

        // VFX 필드
        EditorGUI.BeginChangeCheck();
        entry.vfxPrefab = (GameObject)EditorGUILayout.ObjectField(
            entry.vfxPrefab, typeof(GameObject), false, GUILayout.Width(150));
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(vfxDatabase);
        }

        // Hit 필드
        EditorGUI.BeginChangeCheck();
        entry.hitPrefab = (GameObject)EditorGUILayout.ObjectField(
            entry.hitPrefab, typeof(GameObject), false, GUILayout.Width(100));
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(vfxDatabase);
        }

        // 프리뷰 버튼
        if (hasVFX && GUILayout.Button("👁", GUILayout.Width(30)))
        {
            selectedSkillId = entry.skillId;
            currentTab = Tab.Preview;
            CreatePreview(entry.vfxPrefab);
        }

        EditorGUILayout.EndHorizontal();
    }

    private List<SkillVFXDatabase.Entry> GetDatabaseEntries()
    {
        // Reflection으로 private entries 접근
        var field = typeof(SkillVFXDatabase).GetField("entries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(vfxDatabase) as List<SkillVFXDatabase.Entry>;
    }

    private List<SkillVFXDatabase.Entry> FilterEntries(List<SkillVFXDatabase.Entry> entries)
    {
        return entries.Where(e =>
        {
            // 검색 필터
            if (!string.IsNullOrEmpty(searchFilter))
            {
                var csv = csvSkills.Find(s => s.skill_id == e.skillId);
                string name = csv?.skill_name ?? "";
                if (!name.ToLower().Contains(searchFilter.ToLower()) &&
                    !e.skillId.ToString().Contains(searchFilter))
                    return false;
            }

            // 타입 필터
            if (behaviorTypeFilter != "All")
            {
                var csv = csvSkills.Find(s => s.skill_id == e.skillId);
                if (csv?.behavior_type != behaviorTypeFilter)
                    return false;
            }

            return true;
        }).ToList();
    }

    #endregion

    #region CSV Sync Tab

    private void DrawCSVSyncTab()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("CSV ↔ VFXDatabase 동기화", headerStyle);
        EditorGUILayout.Space(10);

        // CSV 경로
        EditorGUILayout.BeginHorizontal();
        csvPath = EditorGUILayout.TextField("CSV 경로", csvPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFilePanel("CSV 선택", "Assets", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                csvPath = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CSV 다시 로드", GUILayout.Height(25)))
        {
            LoadCSVData();
        }
        GUI.color = csvLoaded ? Color.green : Color.yellow;
        EditorGUILayout.LabelField(csvLoaded ? $"✓ {csvSkills.Count}개 로드됨" : "로드 필요", GUILayout.Width(120));
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        if (!csvLoaded)
        {
            EditorGUILayout.HelpBox("CSV를 먼저 로드하세요.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        // CSV 목록
        EditorGUILayout.LabelField("CSV 스킬 목록", EditorStyles.boldLabel);

        csvScrollPosition = EditorGUILayout.BeginScrollView(csvScrollPosition, GUILayout.Height(200));

        foreach (var skill in csvSkills)
        {
            DrawCSVRow(skill);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // 동기화 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CSV → Database 동기화", GUILayout.Height(35)))
        {
            SyncCSVToDatabase();
        }
        EditorGUILayout.EndHorizontal();

        // 통계
        DrawSyncStatistics();

        EditorGUILayout.Space(10);

        // 초기화 섹션
        DrawCSVResetSection();

        EditorGUILayout.EndVertical();
    }

    private void DrawCSVRow(MainSkillCSVEntry skill)
    {
        bool hasEntry = vfxDatabase != null && vfxDatabase.GetEntry(skill.skill_id) != null;
        bool hasVFX = hasEntry && vfxDatabase.GetVFXPrefab(skill.skill_id) != null;

        EditorGUILayout.BeginHorizontal();

        // 상태 아이콘
        GUI.color = hasVFX ? Color.green : (hasEntry ? Color.yellow : Color.red);
        EditorGUILayout.LabelField(hasVFX ? "●" : (hasEntry ? "◐" : "○"), GUILayout.Width(20));
        GUI.color = Color.white;

        EditorGUILayout.LabelField($"[{skill.skill_id}]", GUILayout.Width(50));
        EditorGUILayout.LabelField(skill.skill_name, GUILayout.Width(120));
        EditorGUILayout.LabelField(skill.behavior_type, GUILayout.Width(130));
        EditorGUILayout.LabelField($"DMG:{skill.base_damage}", GUILayout.Width(80));

        if (!hasEntry && GUILayout.Button("+", GUILayout.Width(25)))
        {
            AddEntryForSkill(skill);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSyncStatistics()
    {
        if (vfxDatabase == null || csvSkills.Count == 0) return;

        int totalCSV = csvSkills.Count;
        int hasEntry = csvSkills.Count(s => vfxDatabase.GetEntry(s.skill_id) != null);
        int hasVFX = csvSkills.Count(s => vfxDatabase.GetVFXPrefab(s.skill_id) != null);

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("동기화 현황", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"CSV 스킬: {totalCSV}개");

        float entryPercent = totalCSV > 0 ? (float)hasEntry / totalCSV * 100 : 0;
        float vfxPercent = totalCSV > 0 ? (float)hasVFX / totalCSV * 100 : 0;
        float progressValue = totalCSV > 0 ? (float)hasVFX / totalCSV : 0;

        EditorGUILayout.LabelField($"Database Entry: {hasEntry}개 ({entryPercent:F0}%)");
        EditorGUILayout.LabelField($"VFX 할당됨: {hasVFX}개 ({vfxPercent:F0}%)");

        // 진행 바
        Rect progressRect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
        EditorGUI.ProgressBar(progressRect, progressValue, $"완료도: {hasVFX}/{totalCSV}");

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Preview Tab

    private void DrawPreviewTab()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("스킬 VFX 프리뷰", headerStyle);
        EditorGUILayout.Space(10);

        if (vfxDatabase == null)
        {
            EditorGUILayout.HelpBox("VFX Database를 먼저 선택하세요.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        // 스킬 선택
        var entries = GetDatabaseEntries();
        if (entries == null || entries.Count == 0)
        {
            EditorGUILayout.HelpBox("등록된 스킬이 없습니다.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        string[] skillNames = entries.Select(e =>
        {
            var csv = csvSkills.Find(s => s.skill_id == e.skillId);
            return $"[{e.skillId}] {csv?.skill_name ?? "Unknown"}";
        }).ToArray();

        int currentIndex = entries.FindIndex(e => e.skillId == selectedSkillId);
        if (currentIndex < 0) currentIndex = 0;

        EditorGUI.BeginChangeCheck();
        currentIndex = EditorGUILayout.Popup("스킬 선택", currentIndex, skillNames);
        if (EditorGUI.EndChangeCheck() && currentIndex >= 0 && currentIndex < entries.Count)
        {
            selectedSkillId = entries[currentIndex].skillId;
            var prefab = entries[currentIndex].vfxPrefab;
            if (prefab != null)
            {
                CreatePreview(prefab);
            }
        }

        EditorGUILayout.Space(10);

        // 선택된 스킬 정보
        if (selectedSkillId > 0)
        {
            var selectedEntry = entries.Find(e => e.skillId == selectedSkillId);
            var csvEntry = csvSkills.Find(s => s.skill_id == selectedSkillId);

            if (csvEntry != null)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("스킬 정보", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"이름: {csvEntry.skill_name}");
                EditorGUILayout.LabelField($"타입: {csvEntry.behavior_type}");
                EditorGUILayout.LabelField($"데미지: {csvEntry.base_damage}");
                EditorGUILayout.LabelField($"사거리: {csvEntry.range}");
                EditorGUILayout.LabelField($"쿨다운: {csvEntry.cooldown}s");
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            // 프리뷰 영역
            if (selectedEntry?.vfxPrefab != null)
            {
                EditorGUILayout.LabelField("VFX 프리뷰", EditorStyles.boldLabel);

                // 프리팹 에디터 프리뷰
                if (gameObjectEditor == null || gameObjectEditor.target != selectedEntry.vfxPrefab)
                {
                    if (gameObjectEditor != null)
                        DestroyImmediate(gameObjectEditor);
                    gameObjectEditor = Editor.CreateEditor(selectedEntry.vfxPrefab);
                }

                if (gameObjectEditor != null)
                {
                    gameObjectEditor.OnInteractivePreviewGUI(
                        GUILayoutUtility.GetRect(256, 256), GUIStyle.none);
                }

                EditorGUILayout.Space(5);

                if (GUILayout.Button("프리팹 선택", GUILayout.Height(25)))
                {
                    Selection.activeObject = selectedEntry.vfxPrefab;
                    EditorGUIUtility.PingObject(selectedEntry.vfxPrefab);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("VFX 프리팹이 할당되지 않았습니다.", MessageType.Warning);
            }
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Test Tab

    private void DrawTestTab()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("스킬 테스트 도구", headerStyle);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "테스트 씬에서 스킬을 빠르게 테스트할 수 있습니다.\n" +
            "SkillTestManager가 있는 씬에서 사용하세요.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 테스트 씬 열기
        if (GUILayout.Button("테스트 씬 열기", GUILayout.Height(35)))
        {
            OpenTestScene();
        }

        EditorGUILayout.Space(10);

        // 빠른 테스트 설정
        EditorGUILayout.LabelField("빠른 테스트 설정", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("몬스터 3마리 배치", GUILayout.Height(25)))
        {
            SpawnTestMonsters(3);
        }

        if (GUILayout.Button("몬스터 10마리 배치", GUILayout.Height(25)))
        {
            SpawnTestMonsters(10);
        }

        if (GUILayout.Button("원형 배치 (8마리)", GUILayout.Height(25)))
        {
            SpawnMonstersInCircle(8, 5f);
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("모든 몬스터 제거", GUILayout.Height(25)))
        {
            ClearTestMonsters();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // 디버그 옵션
        EditorGUILayout.LabelField("디버그 옵션", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("Physics Layer 설정 확인", GUILayout.Height(25)))
        {
            CheckPhysicsLayers();
        }

        if (GUILayout.Button("VFX Database 검증", GUILayout.Height(25)))
        {
            ValidateVFXDatabase();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Helper Methods

    private void FindVFXDatabase()
    {
        if (vfxDatabase != null) return;

        string[] guids = AssetDatabase.FindAssets("t:SkillVFXDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            vfxDatabase = AssetDatabase.LoadAssetAtPath<SkillVFXDatabase>(path);
        }
    }

    private void CreateNewVFXDatabase()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "VFXDatabase 생성", "SkillVFXDatabase", "asset", "저장 위치 선택");

        if (string.IsNullOrEmpty(path)) return;

        var newDatabase = ScriptableObject.CreateInstance<SkillVFXDatabase>();
        AssetDatabase.CreateAsset(newDatabase, path);
        AssetDatabase.SaveAssets();

        vfxDatabase = newDatabase;
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newDatabase;
    }

    private void LoadCSVData()
    {
        csvSkills.Clear();
        csvLoaded = false;

        if (!File.Exists(csvPath))
        {
            Debug.LogWarning($"[SkillEditor] CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 4) return; // 최소 4줄 필요 (주석헤더, 헤더, 타입, 데이터)

            // 헤더 라인 찾기 (skill_id가 포함된 줄)
            int headerLineIndex = -1;
            for (int i = 0; i < Math.Min(5, lines.Length); i++)
            {
                if (lines[i].Contains("skill_id"))
                {
                    headerLineIndex = i;
                    break;
                }
            }

            if (headerLineIndex < 0)
            {
                Debug.LogWarning("[SkillEditor] CSV 헤더를 찾을 수 없습니다 (skill_id 컬럼 필요)");
                return;
            }

            // 헤더 파싱
            string[] headers = ParseCSVLine(lines[headerLineIndex]);
            var headerIndex = new Dictionary<string, int>();
            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i].Trim();
                // //로 시작하는 주석 헤더 처리
                if (header.StartsWith("//"))
                    header = header.Substring(2);
                headerIndex[header] = i;
            }

            // 데이터 시작 라인 찾기 (헤더 다음, 타입 정의 줄 건너뛰기)
            int dataStartLine = headerLineIndex + 1;
            // 타입 정의 줄인지 확인 (int, float, string 등으로 시작)
            if (dataStartLine < lines.Length)
            {
                string firstVal = ParseCSVLine(lines[dataStartLine])[0].Trim().ToLower();
                if (firstVal == "int" || firstVal == "float" || firstVal == "string" || firstVal.StartsWith("//"))
                {
                    dataStartLine++; // 타입 정의 줄 건너뛰기
                }
            }

            // 데이터 파싱
            for (int i = dataStartLine; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                // 주석 줄 건너뛰기
                if (lines[i].TrimStart().StartsWith("//")) continue;

                string[] values = ParseCSVLine(lines[i]);
                var entry = new MainSkillCSVEntry();

                if (headerIndex.TryGetValue("skill_id", out int idIdx))
                    int.TryParse(GetValue(values, idIdx), out entry.skill_id);
                if (headerIndex.TryGetValue("skill_name", out int nameIdx))
                    entry.skill_name = GetValue(values, nameIdx);
                if (headerIndex.TryGetValue("behavior_type", out int typeIdx))
                    entry.behavior_type = GetValue(values, typeIdx);
                if (headerIndex.TryGetValue("base_damage", out int dmgIdx))
                    float.TryParse(GetValue(values, dmgIdx), out entry.base_damage);
                if (headerIndex.TryGetValue("range", out int rangeIdx))
                    float.TryParse(GetValue(values, rangeIdx), out entry.range);
                if (headerIndex.TryGetValue("cooldown", out int cdIdx))
                    float.TryParse(GetValue(values, cdIdx), out entry.cooldown);

                if (entry.skill_id > 0)
                {
                    csvSkills.Add(entry);
                }
            }

            csvLoaded = true;
            Debug.Log($"[SkillEditor] CSV 로드 완료: {csvSkills.Count}개 스킬");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SkillEditor] CSV 파싱 오류: {e.Message}");
        }
    }

    private string[] ParseCSVLine(string line)
    {
        var result = new List<string>();
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
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current);

        return result.ToArray();
    }

    private string GetValue(string[] values, int index)
    {
        if (index < 0 || index >= values.Length) return "";
        return values[index].Trim();
    }

    private void SyncCSVToDatabase()
    {
        if (vfxDatabase == null || csvSkills.Count == 0) return;

        var entries = GetDatabaseEntries();
        if (entries == null)
        {
            Debug.LogError("[SkillEditor] Database entries에 접근할 수 없습니다.");
            return;
        }

        int added = 0;
        foreach (var csv in csvSkills)
        {
            if (!entries.Any(e => e.skillId == csv.skill_id))
            {
                entries.Add(new SkillVFXDatabase.Entry { skillId = csv.skill_id });
                added++;
            }
        }

        EditorUtility.SetDirty(vfxDatabase);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SkillEditor] 동기화 완료: {added}개 Entry 추가됨");
    }

    private void AddEntryForSkill(MainSkillCSVEntry skill)
    {
        if (vfxDatabase == null) return;

        var entries = GetDatabaseEntries();
        if (entries == null) return;

        if (!entries.Any(e => e.skillId == skill.skill_id))
        {
            entries.Add(new SkillVFXDatabase.Entry { skillId = skill.skill_id });
            EditorUtility.SetDirty(vfxDatabase);
            Debug.Log($"[SkillEditor] Entry 추가: [{skill.skill_id}] {skill.skill_name}");
        }
    }

    private void AddMissingEntriesFromCSV()
    {
        SyncCSVToDatabase();
    }

    private void DetectEmptyVFX()
    {
        if (vfxDatabase == null) return;

        var entries = GetDatabaseEntries();
        if (entries == null) return;

        var emptyEntries = entries.Where(e => e.vfxPrefab == null).ToList();

        if (emptyEntries.Count == 0)
        {
            EditorUtility.DisplayDialog("검사 완료", "모든 Entry에 VFX가 할당되어 있습니다.", "확인");
        }
        else
        {
            string message = $"VFX가 없는 Entry: {emptyEntries.Count}개\n\n";
            foreach (var entry in emptyEntries.Take(10))
            {
                var csv = csvSkills.Find(s => s.skill_id == entry.skillId);
                message += $"[{entry.skillId}] {csv?.skill_name ?? "Unknown"}\n";
            }
            if (emptyEntries.Count > 10)
            {
                message += $"... 외 {emptyEntries.Count - 10}개";
            }
            EditorUtility.DisplayDialog("검사 결과", message, "확인");
        }
    }

    private void CreatePreview(GameObject prefab)
    {
        CleanupPreview();

        if (prefab == null) return;

        // Scene View에서 프리뷰 (Play 모드가 아닐 때)
        if (!Application.isPlaying)
        {
            previewInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            previewInstance.name = "[Preview] " + prefab.name;
            previewInstance.hideFlags = HideFlags.DontSave;
            Selection.activeGameObject = previewInstance;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }

    private void CleanupPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        if (gameObjectEditor != null)
        {
            DestroyImmediate(gameObjectEditor);
            gameObjectEditor = null;
        }
    }

    private void OpenTestScene()
    {
        string[] guids = AssetDatabase.FindAssets("SkillTest t:Scene");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
        }
        else
        {
            EditorUtility.DisplayDialog("알림", "SkillTest 씬을 찾을 수 없습니다.", "확인");
        }
    }

    private void SpawnTestMonsters(int count)
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("알림", "Play 모드에서만 사용 가능합니다.", "확인");
            return;
        }

        var testManager = FindFirstObjectByType<SkillTestManager>();
        if (testManager != null)
        {
            // SkillTestManager의 메서드 호출
            for (int i = 0; i < count; i++)
            {
                // Reflection으로 private 메서드 호출
                var method = testManager.GetType().GetMethod("OnAddMonsterClicked",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(testManager, null);
            }
        }
    }

    private void SpawnMonstersInCircle(int count, float radius)
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("알림", "Play 모드에서만 사용 가능합니다.", "확인");
            return;
        }

        // Play 모드에서 원형 배치 로직
        Debug.Log($"[SkillEditor] 원형 배치: {count}마리, 반경 {radius}m");
    }

    private void ClearTestMonsters()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("알림", "Play 모드에서만 사용 가능합니다.", "확인");
            return;
        }

        var monsters = FindObjectsByType<Monster>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            DestroyImmediate(monster.gameObject);
        }
        Debug.Log($"[SkillEditor] {monsters.Length}마리 몬스터 제거됨");
    }

    private void CheckPhysicsLayers()
    {
        string[] layers = { "Monster", "Projectile", "Player" };
        string result = "Physics Layer 상태:\n\n";

        foreach (var layerName in layers)
        {
            int layer = LayerMask.NameToLayer(layerName);
            result += $"{layerName}: {(layer >= 0 ? $"OK (Layer {layer})" : "❌ 없음")}\n";
        }

        EditorUtility.DisplayDialog("Layer 설정", result, "확인");
    }

    private void ValidateVFXDatabase()
    {
        if (vfxDatabase == null)
        {
            EditorUtility.DisplayDialog("검증 실패", "VFX Database가 선택되지 않았습니다.", "확인");
            return;
        }

        var entries = GetDatabaseEntries();
        if (entries == null)
        {
            EditorUtility.DisplayDialog("검증 실패", "Entries에 접근할 수 없습니다.", "확인");
            return;
        }

        int total = entries.Count;
        int withVFX = entries.Count(e => e.vfxPrefab != null);
        int withHit = entries.Count(e => e.hitPrefab != null);

        float vfxPercent = total > 0 ? (float)withVFX / total * 100 : 0;
        float hitPercent = total > 0 ? (float)withHit / total * 100 : 0;

        string result = $"VFX Database 검증 결과:\n\n" +
                       $"총 Entry: {total}개\n" +
                       $"VFX 할당: {withVFX}개 ({vfxPercent:F0}%)\n" +
                       $"Hit VFX 할당: {withHit}개 ({hitPercent:F0}%)\n\n" +
                       $"상태: {(total > 0 && withVFX == total ? "✓ 완료" : "⚠ 미완료")}";

        EditorUtility.DisplayDialog("검증 결과", result, "확인");
    }

    #endregion

    #region Reset/Initialize Methods

    private void DrawVFXDatabaseResetSection()
    {
        EditorGUILayout.LabelField("초기화", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        GUI.color = new Color(1f, 0.7f, 0.7f);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("VFX Database 초기화", GUILayout.Height(25)))
        {
            ResetVFXDatabase();
        }

        if (GUILayout.Button("새 Database 생성", GUILayout.Height(25)))
        {
            CreateNewVFXDatabase();
        }

        EditorGUILayout.EndHorizontal();
        GUI.color = Color.white;

        EditorGUILayout.HelpBox("초기화 시 모든 Entry가 삭제됩니다. 신중하게 선택하세요.", MessageType.Warning);
        EditorGUILayout.EndVertical();
    }

    private void DrawCSVResetSection()
    {
        EditorGUILayout.LabelField("초기화 / 템플릿", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("CSV 템플릿 생성", GUILayout.Height(25)))
        {
            CreateCSVTemplate();
        }

        if (GUILayout.Button("샘플 CSV 생성", GUILayout.Height(25)))
        {
            CreateSampleCSV();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        GUI.color = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("전체 초기화 (CSV + Database + Containers)", GUILayout.Height(30)))
        {
            FullReset();
        }
        GUI.color = Color.white;

        EditorGUILayout.HelpBox("템플릿: 빈 CSV 생성 | 샘플: 예제 스킬 포함된 CSV 생성", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    private void ResetVFXDatabase()
    {
        if (vfxDatabase == null)
        {
            EditorUtility.DisplayDialog("오류", "VFX Database가 선택되지 않았습니다.", "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog("경고",
            "VFX Database의 모든 Entry를 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.",
            "삭제", "취소"))
        {
            return;
        }

        var entries = GetDatabaseEntries();
        if (entries != null)
        {
            entries.Clear();
            EditorUtility.SetDirty(vfxDatabase);
            AssetDatabase.SaveAssets();
            Debug.Log("[SkillEditor] VFX Database 초기화 완료");
        }

        EditorUtility.DisplayDialog("완료", "VFX Database가 초기화되었습니다.", "확인");
    }

    private void CreateCSVTemplate()
    {
        string savePath = EditorUtility.SaveFilePanel(
            "CSV 템플릿 저장",
            "Assets/Data/CSV/Skill",
            "MainSkillTable_Template",
            "csv");

        if (string.IsNullOrEmpty(savePath)) return;

        string header = "skill_id,//skill_name,behavior_type,base_damage,cooldown,range,projectile_speed,aoe_radius,duration,//description";

        File.WriteAllText(savePath, header + "\n");
        AssetDatabase.Refresh();

        Debug.Log($"[SkillEditor] CSV 템플릿 생성됨: {savePath}");
        EditorUtility.DisplayDialog("완료", $"CSV 템플릿이 생성되었습니다.\n{savePath}", "확인");
    }

    private void CreateSampleCSV()
    {
        string savePath = EditorUtility.SaveFilePanel(
            "샘플 CSV 저장",
            "Assets/Data/CSV/Skill",
            "MainSkillTable_Sample",
            "csv");

        if (string.IsNullOrEmpty(savePath)) return;

        var lines = new List<string>
        {
            "skill_id,//skill_name,behavior_type,base_damage,cooldown,range,projectile_speed,aoe_radius,duration,//description",
            // SingleProjectile 예제
            "1001,파이어볼,SingleProjectile,100,2,15,20,0,0,기본 화염 투사체",
            "1002,아이스볼트,SingleProjectile,80,1.5,12,25,0,0,빠른 얼음 투사체",
            // ExplosiveProjectile 예제
            "1101,폭발 화살,ExplosiveProjectile,150,3,20,15,3,0,폭발하는 투사체",
            // FallingProjectile 예제
            "1201,운석 낙하,FallingProjectile,300,8,25,0,5,1.5,하늘에서 떨어지는 운석",
            "1202,오비탈 스트라이크,FallingProjectile,400,10,30,0,4,2,궤도 공격",
            // BeamRay 예제
            "2001,레이저 빔,BeamRay,50,5,20,0,0,3,지속 빔 공격",
            "2002,화염 브레스,BeamRay,30,4,15,0,0,2.5,넓은 화염 브레스",
            // TargetAOE 예제
            "3001,폭발,TargetAOE,200,6,15,0,5,0,즉시 폭발",
            "3002,핵폭발,TargetAOE,500,15,20,0,8,0,대규모 폭발",
            // LinearAOE 예제
            "3101,칼바람,LinearAOE,120,3,12,10,2,0,직선 이동 공격",
            // GroundAOE 예제
            "3201,독 웅덩이,GroundAOE,20,8,15,0,4,5,지속 피해 장판",
            "3202,시간 왜곡 필드,GroundAOE,0,12,10,0,6,8,슬로우 필드",
            // MovingAOE 예제
            "3301,토네이도,MovingAOE,80,10,20,3,3,6,이동하는 회오리",
            // Barrier 예제
            "4001,보호막,Barrier,0,15,0,0,5,10,아군 보호막",
            // Buff 예제
            "4101,공격력 증가,Buff,0,20,0,0,0,10,아군 버프",
            // Debuff 예제
            "4201,약화,Debuff,0,12,15,0,5,8,적 약화",
        };

        File.WriteAllLines(savePath, lines);
        AssetDatabase.Refresh();

        Debug.Log($"[SkillEditor] 샘플 CSV 생성됨: {savePath}");
        EditorUtility.DisplayDialog("완료",
            $"샘플 CSV가 생성되었습니다. ({lines.Count - 1}개 스킬)\n{savePath}",
            "확인");

        // 생성된 CSV 자동 로드 물어보기
        if (EditorUtility.DisplayDialog("CSV 로드",
            "생성된 CSV를 지금 로드하시겠습니까?", "예", "아니오"))
        {
            csvPath = "Assets" + savePath.Substring(Application.dataPath.Length);
            LoadCSVData();
        }
    }

    private void FullReset()
    {
        if (!EditorUtility.DisplayDialog("전체 초기화 경고",
            "다음 항목이 모두 초기화됩니다:\n\n" +
            "• VFX Database의 모든 Entry\n" +
            "• 생성된 Container 프리팹들\n" +
            "• 외부 에셋 스캔 캐시\n\n" +
            "CSV 파일은 유지됩니다.\n" +
            "이 작업은 되돌릴 수 없습니다. 계속하시겠습니까?",
            "전체 초기화", "취소"))
        {
            return;
        }

        // 2차 확인
        if (!EditorUtility.DisplayDialog("최종 확인",
            "정말로 전체 초기화를 진행하시겠습니까?",
            "예, 초기화합니다", "취소"))
        {
            return;
        }

        int deletedContainers = 0;

        // 1. VFX Database 초기화
        if (vfxDatabase != null)
        {
            var entries = GetDatabaseEntries();
            if (entries != null)
            {
                entries.Clear();
                EditorUtility.SetDirty(vfxDatabase);
            }
        }

        // 2. Container 프리팹 삭제
        string containerPath = "Assets/02. Scripts/Skills/VFXContainers";
        if (AssetDatabase.IsValidFolder(containerPath))
        {
            string[] containerGuids = AssetDatabase.FindAssets("t:Prefab", new[] { containerPath });
            foreach (string guid in containerGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Container_"))
                {
                    AssetDatabase.DeleteAsset(path);
                    deletedContainers++;
                }
            }
        }

        // 3. 외부 에셋 캐시 초기화
        externalEffects.Clear();
        externalAssetsLoaded = false;
        selectedExternalEffectIndex = -1;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SkillEditor] 전체 초기화 완료 - Container {deletedContainers}개 삭제됨");
        EditorUtility.DisplayDialog("전체 초기화 완료",
            $"초기화가 완료되었습니다.\n\n" +
            $"• VFX Database 초기화됨\n" +
            $"• Container 프리팹 {deletedContainers}개 삭제됨\n" +
            $"• 외부 에셋 캐시 초기화됨",
            "확인");
    }

    #endregion

    #region CSV Entry Class

    [Serializable]
    private class MainSkillCSVEntry
    {
        public int skill_id;
        public string skill_name = "";
        public string behavior_type = "";
        public float base_damage;
        public float range;
        public float cooldown;
    }

    #endregion

    #region External Assets Tab

    private void DrawExternalAssetsTab()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("외부 VFX 에셋 관리", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "SpecialSkillsEffectsPack 등 외부 에셋을 스킬 시스템에 연동합니다.\n" +
            "• NotScriptBased: VFXDatabase에 직접 등록\n" +
            "• ScriptBased: SkillVFXContainer로 래핑 후 등록",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 에셋 경로 설정
        EditorGUILayout.BeginHorizontal();
        externalAssetPath = EditorGUILayout.TextField("에셋 경로", externalAssetPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("VFX 에셋 폴더 선택", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                externalAssetPath = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("에셋 스캔", GUILayout.Height(25)))
        {
            ScanExternalAssets();
        }
        GUI.color = externalAssetsLoaded ? Color.green : Color.yellow;
        EditorGUILayout.LabelField(externalAssetsLoaded ? $"✓ {externalEffects.Count}개 발견" : "스캔 필요", GUILayout.Width(120));
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        if (!externalAssetsLoaded)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.Space(10);

        // 필터
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("검색:", GUILayout.Width(40));
        externalSearchFilter = EditorGUILayout.TextField(externalSearchFilter);
        showScriptBasedOnly = EditorGUILayout.ToggleLeft("ScriptBased만", showScriptBasedOnly, GUILayout.Width(110));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 이펙트 목록
        DrawExternalEffectsList();

        EditorGUILayout.Space(10);

        // 선택된 이펙트 상세
        if (selectedExternalEffectIndex >= 0 && selectedExternalEffectIndex < externalEffects.Count)
        {
            DrawSelectedExternalEffect();
        }

        EditorGUILayout.Space(10);

        // 일괄 작업 버튼
        DrawBatchOperations();

        EditorGUILayout.EndVertical();
    }

    private void DrawExternalEffectsList()
    {
        var filtered = externalEffects.Where(e =>
        {
            if (showScriptBasedOnly && !e.isScriptBased) return false;
            if (!string.IsNullOrEmpty(externalSearchFilter) &&
                !e.name.ToLower().Contains(externalSearchFilter.ToLower()))
                return false;
            return true;
        }).ToList();

        EditorGUILayout.LabelField($"이펙트 목록 ({filtered.Count}/{externalEffects.Count})", EditorStyles.boldLabel);

        externalAssetsScrollPosition = EditorGUILayout.BeginScrollView(externalAssetsScrollPosition, GUILayout.Height(200));

        for (int i = 0; i < filtered.Count; i++)
        {
            var effect = filtered[i];
            int originalIndex = externalEffects.IndexOf(effect);

            EditorGUILayout.BeginHorizontal("box");

            // 선택 상태
            bool isSelected = selectedExternalEffectIndex == originalIndex;
            GUI.backgroundColor = isSelected ? Color.cyan : Color.white;

            // 타입 아이콘
            GUI.color = effect.isScriptBased ? Color.yellow : Color.green;
            EditorGUILayout.LabelField(effect.isScriptBased ? "S" : "N", GUILayout.Width(15));
            GUI.color = Color.white;

            // 이름
            if (GUILayout.Button(effect.name, EditorStyles.label, GUILayout.Width(200)))
            {
                selectedExternalEffectIndex = originalIndex;
            }

            // 추천 타입
            EditorGUILayout.LabelField(effect.suggestedBehaviorType, GUILayout.Width(120));

            // 상태
            GUI.color = effect.isAddedToDatabase ? Color.green : Color.gray;
            EditorGUILayout.LabelField(effect.isAddedToDatabase ? "등록됨" : "미등록", GUILayout.Width(50));
            GUI.color = Color.white;

            // 빠른 등록 버튼
            if (!effect.isAddedToDatabase && GUILayout.Button("+", GUILayout.Width(25)))
            {
                QuickAddToDatabase(effect);
            }

            // 프리뷰 버튼
            if (GUILayout.Button("👁", GUILayout.Width(25)))
            {
                CreatePreview(effect.prefab);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSelectedExternalEffect()
    {
        var effect = externalEffects[selectedExternalEffectIndex];

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("선택된 이펙트", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"이름: {effect.name}");
        EditorGUILayout.LabelField($"경로: {effect.path}");
        EditorGUILayout.LabelField($"타입: {(effect.isScriptBased ? "ScriptBased (래핑 필요)" : "NotScriptBased (직접 사용)")}");
        EditorGUILayout.LabelField($"추천 behavior_type: {effect.suggestedBehaviorType}");

        if (effect.hasScripts)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("포함된 스크립트:", EditorStyles.boldLabel);
            foreach (var script in effect.scripts)
            {
                EditorGUILayout.LabelField($"  • {script}");
            }
        }

        EditorGUILayout.Space(10);

        // 프리팹 필드
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("프리팹", effect.prefab, typeof(GameObject), false);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);

        // 액션 버튼
        EditorGUILayout.BeginHorizontal();

        if (effect.isScriptBased)
        {
            if (GUILayout.Button("Container 래퍼 생성", GUILayout.Height(30)))
            {
                CreateContainerWrapper(effect);
            }
        }
        else
        {
            if (GUILayout.Button("VFXDatabase에 추가", GUILayout.Height(30)))
            {
                AddEffectToDatabase(effect);
            }
        }

        if (GUILayout.Button("프리팹 선택", GUILayout.Height(30)))
        {
            Selection.activeObject = effect.prefab;
            EditorGUIUtility.PingObject(effect.prefab);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawBatchOperations()
    {
        EditorGUILayout.LabelField("일괄 작업", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("NotScriptBased 전체 등록", GUILayout.Height(30)))
        {
            BatchAddNotScriptBased();
        }
        if (GUILayout.Button("ScriptBased Container 일괄 생성", GUILayout.Height(30)))
        {
            BatchCreateContainers();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("VFXDatabase에 스킬 매핑 도우미", GUILayout.Height(25)))
        {
            OpenMappingHelper();
        }

        EditorGUILayout.EndVertical();
    }

    private void ScanExternalAssets()
    {
        externalEffects.Clear();
        externalAssetsLoaded = false;

        if (!AssetDatabase.IsValidFolder(externalAssetPath))
        {
            Debug.LogWarning($"[SkillEditor] 폴더를 찾을 수 없습니다: {externalAssetPath}");
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { externalAssetPath });

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            // Base 프리팹은 제외 (서브 컴포넌트)
            if (path.Contains("(Base)") || path.Contains("_Base")) continue;

            var effectInfo = new ExternalEffectInfo
            {
                name = prefab.name,
                path = path,
                prefab = prefab,
                isScriptBased = path.Contains("ScriptBased"),
                scripts = new List<string>()
            };

            // 스크립트 분석
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in components)
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;

                // 표준 Unity 컴포넌트 제외
                if (typeName == "Transform" || typeName == "ParticleSystem" ||
                    typeName == "Animator" || typeName == "AudioSource") continue;

                if (!effectInfo.scripts.Contains(typeName))
                {
                    effectInfo.scripts.Add(typeName);
                }
            }

            effectInfo.hasScripts = effectInfo.scripts.Count > 0;

            // behavior_type 추천
            effectInfo.suggestedBehaviorType = GuessBehaviorType(prefab.name);

            // 데이터베이스 등록 여부 확인
            effectInfo.isAddedToDatabase = CheckIfAddedToDatabase(prefab);

            externalEffects.Add(effectInfo);
        }

        externalEffects = externalEffects.OrderBy(e => e.name).ToList();
        externalAssetsLoaded = true;
        Debug.Log($"[SkillEditor] 외부 에셋 스캔 완료: {externalEffects.Count}개");
    }

    private string GuessBehaviorType(string effectName)
    {
        string lowerName = effectName.ToLower();

        foreach (var kvp in EffectNameToBehaviorType)
        {
            if (lowerName.Contains(kvp.Key.ToLower()))
            {
                return kvp.Value;
            }
        }

        return "TargetAOE"; // 기본값
    }

    private bool CheckIfAddedToDatabase(GameObject prefab)
    {
        if (vfxDatabase == null) return false;

        var entries = GetDatabaseEntries();
        if (entries == null) return false;

        return entries.Any(e => e.vfxPrefab == prefab || e.containerPrefab == prefab);
    }

    private void QuickAddToDatabase(ExternalEffectInfo effect)
    {
        if (vfxDatabase == null)
        {
            EditorUtility.DisplayDialog("오류", "VFX Database를 먼저 선택하세요.", "확인");
            return;
        }

        // 적합한 스킬 찾기 (behavior_type 매칭)
        var matchingSkills = csvSkills.Where(s =>
            s.behavior_type == effect.suggestedBehaviorType &&
            vfxDatabase.GetVFXPrefab(s.skill_id) == null).ToList();

        if (matchingSkills.Count == 0)
        {
            EditorUtility.DisplayDialog("알림",
                $"'{effect.suggestedBehaviorType}' 타입의 VFX가 없는 스킬이 없습니다.\n" +
                "수동으로 할당하세요.", "확인");
            return;
        }

        // 첫 번째 매칭 스킬에 할당
        var skill = matchingSkills[0];
        AddEffectToSkill(effect, skill);
    }

    private void AddEffectToSkill(ExternalEffectInfo effect, MainSkillCSVEntry skill)
    {
        var entries = GetDatabaseEntries();
        var entry = entries.FirstOrDefault(e => e.skillId == skill.skill_id);

        if (entry == null)
        {
            entry = new SkillVFXDatabase.Entry { skillId = skill.skill_id };
            entries.Add(entry);
        }

        if (effect.isScriptBased)
        {
            // Container가 필요한 경우
            entry.containerPrefab = effect.prefab;
        }
        else
        {
            entry.vfxPrefab = effect.prefab;
        }

        effect.isAddedToDatabase = true;
        EditorUtility.SetDirty(vfxDatabase);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SkillEditor] '{effect.name}' → [{skill.skill_id}] {skill.skill_name} 에 할당됨");
    }

    private void AddEffectToDatabase(ExternalEffectInfo effect)
    {
        if (vfxDatabase == null)
        {
            EditorUtility.DisplayDialog("오류", "VFX Database를 먼저 선택하세요.", "확인");
            return;
        }

        // behavior_type이 일치하는 스킬 목록 표시
        var matchingSkills = csvSkills.Where(s =>
            s.behavior_type == effect.suggestedBehaviorType).ToList();

        if (matchingSkills.Count == 0)
        {
            EditorUtility.DisplayDialog("알림",
                $"'{effect.suggestedBehaviorType}' 타입의 스킬이 없습니다.", "확인");
            return;
        }

        // GenericMenu로 스킬 선택
        var menu = new GenericMenu();
        foreach (var skill in matchingSkills)
        {
            bool hasVFX = vfxDatabase.GetVFXPrefab(skill.skill_id) != null;
            string label = $"[{skill.skill_id}] {skill.skill_name}" + (hasVFX ? " (VFX 있음)" : "");

            // 클로저 캡처를 위한 로컬 변수
            var capturedSkill = skill;
            var capturedEffect = effect;

            menu.AddItem(new GUIContent(label), false, () =>
            {
                AddEffectToSkill(capturedEffect, capturedSkill);
            });
        }
        menu.ShowAsContext();
    }

    private void CreateContainerWrapper(ExternalEffectInfo effect)
    {
        string containerPath = "Assets/02. Scripts/Skills/VFXContainers";

        // 폴더 생성
        if (!AssetDatabase.IsValidFolder(containerPath))
        {
            AssetDatabase.CreateFolder("Assets/02. Scripts/Skills", "VFXContainers");
        }

        // Container 프리팹 생성
        string prefabPath = $"{containerPath}/Container_{effect.name}.prefab";

        // 기존 파일 확인
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            if (!EditorUtility.DisplayDialog("확인",
                $"'{prefabPath}' 파일이 이미 존재합니다.\n덮어쓰시겠습니까?", "예", "아니오"))
            {
                return;
            }
        }

        // 새 GameObject 생성
        var containerGO = new GameObject($"Container_{effect.name}");

        // SkillVFXContainer 컴포넌트 추가
        var container = containerGO.AddComponent<SkillVFXContainer>();

        // 외부 VFX를 자식으로 인스턴스화
        var vfxInstance = (GameObject)PrefabUtility.InstantiatePrefab(effect.prefab);
        vfxInstance.transform.SetParent(containerGO.transform);
        vfxInstance.transform.localPosition = Vector3.zero;
        vfxInstance.transform.localRotation = Quaternion.identity;

        // activeVFX 필드 설정 (Reflection)
        var activeVFXField = typeof(SkillVFXContainer).GetField("activeVFX",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (activeVFXField != null)
        {
            activeVFXField.SetValue(container, vfxInstance);
        }

        // 프리팹으로 저장
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(containerGO, prefabPath);

        // 임시 GameObject 삭제
        DestroyImmediate(containerGO);

        Debug.Log($"[SkillEditor] Container 생성됨: {prefabPath}");

        // 생성된 프리팹 선택
        Selection.activeObject = savedPrefab;
        EditorGUIUtility.PingObject(savedPrefab);

        // 데이터베이스에 추가할지 물어보기
        if (EditorUtility.DisplayDialog("Container 생성 완료",
            $"Container가 생성되었습니다.\n\n" +
            $"VFXDatabase에 등록하시겠습니까?", "예", "나중에"))
        {
            // effect 정보 업데이트 후 데이터베이스에 추가
            var containerEffect = new ExternalEffectInfo
            {
                name = savedPrefab.name,
                prefab = savedPrefab,
                isScriptBased = false, // Container는 직접 사용 가능
                suggestedBehaviorType = effect.suggestedBehaviorType
            };
            AddEffectToDatabase(containerEffect);
        }
    }

    private void BatchAddNotScriptBased()
    {
        if (vfxDatabase == null)
        {
            EditorUtility.DisplayDialog("오류", "VFX Database를 먼저 선택하세요.", "확인");
            return;
        }

        var notScriptBased = externalEffects.Where(e => !e.isScriptBased && !e.isAddedToDatabase).ToList();

        if (notScriptBased.Count == 0)
        {
            EditorUtility.DisplayDialog("알림", "등록할 NotScriptBased 이펙트가 없습니다.", "확인");
            return;
        }

        int added = 0;
        foreach (var effect in notScriptBased)
        {
            var matchingSkills = csvSkills.Where(s =>
                s.behavior_type == effect.suggestedBehaviorType &&
                vfxDatabase.GetVFXPrefab(s.skill_id) == null).ToList();

            if (matchingSkills.Count > 0)
            {
                AddEffectToSkill(effect, matchingSkills[0]);
                added++;
            }
        }

        AssetDatabase.SaveAssets();
        ScanExternalAssets(); // 새로고침

        EditorUtility.DisplayDialog("완료", $"{added}개의 이펙트가 등록되었습니다.", "확인");
    }

    private void BatchCreateContainers()
    {
        var scriptBased = externalEffects.Where(e => e.isScriptBased && !e.isAddedToDatabase).ToList();

        if (scriptBased.Count == 0)
        {
            EditorUtility.DisplayDialog("알림", "Container를 생성할 ScriptBased 이펙트가 없습니다.", "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog("확인",
            $"{scriptBased.Count}개의 Container를 생성하시겠습니까?", "예", "아니오"))
        {
            return;
        }

        int created = 0;
        foreach (var effect in scriptBased)
        {
            try
            {
                CreateContainerWrapperSilent(effect);
                created++;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillEditor] Container 생성 실패: {effect.name} - {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        ScanExternalAssets();

        EditorUtility.DisplayDialog("완료", $"{created}개의 Container가 생성되었습니다.", "확인");
    }

    private void CreateContainerWrapperSilent(ExternalEffectInfo effect)
    {
        string containerPath = "Assets/02. Scripts/Skills/VFXContainers";

        if (!AssetDatabase.IsValidFolder(containerPath))
        {
            AssetDatabase.CreateFolder("Assets/02. Scripts/Skills", "VFXContainers");
        }

        string prefabPath = $"{containerPath}/Container_{effect.name}.prefab";

        var containerGO = new GameObject($"Container_{effect.name}");
        var container = containerGO.AddComponent<SkillVFXContainer>();

        var vfxInstance = (GameObject)PrefabUtility.InstantiatePrefab(effect.prefab);
        vfxInstance.transform.SetParent(containerGO.transform);
        vfxInstance.transform.localPosition = Vector3.zero;
        vfxInstance.transform.localRotation = Quaternion.identity;

        var activeVFXField = typeof(SkillVFXContainer).GetField("activeVFX",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (activeVFXField != null)
        {
            activeVFXField.SetValue(container, vfxInstance);
        }

        PrefabUtility.SaveAsPrefabAsset(containerGO, prefabPath);
        DestroyImmediate(containerGO);
    }

    private void OpenMappingHelper()
    {
        // 매핑 도우미 윈도우 열기
        SkillVFXMappingWindow.ShowWindow(vfxDatabase, csvSkills, externalEffects);
    }

    #endregion

    #region External Effect Info Class

    [Serializable]
    private class ExternalEffectInfo
    {
        public string name;
        public string path;
        public GameObject prefab;
        public bool isScriptBased;
        public bool hasScripts;
        public List<string> scripts;
        public string suggestedBehaviorType;
        public bool isAddedToDatabase;
    }

    #endregion
}

/// <summary>
/// 스킬 - VFX 매핑 도우미 윈도우
/// </summary>
public class SkillVFXMappingWindow : EditorWindow
{
    private SkillVFXDatabase database;
    private List<object> skills;
    private List<object> effects;
    private Vector2 scrollPos;

    public static void ShowWindow(SkillVFXDatabase db, object csvSkills, object externalEffects)
    {
        var window = GetWindow<SkillVFXMappingWindow>("VFX 매핑 도우미");
        window.database = db;
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("스킬 - VFX 매핑 도우미", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "드래그 앤 드롭으로 스킬과 VFX를 매핑할 수 있습니다.\n" +
            "왼쪽: 스킬 목록 | 오른쪽: VFX 목록",
            MessageType.Info);

        EditorGUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.BeginHorizontal();

        // 스킬 목록 (왼쪽)
        EditorGUILayout.BeginVertical("box", GUILayout.Width(200));
        EditorGUILayout.LabelField("스킬 목록", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("(구현 예정)");
        EditorGUILayout.EndVertical();

        // VFX 목록 (오른쪽)
        EditorGUILayout.BeginVertical("box", GUILayout.Width(200));
        EditorGUILayout.LabelField("VFX 목록", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("(구현 예정)");
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }
}
