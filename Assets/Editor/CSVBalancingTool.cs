using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CSV 밸런싱 도구 - 게임 데이터를 실시간으로 편집하고 저장
/// Issue #333
///
/// 뷰 모드:
/// - 테이블 뷰: CSV 원본 데이터 직접 편집
/// - 엔티티 뷰: 스테이지/캐릭터/스킬 중심의 직관적 편집
/// </summary>
public class CSVBalancingTool : EditorWindow
{
    // 뷰 모드
    private enum ViewMode
    {
        TableView,      // 기존 테이블 기반 뷰
        EntityView      // 새로운 엔티티 중심 뷰
    }

    // 엔티티 뷰 탭
    private enum EntityTab
    {
        Stage,      // 스테이지 → 웨이브 → 몬스터
        Character,  // 캐릭터 → 스탯/스킬
        Skill       // 스킬 → 레벨별 효과
    }

    // 테이블 뷰 탭 (기존)
    private enum TableTab
    {
        Character,
        MainSkill,
        SupportSkill,
        Monster,
        Wave,
        Stage,
        CharacterEnhancement,
        SkillEnhancement,
        MonsterTier,
        Card,
        CardLevel,
        CardList
    }

    private ViewMode currentViewMode = ViewMode.EntityView;
    private EntityTab currentEntityTab = EntityTab.Stage;
    private TableTab currentTableTab = TableTab.Character;

    // 스크롤 위치
    private Vector2 scrollPos;
    private Vector2 leftPanelScroll;
    private Vector2 rightPanelScroll;

    // 데이터 캐시
    private List<CharacterData> characterDataList = new List<CharacterData>();
    private List<MainSkillData> mainSkillDataList = new List<MainSkillData>();
    private List<SupportSkillData> supportSkillDataList = new List<SupportSkillData>();
    private List<MonsterData> monsterDataList = new List<MonsterData>();
    private List<WaveData> waveDataList = new List<WaveData>();
    private List<StageData> stageDataList = new List<StageData>();
    private List<LevelData> levelDataList = new List<LevelData>();
    private List<SkillLevelData> skillLevelDataList = new List<SkillLevelData>();
    private List<MonsterLevelData> monsterLevelDataList = new List<MonsterLevelData>();
    private List<CardData> cardDataList = new List<CardData>();
    private List<CardLevelData> cardLevelDataList = new List<CardLevelData>();
    private List<CardListData> cardListDataList = new List<CardListData>();

    // CSV 경로
    private const string CSV_PATH = "Assets/Data/CSV";

    // 상태
    private bool isDataLoaded = false;
    private string statusMessage = "";
    private MessageType statusType = MessageType.Info;

    // 필터/검색
    private string searchFilter = "";

    // 엔티티 뷰 선택 상태
    private int selectedStageIndex = -1;
    private int selectedWaveIndex = -1;
    private int selectedCharacterIndex = -1;
    private int selectedSkillIndex = -1;

    // 수정 추적
    private HashSet<string> modifiedTables = new HashSet<string>();

    // GUIStyle 캐싱 (Unity 6 호환성)
    private GUIStyle _selectedStyle;
    private Texture2D _selectedBgTex;

    [MenuItem("Tools/CSV Balancing Tool", false, 1)]
    public static void ShowWindow()
    {
        var window = GetWindow<CSVBalancingTool>("CSV Balancing Tool");
        window.minSize = new Vector2(1200, 700);
        window.Show();
    }

    private void OnEnable()
    {
        LoadAllData();
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawViewModeSelector();

        EditorGUILayout.Space(5);

        if (!isDataLoaded)
        {
            EditorGUILayout.HelpBox("데이터가 로드되지 않았습니다. '새로고침' 버튼을 누르거나 Play Mode에 진입하세요.", MessageType.Warning);
        }
        else if (currentViewMode == ViewMode.EntityView)
        {
            DrawEntityView();
        }
        else
        {
            DrawTableView();
        }

        DrawFooter();
    }

