// SkillManagingTool.cs
// 통합 스킬 관리 도구
// 스킬에 대한 설정, 수정, 변경, 밸런싱을 한 곳에서 관리
// Play Mode에서도 실시간 수정 및 CSV 저장 가능

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Novelian.Combat;

public class SkillManagingTool : EditorWindow
{
    // CSV 경로
    private const string CSV_PATH = "Assets/Data/CSV/Skill/MainSkillTable_Unified.csv";
    private const string BACKUP_FOLDER = "Assets/Data/CSV/Skill/Backups";

    // 스킬 데이터
    private List<SkillDataEntry> allSkills = new List<SkillDataEntry>();
    private List<SkillDataEntry> filteredSkills = new List<SkillDataEntry>();
    private SkillDataEntry selectedSkill;
    private int selectedIndex = -1;

    // UI 상태
    private Vector2 listScrollPos;
    private Vector2 detailScrollPos;
    private string searchText = "";
    private int filterBehaviorType = -1; // -1 = All
    private bool showModifiedOnly = false;
    private bool isDirty = false;

    // 탭
    private int currentTab = 0;
    private readonly string[] tabNames = { "스킬 목록", "밸런싱 도구", "CSV 관리" };

    // 스타일
    private GUIStyle headerStyle;
    private GUIStyle modifiedStyle;
    private bool stylesInitialized;

    [MenuItem("Tools/Skills/Skill Managing Tool %#s")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillManagingTool>("Skill Managing Tool");
        window.minSize = new Vector2(1200, 700);
    }

    private void OnEnable()
    {
        LoadCSV();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        if (isDirty)
        {
            if (EditorUtility.DisplayDialog("저장되지 않은 변경사항",
                "저장되지 않은 변경사항이 있습니다. 저장하시겠습니까?",
                "저장", "취소"))
            {
                SaveCSV();
            }
        }
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Play Mode 진입/종료 시 데이터 유지
        if (state == PlayModeStateChange.EnteredPlayMode ||
            state == PlayModeStateChange.EnteredEditMode)
        {
            Repaint();
        }
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };

        modifiedStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(1f, 0.6f, 0f) }
        };

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();

        // 상단 툴바
        DrawToolbar();

        // 탭
        EditorGUILayout.Space(5);
        currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(25));

        EditorGUILayout.Space(5);

        switch (currentTab)
        {
            case 0:
                DrawSkillListTab();
                break;
            case 1:
                DrawBalancingTab();
                break;
            case 2:
                DrawCSVManagementTab();
                break;
        }

        // 하단 상태바
        DrawStatusBar();
    }

    #region Toolbar

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            LoadCSV();
        }

        if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            SaveCSV();
        }

        GUILayout.Space(20);

        // Play Mode 표시
        if (Application.isPlaying)
        {
            GUI.color = Color.green;
            GUILayout.Label("● Play Mode", EditorStyles.toolbarButton, GUILayout.Width(100));
            GUI.color = Color.white;

            if (GUILayout.Button("실시간 적용", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ApplyChangesToRuntime();
            }
        }

        GUILayout.FlexibleSpace();

        // 변경사항 표시
        if (isDirty)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("● 저장되지 않은 변경사항", EditorStyles.toolbarButton);
            GUI.color = Color.white;
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Skill List Tab

    private void DrawSkillListTab()
    {
        EditorGUILayout.BeginHorizontal();

        // 좌측: 스킬 목록
        EditorGUILayout.BeginVertical(GUILayout.Width(400));
        DrawSkillListPanel();
        EditorGUILayout.EndVertical();

        // 우측: 스킬 상세
        EditorGUILayout.BeginVertical();
        DrawSkillDetailPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillListPanel()
    {
        EditorGUILayout.LabelField("스킬 목록", headerStyle);

        // 검색 및 필터
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        searchText = EditorGUILayout.TextField("검색", searchText);
        if (EditorGUI.EndChangeCheck())
        {
            ApplyFilter();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        filterBehaviorType = EditorGUILayout.Popup("행동 타입", filterBehaviorType + 1, new string[]
        {
            "전체", "Projectile", "Spawner_Projectile", "Spawner_AOE", "Visual_AOE", "Field", "Shield", "Beam"
        }) - 1;
        if (EditorGUI.EndChangeCheck())
        {
            ApplyFilter();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        showModifiedOnly = EditorGUILayout.Toggle("수정된 항목만", showModifiedOnly);
        if (EditorGUI.EndChangeCheck())
        {
            ApplyFilter();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 스킬 목록
        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);

        for (int i = 0; i < filteredSkills.Count; i++)
        {
            var skill = filteredSkills[i];
            bool isSelected = (selectedSkill == skill);

            EditorGUILayout.BeginHorizontal(isSelected ? "SelectionRect" : "box");

            // 수정 표시
            if (skill.isModified)
            {
                GUI.color = new Color(1f, 0.6f, 0f);
                GUILayout.Label("●", GUILayout.Width(15));
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Space(18);
            }

            // 스킬 ID
            GUILayout.Label(skill.skill_id.ToString(), GUILayout.Width(50));

            // 스킬 이름
            if (GUILayout.Button(skill.skill_name, EditorStyles.label))
            {
                selectedSkill = skill;
                selectedIndex = i;
            }

            // 행동 타입
            GUILayout.Label(skill.behavior_type, GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.LabelField($"총 {filteredSkills.Count}개 / {allSkills.Count}개");
    }

    private void DrawSkillDetailPanel()
    {
        if (selectedSkill == null)
        {
            EditorGUILayout.HelpBox("좌측에서 스킬을 선택하세요.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"스킬 상세: {selectedSkill.skill_name}", headerStyle);

        detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);

        // 기본 정보
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("스킬 ID", selectedSkill.skill_id.ToString());
        DrawEditableField("스킬 이름", ref selectedSkill.skill_name);
        DrawEditableIntField("스킬 타입 ID", ref selectedSkill.skill_type_ID);
        DrawEditableIntField("속성 타입 ID", ref selectedSkill.element_type_ID);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // 전투 수치
        EditorGUILayout.LabelField("전투 수치", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        DrawEditableFloatField("기본 데미지", ref selectedSkill.base_damage);
        DrawEditableFloatField("쿨다운", ref selectedSkill.cooldown);
        DrawEditableFloatField("시전 시간", ref selectedSkill.cast_time);
        DrawEditableFloatField("사거리", ref selectedSkill.range);
        DrawEditableFloatField("투사체 속도", ref selectedSkill.projectile_speed);
        DrawEditableIntField("투사체 수", ref selectedSkill.projectile_count);
        DrawEditableFloatField("지속시간", ref selectedSkill.skill_lifetime);
        DrawEditableIntField("관통 수", ref selectedSkill.pierce_count);
        DrawEditableBoolField("유도 여부", ref selectedSkill.is_homing);
        DrawEditableFloatField("범위 반경", ref selectedSkill.aoe_radius);
        DrawEditableFloatField("범위 각도", ref selectedSkill.aoe_angle);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // CC/DOT 효과
        EditorGUILayout.LabelField("CC/DOT 효과", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        DrawEditableIntField("CC 타입", ref selectedSkill.cc_type);
        DrawEditableFloatField("CC 지속시간", ref selectedSkill.cc_duration);
        DrawEditableBoolField("스턴 사용", ref selectedSkill.stun_use);
        DrawEditableFloatField("슬로우 량", ref selectedSkill.cc_slow_amount);
        DrawEditableFloatField("DOT 지속시간", ref selectedSkill.dot_duration);
        DrawEditableFloatField("DOT 틱 간격", ref selectedSkill.dot_tick_interval);
        DrawEditableFloatField("DOT 틱 데미지", ref selectedSkill.dot_damage_per_tick);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // 프리팹 정보
        EditorGUILayout.LabelField("프리팹 정보 (원작자 스크립트)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        DrawEditableField("프리팹 이름", ref selectedSkill.prefab_name);
        EditorGUILayout.LabelField("프리팹 경로", selectedSkill.prefab_path);
        DrawEditableBoolField("스크립트 기반", ref selectedSkill.is_script_based);

        // 행동 타입 드롭다운
        string[] behaviorTypes = { "Unknown", "Projectile", "Spawner_Projectile", "Spawner_AOE", "Visual_AOE", "Field", "Shield", "Beam" };
        int currentBehavior = Array.IndexOf(behaviorTypes, selectedSkill.behavior_type);
        if (currentBehavior < 0) currentBehavior = 0;

        EditorGUI.BeginChangeCheck();
        currentBehavior = EditorGUILayout.Popup("행동 타입", currentBehavior, behaviorTypes);
        if (EditorGUI.EndChangeCheck())
        {
            selectedSkill.behavior_type = behaviorTypes[currentBehavior];
            MarkModified(selectedSkill);
        }

        // 데미지 타입 드롭다운
        string[] damageTypes = { "None", "OnHit", "Instant", "OnSpawn", "Periodic" };
        int currentDamage = Array.IndexOf(damageTypes, selectedSkill.damage_type);
        if (currentDamage < 0) currentDamage = 0;

        EditorGUI.BeginChangeCheck();
        currentDamage = EditorGUILayout.Popup("데미지 타입", currentDamage, damageTypes);
        if (EditorGUI.EndChangeCheck())
        {
            selectedSkill.damage_type = damageTypes[currentDamage];
            MarkModified(selectedSkill);
        }

        DrawEditableFloatField("프리팹 이동속도", ref selectedSkill.prefab_move_speed);
        DrawEditableFloatField("프리팹 파괴시간", ref selectedSkill.prefab_destroy_time);
        DrawEditableIntField("프리팹 생성수", ref selectedSkill.prefab_make_count);
        DrawEditableFloatField("프리팹 생성딜레이", ref selectedSkill.prefab_make_delay);
        DrawEditableFloatField("프리팹 시작딜레이", ref selectedSkill.prefab_start_delay);
        DrawEditableFloatField("프리팹 지속시간", ref selectedSkill.prefab_duration);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // 설명
        EditorGUILayout.LabelField("설명", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUI.BeginChangeCheck();
        selectedSkill.description = EditorGUILayout.TextArea(selectedSkill.description, GUILayout.Height(60));
        if (EditorGUI.EndChangeCheck())
        {
            MarkModified(selectedSkill);
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.EndScrollView();

        // 하단 버튼
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("변경사항 되돌리기"))
        {
            RevertSkill(selectedSkill);
        }

        if (Application.isPlaying && GUILayout.Button("실시간 적용"))
        {
            ApplySkillToRuntime(selectedSkill);
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Balancing Tab

    private void DrawBalancingTab()
    {
        EditorGUILayout.LabelField("밸런싱 도구", headerStyle);
        EditorGUILayout.HelpBox("선택한 조건에 맞는 스킬들의 수치를 일괄 변경합니다.", MessageType.Info);

        EditorGUILayout.Space(10);

        // 필터 조건
        EditorGUILayout.LabelField("대상 선택", EditorStyles.boldLabel);
        filterBehaviorType = EditorGUILayout.Popup("행동 타입", filterBehaviorType + 1, new string[]
        {
            "전체", "Projectile", "Spawner_Projectile", "Spawner_AOE", "Visual_AOE", "Field", "Shield", "Beam"
        }) - 1;

        EditorGUILayout.Space(10);

        // 일괄 수정 옵션
        EditorGUILayout.LabelField("일괄 수정", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("데미지 배율:", GUILayout.Width(100));
        float damageMultiplier = EditorGUILayout.FloatField(1.0f, GUILayout.Width(80));
        if (GUILayout.Button("적용", GUILayout.Width(50)))
        {
            ApplyBatchChange((skill) => skill.base_damage *= damageMultiplier);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("쿨다운 배율:", GUILayout.Width(100));
        float cooldownMultiplier = EditorGUILayout.FloatField(1.0f, GUILayout.Width(80));
        if (GUILayout.Button("적용", GUILayout.Width(50)))
        {
            ApplyBatchChange((skill) => skill.cooldown *= cooldownMultiplier);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("범위 배율:", GUILayout.Width(100));
        float radiusMultiplier = EditorGUILayout.FloatField(1.0f, GUILayout.Width(80));
        if (GUILayout.Button("적용", GUILayout.Width(50)))
        {
            ApplyBatchChange((skill) => skill.aoe_radius *= radiusMultiplier);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ApplyBatchChange(Action<SkillDataEntry> modifier)
    {
        int count = 0;
        foreach (var skill in filteredSkills)
        {
            modifier(skill);
            MarkModified(skill);
            count++;
        }
        Debug.Log($"[SkillManagingTool] {count}개 스킬에 일괄 변경 적용");
    }

    #endregion

    #region CSV Management Tab

    private void DrawCSVManagementTab()
    {
        EditorGUILayout.LabelField("CSV 관리", headerStyle);

        EditorGUILayout.Space(10);

        // CSV 경로
        EditorGUILayout.LabelField("CSV 경로:", EditorStyles.boldLabel);
        EditorGUILayout.TextField(CSV_PATH);

        EditorGUILayout.Space(10);

        // 버튼들
        if (GUILayout.Button("CSV 새로고침", GUILayout.Height(30)))
        {
            LoadCSV();
        }

        if (GUILayout.Button("CSV 저장", GUILayout.Height(30)))
        {
            SaveCSV();
        }

        if (GUILayout.Button("백업 생성", GUILayout.Height(30)))
        {
            CreateBackup();
        }

        EditorGUILayout.Space(20);

        // 통계
        EditorGUILayout.LabelField("통계", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"총 스킬 수: {allSkills.Count}");
        EditorGUILayout.LabelField($"수정된 스킬 수: {allSkills.FindAll(s => s.isModified).Count}");

        // 행동 타입별 카운트
        var typeCounts = new Dictionary<string, int>();
        foreach (var skill in allSkills)
        {
            string type = string.IsNullOrEmpty(skill.behavior_type) ? "Unknown" : skill.behavior_type;
            if (!typeCounts.ContainsKey(type)) typeCounts[type] = 0;
            typeCounts[type]++;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("행동 타입별 분포:");
        foreach (var kvp in typeCounts)
        {
            EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}");
        }
    }

    #endregion

    #region Status Bar

    private void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label($"스킬 {allSkills.Count}개 로드됨");

        GUILayout.FlexibleSpace();

        if (selectedSkill != null)
        {
            GUILayout.Label($"선택: {selectedSkill.skill_id} - {selectedSkill.skill_name}");
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Helper Methods

    private void DrawEditableField(string label, ref string value)
    {
        EditorGUI.BeginChangeCheck();
        value = EditorGUILayout.TextField(label, value);
        if (EditorGUI.EndChangeCheck() && selectedSkill != null)
        {
            MarkModified(selectedSkill);
        }
    }

    private void DrawEditableIntField(string label, ref int value)
    {
        EditorGUI.BeginChangeCheck();
        value = EditorGUILayout.IntField(label, value);
        if (EditorGUI.EndChangeCheck() && selectedSkill != null)
        {
            MarkModified(selectedSkill);
        }
    }

    private void DrawEditableFloatField(string label, ref float value)
    {
        EditorGUI.BeginChangeCheck();
        value = EditorGUILayout.FloatField(label, value);
        if (EditorGUI.EndChangeCheck() && selectedSkill != null)
        {
            MarkModified(selectedSkill);
        }
    }

    private void DrawEditableBoolField(string label, ref bool value)
    {
        EditorGUI.BeginChangeCheck();
        value = EditorGUILayout.Toggle(label, value);
        if (EditorGUI.EndChangeCheck() && selectedSkill != null)
        {
            MarkModified(selectedSkill);
        }
    }

    private void MarkModified(SkillDataEntry skill)
    {
        skill.isModified = true;
        isDirty = true;
    }

    private void ApplyFilter()
    {
        filteredSkills.Clear();

        foreach (var skill in allSkills)
        {
            // 검색 필터
            if (!string.IsNullOrEmpty(searchText))
            {
                if (!skill.skill_name.Contains(searchText, StringComparison.OrdinalIgnoreCase) &&
                    !skill.skill_id.ToString().Contains(searchText) &&
                    !skill.prefab_name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            // 행동 타입 필터
            if (filterBehaviorType >= 0)
            {
                string[] types = { "Projectile", "Spawner_Projectile", "Spawner_AOE", "Visual_AOE", "Field", "Shield", "Beam" };
                if (filterBehaviorType < types.Length && skill.behavior_type != types[filterBehaviorType])
                {
                    continue;
                }
            }

            // 수정됨 필터
            if (showModifiedOnly && !skill.isModified)
            {
                continue;
            }

            filteredSkills.Add(skill);
        }
    }

    private void RevertSkill(SkillDataEntry skill)
    {
        // CSV에서 다시 로드
        LoadCSV();
        selectedSkill = allSkills.Find(s => s.skill_id == skill.skill_id);
        ApplyFilter();
    }

    private void ApplyChangesToRuntime()
    {
        if (!Application.isPlaying) return;

        // CSVLoader의 캐시를 갱신
        // 실제 구현에서는 CSVLoader에 갱신 메서드가 필요
        Debug.Log("[SkillManagingTool] 실시간 적용 - CSVLoader 캐시 갱신 필요");
    }

    private void ApplySkillToRuntime(SkillDataEntry skill)
    {
        if (!Application.isPlaying) return;

        Debug.Log($"[SkillManagingTool] 스킬 {skill.skill_id} 실시간 적용");
    }

    #endregion

    #region CSV Loading/Saving

    private void LoadCSV()
    {
        allSkills.Clear();
        string fullPath = Path.Combine(Application.dataPath, CSV_PATH.Substring(7));

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[SkillManagingTool] CSV 파일을 찾을 수 없습니다: {fullPath}");

            // 기존 MainSkillTable.csv 시도
            fullPath = Path.Combine(Application.dataPath, "Data/CSV/Skill/MainSkillTable.csv");
            if (!File.Exists(fullPath))
            {
                return;
            }
        }

        try
        {
            string[] lines = File.ReadAllLines(fullPath, Encoding.UTF8);

            // Skip header rows (한글, 영문, 타입)
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var skill = ParseSkillLine(line);
                if (skill != null && skill.skill_id > 0)
                {
                    allSkills.Add(skill);
                }
            }

            isDirty = false;
            ApplyFilter();
            Debug.Log($"[SkillManagingTool] {allSkills.Count}개 스킬 로드 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SkillManagingTool] CSV 로드 오류: {ex.Message}");
        }
    }

    private SkillDataEntry ParseSkillLine(string line)
    {
        var parts = ParseCSVLine(line);
        if (parts.Count < 32) return null;

        var skill = new SkillDataEntry();
        int.TryParse(parts[0], out skill.skill_id);
        skill.skill_name = parts[1];
        int.TryParse(parts[2], out skill.skill_type_ID);
        int.TryParse(parts[3], out skill.element_type_ID);
        float.TryParse(parts[4], out skill.base_damage);
        int.TryParse(parts[5], out skill.buff_type);
        float.TryParse(parts[6], out skill.base_buff_value);
        int.TryParse(parts[7], out skill.debuff_type);
        float.TryParse(parts[8], out skill.base_debuff_value);
        int.TryParse(parts[9], out skill.cc_type);
        float.TryParse(parts[10], out skill.cc_duration);
        bool.TryParse(parts[11], out skill.stun_use);
        float.TryParse(parts[12], out skill.cc_slow_amount);
        float.TryParse(parts[13], out skill.dot_duration);
        float.TryParse(parts[14], out skill.dot_tick_interval);
        float.TryParse(parts[15], out skill.dot_damage_per_tick);
        float.TryParse(parts[16], out skill.mark_duration);
        float.TryParse(parts[17], out skill.mark_damage_mult);
        float.TryParse(parts[18], out skill.cooldown);
        float.TryParse(parts[19], out skill.cast_time);
        float.TryParse(parts[20], out skill.range);
        float.TryParse(parts[21], out skill.projectile_speed);
        int.TryParse(parts[22], out skill.projectile_count);
        float.TryParse(parts[23], out skill.skill_lifetime);
        int.TryParse(parts[24], out skill.pierce_count);
        bool.TryParse(parts[25], out skill.is_homing);
        float.TryParse(parts[26], out skill.aoe_radius);
        float.TryParse(parts[27], out skill.aoe_angle);
        float.TryParse(parts[28], out skill.channel_duration);
        float.TryParse(parts[29], out skill.channel_tick_interval);
        bool.TryParse(parts[30], out skill.interruptible);
        skill.description = parts[31];

        // 통합 CSV 컬럼 (옵션)
        if (parts.Count > 32) skill.prefab_name = parts[32];
        if (parts.Count > 33) skill.prefab_path = parts[33];
        if (parts.Count > 34) bool.TryParse(parts[34], out skill.is_script_based);
        if (parts.Count > 35) skill.behavior_type = parts[35];
        if (parts.Count > 36) skill.damage_type = parts[36];
        if (parts.Count > 37) float.TryParse(parts[37], out skill.prefab_move_speed);
        if (parts.Count > 38) float.TryParse(parts[38], out skill.prefab_destroy_time);
        if (parts.Count > 39) int.TryParse(parts[39], out skill.prefab_make_count);
        if (parts.Count > 40) float.TryParse(parts[40], out skill.prefab_make_delay);
        if (parts.Count > 41) float.TryParse(parts[41], out skill.prefab_start_delay);
        if (parts.Count > 42) float.TryParse(parts[42], out skill.prefab_duration);
        if (parts.Count > 43) skill.classification_reason = parts[43];
        if (parts.Count > 44) skill.note = parts[44];

        return skill;
    }

    private void SaveCSV()
    {
        string fullPath = Path.Combine(Application.dataPath, CSV_PATH.Substring(7));
        string directory = Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sb = new StringBuilder();

        // Header rows
        sb.AppendLine("스킬ID,//스킬명,스킬타입ID,속성타입ID,기본데미지,버프타입,기본버프수치,디버프타입,기본디버프수치,CC타입,CC지속,스턴여부,슬로우량,DOT지속,DOT틱간격,DOT틱데미지,표식지속,표식데미지배율,쿨다운,시전시간,사거리,투사체속도,투사체수,지속시간,관통수,유도여부,범위반경,범위각도,채널링지속,채널링틱간격,중단가능여부,//설명,프리팹명,프리팹경로,스크립트기반,행동타입,데미지타입,프리팹이동속도,프리팹파괴시간,프리팹생성수,프리팹생성딜레이,프리팹시작딜레이,프리팹지속시간,분류사유,//note");
        sb.AppendLine("skill_id,//skill_name,skill_type_ID,element_type_ID,base_damage,buff_type,base_buff_value,debuff_type,base_debuff_value,cc_type,cc_duration,stun_use,cc_slow_amount,dot_duration,dot_tick_interval,dot_damage_per_tick,mark_duration,mark_damage_mult,cooldown,cast_time,range,projectile_speed,projectile_count,skill_lifetime,pierce_count,is_homing,aoe_radius,aoe_angle,channel_duration,channel_tick_interval,interruptible,//description,prefab_name,prefab_path,is_script_based,behavior_type,damage_type,prefab_move_speed,prefab_destroy_time,prefab_make_count,prefab_make_delay,prefab_start_delay,prefab_duration,classification_reason,//note");
        sb.AppendLine("int,//string,int,int,float,int,float,int,float,int,float,bool,float,float,float,float,float,float,float,float,float,float,int,float,int,bool,float,float,float,float,bool,//string,string,string,bool,string,string,float,float,int,float,float,float,string,//string");

        // Data rows
        foreach (var s in allSkills)
        {
            sb.AppendLine($"{s.skill_id},{EscapeCSV(s.skill_name)},{s.skill_type_ID},{s.element_type_ID},{s.base_damage},{s.buff_type},{s.base_buff_value},{s.debuff_type},{s.base_debuff_value},{s.cc_type},{s.cc_duration},{s.stun_use.ToString().ToLower()},{s.cc_slow_amount},{s.dot_duration},{s.dot_tick_interval},{s.dot_damage_per_tick},{s.mark_duration},{s.mark_damage_mult},{s.cooldown},{s.cast_time},{s.range},{s.projectile_speed},{s.projectile_count},{s.skill_lifetime},{s.pierce_count},{s.is_homing.ToString().ToLower()},{s.aoe_radius},{s.aoe_angle},{s.channel_duration},{s.channel_tick_interval},{s.interruptible.ToString().ToLower()},{EscapeCSV(s.description)},{s.prefab_name},{s.prefab_path},{s.is_script_based.ToString().ToLower()},{s.behavior_type},{s.damage_type},{s.prefab_move_speed},{s.prefab_destroy_time},{s.prefab_make_count},{s.prefab_make_delay},{s.prefab_start_delay},{s.prefab_duration},{EscapeCSV(s.classification_reason)},{EscapeCSV(s.note)}");
        }

        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        // 저장 후 수정 플래그 초기화
        foreach (var skill in allSkills)
        {
            skill.isModified = false;
        }
        isDirty = false;

        Debug.Log($"[SkillManagingTool] CSV 저장 완료: {fullPath}");
    }

    private void CreateBackup()
    {
        string fullPath = Path.Combine(Application.dataPath, CSV_PATH.Substring(7));
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("[SkillManagingTool] 백업할 파일이 없습니다.");
            return;
        }

        string backupPath = Path.Combine(Application.dataPath, BACKUP_FOLDER.Substring(7));
        if (!Directory.Exists(backupPath))
        {
            Directory.CreateDirectory(backupPath);
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupFile = Path.Combine(backupPath, $"MainSkillTable_backup_{timestamp}.csv");
        File.Copy(fullPath, backupFile);

        AssetDatabase.Refresh();
        Debug.Log($"[SkillManagingTool] 백업 생성: {backupFile}");
    }

    private string EscapeCSV(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    private List<string> ParseCSVLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    #endregion

    #region Data Class

    [Serializable]
    private class SkillDataEntry
    {
        // 기본 정보
        public int skill_id;
        public string skill_name = "";
        public int skill_type_ID;
        public int element_type_ID;
        public float base_damage;

        // 버프/디버프
        public int buff_type;
        public float base_buff_value;
        public int debuff_type;
        public float base_debuff_value;

        // CC/DOT
        public int cc_type;
        public float cc_duration;
        public bool stun_use;
        public float cc_slow_amount;
        public float dot_duration;
        public float dot_tick_interval;
        public float dot_damage_per_tick;
        public float mark_duration;
        public float mark_damage_mult;

        // 스킬 속성
        public float cooldown;
        public float cast_time;
        public float range;
        public float projectile_speed;
        public int projectile_count;
        public float skill_lifetime;
        public int pierce_count;
        public bool is_homing;
        public float aoe_radius;
        public float aoe_angle;
        public float channel_duration;
        public float channel_tick_interval;
        public bool interruptible;
        public string description = "";
        public string note = "";

        // 프리팹 정보
        public string prefab_name = "";
        public string prefab_path = "";
        public bool is_script_based;
        public string behavior_type = "";
        public string damage_type = "";
        public float prefab_move_speed;
        public float prefab_destroy_time;
        public int prefab_make_count;
        public float prefab_make_delay;
        public float prefab_start_delay;
        public float prefab_duration;
        public string classification_reason = "";

        // 에디터 상태
        public bool isModified;
    }

    #endregion
}
