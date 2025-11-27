using System;
using UnityEngine;

/// <summary>
/// Issue #273: 스킬 데이터 ScriptableObject
/// SkillCreatorWindow를 통해 생성됨
/// </summary>
[CreateAssetMenu(fileName = "NewSkill", menuName = "Skills/Skill Asset Data", order = 0)]
public class SkillAssetData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName = "New Skill";
    public SkillCategory skillCategory = SkillCategory.Main;
    public SkillAssetType skillType = SkillAssetType.Projectile;
    [TextArea(3, 5)]
    public string description = "";

    [Header("기본 능력치")]
    public float baseDamage = 10f;
    public float cooldown = 1f;
    public float manaCost = 10f;
    public float castTime = 0f;
    public float range = 10f;

    [Header("속성 태그")]
    public ElementType elementType = ElementType.None;
    public DamageType damageType = DamageType.Physical;

    [Header("이펙트")]
    public GameObject castEffectPrefab;
    public GameObject projectileEffectPrefab;
    public GameObject hitEffectPrefab;
    public GameObject areaEffectPrefab;

    [Header("Projectile 설정")]
    public float projectileSpeed = 15f;
    public float projectileLifetime = 5f;
    public int projectileCount = 1;
    public int pierceCount = 0;
    public bool isHoming = false;

    [Header("AOE 설정")]
    public float aoeRadius = 3f;
    public float aoeAngle = 360f;
    public bool aoeCenterOnCaster = false;

    [Header("DOT 설정")]
    public float dotDuration = 5f;
    public float dotTickInterval = 0.5f;
    public float dotDamagePerTick = 5f;

    [Header("Buff/Debuff 설정")]
    public float buffDuration = 10f;
    public bool isStackable = false;
    public int maxStacks = 1;
    public StatModifier[] statModifiers;

    [Header("Flicker 설정")]
    public int flickerDashCount = 5;
    public float flickerDashRange = 5f;
    public float flickerDashInterval = 0.1f;
    public bool flickerReturnToOrigin = true;

    [Header("Channeling 설정")]
    public float channelDuration = 3f;
    public float channelTickInterval = 0.2f;
    public bool interruptible = true;

    [Header("Summon 설정")]
    public GameObject summonPrefab;
    public int summonCount = 1;
    public float summonDuration = 30f;

    [Header("Shield 설정")]
    public float shieldAmount = 50f;
    public float shieldDuration = 5f;
    public bool absorbsDamage = true;

    [Header("Trap/Mine 설정")]
    public GameObject trapPrefab;
    public float trapArmTime = 0.5f;
    public float trapTriggerRadius = 2f;
    public float trapDuration = 10f;

    [Header("보조 스킬 설정 (Support 전용)")]
    [Tooltip("메인 스킬 변형 효과")]
    public int additionalProjectiles = 0;
    public int additionalPierceCount = 0;
    public float aoeRadiusMultiplier = 0f;
    public float projectileSpeedMultiplier = 0f;
    public float durationMultiplier = 0f;

    [Header("보조 스킬 스텟 변형 (%)")]
    [Tooltip("캐릭터 스텟 변형 (%)")]
    public float damageModifier = 0f;
    public float attackSpeedModifier = 0f;
    public float manaCostModifier = 0f;
    public float castTimeModifier = 0f;

    [Header("상태 이상 효과 (Support 전용)")]
    [Tooltip("부여할 상태 이상 효과 타입")]
    public StatusEffectType statusEffectType = StatusEffectType.None;

    [Header("CC 설정")]
    public CCType ccType = CCType.None;
    public float ccDuration = 2f;
    [Range(0f, 100f), Tooltip("Slow인 경우 이동 속도 감소율 (%)")]
    public float ccSlowAmount = 50f;
    [Tooltip("CC 이펙트 Prefab (몬스터를 따라다니면서 재생)")]
    public GameObject ccEffectPrefab;

    [Header("Support DOT 설정")]
    public DOTType dotType = DOTType.None;
    // dotDamagePerTick, dotTickInterval, dotDuration은 기존 필드 재사용
    [Tooltip("DOT 이펙트 Prefab (몬스터를 따라다니면서 재생)")]
    public GameObject dotEffectPrefab;

    [Header("Mark 설정")]
    public MarkType markType = MarkType.None;
    public float markDuration = 10f;
    [Tooltip("Mark가 있는 대상에게 주는 추가 데미지 배율 (%)")]
    public float markDamageMultiplier = 50f;
    public GameObject markEffectPrefab;

    [Header("Chain 설정")]
    [Tooltip("투사체가 튕기는 횟수 (0 = 튕기지 않음)")]
    public int chainCount = 0;
    [Tooltip("다음 Chain 타겟을 찾는 범위")]
    public float chainRange = 10f;
    [Tooltip("Chain될 때마다 데미지 감소율 (%). 0이면 감소 없음")]
    [Range(0f, 100f)]
    public float chainDamageReduction = 0f;
    [Tooltip("Chain 이펙트 Prefab (적에서 적으로 튕기는 비주얼)")]
    public GameObject chainEffectPrefab;

    /// <summary>
    /// 스킬 타입별 유효성 검증
    /// </summary>
    public bool Validate(out string errorMessage)
    {
        errorMessage = "";

        if (string.IsNullOrWhiteSpace(skillName))
        {
            errorMessage = "스킬 이름 비어있음";
            return false;
        }

        // Support 스킬은 Main 스킬에 영향을 주는 modifier이므로 Prefab 검증 스킵
        if (skillCategory == SkillCategory.Support)
        {
            return true;
        }

        // Main 스킬만 Prefab 검증
        switch (skillType)
        {
            case SkillAssetType.Projectile:
                // Projectile 타입은 이펙트만으로 구성 가능 (파티클 직접 발사)
                if (projectileEffectPrefab == null && hitEffectPrefab == null)
                {
                    errorMessage = "Projectile 타입은 이펙트 중 하나 이상 필요";
                    return false;
                }
                break;

            case SkillAssetType.Summon:
                if (summonPrefab == null)
                {
                    errorMessage = "Summon 프리팹 누락";
                    return false;
                }
                break;

            case SkillAssetType.Trap:
            case SkillAssetType.Mine:
                if (trapPrefab == null)
                {
                    errorMessage = "Trap/Mine 프리팹 누락";
                    return false;
                }
                break;

            case SkillAssetType.AOE:
                // AOE는 범위 이펙트만 있으면 됨
                if (areaEffectPrefab == null && hitEffectPrefab == null)
                {
                    errorMessage = "AOE 타입은 범위 이펙트 또는 피격 이펙트 중 하나 이상 필요";
                    return false;
                }
                break;

            case SkillAssetType.Channeling:
                // 채널링은 이펙트만 있으면 됨
                if (projectileEffectPrefab == null && castEffectPrefab == null)
                {
                    errorMessage = "Channeling 타입은 시전 이펙트 또는 투사체 이펙트 중 하나 이상 필요";
                    return false;
                }
                break;
        }

        return true;
    }
}

