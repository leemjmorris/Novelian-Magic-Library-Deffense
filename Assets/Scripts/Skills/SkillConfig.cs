using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skill Configuration ScriptableObject
/// Create via: Assets > Create > Skills > Skill Config
/// </summary>
[CreateAssetMenu(fileName = "New Skill Config", menuName = "Skills/Skill Config", order = 1)]
public class SkillConfig : ScriptableObject
{
    [Header("📋 Basic Info")]
    [Tooltip("스킬을 드롭다운에서 선택하세요 (자동으로 모든 정보 로드)")]
    [HideInInspector] public int skillID;

    [Tooltip("자동으로 CSV에서 로드됩니다")]
    [ReadOnly] public string skillName;
    [ReadOnly] public SkillType skillType;
    [ReadOnly] public AttackRange attackRange;
    [ReadOnly] public float cooldown;
    [ReadOnly] public float castTime;
    [ReadOnly] public int effectID;

    [Header("🎭 Casting Mode")]
    [Tooltip("스킬 발동 방식 선택")]
    public CastMode castMode = CastMode.Instant;

    [Header("🎯 Projectile Settings")]
    [Tooltip("투사체 사용 여부")]
    public bool hasProjectile;

    [ShowIf("hasProjectile")]
    public GameObject projectilePrefab;

    [ShowIf("hasProjectile")]
    [Range(1f, 50f)]
    public float projectileSpeed = 10f;

    [ShowIf("hasProjectile")]
    [Tooltip("투사체 지속 시간 (초)")]
    [Range(0.5f, 20f)]
    public float projectileDuration = 5f;

    [ShowIf("hasProjectile")]
    [Tooltip("유도탄 여부 (타겟 추적)")]
    public bool isHoming;

    [ShowIf("hasProjectile")]
    [Tooltip("관통 여부")]
    public bool isPiercing;

    [ShowIf("hasProjectile")]
    [Tooltip("최대 관통 횟수 (0 = 무제한)")]
    [Range(0, 20)]
    public int maxPierceCount = 0;

    [Header("💥 AOE Settings")]
    [Tooltip("범위 효과 타입")]
    public AreaOfEffectType aoeType = AreaOfEffectType.None;

    [ShowIf("aoeType", AreaOfEffectType.Circle, AreaOfEffectType.Cone, AreaOfEffectType.Line)]
    [Range(0.5f, 10f)]
    public float aoeRadius = 2f;

    [ShowIf("aoeType", AreaOfEffectType.Cone)]
    [Range(15f, 180f)]
    public float aoeAngle = 90f;

    [Header("⚡ Dash Settings")]
    [Tooltip("돌진형 스킬 (Flicker Strike)")]
    public bool isDashSkill;

    [ShowIf("isDashSkill")]
    [Range(1, 10)]
    public int maxDashTargets = 5;

    [ShowIf("isDashSkill")]
    [Range(0.05f, 0.5f)]
    public float dashInterval = 0.15f;

    [ShowIf("isDashSkill")]
    [Range(1f, 10f)]
    public float dashRange = 5f;

    [ShowIf("isDashSkill")]
    public bool returnToOrigin = true;

    [ShowIf("isDashSkill")]
    public GameObject dashTrailEffect;

    [ShowIf("isDashSkill")]
    public GameObject slashEffect;

    [Header("🌪️ Moving AOE Settings")]
    [Tooltip("이동형 장판 (먹구름처럼 천천히 이동)")]
    public bool isMovingAOE;

    [ShowIf("isMovingAOE")]
    public MovementPattern movePattern = MovementPattern.Forward;

    [ShowIf("isMovingAOE")]
    [Range(0.5f, 10f)]
    public float moveSpeed = 2f;

    [ShowIf("isMovingAOE")]
    [Range(1f, 20f)]
    public float lifetime = 8f;

    [ShowIf("isMovingAOE")]
    [Range(0.1f, 2f)]
    public float tickInterval = 0.5f;

    [ShowIf("isMovingAOE")]
    public GameObject aoeEffectPrefab;

    [Header("✨ Visual Effects")]
    [Tooltip("발사 시 이펙트 (총구 섬광, 발사 위치에 남음)")]
    public GameObject muzzleFlashEffectPrefab;

    [Tooltip("투사체 자체 이펙트 (발사체 비주얼)")]
    public GameObject projectileEffectPrefab;

    [Tooltip("트레일 이펙트 (투사체를 따라다니는 꼬리)")]
    public GameObject[] trailEffectPrefabs;

    [Tooltip("피격 시 이펙트 (적이 맞는 순간)")]
    public GameObject onHitEffectPrefab;

    [Tooltip("피격 후 이펙트 (데미지 후 지속)")]
    public GameObject afterHitEffectPrefab;

    [Tooltip("스킬 착탄/폭발 이펙트 (Deprecated - use onHitEffectPrefab)")]
    public GameObject impactEffectPrefab;

    [Header("👥 Character Assignment")]
    [Tooltip("이 스킬을 사용할 캐릭터들 (드래그 & 드롭)")]
    public List<GameObject> assignedCharacters = new List<GameObject>();

    /// <summary>
    /// Load skill data from CSV
    /// </summary>
    public void LoadFromCSV()
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            Debug.LogWarning("[SkillConfig] CSVLoader not initialized yet!");
            return;
        }

        SkillData data = CSVLoader.Instance.GetData<SkillData>(skillID);
        if (data != null)
        {
            skillName = data.Skill_Name;
            skillType = data.Skill_Type;
            attackRange = data.Attack_Range;
            cooldown = data.Cooldown;
            castTime = data.Cast_Time;
            effectID = data.Effect_ID;

            Debug.Log($"[SkillConfig] Loaded data for Skill ID {skillID}: {skillName}");
        }
        else
        {
            Debug.LogError($"[SkillConfig] Skill ID {skillID} not found in CSV!");
        }
    }

    private void OnValidate()
    {
        // Auto-load CSV data when Skill ID changes
        if (Application.isPlaying && CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            LoadFromCSV();
        }
    }
}

/// <summary>
/// Cast Mode Enum
/// </summary>
public enum CastMode
{
    Instant,      // 즉발
    Projectile,   // 투사체
    Placement,    // 설치형
    Channeling,   // 채널링
    Dash,         // 돌진형
    MovingAOE     // 이동형 장판
}

/// <summary>
/// Area of Effect Type
/// </summary>
public enum AreaOfEffectType
{
    None,      // 단일 타겟
    Circle,    // 원형 범위
    Cone,      // 부채꼴
    Line,      // 직선
    Full       // 전체
}

/// <summary>
/// Movement Pattern for Moving AOE
/// </summary>
public enum MovementPattern
{
    Forward,      // 전방으로 직진
    Random,       // 랜덤 방향
    TowardsEnemy, // 가장 많은 적이 있는 방향
    Circular,     // 원형 궤도
    Zigzag        // 지그재그
}

/// <summary>
/// ReadOnly attribute for Inspector
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }

/// <summary>
/// ShowIf attribute - conditionally show fields
/// </summary>
public class ShowIfAttribute : PropertyAttribute
{
    public string fieldName;
    public object[] compareValues;

    public ShowIfAttribute(string fieldName, params object[] compareValues)
    {
        this.fieldName = fieldName;
        this.compareValues = compareValues;
    }
}
