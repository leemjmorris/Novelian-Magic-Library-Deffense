using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스킬 관리 에디터 윈도우
/// CSV 데이터를 읽고 Prefab을 연결하는 통합 관리 툴
/// </summary>
public class SkillManagerWindow : EditorWindow
{
    // 탭
    private enum Tab
    {
        MainSkills,
        SupportSkills,
        Settings
    }

    private Tab currentTab = Tab.MainSkills;

    // 스크롤 위치
    private Vector2 mainSkillScrollPos;
    private Vector2 supportSkillScrollPos;

    // 데이터 캐시
    private List<MainSkillData> mainSkillDataList = new List<MainSkillData>();
    private List<SupportSkillData> supportSkillDataList = new List<SupportSkillData>();

    // SkillPrefabDatabase 참조
    private SkillPrefabDatabase prefabDatabase;

    // 필터
    private string searchFilter = "";
    private bool showOnlyUnlinked = false;

    // CSV 경로
    private const string CSV_PATH = "Assets/Data/CSV";

    // GUIStyle 캐싱 (Unity 6 호환성)
    private GUIStyle _okStyle;
    private GUIStyle _errorStyle;

    private GUIStyle OkStyle
    {
        get
        {
            if (_okStyle == null)
            {
                _okStyle = new GUIStyle(EditorStyles.label);
                _okStyle.normal.textColor = Color.green;
            }
            return _okStyle;
        }
    }

    private GUIStyle ErrorStyle
    {
        get
        {
            if (_errorStyle == null)
            {
                _errorStyle = new GUIStyle(EditorStyles.label);
                _errorStyle.normal.textColor = Color.red;
            }
            return _errorStyle;
        }
    }

    [MenuItem("Tools/Skills/Skill Manager", false, 100)]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillManagerWindow>("Skill Manager");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnEnable()
    {
        LoadData();
        LoadPrefabDatabase();
    }

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.Space(5);

        // 탭 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == Tab.MainSkills, "Main Skills", EditorStyles.toolbarButton))
            currentTab = Tab.MainSkills;
        if (GUILayout.Toggle(currentTab == Tab.SupportSkills, "Support Skills", EditorStyles.toolbarButton))
            currentTab = Tab.SupportSkills;
        if (GUILayout.Toggle(currentTab == Tab.Settings, "Settings", EditorStyles.toolbarButton))
            currentTab = Tab.Settings;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 탭 내용
        switch (currentTab)
        {
            case Tab.MainSkills:
                DrawMainSkillsTab();
                break;
            case Tab.SupportSkills:
                DrawSupportSkillsTab();
                break;
            case Tab.Settings:
                DrawSettingsTab();
                break;
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("CSV Reload", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            LoadData();
        }

        if (GUILayout.Button("Save Database", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            SavePrefabDatabase();
        }

        GUILayout.FlexibleSpace();

        // 검색
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));

