using System;
using UnityEngine;

/// <summary>
/// Issue #273: 스킬 데이터 ScriptableObject
/// SkillCreatorWindow를 통해 생성됨
/// </summary>
[CreateAssetMenu(fileName = "NewSkill", menuName = "Skills/Skill Asset Data", order = 0)]
public class SkillAssetData : ScriptableObject
{
    [Header("📋 기본 정보")]
    public string skillName = "New Skill";
    public SkillCategory skillCategory = SkillCategory.Main;
    public SkillAssetType skillType = SkillAssetType.Projectile;
    [TextArea(3, 5)]
    public string description = "";

    [Header("⚔️ 기본 능력치")]
    public float baseDamage = 10f;
    public float cooldown = 1f;
    public float manaCost = 10f;
    public float castTime = 0f;
    public float range = 10f;

    [Header("🎨 속성 태그")]
    public ElementType elementType = ElementType.None;
    public DamageType damageType = DamageType.Physical;

    [Header("✨ 이펙트")]
    public GameObject castEffectPrefab;
    public GameObject projectileEffectPrefab;
    public GameObject hitEffectPrefab;
    public GameObject areaEffectPrefab;

    [Header("🎯 투사체 프리팹")]
    [Tooltip("실제 투사체 게임오브젝트 (Projectile.cs 컴포넌트 필수)")]
    public GameObject projectilePrefab;

    [Header("🎯 Projectile 설정")]
    public float projectileSpeed = 15f;
    public float projectileLifetime = 5f;
    public int projectileCount = 1;
    public int pierceCount = 0;
    public bool isHoming = false;

    [Header("💥 AOE 설정")]
    public float aoeRadius = 3f;
    public float aoeAngle = 360f;
    public bool aoeCenterOnCaster = false;

    [Header("🔥 DOT 설정")]
    public float dotDuration = 5f;
    public float dotTickInterval = 0.5f;
    public float dotDamagePerTick = 5f;

    [Header("⚡ Buff/Debuff 설정")]
    public float buffDuration = 10f;
    public bool isStackable = false;
    public int maxStacks = 1;
    public StatModifier[] statModifiers;

    [Header("👻 Flicker 설정")]
    public int flickerDashCount = 5;
    public float flickerDashRange = 5f;
    public float flickerDashInterval = 0.1f;
    public bool flickerReturnToOrigin = true;

    [Header("🌊 Channeling 설정")]
    public float channelDuration = 3f;
    public float channelTickInterval = 0.2f;
    public bool interruptible = true;

    [Header("💎 Summon 설정")]
    public GameObject summonPrefab;
    public int summonCount = 1;
    public float summonDuration = 30f;

    [Header("🛡️ Shield 설정")]
    public float shieldAmount = 50f;
    public float shieldDuration = 5f;
    public bool absorbsDamage = true;

    [Header("🎭 Trap/Mine 설정")]
    public GameObject trapPrefab;
    public float trapArmTime = 0.5f;
    public float trapTriggerRadius = 2f;
    public float trapDuration = 10f;

    [Header("🔧 보조 스킬 설정 (Support 전용)")]
    [Tooltip("메인 스킬 변형 효과")]
    public int additionalProjectiles = 0;
    public int additionalPierceCount = 0;
    public float aoeRadiusMultiplier = 0f;
    public float projectileSpeedMultiplier = 0f;
    public float durationMultiplier = 0f;

    [Header("⚡ 보조 스킬 스텟 변형 (%)")]
    [Tooltip("캐릭터 스텟 변형 (%)")]
    public float damageModifier = 0f;
    public float attackSpeedModifier = 0f;
    public float manaCostModifier = 0f;
    public float castTimeModifier = 0f;

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

        switch (skillType)
        {
            case SkillAssetType.Projectile:
                if (projectilePrefab == null)
                {
                    errorMessage = "Projectile 프리팹 누락";
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
