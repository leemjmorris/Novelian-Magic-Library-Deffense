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
        SkillCreator,
        VFXDatabase,
        CSVSync,
        CombinationRules,
        Preview,
        Test,
        ExternalAssets
    }
    private Tab currentTab = Tab.SkillCreator;
    private readonly string[] tabNames = { "스킬 제작", "VFX Database", "CSV 동기화", "조합 규칙", "프리뷰", "테스트", "외부 에셋" };
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
    // 신규 시스템: 3개 behavior_type + Legacy 호환
    private readonly string[] behaviorTypes = {
        "All", "Projectile", "BeamRay", "AOE",
        // Legacy (이전 스킬 데이터 호환용)
        "SingleProjectile", "ExplosiveProjectile", "FallingProjectile",
        "TargetAOE", "LinearAOE", "GroundAOE", "MovingAOE",
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
    private List<string> scanPaths = new List<string>();
    private Vector2 scanPathsScrollPosition;
    private int selectedExternalEffectIndex = -1;
    private bool showScriptBasedOnly = false;
    private string externalSearchFilter = "";

    // EditorPrefs 키
    private const string SCAN_PATHS_PREF_KEY = "SkillEditor_ScanPaths";

    // behavior_type 자동 매핑 - 신규 시스템: Projectile, BeamRay, AOE 3개 타입
    private static readonly Dictionary<string, string> EffectNameToBehaviorType = new Dictionary<string, string>
    {
        // AOE 타입 (지속형, 폭발형, 이동형 모두 AOE로 통합)
        { "tornado", "AOE" },
        { "storm", "AOE" },
        { "cyclone", "AOE" },
        { "blackhole", "AOE" },
        { "field", "AOE" },
        { "swamp", "AOE" },
        { "poison", "AOE" },
        { "timeField", "AOE" },
        { "nuke", "AOE" },
        { "explosion", "AOE" },
        { "boom", "AOE" },
        { "blast", "AOE" },
        { "slash", "AOE" },
        { "wave", "AOE" },
        { "orbital", "AOE" },
        { "strike", "AOE" },
        { "meteor", "AOE" },
        { "airstrike", "AOE" },
        { "fleet", "AOE" },
        { "satelite", "AOE" },

        // Beam 타입
        { "beam", "BeamRay" },
        { "laser", "BeamRay" },
        { "breath", "BeamRay" },
        { "ray", "BeamRay" },

        // Projectile 타입 (모든 투사체 통합)
        { "shot", "Projectile" },
        { "fire", "Projectile" },
        { "ball", "Projectile" },
        { "bullet", "Projectile" },
        { "fist", "Projectile" },
        { "arrow", "Projectile" },
        { "bolt", "Projectile" },
        { "missile", "Projectile" }
    };
    #endregion

    #region Styles
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialized = false;
    #endregion

    #region Skill Creator
    private enum CreatorMode { Create, Edit }
    private CreatorMode creatorMode = CreatorMode.Create;
    private int editingSkillIndex = -1;

    // 새 스킬 데이터
    private int newSkillId;
    private string newSkillName = "";
    private int newBehaviorTypeIndex = 0;
    private float newBaseDamage = 100f;
    private float newCooldown = 2f;
    private float newRange = 10f;
    private float newProjectileSpeed = 15f;
    private float newAoeRadius = 0f;
    private float newDuration = 0f;
    private string newDescription = "";
    private GameObject newVfxPrefab;
    private GameObject newHitPrefab;

    // Hit Effect 스케일 및 자동 범위 설정
    private float hitPrefabScale = 1f;
    private float baseHitRadius = 1f;  // 스케일 1일 때의 기준 반경 (사용자가 직접 설정)
    private bool autoRadiusFromHitEffect = false;
    private float estimatedHitRadius = 0f;

    private Vector2 creatorScrollPosition;
    private Vector2 skillListScrollPosition;

    // behavior_type 목록 (All 제외) - 신규 시스템: 3개만 사용
    private readonly string[] creatableBehaviorTypes = {
        "Projectile", "BeamRay", "AOE"
    };
    #endregion

    #region Combination Rules
    private SkillCombinationRuleData combinationRuleData;
    private Vector2 combinationRulesScrollPosition;
    private const string COMBINATION_RULES_ASSET_PATH = "Assets/Data/SkillCombinationRules.asset";

    // 서포트 타입 목록 - 신규 시스템: 8개 타입
    private readonly string[] supportTypes = {
        "Pierce", "Homing", "MultiShot", "CC", "DOT", "Enhance", "Bounce", "Split"
    };

    // 서포트 타입 한글명 (툴팁용)
    private readonly string[] supportTypeNames = {
        "관통", "유도", "다중발사", "군중제어", "도트", "강화", "바운스", "분열"
    };
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
        LoadScanPaths();
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
            case Tab.SkillCreator:
                DrawSkillCreatorTab();
                break;
            case Tab.VFXDatabase:
                DrawVFXDatabaseTab();
                break;
            case Tab.CSVSync:
                DrawCSVSyncTab();
                break;
            case Tab.CombinationRules:
                DrawCombinationRulesTab();
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

    #region Skill Creator Tab

    private void DrawSkillCreatorTab()
    {
        EditorGUILayout.BeginHorizontal();

        // 왼쪽: 스킬 목록
        EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(250));
        DrawSkillListPanel();
        EditorGUILayout.EndVertical();

        // 오른쪽: 스킬 편집 폼
        EditorGUILayout.BeginVertical(boxStyle);
        DrawSkillEditorPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillListPanel()
    {
        EditorGUILayout.LabelField("스킬 목록", headerStyle);
        EditorGUILayout.Space(5);

        // 새 스킬 버튼
        GUI.color = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("+ 새 스킬 만들기", GUILayout.Height(30)))
        {
            CreateNewSkill();
        }
        GUI.color = Color.white;

        EditorGUILayout.Space(10);

        // 스킬 목록
        if (!csvLoaded || csvSkills.Count == 0)
        {
            EditorGUILayout.HelpBox("CSV를 먼저 로드하세요.", MessageType.Info);
            if (GUILayout.Button("CSV 로드"))
            {
                LoadCSVData();
            }
            return;
        }

        EditorGUILayout.LabelField($"총 {csvSkills.Count}개 스킬", EditorStyles.miniLabel);

        skillListScrollPosition = EditorGUILayout.BeginScrollView(skillListScrollPosition);

        for (int i = 0; i < csvSkills.Count; i++)
        {
            var skill = csvSkills[i];
            bool isEditing = creatorMode == CreatorMode.Edit && editingSkillIndex == i;

            EditorGUILayout.BeginHorizontal(isEditing ? "selectionRect" : "box");

            // 스킬 ID와 이름
            GUI.color = isEditing ? Color.cyan : Color.white;
            if (GUILayout.Button($"[{skill.skill_id}] {skill.skill_name}", EditorStyles.label, GUILayout.Width(180)))
            {
                SelectSkillForEdit(i);
            }
            GUI.color = Color.white;

            // 삭제 버튼
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("스킬 삭제",
                    $"[{skill.skill_id}] {skill.skill_name}을(를) 삭제하시겠습니까?\n\n이 작업은 CSV에 즉시 반영됩니다.",
                    "삭제", "취소"))
                {
                    DeleteSkill(i);
                }
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSkillEditorPanel()
    {
        string title = creatorMode == CreatorMode.Create ? "새 스킬 만들기" : $"스킬 편집: [{newSkillId}] {newSkillName}";
        EditorGUILayout.LabelField(title, headerStyle);
        EditorGUILayout.Space(10);

        creatorScrollPosition = EditorGUILayout.BeginScrollView(creatorScrollPosition);

        // 기본 정보
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        // 스킬 ID
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("스킬 ID", GUILayout.Width(120));
        GUI.enabled = creatorMode == CreatorMode.Create;
        newSkillId = EditorGUILayout.IntField(newSkillId);
        if (creatorMode == CreatorMode.Create && GUILayout.Button("자동", GUILayout.Width(50)))
        {
            newSkillId = GetNextAvailableSkillId();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // ID 중복 체크
        if (creatorMode == CreatorMode.Create && IsSkillIdDuplicate(newSkillId))
        {
            EditorGUILayout.HelpBox($"ID {newSkillId}는 이미 사용 중입니다!", MessageType.Error);
        }

        // 스킬 이름
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("스킬명", GUILayout.Width(120));
        newSkillName = EditorGUILayout.TextField(newSkillName);
        EditorGUILayout.EndHorizontal();

        // behavior_type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("행동 타입", GUILayout.Width(120));
        newBehaviorTypeIndex = EditorGUILayout.Popup(newBehaviorTypeIndex, creatableBehaviorTypes);
        EditorGUILayout.EndHorizontal();

        // 설명
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("설명", GUILayout.Width(120));
        newDescription = EditorGUILayout.TextField(newDescription);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // 스탯 정보
        EditorGUILayout.LabelField("스탯", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        DrawStatField("기본 데미지", ref newBaseDamage, "스킬의 기본 데미지");
        DrawStatField("쿨다운 (초)", ref newCooldown, "스킬 재사용 대기시간");
        DrawStatField("사거리", ref newRange, "스킬 사용 가능 거리");

        // behavior_type에 따른 조건부 필드
        string selectedBehavior = creatableBehaviorTypes[newBehaviorTypeIndex];

        if (IsProjectileType(selectedBehavior))
        {
            DrawStatField("투사체 속도", ref newProjectileSpeed, "투사체 이동 속도");
        }

        if (IsAOEType(selectedBehavior))
        {
            DrawStatField("범위 반경", ref newAoeRadius, "AOE 효과 범위");
        }

        if (IsDurationType(selectedBehavior))
        {
            DrawStatField("지속시간 (초)", ref newDuration, "효과 지속 시간");
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // VFX 설정
        EditorGUILayout.LabelField("VFX 설정 (선택)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("VFX 프리팹", GUILayout.Width(120));
        newVfxPrefab = (GameObject)EditorGUILayout.ObjectField(newVfxPrefab, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

        // Hit 프리팹
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Hit 프리팹", GUILayout.Width(120));
        var prevHitPrefab = newHitPrefab;
        newHitPrefab = (GameObject)EditorGUILayout.ObjectField(newHitPrefab, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

        // Hit 프리팹이 있을 때만 추가 옵션 표시
        if (newHitPrefab != null)
        {
            EditorGUILayout.Space(5);

            // 기준 반경 슬라이더 (스케일 1일 때의 범위)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("기준 반경", GUILayout.Width(120));
            float prevBaseRadius = baseHitRadius;
            baseHitRadius = EditorGUILayout.Slider(baseHitRadius, 0.1f, 20f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("  ↳ 스케일 1일 때 이펙트와 일치하는 범위 설정", EditorStyles.miniLabel);

            EditorGUILayout.Space(3);

            // Hit Prefab 스케일 슬라이더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hit 스케일", GUILayout.Width(120));
            float prevScale = hitPrefabScale;
            hitPrefabScale = EditorGUILayout.Slider(hitPrefabScale, 0.1f, 50f);
            EditorGUILayout.EndHorizontal();

            // 범위 = 기준 반경 * 스케일
            estimatedHitRadius = baseHitRadius * hitPrefabScale;

            // 스케일 또는 기준 반경 변경 시 자동 저장
            bool valueChanged = Mathf.Abs(prevScale - hitPrefabScale) > 0.01f || Mathf.Abs(prevBaseRadius - baseHitRadius) > 0.01f;
            if (valueChanged && autoRadiusFromHitEffect && creatorMode == CreatorMode.Edit && editingSkillIndex >= 0)
            {
                newAoeRadius = estimatedHitRadius;
                AutoSaveCurrentSkill();
            }

            // 계산된 범위 표시
            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("계산된 범위", GUILayout.Width(120));
            GUI.enabled = false;
            EditorGUILayout.FloatField(estimatedHitRadius);
            GUI.enabled = true;
            EditorGUILayout.LabelField($"= {baseHitRadius:F1} × {hitPrefabScale:F1}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 자동 범위 설정 토글
            EditorGUILayout.BeginHorizontal();
            bool prevAutoRadius = autoRadiusFromHitEffect;
            autoRadiusFromHitEffect = EditorGUILayout.Toggle(autoRadiusFromHitEffect, GUILayout.Width(20));
            EditorGUILayout.LabelField("스케일에 맞춰 범위 반경 자동 설정 (Play Mode Hot Reload)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // 자동 설정이 활성화되면 범위 반경 동기화
            if (autoRadiusFromHitEffect && estimatedHitRadius > 0)
            {
                newAoeRadius = estimatedHitRadius;

                // 토글이 켜졌을 때도 자동 저장 (편집 모드일 경우)
                if (!prevAutoRadius && creatorMode == CreatorMode.Edit && editingSkillIndex >= 0)
                {
                    AutoSaveCurrentSkill();
                }
            }

            // 동기화 상태 표시
            if (autoRadiusFromHitEffect)
            {
                EditorGUILayout.HelpBox($"aoe_radius가 {estimatedHitRadius:F2} ({baseHitRadius:F1} × {hitPrefabScale:F1})로 자동 설정됩니다.\nPlay Mode에서 스케일/기준 반경 변경 시 자동으로 Hot Reload됩니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("토글을 켜면 이펙트 스케일과 데미지 범위가 동기화됩니다.\n1. 먼저 스케일 1에서 기준 반경을 이펙트와 맞춤\n2. 스케일 조절 시 범위도 같이 변경됨", MessageType.None);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // 저장 버튼
        DrawSaveButtons();
    }

    private void DrawStatField(string label, ref float value, string tooltip)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(120));
        value = EditorGUILayout.FloatField(value);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSaveButtons()
    {
        EditorGUILayout.BeginHorizontal();

        // 유효성 검사
        bool isValid = ValidateSkillData();

        GUI.enabled = isValid;
        GUI.color = new Color(0.5f, 0.8f, 1f);
        string saveButtonText = creatorMode == CreatorMode.Create ? "CSV에 저장" : "변경사항 저장";
        if (GUILayout.Button(saveButtonText, GUILayout.Height(35)))
        {
            SaveSkillToCSV();
        }
        GUI.color = Color.white;
        GUI.enabled = true;

        // 취소/초기화 버튼
        if (GUILayout.Button(creatorMode == CreatorMode.Create ? "초기화" : "편집 취소", GUILayout.Height(35), GUILayout.Width(100)))
        {
            if (creatorMode == CreatorMode.Edit)
            {
                creatorMode = CreatorMode.Create;
                editingSkillIndex = -1;
            }
            ResetSkillForm();
        }

        EditorGUILayout.EndHorizontal();

        if (!isValid)
        {
            EditorGUILayout.HelpBox("스킬 ID와 스킬명을 입력하세요.", MessageType.Warning);
        }
    }

    private bool ValidateSkillData()
    {
        if (newSkillId <= 0) return false;
        if (string.IsNullOrWhiteSpace(newSkillName)) return false;
        if (creatorMode == CreatorMode.Create && IsSkillIdDuplicate(newSkillId)) return false;
        return true;
    }

    private bool IsSkillIdDuplicate(int skillId)
    {
        for (int i = 0; i < csvSkills.Count; i++)
        {
            if (csvSkills[i].skill_id == skillId)
            {
                // 편집 모드에서 자기 자신은 중복이 아님
                if (creatorMode == CreatorMode.Edit && editingSkillIndex == i)
                    return false;
                return true;
            }
        }
        return false;
    }

    private int GetNextAvailableSkillId()
    {
        if (csvSkills.Count == 0) return 1001;

        // 현재 선택된 behavior_type에 맞는 ID 범위 찾기
        string selectedBehavior = creatableBehaviorTypes[newBehaviorTypeIndex];
        int baseId = GetBaseIdForBehaviorType(selectedBehavior);

        // 해당 범위에서 사용 가능한 다음 ID 찾기
        var usedIds = csvSkills.Select(s => s.skill_id).ToHashSet();
        for (int id = baseId; id < baseId + 100; id++)
        {
            if (!usedIds.Contains(id))
                return id;
        }

        // 범위 내에 없으면 전체에서 최대값 + 1
        return csvSkills.Max(s => s.skill_id) + 1;
    }

    private int GetBaseIdForBehaviorType(string behaviorType)
    {
        return behaviorType switch
        {
            "SingleProjectile" => 1001,
            "ExplosiveProjectile" => 1101,
            "FallingProjectile" => 1201,
            "BeamRay" => 1201,
            "TargetAOE" => 1301,
            "LinearAOE" => 1401,
            "GroundAOE" => 1501,
            "MovingAOE" => 1501,
            "Barrier" => 1601,
            "Buff" => 1701,
            "Debuff" => 1801,
            "Trap" => 1901,
            "Instant" => 2001,
            _ => 1001
        };
    }

    private bool IsProjectileType(string behaviorType)
    {
        return behaviorType == "SingleProjectile" || behaviorType == "ExplosiveProjectile" ||
               behaviorType == "FallingProjectile";
    }

    private bool IsAOEType(string behaviorType)
    {
        return behaviorType == "ExplosiveProjectile" || behaviorType == "TargetAOE" ||
               behaviorType == "LinearAOE" || behaviorType == "GroundAOE" || behaviorType == "MovingAOE";
    }

    private bool IsDurationType(string behaviorType)
    {
        return behaviorType == "BeamRay" || behaviorType == "GroundAOE" ||
               behaviorType == "Barrier" || behaviorType == "Buff" || behaviorType == "Debuff";
    }

    /// <summary>
    /// 이펙트 프리팹의 실제 범위(반경)를 계산합니다.
    /// ParticleSystem, Renderer, Collider를 분석하여 가장 큰 범위를 반환합니다.
    /// </summary>
    private float CalculateEffectRadius(GameObject prefab, float scale)
    {
        if (prefab == null) return 0f;

        float maxRadius = 0f;

        // 1. ParticleSystem에서 크기 추출
        var particles = prefab.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            var main = ps.main;
            var shape = ps.shape;

            // Shape 모듈의 반경 확인
            if (shape.enabled)
            {
                float shapeRadius = 0f;
                switch (shape.shapeType)
                {
                    case ParticleSystemShapeType.Sphere:
                    case ParticleSystemShapeType.Hemisphere:
                        shapeRadius = shape.radius;
                        break;
                    case ParticleSystemShapeType.Circle:
                        shapeRadius = shape.radius;
                        break;
                    case ParticleSystemShapeType.Cone:
                    case ParticleSystemShapeType.ConeVolume:
                        shapeRadius = shape.radius;
                        break;
                    case ParticleSystemShapeType.Box:
                        shapeRadius = Mathf.Max(shape.scale.x, shape.scale.z) * 0.5f;
                        break;
                }

                // 파티클 시작 크기도 고려
                float startSize = main.startSize.constantMax;
                maxRadius = Mathf.Max(maxRadius, shapeRadius + startSize * 0.5f);
            }
        }

        // 2. Renderer Bounds에서 크기 추출
        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            // ParticleSystemRenderer는 런타임에만 정확한 bounds를 가지므로 제외
            if (renderer is ParticleSystemRenderer) continue;

            var bounds = renderer.bounds;
            float boundsRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            maxRadius = Mathf.Max(maxRadius, boundsRadius);
        }

        // 3. Collider에서 크기 추출 (있다면)
        var colliders = prefab.GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
        {
            if (collider is SphereCollider sphere)
            {
                maxRadius = Mathf.Max(maxRadius, sphere.radius);
            }
            else if (collider is CapsuleCollider capsule)
            {
                maxRadius = Mathf.Max(maxRadius, capsule.radius);
            }
            else if (collider is BoxCollider box)
            {
                maxRadius = Mathf.Max(maxRadius, Mathf.Max(box.size.x, box.size.z) * 0.5f);
            }
        }

        // 4. 기본값 (아무것도 없을 경우)
        if (maxRadius <= 0f)
        {
            // 프리팹의 루트 스케일 기반으로 추정
            maxRadius = Mathf.Max(prefab.transform.localScale.x, prefab.transform.localScale.z);
            if (maxRadius <= 0f) maxRadius = 1f;
        }

        // 스케일 적용
        return maxRadius * scale;
    }

    private void CreateNewSkill()
    {
        creatorMode = CreatorMode.Create;
        editingSkillIndex = -1;
        ResetSkillForm();
        newSkillId = GetNextAvailableSkillId();
    }

    private void SelectSkillForEdit(int index)
    {
        creatorMode = CreatorMode.Edit;
        editingSkillIndex = index;

        var skill = csvSkills[index];
        newSkillId = skill.skill_id;
        newSkillName = skill.skill_name;
        newBehaviorTypeIndex = System.Array.IndexOf(creatableBehaviorTypes, skill.behavior_type);
        if (newBehaviorTypeIndex < 0) newBehaviorTypeIndex = 0;
        newBaseDamage = skill.base_damage;
        newCooldown = skill.cooldown;
        newRange = skill.range;
        newProjectileSpeed = skill.projectile_speed;
        newAoeRadius = skill.aoe_radius;
        newDuration = skill.duration;
        newDescription = skill.description ?? "";

        // VFX 로드
        if (vfxDatabase != null)
        {
            var entry = vfxDatabase.GetEntry(skill.skill_id);
            if (entry != null)
            {
                newVfxPrefab = entry.vfxPrefab;
                newHitPrefab = entry.hitPrefab;
                hitPrefabScale = entry.hitScale > 0 ? entry.hitScale : 1f;
                baseHitRadius = entry.baseRadius > 0 ? entry.baseRadius : 1f;

                // hitPrefab이 있으면 예상 반경 계산 (기준 반경 * 스케일)
                if (newHitPrefab != null)
                {
                    estimatedHitRadius = baseHitRadius * hitPrefabScale;
                }
            }
            else
            {
                newVfxPrefab = null;
                newHitPrefab = null;
                hitPrefabScale = 1f;
                baseHitRadius = 1f;
                estimatedHitRadius = 0f;
            }
        }

        // 자동 범위 설정 토글은 스킬 선택 시 기본값 off
        autoRadiusFromHitEffect = false;
    }

    private void ResetSkillForm()
    {
        newSkillId = GetNextAvailableSkillId();
        newSkillName = "";
        newBehaviorTypeIndex = 0;
        newBaseDamage = 100f;
        newCooldown = 2f;
        newRange = 10f;
        newProjectileSpeed = 15f;
        newAoeRadius = 0f;
        newDuration = 0f;
        newDescription = "";
        newVfxPrefab = null;
        newHitPrefab = null;

        // Hit Effect 관련 필드 초기화
        hitPrefabScale = 1f;
        baseHitRadius = 1f;
        autoRadiusFromHitEffect = false;
        estimatedHitRadius = 0f;
    }

    private void DeleteSkill(int index)
    {
        // 백업 생성
        CreateCSVBackup();

        csvSkills.RemoveAt(index);
        SaveAllSkillsToCSV();

        if (editingSkillIndex == index)
        {
            creatorMode = CreatorMode.Create;
            editingSkillIndex = -1;
            ResetSkillForm();
        }
        else if (editingSkillIndex > index)
        {
            editingSkillIndex--;
        }

        Debug.Log("[SkillEditor] 스킬이 삭제되었습니다.");
    }

    private void SaveSkillToCSV()
    {
        // 백업 생성
        CreateCSVBackup();

        string selectedBehavior = creatableBehaviorTypes[newBehaviorTypeIndex];

        if (creatorMode == CreatorMode.Create)
        {
            // 새 스킬 추가
            var newSkill = new MainSkillCSVEntry
            {
                skill_id = newSkillId,
                skill_name = newSkillName,
                behavior_type = selectedBehavior,
                base_damage = newBaseDamage,
                cooldown = newCooldown,
                range = newRange,
                projectile_speed = newProjectileSpeed,
                aoe_radius = newAoeRadius,
                duration = newDuration,
                description = newDescription
            };
            csvSkills.Add(newSkill);

            Debug.Log($"[SkillEditor] 새 스킬 추가됨: [{newSkillId}] {newSkillName}");
        }
        else
        {
            // 기존 스킬 수정
            var skill = csvSkills[editingSkillIndex];
            skill.skill_name = newSkillName;
            skill.behavior_type = selectedBehavior;
            skill.base_damage = newBaseDamage;
            skill.cooldown = newCooldown;
            skill.range = newRange;
            skill.projectile_speed = newProjectileSpeed;
            skill.aoe_radius = newAoeRadius;
            skill.duration = newDuration;
            skill.description = newDescription;

            Debug.Log($"[SkillEditor] 스킬 수정됨: [{newSkillId}] {newSkillName}");
        }

        // CSV 파일에 저장
        SaveAllSkillsToCSV();

        // VFX 할당 (hitScale 포함)
        if (newVfxPrefab != null || newHitPrefab != null || hitPrefabScale != 1f)
        {
            AssignVFXToSkill(newSkillId, newVfxPrefab, newHitPrefab, hitPrefabScale, baseHitRadius);
        }

        // 폼 초기화
        if (creatorMode == CreatorMode.Create)
        {
            ResetSkillForm();
        }

        EditorUtility.DisplayDialog("저장 완료", "스킬이 CSV에 저장되었습니다.", "확인");
    }

    /// <summary>
    /// 스케일/범위 변경 시 자동 저장 (Play Mode Hot Reload용)
    /// 대화상자 없이 즉시 저장
    /// </summary>
    private void AutoSaveCurrentSkill()
    {
        if (creatorMode != CreatorMode.Edit || editingSkillIndex < 0 || editingSkillIndex >= csvSkills.Count)
            return;

        // 현재 편집 중인 스킬의 aoe_radius 업데이트
        var skill = csvSkills[editingSkillIndex];
        skill.aoe_radius = newAoeRadius;

        // CSV 파일에 저장 (백업 없이 빠르게)
        SaveAllSkillsToCSV();

        // VFXDatabase에 hitScale, baseRadius 저장
        if (newHitPrefab != null || hitPrefabScale != 1f || baseHitRadius != 1f)
        {
            AssignVFXToSkill(skill.skill_id, newVfxPrefab, newHitPrefab, hitPrefabScale, baseHitRadius);
        }

        Debug.Log($"[SkillEditor] 자동 저장됨: [{skill.skill_id}] {skill.skill_name} - aoe_radius: {newAoeRadius:F2}, hitScale: {hitPrefabScale:F2}, baseRadius: {baseHitRadius:F2}");
    }

    private void CreateCSVBackup()
    {
        string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), csvPath);
        if (File.Exists(fullPath))
        {
            string backupPath = fullPath + ".backup";
            File.Copy(fullPath, backupPath, true);
            Debug.Log($"[SkillEditor] 백업 생성됨: {backupPath}");
        }
    }

    private void SaveAllSkillsToCSV()
    {
        string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), csvPath);

        using (StreamWriter writer = new StreamWriter(fullPath, false, System.Text.Encoding.UTF8))
        {
            // 헤더 (한글 주석)
            writer.WriteLine("//스킬ID,//스킬명,행동타입,기본데미지,쿨다운,사거리,투사체속도,범위반경,지속시간,//설명");
            // 헤더 (영문)
            writer.WriteLine("skill_id,//skill_name,behavior_type,base_damage,cooldown,range,projectile_speed,aoe_radius,duration,//description");
            // 타입 정의
            writer.WriteLine("int,//string,string,float,float,float,float,float,float,//string");

            // 스킬 데이터
            foreach (var skill in csvSkills.OrderBy(s => s.skill_id))
            {
                string line = $"{skill.skill_id},{skill.skill_name},{skill.behavior_type}," +
                              $"{skill.base_damage},{skill.cooldown},{skill.range}," +
                              $"{skill.projectile_speed},{skill.aoe_radius},{skill.duration}," +
                              $"{skill.description}";
                writer.WriteLine(line);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[SkillEditor] CSV 저장됨: {csvPath}");
    }

    private void AssignVFXToSkill(int skillId, GameObject vfxPrefab, GameObject hitPrefab, float hitScale = 1f, float baseRadius = 1f)
    {
        if (vfxDatabase == null) return;

        var field = typeof(SkillVFXDatabase).GetField("entries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entries = field?.GetValue(vfxDatabase) as List<SkillVFXDatabase.Entry>;

        if (entries == null) return;

        var entry = entries.FirstOrDefault(e => e.skillId == skillId);
        if (entry == null)
        {
            entry = new SkillVFXDatabase.Entry { skillId = skillId };
            entries.Add(entry);
        }

        if (vfxPrefab != null) entry.vfxPrefab = vfxPrefab;
        if (hitPrefab != null) entry.hitPrefab = hitPrefab;
        if (hitScale > 0) entry.hitScale = hitScale;
        if (baseRadius > 0) entry.baseRadius = baseRadius;

        EditorUtility.SetDirty(vfxDatabase);
        AssetDatabase.SaveAssets();
        vfxDatabase.RefreshCache();
        Debug.Log($"[SkillEditor] VFX 할당됨: [{skillId}] hitScale: {hitScale}, baseRadius: {baseRadius}");
    }

    #endregion

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
                if (headerIndex.TryGetValue("cooldown", out int cdIdx))
                    float.TryParse(GetValue(values, cdIdx), out entry.cooldown);
                if (headerIndex.TryGetValue("range", out int rangeIdx))
                    float.TryParse(GetValue(values, rangeIdx), out entry.range);
                if (headerIndex.TryGetValue("projectile_speed", out int speedIdx))
                    float.TryParse(GetValue(values, speedIdx), out entry.projectile_speed);
                if (headerIndex.TryGetValue("aoe_radius", out int aoeIdx))
                    float.TryParse(GetValue(values, aoeIdx), out entry.aoe_radius);
                if (headerIndex.TryGetValue("duration", out int durIdx))
                    float.TryParse(GetValue(values, durIdx), out entry.duration);
                if (headerIndex.TryGetValue("description", out int descIdx))
                    entry.description = GetValue(values, descIdx);

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
        public float cooldown;
        public float range;
        public float projectile_speed;
        public float aoe_radius;
        public float duration;
        public string description = "";
    }

    #endregion

    #region External Assets Tab

    private void DrawExternalAssetsTab()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("외부 VFX 에셋 관리", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "VFX 에셋 폴더를 드래그 앤 드롭으로 추가하세요.\n" +
            "• NotScriptBased: VFXDatabase에 직접 등록\n" +
            "• ScriptBased: SkillVFXContainer로 래핑 후 등록",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 스캔 경로 관리 섹션
        DrawScanPathsSection();

        EditorGUILayout.Space(10);

        // 스캔 버튼
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = scanPaths.Count > 0;
        if (GUILayout.Button("에셋 스캔", GUILayout.Height(25)))
        {
            ScanExternalAssets();
        }
        GUI.enabled = true;
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

    #region Scan Paths Management

    private void DrawScanPathsSection()
    {
        EditorGUILayout.LabelField("스캔 경로", EditorStyles.boldLabel);

        // 드래그 앤 드롭 영역
        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "폴더를 여기에 드래그 앤 드롭", EditorStyles.helpBox);

        // 드래그 앤 드롭 이벤트 처리
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        string path = AssetDatabase.GetAssetPath(draggedObject);
                        if (AssetDatabase.IsValidFolder(path) && !scanPaths.Contains(path))
                        {
                            scanPaths.Add(path);
                            SaveScanPaths();
                            externalAssetsLoaded = false; // 재스캔 필요
                        }
                    }
                }
                evt.Use();
                break;
        }

        EditorGUILayout.Space(5);

        // 경로 목록 표시
        if (scanPaths.Count > 0)
        {
            scanPathsScrollPosition = EditorGUILayout.BeginScrollView(scanPathsScrollPosition, GUILayout.Height(Mathf.Min(100, scanPaths.Count * 22 + 10)));

            for (int i = scanPaths.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();

                // 폴더 아이콘과 경로
                EditorGUILayout.LabelField(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(20));
                EditorGUILayout.LabelField(scanPaths[i], EditorStyles.miniLabel);

                // 폴더 열기 버튼
                if (GUILayout.Button("↗", GUILayout.Width(25)))
                {
                    var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scanPaths[i]);
                    if (folder != null)
                    {
                        EditorGUIUtility.PingObject(folder);
                        Selection.activeObject = folder;
                    }
                }

                // 삭제 버튼
                GUI.color = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    scanPaths.RemoveAt(i);
                    SaveScanPaths();
                    externalAssetsLoaded = false;
                }
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("스캔할 폴더가 없습니다. 위 영역에 폴더를 드래그하세요.", MessageType.Info);
        }

        // 경로 관리 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("폴더 추가...", GUILayout.Height(20)))
        {
            string path = EditorUtility.OpenFolderPanel("VFX 에셋 폴더 선택", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                if (!scanPaths.Contains(relativePath))
                {
                    scanPaths.Add(relativePath);
                    SaveScanPaths();
                    externalAssetsLoaded = false;
                }
            }
        }

        GUI.color = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("전체 삭제", GUILayout.Width(70), GUILayout.Height(20)))
        {
            if (EditorUtility.DisplayDialog("확인", "모든 스캔 경로를 삭제하시겠습니까?", "예", "아니오"))
            {
                scanPaths.Clear();
                SaveScanPaths();
                externalAssetsLoaded = false;
            }
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void LoadScanPaths()
    {
        scanPaths.Clear();
        string savedPaths = EditorPrefs.GetString(SCAN_PATHS_PREF_KEY, "");
        if (!string.IsNullOrEmpty(savedPaths))
        {
            string[] paths = savedPaths.Split('|');
            foreach (string path in paths)
            {
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                {
                    scanPaths.Add(path);
                }
            }
        }
    }

    private void SaveScanPaths()
    {
        string combined = string.Join("|", scanPaths);
        EditorPrefs.SetString(SCAN_PATHS_PREF_KEY, combined);
    }

    #endregion

    private void ScanExternalAssets()
    {
        externalEffects.Clear();
        externalAssetsLoaded = false;

        if (scanPaths.Count == 0)
        {
            Debug.LogWarning("[SkillEditor] 스캔할 폴더가 없습니다. 폴더를 추가하세요.");
            return;
        }

        // 유효한 경로만 필터링
        var validPaths = scanPaths.Where(p => AssetDatabase.IsValidFolder(p)).ToArray();
        if (validPaths.Length == 0)
        {
            Debug.LogWarning("[SkillEditor] 유효한 스캔 경로가 없습니다.");
            return;
        }

        // 모든 경로에서 프리팹 검색
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", validPaths);

        // 중복 제거를 위한 HashSet
        var processedPaths = new HashSet<string>();

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // 중복 체크
            if (processedPaths.Contains(path)) continue;
            processedPaths.Add(path);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            // Base 프리팹은 제외 (서브 컴포넌트)
            if (path.Contains("(Base)") || path.Contains("_Base")) continue;

            // VFX 관련 프리팹인지 확인 (ParticleSystem 또는 TrailRenderer 포함)
            bool hasVFXComponents = prefab.GetComponentInChildren<ParticleSystem>(true) != null ||
                                    prefab.GetComponentInChildren<TrailRenderer>(true) != null ||
                                    prefab.GetComponentInChildren<LineRenderer>(true) != null;

            // VFX 관련 이름 패턴 확인
            string lowerPath = path.ToLower();
            bool hasVFXPath = lowerPath.Contains("vfx") || lowerPath.Contains("effect") ||
                              lowerPath.Contains("fx") || lowerPath.Contains("particle") ||
                              lowerPath.Contains("skill") || lowerPath.Contains("magic") ||
                              lowerPath.Contains("spell");

            // VFX 컴포넌트나 VFX 경로가 없으면 스킵
            if (!hasVFXComponents && !hasVFXPath) continue;

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

                // 표준 Unity 컴포넌트 및 우리 컴포넌트 제외
                if (typeName == "Transform" || typeName == "ParticleSystem" ||
                    typeName == "Animator" || typeName == "AudioSource" ||
                    typeName == "SkillVFXContainer" || typeName == "SkillProjectile") continue;

                if (!effectInfo.scripts.Contains(typeName))
                {
                    effectInfo.scripts.Add(typeName);
                }
            }

            effectInfo.hasScripts = effectInfo.scripts.Count > 0;

            // ScriptBased 판단 - 경로에 없어도 스크립트가 있으면 ScriptBased로 판단
            if (!effectInfo.isScriptBased && effectInfo.hasScripts)
            {
                effectInfo.isScriptBased = true;
            }

            // behavior_type 추천
            effectInfo.suggestedBehaviorType = GuessBehaviorType(prefab.name);

            // 데이터베이스 등록 여부 확인
            effectInfo.isAddedToDatabase = CheckIfAddedToDatabase(prefab);

            externalEffects.Add(effectInfo);
        }

        externalEffects = externalEffects.OrderBy(e => e.name).ToList();
        externalAssetsLoaded = true;
        Debug.Log($"[SkillEditor] 외부 에셋 스캔 완료: {externalEffects.Count}개 ({validPaths.Length}개 폴더)");
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

    #region Combination Rules Tab

    private void DrawCombinationRulesTab()
    {
        EditorGUILayout.LabelField("메인 스킬 × 서포트 스킬 조합 규칙", headerStyle);
        EditorGUILayout.Space(5);

        // 에셋 로드/생성
        if (combinationRuleData == null)
        {
            combinationRuleData = AssetDatabase.LoadAssetAtPath<SkillCombinationRuleData>(COMBINATION_RULES_ASSET_PATH);
        }

        if (combinationRuleData == null)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("규칙 데이터 생성", GUILayout.Height(25)))
            {
                CreateCombinationRuleAsset();
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("조합 규칙 데이터가 없습니다. '규칙 데이터 생성' 버튼을 눌러 새로 만드세요.", MessageType.Info);
            return;
        }

        // CSV 로드/저장 버튼 (첫 번째 줄)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("CSV 파일:", GUILayout.Width(60));
        EditorGUILayout.SelectableLabel(SkillCombinationRuleData.CSV_PATH, EditorStyles.textField, GUILayout.Height(18));

        GUI.color = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("CSV 로드", GUILayout.Width(80), GUILayout.Height(20)))
        {
            LoadCombinationRulesFromCSV();
        }
        GUI.color = Color.white;

        GUI.color = new Color(1f, 0.85f, 0.6f);
        if (GUILayout.Button("CSV 저장", GUILayout.Width(80), GUILayout.Height(20)))
        {
            SaveCombinationRulesToCSV();
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 에셋 저장 및 기타 버튼 (두 번째 줄)
        EditorGUILayout.BeginHorizontal();

        // 에셋 저장 버튼
        GUI.color = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("에셋 저장", GUILayout.Width(80), GUILayout.Height(25)))
        {
            SaveCombinationRules();
        }
        GUI.color = Color.white;

        // 기본값 초기화 버튼
        GUI.color = new Color(1f, 0.8f, 0.5f);
        if (GUILayout.Button("기본값으로 초기화", GUILayout.Width(120), GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("기본값 초기화",
                "모든 조합 규칙을 기본값으로 초기화하시겠습니까?\n\n현재 설정은 덮어씌워집니다.",
                "초기화", "취소"))
            {
                InitializeDefaultCombinationRules();
            }
        }
        GUI.color = Color.white;

        // 모두 선택 버튼
        if (GUILayout.Button("모두 선택", GUILayout.Width(80), GUILayout.Height(25)))
        {
            SetAllCombinationRules(true);
        }

        // 모두 해제 버튼
        if (GUILayout.Button("모두 해제", GUILayout.Width(80), GUILayout.Height(25)))
        {
            SetAllCombinationRules(false);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 체크박스 그리드 표시
        DrawCombinationRulesGrid();
    }

    private void DrawCombinationRulesGrid()
    {
        // 셀 크기 설정
        float labelWidth = 130f;
        float cellWidth = 45f;
        float cellHeight = 22f;
        float headerHeight = 80f;

        combinationRulesScrollPosition = EditorGUILayout.BeginScrollView(combinationRulesScrollPosition);

        // 헤더 행 (서포트 타입)
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(labelWidth); // 좌측 빈 공간

        for (int i = 0; i < supportTypes.Length; i++)
        {
            // 세로 텍스트 표시를 위해 Matrix 회전 사용
            Rect cellRect = GUILayoutUtility.GetRect(cellWidth, headerHeight);

            // 배경색 설정 (카테고리별)
            Color bgColor = GetSupportCategoryColor(i);
            EditorGUI.DrawRect(cellRect, bgColor);

            // 세로 텍스트 그리기
            Matrix4x4 matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(-60f, new Vector2(cellRect.x + cellWidth / 2, cellRect.y + headerHeight / 2));

            GUIStyle verticalStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10
            };

            string labelText = $"{supportTypeNames[i]}\n({supportTypes[i]})";
            GUI.Label(new Rect(cellRect.x - 10, cellRect.y + 20, 100, 40), labelText, verticalStyle);

            GUI.matrix = matrixBackup;
        }
        EditorGUILayout.EndHorizontal();

        // 각 behavior_type 행
        for (int row = 0; row < creatableBehaviorTypes.Length; row++)
        {
            string behaviorType = creatableBehaviorTypes[row];

            EditorGUILayout.BeginHorizontal();

            // behavior_type 라벨
            Color rowColor = GetBehaviorCategoryColor(row);
            Rect labelRect = GUILayoutUtility.GetRect(labelWidth, cellHeight);
            EditorGUI.DrawRect(labelRect, rowColor);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };
            GUI.Label(labelRect, " " + behaviorType, labelStyle);

            // 각 서포트 타입에 대한 체크박스
            for (int col = 0; col < supportTypes.Length; col++)
            {
                string supportType = supportTypes[col];
                bool isAllowed = combinationRuleData.GetRule(behaviorType, supportType);

                Rect checkRect = GUILayoutUtility.GetRect(cellWidth, cellHeight);

                // 배경색
                Color cellColor = isAllowed ? new Color(0.3f, 0.7f, 0.3f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.2f);
                EditorGUI.DrawRect(checkRect, cellColor);

                // 체크박스 (중앙 정렬)
                Rect toggleRect = new Rect(checkRect.x + (cellWidth - 16) / 2, checkRect.y + (cellHeight - 16) / 2, 16, 16);
                bool newValue = EditorGUI.Toggle(toggleRect, isAllowed);

                if (newValue != isAllowed)
                {
                    combinationRuleData.SetRule(behaviorType, supportType, newValue);
                    EditorUtility.SetDirty(combinationRuleData);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // 범례
        EditorGUILayout.Space(10);
        DrawCombinationRulesLegend();
    }

    private void DrawCombinationRulesLegend()
    {
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField("범례:", EditorStyles.boldLabel, GUILayout.Width(50));

        // 서포트 카테고리 범례
        DrawLegendItem("투사체", new Color(0.4f, 0.6f, 0.9f, 0.5f));
        DrawLegendItem("CC", new Color(0.9f, 0.5f, 0.5f, 0.5f));
        DrawLegendItem("스탯", new Color(0.5f, 0.9f, 0.5f, 0.5f));
        DrawLegendItem("AOE", new Color(0.9f, 0.7f, 0.4f, 0.5f));

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLegendItem(string label, Color color)
    {
        Rect rect = GUILayoutUtility.GetRect(60, 18);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + 2, 14, 14), color);
        GUI.Label(new Rect(rect.x + 18, rect.y, 50, 18), label, EditorStyles.miniLabel);
    }

    private Color GetSupportCategoryColor(int index)
    {
        // 0-4: 투사체 (Chain, Pierce, Split, Homing, MultiShot)
        // 5-9: CC (Slow, Stun, DOT, Knockback, PullIn)
        // 10-12: 스탯 (AreaUp, DamageUp, CooldownDown)
        // 13-15: AOE (Linger, Moving, Expand)

        if (index < 5) return new Color(0.4f, 0.6f, 0.9f, 0.3f);  // 투사체 - 파랑
        if (index < 10) return new Color(0.9f, 0.5f, 0.5f, 0.3f); // CC - 빨강
        if (index < 13) return new Color(0.5f, 0.9f, 0.5f, 0.3f); // 스탯 - 초록
        return new Color(0.9f, 0.7f, 0.4f, 0.3f);                  // AOE - 주황
    }

    private Color GetBehaviorCategoryColor(int index)
    {
        string behaviorType = creatableBehaviorTypes[index];

        // 투사체 타입
        if (behaviorType.Contains("Projectile"))
            return new Color(0.4f, 0.6f, 0.9f, 0.2f);

        // 빔 타입
        if (behaviorType == "BeamRay")
            return new Color(0.7f, 0.4f, 0.9f, 0.2f);

        // AOE 타입
        if (behaviorType.Contains("AOE"))
            return new Color(0.9f, 0.7f, 0.4f, 0.2f);

        // 유틸리티 타입
        return new Color(0.5f, 0.5f, 0.5f, 0.2f);
    }

    private void CreateCombinationRuleAsset()
    {
        // 폴더 확인/생성
        string folderPath = Path.GetDirectoryName(COMBINATION_RULES_ASSET_PATH);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }

        // 에셋 생성
        combinationRuleData = ScriptableObject.CreateInstance<SkillCombinationRuleData>();
        combinationRuleData.InitializeDefaultRules(creatableBehaviorTypes, supportTypes);

        AssetDatabase.CreateAsset(combinationRuleData, COMBINATION_RULES_ASSET_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[스킬 에디터] 조합 규칙 데이터 생성됨: {COMBINATION_RULES_ASSET_PATH}");
    }

    private void SaveCombinationRules()
    {
        if (combinationRuleData == null) return;

        EditorUtility.SetDirty(combinationRuleData);
        AssetDatabase.SaveAssets();

        Debug.Log("[스킬 에디터] 조합 규칙이 저장되었습니다.");
    }

    private void InitializeDefaultCombinationRules()
    {
        if (combinationRuleData == null) return;

        combinationRuleData.InitializeDefaultRules(creatableBehaviorTypes, supportTypes);
        EditorUtility.SetDirty(combinationRuleData);
        AssetDatabase.SaveAssets();

        Debug.Log("[스킬 에디터] 조합 규칙이 기본값으로 초기화되었습니다.");
    }

    private void SetAllCombinationRules(bool value)
    {
        if (combinationRuleData == null) return;

        foreach (var behaviorType in creatableBehaviorTypes)
        {
            foreach (var supportType in supportTypes)
            {
                combinationRuleData.SetRule(behaviorType, supportType, value);
            }
        }

        EditorUtility.SetDirty(combinationRuleData);
    }

    private void LoadCombinationRulesFromCSV()
    {
        if (combinationRuleData == null) return;

        if (combinationRuleData.LoadFromCSV())
        {
            EditorUtility.SetDirty(combinationRuleData);
            AssetDatabase.SaveAssets();
            Repaint();
        }
    }

    private void SaveCombinationRulesToCSV()
    {
        if (combinationRuleData == null) return;

        if (combinationRuleData.SaveToCSV(supportTypes))
        {
            AssetDatabase.Refresh();
        }
    }

    #endregion
}

/// <summary>
/// 스킬 - VFX 매핑 도우미 윈도우
/// 드래그 앤 드롭으로 스킬에 VFX 프리팹 할당
/// </summary>
public class SkillVFXMappingWindow : EditorWindow
{
    #region Data
    private SkillVFXDatabase database;
    private List<SkillMappingEntry> skillEntries = new List<SkillMappingEntry>();
    private Vector2 scrollPos;
    private string skillSearchFilter = "";
    private string behaviorTypeFilter = "All";
    private bool showUnmappedOnly = false;

    // 신규 시스템: 3개 behavior_type + Legacy 호환
    private readonly string[] behaviorTypes = {
        "All", "Projectile", "BeamRay", "AOE",
        // Legacy (이전 스킬 데이터 호환용)
        "SingleProjectile", "ExplosiveProjectile", "FallingProjectile",
        "TargetAOE", "LinearAOE", "GroundAOE", "MovingAOE",
        "Barrier", "Buff", "Debuff", "Trap", "Instant"
    };

    [Serializable]
    private class SkillMappingEntry
    {
        public int skillId;
        public string skillName;
        public string behaviorType;
        public float baseDamage;
        public GameObject vfxPrefab;
        public GameObject hitPrefab;
        public bool hasVFX;
    }
    #endregion

    #region Styles
    private GUIStyle headerStyle;
    private GUIStyle dropAreaStyle;
    private bool stylesInitialized = false;
    #endregion

    public static void ShowWindow(SkillVFXDatabase db, object csvSkills, object externalEffects)
    {
        var window = GetWindow<SkillVFXMappingWindow>("VFX 매핑 도우미");
        window.database = db;
        window.minSize = new Vector2(700, 500);

        // CSV 스킬 데이터 복사
        if (csvSkills is System.Collections.IList list)
        {
            window.skillEntries.Clear();
            foreach (var item in list)
            {
                // Reflection으로 private class 접근
                var type = item.GetType();
                var entry = new SkillMappingEntry
                {
                    skillId = (int)type.GetField("skill_id").GetValue(item),
                    skillName = (string)type.GetField("skill_name").GetValue(item) ?? "",
                    behaviorType = (string)type.GetField("behavior_type").GetValue(item) ?? "",
                    baseDamage = (float)type.GetField("base_damage").GetValue(item)
                };

                // 현재 VFX 할당 상태 확인
                if (db != null)
                {
                    var dbEntry = db.GetEntry(entry.skillId);
                    if (dbEntry != null)
                    {
                        entry.vfxPrefab = dbEntry.vfxPrefab;
                        entry.hitPrefab = dbEntry.hitPrefab;
                        entry.hasVFX = dbEntry.vfxPrefab != null;
                    }
                }

                window.skillEntries.Add(entry);
            }
        }

        window.Show();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        dropAreaStyle = new GUIStyle("box")
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic
        };

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();

        EditorGUILayout.LabelField("스킬 - VFX 매핑 도우미", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "VFX 프리팹을 드래그하여 스킬의 드롭 영역에 놓으세요.\n" +
            "Project 창에서 프리팹을 직접 드래그할 수 있습니다.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 필터 섹션
        DrawFilterSection();

        EditorGUILayout.Space(10);

        // 스킬 목록
        DrawSkillList();

        EditorGUILayout.Space(10);

        // 하단 버튼
        DrawBottomButtons();
    }

    private void DrawFilterSection()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("검색:", GUILayout.Width(40));
        skillSearchFilter = EditorGUILayout.TextField(skillSearchFilter, GUILayout.Width(150));

        EditorGUILayout.LabelField("타입:", GUILayout.Width(35));
        int typeIndex = System.Array.IndexOf(behaviorTypes, behaviorTypeFilter);
        typeIndex = EditorGUILayout.Popup(typeIndex, behaviorTypes, GUILayout.Width(130));
        behaviorTypeFilter = behaviorTypes[Mathf.Max(0, typeIndex)];

        showUnmappedOnly = EditorGUILayout.ToggleLeft("미할당만", showUnmappedOnly, GUILayout.Width(80));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillList()
    {
        // 필터링
        var filtered = skillEntries.Where(e =>
        {
            if (showUnmappedOnly && e.hasVFX) return false;
            if (behaviorTypeFilter != "All" && e.behaviorType != behaviorTypeFilter) return false;
            if (!string.IsNullOrEmpty(skillSearchFilter))
            {
                if (!e.skillName.ToLower().Contains(skillSearchFilter.ToLower()) &&
                    !e.skillId.ToString().Contains(skillSearchFilter))
                    return false;
            }
            return true;
        }).ToList();

        EditorGUILayout.LabelField($"스킬 목록 ({filtered.Count}/{skillEntries.Count})", EditorStyles.boldLabel);

        // 헤더
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField("ID", EditorStyles.boldLabel, GUILayout.Width(50));
        EditorGUILayout.LabelField("스킬명", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField("타입", EditorStyles.boldLabel, GUILayout.Width(120));
        EditorGUILayout.LabelField("VFX 프리팹 (드래그 앤 드롭)", EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUILayout.LabelField("Hit 프리팹", EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField("", GUILayout.Width(50)); // 버튼 공간
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var entry in filtered)
        {
            DrawSkillRow(entry);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSkillRow(SkillMappingEntry entry)
    {
        EditorGUILayout.BeginHorizontal("box");

        // ID
        EditorGUILayout.LabelField(entry.skillId.ToString(), GUILayout.Width(50));

        // 스킬명
        EditorGUILayout.LabelField(entry.skillName, GUILayout.Width(100));

        // 타입
        EditorGUILayout.LabelField(entry.behaviorType, GUILayout.Width(120));

        // VFX 프리팹 드롭 영역
        Rect vfxDropRect = GUILayoutUtility.GetRect(200, 20);
        DrawVFXDropArea(vfxDropRect, entry, isHit: false);

        // Hit 프리팹 드롭 영역
        Rect hitDropRect = GUILayoutUtility.GetRect(150, 20);
        DrawVFXDropArea(hitDropRect, entry, isHit: true);

        // 클리어 버튼
        if (entry.hasVFX)
        {
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                ClearVFXMapping(entry);
            }
            GUI.color = Color.white;
        }
        else
        {
            GUILayout.Space(29);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawVFXDropArea(Rect rect, SkillMappingEntry entry, bool isHit)
    {
        GameObject currentPrefab = isHit ? entry.hitPrefab : entry.vfxPrefab;

        // 배경색 설정
        Color bgColor = currentPrefab != null ? new Color(0.5f, 0.8f, 0.5f, 0.3f) : new Color(0.8f, 0.8f, 0.8f, 0.3f);
        EditorGUI.DrawRect(rect, bgColor);

        // 현재 할당된 프리팹 또는 안내 텍스트
        string displayText = currentPrefab != null ? currentPrefab.name : (isHit ? "Hit VFX 드롭" : "VFX 드롭");
        GUI.Label(rect, displayText, EditorStyles.centeredGreyMiniLabel);

        // 드래그 앤 드롭 처리
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!rect.Contains(evt.mousePosition))
                    break;

                // GameObject 프리팹인지 확인
                bool hasValidPrefab = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go))
                    {
                        hasValidPrefab = true;
                        break;
                    }
                }

                if (!hasValidPrefab) break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Link;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go))
                        {
                            AssignVFXToSkill(entry, go, isHit);
                            break; // 첫 번째 유효한 프리팹만 사용
                        }
                    }
                }
                evt.Use();
                break;
        }
    }

    private void AssignVFXToSkill(SkillMappingEntry entry, GameObject prefab, bool isHit)
    {
        if (database == null)
        {
            EditorUtility.DisplayDialog("오류", "VFX Database가 설정되지 않았습니다.", "확인");
            return;
        }

        // Database entries 접근
        var field = typeof(SkillVFXDatabase).GetField("entries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entries = field?.GetValue(database) as List<SkillVFXDatabase.Entry>;

        if (entries == null) return;

        // 해당 스킬 Entry 찾기 또는 생성
        var dbEntry = entries.FirstOrDefault(e => e.skillId == entry.skillId);
        if (dbEntry == null)
        {
            dbEntry = new SkillVFXDatabase.Entry { skillId = entry.skillId };
            entries.Add(dbEntry);
        }

        // 프리팹 할당
        if (isHit)
        {
            dbEntry.hitPrefab = prefab;
            entry.hitPrefab = prefab;
        }
        else
        {
            dbEntry.vfxPrefab = prefab;
            entry.vfxPrefab = prefab;
            entry.hasVFX = true;
        }

        EditorUtility.SetDirty(database);
        Debug.Log($"[VFX Mapping] [{entry.skillId}] {entry.skillName} ← {prefab.name} ({(isHit ? "Hit" : "VFX")})");
    }

    private void ClearVFXMapping(SkillMappingEntry entry)
    {
        if (database == null) return;

        var field = typeof(SkillVFXDatabase).GetField("entries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entries = field?.GetValue(database) as List<SkillVFXDatabase.Entry>;

        if (entries == null) return;

        var dbEntry = entries.FirstOrDefault(e => e.skillId == entry.skillId);
        if (dbEntry != null)
        {
            dbEntry.vfxPrefab = null;
            dbEntry.hitPrefab = null;
            entry.vfxPrefab = null;
            entry.hitPrefab = null;
            entry.hasVFX = false;

            EditorUtility.SetDirty(database);
            Debug.Log($"[VFX Mapping] [{entry.skillId}] {entry.skillName} VFX 매핑 해제됨");
        }
    }

    private void DrawBottomButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("변경사항 저장", GUILayout.Height(30)))
        {
            if (database != null)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("저장 완료", "VFX 매핑이 저장되었습니다.", "확인");
            }
        }

        if (GUILayout.Button("새로고침", GUILayout.Height(30)))
        {
            RefreshMappingStatus();
        }

        // 통계
        int total = skillEntries.Count;
        int mapped = skillEntries.Count(e => e.hasVFX);
        float percent = total > 0 ? (float)mapped / total * 100 : 0;

        EditorGUILayout.LabelField($"매핑 현황: {mapped}/{total} ({percent:F0}%)", GUILayout.Width(150));

        EditorGUILayout.EndHorizontal();
    }

    private void RefreshMappingStatus()
    {
        if (database == null) return;

        foreach (var entry in skillEntries)
        {
            var dbEntry = database.GetEntry(entry.skillId);
            if (dbEntry != null)
            {
                entry.vfxPrefab = dbEntry.vfxPrefab;
                entry.hitPrefab = dbEntry.hitPrefab;
                entry.hasVFX = dbEntry.vfxPrefab != null;
            }
            else
            {
                entry.vfxPrefab = null;
                entry.hitPrefab = null;
                entry.hasVFX = false;
            }
        }

        Repaint();
    }
}