/// <summary>
/// 스킬 에셋 타입
/// </summary>
public enum SkillAssetType
{
    Projectile,     // 투사체
    AOE,            // 범위 공격
    DOT,            // 지속 데미지
    Buff,           // 버프
    Debuff,         // 디버프
    Heal,           // 힐
    Summon,         // 소환
    Teleport,       // 순간이동
    Dash,           // 돌진
    Flicker,        // 플리커 스트라이크
    Channeling,     // 채널링
    Trap,           // 트랩
    Mine,           // 지뢰
    Aura,           // 오라
    Shield,         // 보호막
    Pull,           // 끌어당기기
    Push,           // 밀어내기
    Stun,           // 스턴
    Slow,           // 슬로우
    Root,           // 속박
}

/// <summary>
/// 속성 타입
/// </summary>
public enum ElementType
{
    None,
    Fire,
    Ice,
    Lightning,
    Poison,
    Holy,
    Dark,
    Nature,
    Arcane,
}

/// <summary>
/// 데미지 타입
/// </summary>
public enum DamageType
{
    Physical,
    Magical,
    Pure,
    Hybrid,
}

/// <summary>
/// 스탯 수정자 (Buff/Debuff용)
/// </summary>
[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public ModifierType modifierType;
    public float value;
}

public enum StatType
{
    AttackSpeed,
    MoveSpeed,
    Damage,
    Defense,
    MaxHealth,
    MaxMana,
    CriticalChance,
    CriticalDamage,
}

public enum ModifierType
{
    Flat,        // 고정값
    Percentage,  // %
}

/// <summary>
/// 스킬 카테고리
/// </summary>
public enum SkillCategory
{
    Main,       // 메인 스킬
    Support,    // 보조 스킬
}

/// <summary>
/// 상태 이상 효과 타입 (Support 스킬 전용)
/// </summary>
public enum StatusEffectType
{
    None,           // 효과 없음
    CC,             // 군중 제어 (Crowd Control)
    DOT,            // 지속 데미지 (Damage Over Time)
    Mark,           // 표식
    Chain,          // 연쇄 공격 (투사체가 여러 적에게 튕김)
}

/// <summary>
/// CC (군중 제어) 타입
/// </summary>
public enum CCType
{
    None,           // 효과 없음
    Stun,           // 기절 (이동/공격 불가)
    Slow,           // 둔화 (이동 속도 감소)
    Root,           // 속박 (이동 불가, 공격 가능)
    Freeze,         // 빙결 (이동/공격 불가 + 비주얼 효과)
    Knockback,      // 넉백 (뒤로 밀림)
    Silence,        // 침묵 (스킬 사용 불가)
}

/// <summary>
/// DOT (지속 데미지) 타입
/// </summary>
public enum DOTType
{
    None,           // 효과 없음
    Burn,           // 화상
    Poison,         // 중독
    Bleed,          // 출혈
    Corrosion,      // 부식
    Curse,          // 저주
}

/// <summary>
/// Mark (표식) 타입
/// </summary>
public enum MarkType
{
    None,           // 효과 없음
    Flame,          // 화염 표식
    Ice,            // 빙결 표식
    Lightning,      // 번개 표식
    Poison,         // 독 표식
    Holy,           // 신성 표식
    Curse,          // 저주 표식
    Focus,          // 집중 표식
}