        // 필터
        showOnlyUnlinked = GUILayout.Toggle(showOnlyUnlinked, "Unlinked Only", EditorStyles.toolbarButton, GUILayout.Width(100));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawMainSkillsTab()
    {
        if (prefabDatabase == null)
        {
            EditorGUILayout.HelpBox("SkillPrefabDatabase not found. Create one in Settings tab.", MessageType.Warning);
            return;
        }

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("ID", GUILayout.Width(50));
        EditorGUILayout.LabelField("Name", GUILayout.Width(120));
        EditorGUILayout.LabelField("Type", GUILayout.Width(80));
        EditorGUILayout.LabelField("Status", GUILayout.Width(60));
        EditorGUILayout.LabelField("Projectile", GUILayout.MinWidth(100));
        EditorGUILayout.LabelField("Hit Effect", GUILayout.MinWidth(100));
        EditorGUILayout.EndHorizontal();

        mainSkillScrollPos = EditorGUILayout.BeginScrollView(mainSkillScrollPos);

        foreach (var skill in mainSkillDataList)
        {
            // 필터 적용
            if (!string.IsNullOrEmpty(searchFilter))
            {
                if (!skill.skill_id.ToString().Contains(searchFilter) &&
                    (skill.skill_name == null || !skill.skill_name.ToLower().Contains(searchFilter.ToLower())))
                    continue;
            }

            var entry = GetOrCreateMainSkillEntry(skill.skill_id);
            bool hasAnyPrefab = entry.HasAnyPrefab();

            if (showOnlyUnlinked && hasAnyPrefab)
                continue;

            // 스킬 이름 동기화
            entry.skillName = skill.skill_name ?? $"Skill_{skill.skill_id}";

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // ID
            EditorGUILayout.LabelField(skill.skill_id.ToString(), GUILayout.Width(50));

            // Name
            EditorGUILayout.LabelField(entry.skillName, GUILayout.Width(120));

            // Type
            EditorGUILayout.LabelField(skill.GetSkillType().ToString(), GUILayout.Width(80));

            // Status
            EditorGUILayout.LabelField(hasAnyPrefab ? "OK" : "X", hasAnyPrefab ? OkStyle : ErrorStyle, GUILayout.Width(60));

            // Projectile Prefab
            entry.projectilePrefab = (GameObject)EditorGUILayout.ObjectField(
                entry.projectilePrefab, typeof(GameObject), false, GUILayout.MinWidth(100));

            // Hit Effect Prefab
            entry.hitEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
                entry.hitEffectPrefab, typeof(GameObject), false, GUILayout.MinWidth(100));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // 통계
        EditorGUILayout.Space(5);
        int linked = 0;
        foreach (var skill in mainSkillDataList)
        {
            var entry = FindMainSkillEntry(skill.skill_id);
            if (entry != null && entry.HasAnyPrefab()) linked++;
        }
        EditorGUILayout.LabelField($"Total: {mainSkillDataList.Count} | Linked: {linked} | Unlinked: {mainSkillDataList.Count - linked}");
    }

    private void DrawSupportSkillsTab()
    {
        if (prefabDatabase == null)
        {
            EditorGUILayout.HelpBox("SkillPrefabDatabase not found. Create one in Settings tab.", MessageType.Warning);
            return;
        }

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("ID", GUILayout.Width(50));
        EditorGUILayout.LabelField("Name", GUILayout.Width(120));
        EditorGUILayout.LabelField("Category", GUILayout.Width(80));
        EditorGUILayout.LabelField("Status", GUILayout.Width(60));
        EditorGUILayout.LabelField("Effect", GUILayout.MinWidth(100));
        EditorGUILayout.LabelField("CC Effect", GUILayout.MinWidth(100));
        EditorGUILayout.EndHorizontal();

        supportSkillScrollPos = EditorGUILayout.BeginScrollView(supportSkillScrollPos);

        foreach (var skill in supportSkillDataList)
        {
            // 필터 적용
            if (!string.IsNullOrEmpty(searchFilter))
            {
                if (!skill.support_id.ToString().Contains(searchFilter) &&
                    (skill.support_name == null || !skill.support_name.ToLower().Contains(searchFilter.ToLower())))
                    continue;
            }

            var entry = GetOrCreateSupportSkillEntry(skill.support_id);
            bool hasAnyPrefab = entry.HasAnyPrefab();

            if (showOnlyUnlinked && hasAnyPrefab)
                continue;

            // 스킬 이름 동기화
            entry.supportName = skill.support_name ?? $"Support_{skill.support_id}";

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // ID
            EditorGUILayout.LabelField(skill.support_id.ToString(), GUILayout.Width(50));

            // Name
            EditorGUILayout.LabelField(entry.supportName, GUILayout.Width(120));

            // Category
            EditorGUILayout.LabelField(skill.GetSupportCategory().ToString(), GUILayout.Width(80));

            // Status
            EditorGUILayout.LabelField(hasAnyPrefab ? "OK" : "X", hasAnyPrefab ? OkStyle : ErrorStyle, GUILayout.Width(60));

            // Effect Prefab
            entry.effectPrefab = (GameObject)EditorGUILayout.ObjectField(
                entry.effectPrefab, typeof(GameObject), false, GUILayout.MinWidth(100));

            // CC Effect Prefab
            entry.ccEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
                entry.ccEffectPrefab, typeof(GameObject), false, GUILayout.MinWidth(100));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // 통계
        EditorGUILayout.Space(5);
        int linked = 0;
        foreach (var skill in supportSkillDataList)
        {
            var entry = FindSupportSkillEntry(skill.support_id);
            if (entry != null && entry.HasAnyPrefab()) linked++;
        }
        EditorGUILayout.LabelField($"Total: {supportSkillDataList.Count} | Linked: {linked} | Unlinked: {supportSkillDataList.Count - linked}");
    }