    #region Header & Footer

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("CSV Balancing Tool", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (modifiedTables.Count > 0)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"[수정됨: {string.Join(", ", modifiedTables)}]");
                    GUI.color = Color.white;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("게임 데이터를 실시간으로 편집하고 CSV에 저장", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawViewModeSelector()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            GUILayout.Label("뷰 모드:", GUILayout.Width(60));

            if (GUILayout.Toggle(currentViewMode == ViewMode.EntityView, "엔티티 뷰", EditorStyles.toolbarButton, GUILayout.Width(100)))
                currentViewMode = ViewMode.EntityView;
            if (GUILayout.Toggle(currentViewMode == ViewMode.TableView, "테이블 뷰", EditorStyles.toolbarButton, GUILayout.Width(100)))
                currentViewMode = ViewMode.TableView;

            GUILayout.FlexibleSpace();

            // 상태 메시지 (statusType에 따른 색상 표시)
            if (!string.IsNullOrEmpty(statusMessage))
            {
                Color originalColor = GUI.color;
                switch (statusType)
                {
                    case MessageType.Error:
                        GUI.color = Color.red;
                        break;
                    case MessageType.Warning:
                        GUI.color = Color.yellow;
                        break;
                    default:
                        GUI.color = Color.green;
                        break;
                }
                GUILayout.Label(statusMessage, EditorStyles.miniLabel);
                GUI.color = originalColor;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawFooter()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        {
            // 검색
            GUILayout.Label("검색:", GUILayout.Width(40));
            searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            // 리로드
            if (GUILayout.Button("새로고침", GUILayout.Width(80)))
            {
                ReloadCSVData();
            }

            // 모두 저장
            GUI.backgroundColor = modifiedTables.Count > 0 ? Color.yellow : Color.green;
            if (GUILayout.Button(modifiedTables.Count > 0 ? "모두 저장 *" : "모두 저장", GUILayout.Width(100)))
            {
                SaveAllModifiedTables();
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Entity View

    private void DrawEntityView()
    {
        // 엔티티 탭
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            if (GUILayout.Toggle(currentEntityTab == EntityTab.Stage, "스테이지/몬스터", EditorStyles.toolbarButton))
                currentEntityTab = EntityTab.Stage;
            if (GUILayout.Toggle(currentEntityTab == EntityTab.Character, "캐릭터", EditorStyles.toolbarButton))
                currentEntityTab = EntityTab.Character;
            if (GUILayout.Toggle(currentEntityTab == EntityTab.Skill, "스킬", EditorStyles.toolbarButton))
                currentEntityTab = EntityTab.Skill;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        switch (currentEntityTab)
        {
            case EntityTab.Stage:
                DrawStageEntityView();
                break;
            case EntityTab.Character:
                DrawCharacterEntityView();
                break;
            case EntityTab.Skill:
                DrawSkillEntityView();
                break;
        }
    }

    private void DrawStageEntityView()
    {
        EditorGUILayout.BeginHorizontal();
        {
            // 좌측: 스테이지 목록
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(250));
            {
                GUILayout.Label("스테이지 선택", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);

                leftPanelScroll = EditorGUILayout.BeginScrollView(leftPanelScroll);
                {
                    for (int i = 0; i < stageDataList.Count; i++)
                    {
                        var stage = stageDataList[i];
                        string stageName = $"Stage {stage.Chapter_Number} (ID: {stage.Stage_ID})";

                        // 웨이브 정보 미리보기
                        int waveCount = CountWaves(stage);
                        int totalMonsters = GetTotalMonsterCount(stage);

                        EditorGUILayout.BeginVertical(selectedStageIndex == i ?
                            GetSelectedStyle() : EditorStyles.helpBox);
                        {
                            if (GUILayout.Button(stageName, EditorStyles.boldLabel))
                            {
                                selectedStageIndex = i;
                                selectedWaveIndex = -1;
                            }
                            EditorGUILayout.LabelField($"웨이브: {waveCount}개 | 몬스터: {totalMonsters}마리", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            // 중앙: 웨이브 목록
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(300));
            {
                if (selectedStageIndex >= 0 && selectedStageIndex < stageDataList.Count)
                {
                    var stage = stageDataList[selectedStageIndex];
                    GUILayout.Label($"Stage {stage.Chapter_Number} - 웨이브", EditorStyles.boldLabel);

                    // 스테이지 기본 정보 편집
                    EditorGUILayout.Space(3);
                    EditorGUI.BeginChangeCheck();
                    stage.Time_Limit = EditorGUILayout.FloatField("제한 시간", stage.Time_Limit);
                    stage.Barrier_HP = EditorGUILayout.FloatField("장벽 HP", stage.Barrier_HP);
                    stage.AP_Cost = EditorGUILayout.IntField("AP 비용", stage.AP_Cost);
                    if (EditorGUI.EndChangeCheck())
                        MarkModified("StageTable");

                    EditorGUILayout.Space(5);
                    GUILayout.Label("웨이브 목록", EditorStyles.miniBoldLabel);

                    rightPanelScroll = EditorGUILayout.BeginScrollView(rightPanelScroll);
                    {
                        var waveIds = GetStageWaveIds(stage);
                        for (int i = 0; i < waveIds.Count; i++)
                        {
                            var wave = waveDataList.FirstOrDefault(w => w.Wave_ID == waveIds[i]);
                            if (wave == null) continue;

                            var monster = monsterDataList.FirstOrDefault(m => m.Monster_ID == wave.Monster_ID);
                            var monsterLevel = monsterLevelDataList.FirstOrDefault(ml => ml.Mon_Level_ID == wave.Mon_Level_ID);

                            string monsterName = monster != null ? $"몬스터 {monster.Monster_ID}" : "???";

                            EditorGUILayout.BeginVertical(selectedWaveIndex == i ?
                                GetSelectedStyle() : EditorStyles.helpBox);
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    if (GUILayout.Button($"웨이브 {i + 1}", EditorStyles.boldLabel, GUILayout.Width(80)))
                                    {
                                        selectedWaveIndex = i;
                                    }
                                    GUILayout.Label($"({wave.Spawn_Time}초)", EditorStyles.miniLabel);
                                }
                                EditorGUILayout.EndHorizontal();

                                EditorGUILayout.LabelField($"{monsterName} x{wave.Monster_Count}", EditorStyles.miniLabel);
                                if (monsterLevel != null)
                                {
                                    EditorGUILayout.LabelField($"HP: {monsterLevel.HP} | ATK: {monsterLevel.ATK}", EditorStyles.miniLabel);
                                }
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    GUILayout.Label("← 스테이지를 선택하세요", EditorStyles.centeredGreyMiniLabel);
                }
            }
            EditorGUILayout.EndVertical();

            // 우측: 상세 편집
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                if (selectedStageIndex >= 0 && selectedWaveIndex >= 0)
                {
                    var stage = stageDataList[selectedStageIndex];
                    var waveIds = GetStageWaveIds(stage);

                    if (selectedWaveIndex < waveIds.Count)
                    {
                        var wave = waveDataList.FirstOrDefault(w => w.Wave_ID == waveIds[selectedWaveIndex]);
                        if (wave != null)
                        {
                            DrawWaveDetailEditor(wave);
                        }
                    }
                }
                else
                {
                    GUILayout.Label("← 웨이브를 선택하세요", EditorStyles.centeredGreyMiniLabel);
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawWaveDetailEditor(WaveData wave)
    {
        GUILayout.Label($"웨이브 상세 편집 (ID: {wave.Wave_ID})", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 웨이브 기본 정보
        EditorGUILayout.LabelField("웨이브 설정", EditorStyles.miniBoldLabel);
        EditorGUI.BeginChangeCheck();
        wave.Spawn_Time = EditorGUILayout.FloatField("시작 시간 (초)", wave.Spawn_Time);
        wave.Monster_Count = EditorGUILayout.IntField("몬스터 수", wave.Monster_Count);
        wave.Spawn_Interval = EditorGUILayout.FloatField("스폰 간격 (초)", wave.Spawn_Interval);
        if (EditorGUI.EndChangeCheck())
            MarkModified("WaveTable");

        EditorGUILayout.Space(10);

        // 몬스터 선택
        EditorGUILayout.LabelField("몬스터 설정", EditorStyles.miniBoldLabel);

        // 몬스터 드롭다운
        var monsterOptions = monsterDataList.Select(m => $"{m.Monster_ID} - Genre {(int)m.Genre}").ToArray();
        int currentMonsterIdx = monsterDataList.FindIndex(m => m.Monster_ID == wave.Monster_ID);
        EditorGUI.BeginChangeCheck();
        int newMonsterIdx = EditorGUILayout.Popup("몬스터", currentMonsterIdx, monsterOptions);
        if (EditorGUI.EndChangeCheck() && newMonsterIdx >= 0)
        {
            wave.Monster_ID = monsterDataList[newMonsterIdx].Monster_ID;
            MarkModified("WaveTable");
        }

        // 몬스터 레벨 드롭다운
        var levelOptions = monsterLevelDataList.Select(ml => $"Tier {ml.Level_Type} (HP:{ml.HP}, ATK:{ml.ATK})").ToArray();
        int currentLevelIdx = monsterLevelDataList.FindIndex(ml => ml.Mon_Level_ID == wave.Mon_Level_ID);
        EditorGUI.BeginChangeCheck();
        int newLevelIdx = EditorGUILayout.Popup("몬스터 티어", currentLevelIdx, levelOptions);
        if (EditorGUI.EndChangeCheck() && newLevelIdx >= 0)
        {
            wave.Mon_Level_ID = monsterLevelDataList[newLevelIdx].Mon_Level_ID;
            MarkModified("WaveTable");
        }

        EditorGUILayout.Space(10);

        // 선택된 몬스터 레벨 상세 편집
        var monsterLevel = monsterLevelDataList.FirstOrDefault(ml => ml.Mon_Level_ID == wave.Mon_Level_ID);
        if (monsterLevel != null)
        {
            EditorGUILayout.LabelField("몬스터 스탯 (티어 공통)", EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();
            monsterLevel.HP = EditorGUILayout.Slider("HP", monsterLevel.HP, 10, 5000);
            monsterLevel.ATK = EditorGUILayout.Slider("ATK", monsterLevel.ATK, 1, 1000);
            monsterLevel.Move_Speed = EditorGUILayout.Slider("이동속도", monsterLevel.Move_Speed, 0.1f, 5f);
            monsterLevel.Attack_Speed = EditorGUILayout.Slider("공격속도", monsterLevel.Attack_Speed, 0.1f, 3f);
            monsterLevel.Exp_Value = EditorGUILayout.IntSlider("경험치", monsterLevel.Exp_Value, 1, 100);
            if (EditorGUI.EndChangeCheck())
                MarkModified("MonsterLevelTable");
        }

        EditorGUILayout.Space(10);

        // 예상 정보
        EditorGUILayout.LabelField("예상 정보", EditorStyles.miniBoldLabel);
        if (monsterLevel != null)
        {
            float totalHP = monsterLevel.HP * wave.Monster_Count;
            float totalExp = monsterLevel.Exp_Value * wave.Monster_Count;
            float spawnDuration = wave.Spawn_Interval * (wave.Monster_Count - 1);

            EditorGUILayout.HelpBox(
                $"총 HP: {totalHP:N0}\n" +
                $"총 경험치: {totalExp:N0}\n" +
                $"스폰 완료까지: {spawnDuration:F1}초",
                MessageType.Info);
        }
    }

    private void DrawCharacterEntityView()
    {
        EditorGUILayout.BeginHorizontal();
        {
            // 좌측: 캐릭터 목록
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(250));
            {
                GUILayout.Label("캐릭터 선택", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);

                leftPanelScroll = EditorGUILayout.BeginScrollView(leftPanelScroll);
                {
                    for (int i = 0; i < characterDataList.Count; i++)
                    {
                        var character = characterDataList[i];
                        string charName = $"캐릭터 {character.Character_ID}";

                        EditorGUILayout.BeginVertical(selectedCharacterIndex == i ?
                            GetSelectedStyle() : EditorStyles.helpBox);
                        {
                            if (GUILayout.Button(charName, EditorStyles.boldLabel))
                            {
                                selectedCharacterIndex = i;
                            }
                            EditorGUILayout.LabelField($"장르: {character.Genre} | 스킬: {character.Base_Skill_ID}", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            // 우측: 캐릭터 상세 편집
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterDataList.Count)
                {
                    DrawCharacterDetailEditor(characterDataList[selectedCharacterIndex]);
                }
                else
                {
                    GUILayout.Label("← 캐릭터를 선택하세요", EditorStyles.centeredGreyMiniLabel);
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCharacterDetailEditor(CharacterData character)
    {
        GUILayout.Label($"캐릭터 상세 편집 (ID: {character.Character_ID})", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 기본 정보
        EditorGUILayout.LabelField("기본 정보", EditorStyles.miniBoldLabel);
        EditorGUI.BeginChangeCheck();
        character.Genre = (Genre)EditorGUILayout.EnumPopup("장르", character.Genre);

        // 스킬 드롭다운
        var skillOptions = mainSkillDataList.Select(s => $"{s.skill_id} - {s.skill_name}").ToArray();
        int currentSkillIdx = mainSkillDataList.FindIndex(s => s.skill_id == character.Base_Skill_ID);
        int newSkillIdx = EditorGUILayout.Popup("기본 스킬", currentSkillIdx, skillOptions);
        if (newSkillIdx >= 0 && newSkillIdx != currentSkillIdx)
        {
            character.Base_Skill_ID = mainSkillDataList[newSkillIdx].skill_id;
        }
        if (EditorGUI.EndChangeCheck())
            MarkModified("CharacterTable");

        EditorGUILayout.Space(10);

        // 레벨별 스탯
        EditorGUILayout.LabelField("레벨별 스탯", EditorStyles.miniBoldLabel);

        rightPanelScroll = EditorGUILayout.BeginScrollView(rightPanelScroll);
        {
            var levelIds = new[] {
                character.Cha_Level_1_ID, character.Cha_Level_2_ID, character.Cha_Level_3_ID,
                character.Cha_Level_4_ID, character.Cha_Level_5_ID, character.Cha_Level_6_ID,
                character.Cha_Level_7_ID, character.Cha_Level_8_ID, character.Cha_Level_9_ID,
                character.Cha_Level_10_ID
            };

            for (int lvl = 0; lvl < levelIds.Length; lvl++)
            {
                var levelData = levelDataList.FirstOrDefault(l => l.Cha_Level_ID == levelIds[lvl]);
                if (levelData == null) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField($"레벨 {lvl + 1}", EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();

                    // ATK 필드
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("ATK", GUILayout.Width(80));
                    levelData.Base_ATK = EditorGUILayout.FloatField(levelData.Base_ATK);
                    EditorGUILayout.EndHorizontal();

                    // ATK Speed 필드
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("ATK Speed", GUILayout.Width(80));
                    levelData.Base_ATK_Speed = EditorGUILayout.FloatField(levelData.Base_ATK_Speed);
                    EditorGUILayout.EndHorizontal();

                    // Power 필드
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Power", GUILayout.Width(80));
                    levelData.Base_Power = EditorGUILayout.FloatField(levelData.Base_Power);
                    EditorGUILayout.EndHorizontal();

                    if (EditorGUI.EndChangeCheck())
                        MarkModified("LevelTable");
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // 연결된 스킬 정보
        var skill = mainSkillDataList.FirstOrDefault(s => s.skill_id == character.Base_Skill_ID);
        if (skill != null)
        {
            EditorGUILayout.LabelField("연결된 스킬 정보", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                $"스킬: {skill.skill_name}\n" +
                $"기본 데미지: {skill.base_damage}\n" +
                $"쿨다운: {skill.cooldown}초\n" +
                $"사거리: {skill.range}",
                MessageType.Info);
        }
    }

    private void DrawSkillEntityView()
    {
        EditorGUILayout.BeginHorizontal();
        {
            // 좌측: 스킬 목록
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(300));
            {
                GUILayout.Label("스킬 선택", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);

                leftPanelScroll = EditorGUILayout.BeginScrollView(leftPanelScroll);
                {
                    for (int i = 0; i < mainSkillDataList.Count; i++)
                    {
                        var skill = mainSkillDataList[i];

                        EditorGUILayout.BeginVertical(selectedSkillIndex == i ?
                            GetSelectedStyle() : EditorStyles.helpBox);
                        {
                            if (GUILayout.Button($"{skill.skill_id} - {skill.skill_name}", EditorStyles.boldLabel))
                            {
                                selectedSkillIndex = i;
                            }
                            EditorGUILayout.LabelField($"데미지: {skill.base_damage} | 쿨다운: {skill.cooldown}초", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            // 우측: 스킬 상세 편집
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                if (selectedSkillIndex >= 0 && selectedSkillIndex < mainSkillDataList.Count)
                {
                    DrawSkillDetailEditor(mainSkillDataList[selectedSkillIndex]);
                }
                else
                {
                    GUILayout.Label("← 스킬을 선택하세요", EditorStyles.centeredGreyMiniLabel);
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillDetailEditor(MainSkillData skill)
    {
        GUILayout.Label($"스킬 상세 편집: {skill.skill_name}", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 기본 스탯
        EditorGUILayout.LabelField("기본 스탯", EditorStyles.miniBoldLabel);
        EditorGUI.BeginChangeCheck();
        skill.base_damage = EditorGUILayout.Slider("기본 데미지", skill.base_damage, 1, 500);
        skill.cooldown = EditorGUILayout.Slider("쿨다운", skill.cooldown, 0.1f, 30f);
        skill.range = EditorGUILayout.Slider("사거리", skill.range, 1, 200);
        skill.projectile_speed = EditorGUILayout.Slider("투사체 속도", skill.projectile_speed, 1, 100);
        skill.projectile_count = EditorGUILayout.IntSlider("투사체 수", skill.projectile_count, 1, 10);
        skill.pierce_count = EditorGUILayout.IntSlider("관통 수", skill.pierce_count, 0, 10);
        skill.aoe_radius = EditorGUILayout.Slider("범위 반경", skill.aoe_radius, 0, 20);
        if (EditorGUI.EndChangeCheck())
            MarkModified("MainSkillTable");

        EditorGUILayout.Space(10);

        // 레벨별 배율
        EditorGUILayout.LabelField("레벨별 강화 (아웃게임)", EditorStyles.miniBoldLabel);

        rightPanelScroll = EditorGUILayout.BeginScrollView(rightPanelScroll);
        {
            var skillLevels = skillLevelDataList.Where(sl => sl.skill_id == skill.skill_id).OrderBy(sl => sl.level).ToList();

            foreach (var levelData in skillLevels)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField($"강화 {levelData.level}단계", EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();

                    // 데미지 배율
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("데미지 배율", GUILayout.Width(100));
                    levelData.damage_mult = EditorGUILayout.FloatField(levelData.damage_mult);
                    EditorGUILayout.EndHorizontal();

                    // 쿨다운 배율
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("쿨다운 배율", GUILayout.Width(100));
                    levelData.cooldown_mult = EditorGUILayout.FloatField(levelData.cooldown_mult);
                    EditorGUILayout.EndHorizontal();

                    // 사거리 배율
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("사거리 배율", GUILayout.Width(100));
                    levelData.range_mult = EditorGUILayout.FloatField(levelData.range_mult);
                    EditorGUILayout.EndHorizontal();

                    // 범위 배율
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("범위 배율", GUILayout.Width(100));
                    levelData.aoe_mult = EditorGUILayout.FloatField(levelData.aoe_mult);
                    EditorGUILayout.EndHorizontal();

                    // 추가 투사체
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("추가 투사체", GUILayout.Width(100));
                    levelData.projectile_add = EditorGUILayout.IntField(levelData.projectile_add);
                    EditorGUILayout.EndHorizontal();

                    // 추가 관통
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("추가 관통", GUILayout.Width(100));
                    levelData.pierce_add = EditorGUILayout.IntField(levelData.pierce_add);
                    EditorGUILayout.EndHorizontal();

                    if (EditorGUI.EndChangeCheck())
                        MarkModified("SkillLevelTable");

                    // 계산된 값 표시
                    float calcDamage = skill.base_damage * levelData.damage_mult;
                    float calcCooldown = skill.cooldown * levelData.cooldown_mult;
                    EditorGUILayout.LabelField($"→ 데미지: {calcDamage:F1}, 쿨다운: {calcCooldown:F2}초", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region Table View (기존 코드)

    private void DrawTableView()
    {
        // 탭
        DrawTableTabs();

        EditorGUILayout.Space(5);

        // 테이블 컨텐츠
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        {
            switch (currentTableTab)
            {
                case TableTab.Character:
                    DrawCharacterTable();
                    break;
                case TableTab.MainSkill:
                    DrawMainSkillTable();
                    break;
                case TableTab.SupportSkill:
                    DrawSupportSkillTable();
                    break;
                case TableTab.Monster:
                    DrawMonsterTable();
                    break;
                case TableTab.Wave:
                    DrawWaveTable();
                    break;
                case TableTab.Stage:
                    DrawStageTable();
                    break;
                case TableTab.CharacterEnhancement:
                    DrawCharacterEnhancementTable();
                    break;
                case TableTab.SkillEnhancement:
                    DrawSkillEnhancementTable();
                    break;
                case TableTab.MonsterTier:
                    DrawMonsterTierTable();
                    break;
                case TableTab.Card:
                    DrawCardTable();
                    break;
                case TableTab.CardLevel:
                    DrawCardLevelTable();
                    break;
                case TableTab.CardList:
                    DrawCardListTable();
                    break;
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawTableTabs()
    {
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Toggle(currentTableTab == TableTab.Character, "Character", EditorStyles.toolbarButton))
                currentTableTab = TableTab.Character;
            if (GUILayout.Toggle(currentTableTab == TableTab.MainSkill, "Main Skill", EditorStyles.toolbarButton))
                currentTableTab = TableTab.MainSkill;
            if (GUILayout.Toggle(currentTableTab == TableTab.SupportSkill, "Support Skill", EditorStyles.toolbarButton))
                currentTableTab = TableTab.SupportSkill;
            if (GUILayout.Toggle(currentTableTab == TableTab.Monster, "Monster", EditorStyles.toolbarButton))
                currentTableTab = TableTab.Monster;
            if (GUILayout.Toggle(currentTableTab == TableTab.Wave, "Wave", EditorStyles.toolbarButton))
                currentTableTab = TableTab.Wave;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Toggle(currentTableTab == TableTab.Stage, "Stage", EditorStyles.toolbarButton))
                currentTableTab = TableTab.Stage;
            if (GUILayout.Toggle(currentTableTab == TableTab.CharacterEnhancement, "Char Enhance", EditorStyles.toolbarButton))
                currentTableTab = TableTab.CharacterEnhancement;
            if (GUILayout.Toggle(currentTableTab == TableTab.SkillEnhancement, "Skill Enhance", EditorStyles.toolbarButton))
                currentTableTab = TableTab.SkillEnhancement;
            if (GUILayout.Toggle(currentTableTab == TableTab.MonsterTier, "Monster Tier", EditorStyles.toolbarButton))
                currentTableTab = TableTab.MonsterTier;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Toggle(currentTableTab == TableTab.Card, "Card", EditorStyles.toolbarButton))
                currentTableTab = TableTab.Card;
            if (GUILayout.Toggle(currentTableTab == TableTab.CardLevel, "Card Level", EditorStyles.toolbarButton))
                currentTableTab = TableTab.CardLevel;
            if (GUILayout.Toggle(currentTableTab == TableTab.CardList, "Card List", EditorStyles.toolbarButton))
                currentTableTab = TableTab.CardList;
        }
        EditorGUILayout.EndHorizontal();
    }

    // 기존 테이블 Draw 메서드들...
    private void DrawCharacterTable()
    {
        if (characterDataList == null || characterDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Character data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Character Table ({characterDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in characterDataList)
        {
            if (!string.IsNullOrEmpty(searchFilter) && !data.Character_ID.ToString().Contains(searchFilter))
                continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"ID: {data.Character_ID}", EditorStyles.boldLabel);
                data.Genre = (Genre)EditorGUILayout.EnumPopup("Genre", data.Genre);
                data.Base_Skill_ID = EditorGUILayout.IntField("Base Skill", data.Base_Skill_ID);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("CharacterTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawMainSkillTable()
    {
        if (mainSkillDataList == null || mainSkillDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Main Skill data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Main Skill Table ({mainSkillDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in mainSkillDataList)
        {
            if (!string.IsNullOrEmpty(searchFilter) && !data.skill_id.ToString().Contains(searchFilter))
                continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"{data.skill_id} - {data.skill_name}", EditorStyles.boldLabel);
                data.base_damage = EditorGUILayout.FloatField("Base Damage", data.base_damage);
                data.cooldown = EditorGUILayout.FloatField("Cooldown", data.cooldown);
                data.range = EditorGUILayout.FloatField("Range", data.range);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("MainSkillTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawSupportSkillTable()
    {
        if (supportSkillDataList == null || supportSkillDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Support Skill data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Support Skill Table ({supportSkillDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in supportSkillDataList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"{data.support_id} - {data.support_name}", EditorStyles.boldLabel);
                data.damage_mult = EditorGUILayout.FloatField("Damage Mult", data.damage_mult);
                data.attack_speed_mult = EditorGUILayout.FloatField("ATK Speed Mult", data.attack_speed_mult);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("SupportSkillTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawMonsterTable()
    {
        if (monsterDataList == null || monsterDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Monster data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Monster Table ({monsterDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in monsterDataList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"Monster ID: {data.Monster_ID}", EditorStyles.boldLabel);
                data.Genre = (Genre)EditorGUILayout.EnumPopup("Genre", data.Genre);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("MonsterTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawWaveTable()
    {
        if (waveDataList == null || waveDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Wave data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Wave Table ({waveDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in waveDataList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"Wave ID: {data.Wave_ID}", EditorStyles.boldLabel);
                data.Monster_Count = EditorGUILayout.IntField("Monster Count", data.Monster_Count);
                data.Spawn_Time = EditorGUILayout.FloatField("Spawn Time", data.Spawn_Time);
                data.Spawn_Interval = EditorGUILayout.FloatField("Spawn Interval", data.Spawn_Interval);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("WaveTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawStageTable()
    {
        if (stageDataList == null || stageDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Stage data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Stage Table ({stageDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in stageDataList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"Stage ID: {data.Stage_ID}", EditorStyles.boldLabel);
                data.Time_Limit = EditorGUILayout.FloatField("Time Limit", data.Time_Limit);
                data.Barrier_HP = EditorGUILayout.FloatField("Barrier HP", data.Barrier_HP);
                data.AP_Cost = EditorGUILayout.IntField("AP Cost", data.AP_Cost);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("StageTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawCharacterEnhancementTable()
    {
        if (levelDataList == null || levelDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Level data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Character Enhancement Table ({levelDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in levelDataList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"Level {data.Level}", EditorStyles.boldLabel);
                data.Base_ATK = EditorGUILayout.FloatField("Base ATK", data.Base_ATK);
                data.Base_ATK_Speed = EditorGUILayout.FloatField("ATK Speed", data.Base_ATK_Speed);
                data.Base_Power = EditorGUILayout.FloatField("Power", data.Base_Power);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("LevelTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawSkillEnhancementTable()
    {
        if (skillLevelDataList == null || skillLevelDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Skill Level data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Skill Enhancement Table ({skillLevelDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in skillLevelDataList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"Skill {data.skill_id} - Lv {data.level}", EditorStyles.boldLabel);
                data.damage_mult = EditorGUILayout.FloatField("Damage Mult", data.damage_mult);
                data.cooldown_mult = EditorGUILayout.FloatField("Cooldown Mult", data.cooldown_mult);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("SkillLevelTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawMonsterTierTable()
    {
        if (monsterLevelDataList == null || monsterLevelDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Monster Tier data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Monster Tier Table ({monsterLevelDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in monsterLevelDataList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"Tier {data.Level_Type}", EditorStyles.boldLabel);
                data.HP = EditorGUILayout.FloatField("HP", data.HP);
                data.ATK = EditorGUILayout.FloatField("ATK", data.ATK);
                data.Move_Speed = EditorGUILayout.FloatField("Move Speed", data.Move_Speed);
                data.Exp_Value = EditorGUILayout.IntField("Exp", data.Exp_Value);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("MonsterLevelTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawCardTable()
    {
        if (cardDataList == null || cardDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Card data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Card Table ({cardDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in cardDataList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"Card ID: {data.Card_ID}", EditorStyles.boldLabel);
                data.Card_Type = EditorGUILayout.IntField("Type", data.Card_Type);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("CardTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawCardLevelTable()
    {
        if (cardLevelDataList == null || cardLevelDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Card Level data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Card Level Table ({cardLevelDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in cardLevelDataList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"ID: {data.Card_Level_ID} - Tier {data.Tier}", EditorStyles.boldLabel);
                data.value_change = EditorGUILayout.FloatField("Value Change", data.value_change);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("CardLevelTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawCardListTable()
    {
        if (cardListDataList == null || cardListDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("No Card List data loaded", MessageType.Warning);
            return;
        }

        GUILayout.Label($"Card List Table ({cardListDataList.Count} entries)", EditorStyles.boldLabel);

        foreach (var data in cardListDataList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField($"List ID: {data.Card_List_ID}", EditorStyles.boldLabel);
                data.Card_1_ID = EditorGUILayout.IntField("Card 1", data.Card_1_ID);
                data.Card_2_ID = EditorGUILayout.IntField("Card 2", data.Card_2_ID);
                if (EditorGUI.EndChangeCheck())
                    MarkModified("CardListTable");
            }
            EditorGUILayout.EndVertical();
        }
    }

    #endregion

    #region Helper Methods

    private GUIStyle GetSelectedStyle()
    {
        if (_selectedStyle == null)
        {
            _selectedStyle = new GUIStyle(EditorStyles.helpBox);
            _selectedBgTex = MakeTex(2, 2, new Color(0.3f, 0.5f, 0.8f, 0.3f));
            _selectedStyle.normal.background = _selectedBgTex;
        }
        return _selectedStyle;
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.hideFlags = HideFlags.HideAndDontSave; // Unity 6 에러 방지
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private int CountWaves(StageData stage)
    {
        int count = 0;
        if (stage.Wave_1_ID > 0) count++;
        if (stage.Wave_2_ID > 0) count++;
        if (stage.Wave_3_ID > 0) count++;
        if (stage.Wave_4_ID > 0) count++;
        return count;
    }

    private int GetTotalMonsterCount(StageData stage)
    {
        int total = 0;
        var waveIds = GetStageWaveIds(stage);
        foreach (var id in waveIds)
        {
            var wave = waveDataList.FirstOrDefault(w => w.Wave_ID == id);
            if (wave != null) total += wave.Monster_Count;
        }
        return total;
    }

    private List<int> GetStageWaveIds(StageData stage)
    {
        var ids = new List<int>();
        if (stage.Wave_1_ID > 0) ids.Add(stage.Wave_1_ID);
        if (stage.Wave_2_ID > 0) ids.Add(stage.Wave_2_ID);
        if (stage.Wave_3_ID > 0) ids.Add(stage.Wave_3_ID);
        if (stage.Wave_4_ID > 0) ids.Add(stage.Wave_4_ID);
        return ids;
    }

    private void MarkModified(string tableName)
    {
        modifiedTables.Add(tableName);
    }

    #endregion

    #region Data Loading

    private void LoadAllData()
    {
        try
        {
            statusMessage = "Loading CSV data...";
            statusType = MessageType.Info;
            Repaint();

            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                statusMessage = "CSVLoader not initialized. Enter Play Mode first.";
                statusType = MessageType.Warning;
                isDataLoaded = false;
                return;
            }

            characterDataList = CSVLoader.Instance.GetTable<CharacterData>()?.GetAll() ?? new List<CharacterData>();
            mainSkillDataList = CSVLoader.Instance.GetTable<MainSkillData>()?.GetAll() ?? new List<MainSkillData>();
            supportSkillDataList = CSVLoader.Instance.GetTable<SupportSkillData>()?.GetAll() ?? new List<SupportSkillData>();
            monsterDataList = CSVLoader.Instance.GetTable<MonsterData>()?.GetAll() ?? new List<MonsterData>();
            waveDataList = CSVLoader.Instance.GetTable<WaveData>()?.GetAll() ?? new List<WaveData>();
            stageDataList = CSVLoader.Instance.GetTable<StageData>()?.GetAll() ?? new List<StageData>();
            levelDataList = CSVLoader.Instance.GetTable<LevelData>()?.GetAll() ?? new List<LevelData>();
            skillLevelDataList = CSVLoader.Instance.GetTable<SkillLevelData>()?.GetAll() ?? new List<SkillLevelData>();
            monsterLevelDataList = CSVLoader.Instance.GetTable<MonsterLevelData>()?.GetAll() ?? new List<MonsterLevelData>();
            cardDataList = CSVLoader.Instance.GetTable<CardData>()?.GetAll() ?? new List<CardData>();
            cardLevelDataList = CSVLoader.Instance.GetTable<CardLevelData>()?.GetAll() ?? new List<CardLevelData>();
            cardListDataList = CSVLoader.Instance.GetTable<CardListData>()?.GetAll() ?? new List<CardListData>();

            isDataLoaded = true;
            statusMessage = $"Loaded: {characterDataList.Count} chars, {mainSkillDataList.Count} skills, {monsterDataList.Count} monsters";
            statusType = MessageType.Info;
        }
        catch (Exception e)
        {
            statusMessage = $"Failed: {e.Message}";
            statusType = MessageType.Error;
            isDataLoaded = false;
        }

        Repaint();
    }

    private async void ReloadCSVData()
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            EditorUtility.DisplayDialog("Error", "CSVLoader not initialized. Enter Play Mode first.", "OK");
            return;
        }

        try
        {
            await CSVLoader.Instance.ReloadAllTablesAsync();
            LoadAllData();
            modifiedTables.Clear();
            EditorUtility.DisplayDialog("Success", "CSV data reloaded!", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed: {e.Message}", "OK");
        }

        Repaint();
    }

    #endregion

    #region Data Saving

    private void SaveAllModifiedTables()
    {
        if (modifiedTables.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No changes to save.", "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Save Changes",
            $"Save {modifiedTables.Count} modified table(s)?\n\n{string.Join("\n", modifiedTables)}",
            "Save All",
            "Cancel"
        );

        if (!confirmed) return;

        try
        {
            foreach (var table in modifiedTables.ToList())
            {
                switch (table)
                {
                    case "CharacterTable":
                        SaveStandardCSV("CharacterTable.csv", characterDataList);
                        break;
                    case "MainSkillTable":
                        SaveSkillCSV("MainSkillTable.csv", mainSkillDataList);
                        break;
                    case "SupportSkillTable":
                        SaveSkillCSV("SupportSkillTable.csv", supportSkillDataList);
                        break;
                    case "MonsterTable":
                        SaveStandardCSV("MonsterTable.csv", monsterDataList);
                        break;
                    case "WaveTable":
                        SaveStandardCSV("WaveTable.csv", waveDataList);
                        break;
                    case "StageTable":
                        SaveStandardCSV("StageTable.csv", stageDataList);
                        break;
                    case "LevelTable":
                        SaveStandardCSV("LevelTable.csv", levelDataList);
                        break;
                    case "SkillLevelTable":
                        SaveSkillCSV("SkillLevelTable.csv", skillLevelDataList);
                        break;
                    case "MonsterLevelTable":
                        SaveStandardCSV("MonsterLevelTable.csv", monsterLevelDataList);
                        break;
                    case "CardTable":
                        SaveSkillCSV("CardTable.csv", cardDataList);
                        break;
                    case "CardLevelTable":
                        SaveSkillCSV("CardLevelTable.csv", cardLevelDataList);
                        break;
                    case "CardListTable":
                        SaveSkillCSV("CardListTable.csv", cardListDataList);
                        break;
                }
            }

            modifiedTables.Clear();
            statusMessage = "All changes saved!";
            statusType = MessageType.Info;
            EditorUtility.DisplayDialog("Success", "All changes saved!", "OK");
        }
        catch (Exception e)
        {
            statusMessage = $"Save failed: {e.Message}";
            statusType = MessageType.Error;
            EditorUtility.DisplayDialog("Error", $"Failed: {e.Message}", "OK");
        }

        Repaint();
    }

    private void SaveStandardCSV<T>(string fileName, List<T> dataList)
    {
        string filePath = $"{CSV_PATH}/{fileName}";

        using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)))
        {
            csv.WriteRecords(dataList);
        }

        AssetDatabase.Refresh();
        Debug.Log($"[CSVBalancingTool] Saved {fileName}");
    }

    private void SaveSkillCSV<T>(string fileName, List<T> dataList)
    {
        string filePath = $"{CSV_PATH}/{fileName}";
        string tempFile = filePath + ".temp";

        string[] originalLines = File.ReadAllLines(filePath);
        if (originalLines.Length < 4)
        {
            throw new Exception("Invalid skill CSV format");
        }

        string koreanHeader = originalLines[0];
        string englishHeader = originalLines[1];
        string typeHeader = originalLines[2];

        using (var writer = new StreamWriter(tempFile, false, Encoding.UTF8))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)))
        {
            csv.WriteRecords(dataList);
        }

        string[] tempLines = File.ReadAllLines(tempFile);
        string[] dataRows = tempLines.Skip(1).ToArray();

        var finalLines = new List<string> { koreanHeader, englishHeader, typeHeader };
        finalLines.AddRange(dataRows);

        File.WriteAllLines(filePath, finalLines, Encoding.UTF8);
        File.Delete(tempFile);

        AssetDatabase.Refresh();
        Debug.Log($"[CSVBalancingTool] Saved {fileName}");
    }

    #endregion
}
