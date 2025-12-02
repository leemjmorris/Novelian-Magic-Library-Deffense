using System;
using UnityEngine;

/// <summary>
/// 스킬 관련 Enum 정의
/// CSV의 SkillTypeTable과 매핑됩니다
/// 새 ID 체계 (3000100=Projectile, 3101101=Romance, etc.)
/// </summary>

/// <summary>
/// 스킬 에셋 타입 (MainSkillTable의 skill_type_ID)
/// 3000100=Projectile, 3000201=InstantSingle, 3000302=AOE, etc.
/// </summary>
public enum SkillAssetType
{
    Projectile,     // 투사체 (3000100)
    InstantSingle,  // 인스턴트 단일 (3000201)
    AOE,            // 범위 공격 (3000302)
    DOT,            // 지속 데미지 (3000403)
    Buff,           // 버프 (3000504)
    Debuff,         // 디버프 (3000605)
    Channeling,     // 채널링 (3000706)
    Trap,           // 트랩 (3000807)
    Mine,           // 지뢰 (3000908)
    // 레거시 호환성 유지
    Heal,           // 힐 (미사용)
    Summon,         // 소환 (미사용)
    Teleport,       // 순간이동 (미사용)
    Dash,           // 돌진 (미사용)
    Flicker,        // 플리커 스트라이크 (미사용)
    Aura,           // 오라 (미사용)
    Shield,         // 보호막 (미사용)
}

/// <summary>
/// 속성 타입 (MainSkillTable의 element_type_ID)
/// 3101000=None, 3101101=Romance, 3101202=Comedy, etc.
/// </summary>
public enum ElementType
{
    None,       // 무속성 (3101000)
    Romance,    // 로맨스 (3101101)
    Comedy,     // 코미디 (3101202)
    Adventure,  // 모험 (3101303)
    Mystery,    // 추리 (3101404)
    Fear,       // 공포 (3101505)
    // 레거시 호환성 유지
    Fire,       // 화염 (미사용)
    Ice,        // 냉기 (미사용)
    Lightning,  // 번개 (미사용)
    Poison,     // 독 (미사용)
    Holy,       // 신성 (미사용)
    Dark,       // 암흑 (미사용)
    Nature,     // 자연 (미사용)
    Arcane,     // 비전 (미사용)
}

/// <summary>
/// 데미지 타입 (레거시 - 새 시스템에서는 사용하지 않음)
/// </summary>
public enum DamageType
{
    Physical,   // 물리
    Magical,    // 마법
    Pure,       // 순수
    Hybrid,     // 혼합
}

/// <summary>
/// 상태 이상 효과 타입 (SupportSkillTable의 status_effect)
/// 3301600=None, 3301701=CC, 3301802=DOT, 3301903=Mark, 3302004=Chain
/// </summary>
public enum StatusEffectType
{
    None,   // 효과 없음 (3301600)
    CC,     // 군중 제어 (3301701)
    DOT,    // 지속 데미지 (3301802)
    Mark,   // 표식 (3301903)
    Chain,  // 연쇄 공격 (3302004)
}

/// <summary>
/// CC (군중 제어) 타입 (SupportSkillTable의 cc_type)
/// 3402100=None, 3402201=Stun, 3402302=Slow, 3402403=Root, 3402505=Knockback
/// </summary>
public enum CCType
{
    None,       // 없음 (3402100)
    Stun,       // 스턴 (3402201)
    Slow,       // 슬로우 (3402302)
    Root,       // 속박 (3402403)
    Knockback,  // 넉백 (3402505)
    // 레거시 호환성 유지
    Freeze,     // 빙결 (미사용)
    Silence,    // 침묵 (미사용)
}

/// <summary>
/// DOT (지속 데미지) 타입
/// 3502600=None, 3502701=Burn, 3502802=Poison, 3502903=Bleed, 3503004=Corrosion, 3503105=Curse
/// </summary>
public enum DOTType
{
    None,       // 없음 (3502600)
    Burn,       // 화상 (3502701)
    Poison,     // 중독 (3502802)
    Bleed,      // 출혈 (3502903)
    Corrosion,  // 부식 (3503004)
    Curse,      // 저주 (3503105)
}

/// <summary>
/// Mark (표식) 타입
/// 3603200=None, 3603301=Romance, 3603402=Comedy, 3603503=Adventure, 3603604=Mystery, 3603705=Fear
/// </summary>
public enum MarkType
{
    None,       // 없음 (3603200)
    Romance,    // 로맨스 표식 (3603301)
    Comedy,     // 코미디 표식 (3603402)
    Adventure,  // 모험 표식 (3603503)
    Mystery,    // 추리 표식 (3603604)
    Fear,       // 공포 표식 (3603705)
    // 레거시 호환성 유지
    Flame,      // 화염 표식 (미사용)
    Ice,        // 빙결 표식 (미사용)
    Lightning,  // 번개 표식 (미사용)
    Poison,     // 독 표식 (미사용)
    Holy,       // 신성 표식 (미사용)
    Curse,      // 저주 표식 (미사용)
    Focus,      // 집중 표식 (미사용)
}

/// <summary>
/// 스킬 카테고리
/// </summary>
public enum SkillCategory
{
    Main,       // 메인 스킬
    Support,    // 보조 스킬
}

// JML: StatType은 Defines.cs에서 정의됨 (Issue #349)
// CardLevelTable 기반 인게임 스텟 카드 시스템 통합

/// <summary>
/// 스탯 수정자 타입
/// </summary>
public enum ModifierType
{
    Flat,        // 고정값
    Percentage,  // %
}

/// <summary>
/// 스탯 수정자 (Buff/Debuff용)
/// </summary>
[Serializable]
public class StatModifier
{
    public StatType statType;
    public ModifierType modifierType;
    public float value;
}