    private void DrawSettingsTab()
    {
        EditorGUILayout.LabelField("Skill Prefab Database", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        prefabDatabase = (SkillPrefabDatabase)EditorGUILayout.ObjectField(
            "Database Asset", prefabDatabase, typeof(SkillPrefabDatabase), false);

        if (GUILayout.Button("Create New", GUILayout.Width(100)))
        {
            CreatePrefabDatabase();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("CSV Files", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Path: {CSV_PATH}");

        EditorGUILayout.Space(5);

        // CSV 파일 상태
        DrawCsvFileStatus("MainSkillTable.csv");
        DrawCsvFileStatus("SupportSkillTable.csv");
        DrawCsvFileStatus("SkillLevelTable.csv");
        DrawCsvFileStatus("SkillEnumTable.csv");

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Open CSV Folder"))
        {
            EditorUtility.RevealInFinder(CSV_PATH);
        }

        EditorGUILayout.Space(20);

        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Sync All Skill Names from CSV"))
        {
            SyncSkillNamesFromCSV();
        }

        if (GUILayout.Button("Remove Entries with Missing Prefabs"))
        {
            RemoveEntriesWithMissingPrefabs();
        }
    }

    private void DrawCsvFileStatus(string fileName)
    {
        string filePath = Path.Combine(CSV_PATH, fileName);
        bool exists = File.Exists(filePath);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(exists ? "[OK]" : "[X]", exists ? OkStyle : ErrorStyle, GUILayout.Width(40));
        EditorGUILayout.LabelField(fileName);

        if (exists)
        {
            var fileInfo = new FileInfo(filePath);
            EditorGUILayout.LabelField($"({fileInfo.Length} bytes)", GUILayout.Width(100));
        }

        EditorGUILayout.EndHorizontal();
    }

    #region Data Loading

    private void LoadData()
    {
        mainSkillDataList.Clear();
        supportSkillDataList.Clear();

        // MainSkillTable.csv 로드
        string mainSkillPath = Path.Combine(CSV_PATH, "MainSkillTable.csv");
        if (File.Exists(mainSkillPath))
        {
            string csvText = File.ReadAllText(mainSkillPath);
            csvText = ProcessSkillCsvFormat(csvText);
            mainSkillDataList = CsvUtility.LoadCsvFromText<MainSkillData>(csvText);
            Debug.Log($"[SkillManager] Loaded {mainSkillDataList.Count} main skills");
        }

        // SupportSkillTable.csv 로드
        string supportSkillPath = Path.Combine(CSV_PATH, "SupportSkillTable.csv");
        if (File.Exists(supportSkillPath))
        {
            string csvText = File.ReadAllText(supportSkillPath);
            csvText = ProcessSkillCsvFormat(csvText);
            supportSkillDataList = CsvUtility.LoadCsvFromText<SupportSkillData>(csvText);
            Debug.Log($"[SkillManager] Loaded {supportSkillDataList.Count} support skills");
        }

        Repaint();
    }

    private string ProcessSkillCsvFormat(string csvText)
    {
        var lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

        if (lines.Length < 4)
            return csvText;

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

    #endregion

    #region Prefab Database Management

    private void LoadPrefabDatabase()
    {
        // 지정된 경로에서 찾기
        string targetPath = "Assets/ScriptableObjects/Skills/SkillPrefabDatabase.asset";
        prefabDatabase = AssetDatabase.LoadAssetAtPath<SkillPrefabDatabase>(targetPath);

        // 없으면 프로젝트 전체에서 찾기
        if (prefabDatabase == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:SkillPrefabDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                prefabDatabase = AssetDatabase.LoadAssetAtPath<SkillPrefabDatabase>(path);
            }
        }
    }

    private void CreatePrefabDatabase()
    {
        // ScriptableObjects/Skills 폴더 확인/생성
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Skills"))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Skills");
        }

        // 새 데이터베이스 생성
        prefabDatabase = ScriptableObject.CreateInstance<SkillPrefabDatabase>();
        string assetPath = "Assets/ScriptableObjects/Skills/SkillPrefabDatabase.asset";

        AssetDatabase.CreateAsset(prefabDatabase, assetPath);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefabDatabase;

        Debug.Log($"[SkillManager] Created SkillPrefabDatabase at {assetPath}");
    }

    private void SavePrefabDatabase()
    {
        if (prefabDatabase != null)
        {
            EditorUtility.SetDirty(prefabDatabase);
            AssetDatabase.SaveAssets();
            Debug.Log("[SkillManager] Database saved");
        }
    }

    private MainSkillPrefabEntry GetOrCreateMainSkillEntry(int skillId)
    {
        var entry = FindMainSkillEntry(skillId);
        if (entry == null)
        {
            entry = new MainSkillPrefabEntry { skillId = skillId };
            prefabDatabase.mainSkillPrefabs.Add(entry);
        }
        return entry;
    }

    private MainSkillPrefabEntry FindMainSkillEntry(int skillId)
    {
        foreach (var entry in prefabDatabase.mainSkillPrefabs)
        {
            if (entry.skillId == skillId)
                return entry;
        }
        return null;
    }

    private SupportSkillPrefabEntry GetOrCreateSupportSkillEntry(int supportId)
    {
        var entry = FindSupportSkillEntry(supportId);
        if (entry == null)
        {
            entry = new SupportSkillPrefabEntry { supportId = supportId };
            prefabDatabase.supportSkillPrefabs.Add(entry);
        }
        return entry;
    }

    private SupportSkillPrefabEntry FindSupportSkillEntry(int supportId)
    {
        foreach (var entry in prefabDatabase.supportSkillPrefabs)
        {
            if (entry.supportId == supportId)
                return entry;
        }
        return null;
    }

    #endregion

    #region Utility Methods

    private void SyncSkillNamesFromCSV()
    {
        if (prefabDatabase == null) return;

        foreach (var skill in mainSkillDataList)
        {
            var entry = FindMainSkillEntry(skill.skill_id);
            if (entry != null)
            {
                entry.skillName = skill.skill_name ?? $"Skill_{skill.skill_id}";
            }
        }

        foreach (var skill in supportSkillDataList)
        {
            var entry = FindSupportSkillEntry(skill.support_id);
            if (entry != null)
            {
                entry.supportName = skill.support_name ?? $"Support_{skill.support_id}";
            }
        }

        SavePrefabDatabase();
        Debug.Log("[SkillManager] Skill names synced from CSV");
    }

    private void RemoveEntriesWithMissingPrefabs()
    {
        if (prefabDatabase == null) return;

        int removed = 0;

        // 메인 스킬에서 Prefab이 없는 엔트리 제거
        for (int i = prefabDatabase.mainSkillPrefabs.Count - 1; i >= 0; i--)
        {
            if (!prefabDatabase.mainSkillPrefabs[i].HasAnyPrefab())
            {
                prefabDatabase.mainSkillPrefabs.RemoveAt(i);
                removed++;
            }
        }

        // 서포트 스킬에서 Prefab이 없는 엔트리 제거
        for (int i = prefabDatabase.supportSkillPrefabs.Count - 1; i >= 0; i--)
        {
            if (!prefabDatabase.supportSkillPrefabs[i].HasAnyPrefab())
            {
                prefabDatabase.supportSkillPrefabs.RemoveAt(i);
                removed++;
            }
        }

        SavePrefabDatabase();
        Debug.Log($"[SkillManager] Removed {removed} entries without prefabs");
    }

    #endregion
}
