using System;
using CsvHelper.Configuration.Attributes;

/// <summary>
/// SupportSkillTable.csv 데이터 클래스
/// 새로운 보조 스킬 시스템 - ID 기반 타입
/// </summary>
[Serializable]
public class SupportSkillData
{
    [Name("support_id")]
    public int support_id { get; set; }

    [Name("support_category")]
    public int support_category { get; set; }

    [Name("compatible_types")]
    public int compatible_types { get; set; }

    [Name("add_projectiles")]
    public int add_projectiles { get; set; }

    [Name("add_pierce")]
    public int add_pierce { get; set; }

    [Name("damage_mult")]
    public float damage_mult { get; set; }

    [Name("speed_mult")]
    public float speed_mult { get; set; }

    [Name("aoe_mult")]
    public float aoe_mult { get; set; }

    [Name("cooldown_mult")]
    public float cooldown_mult { get; set; }

    [Name("attack_speed_mult")]
    public float attack_speed_mult { get; set; }

    [Name("cast_time_mult")]
    public float cast_time_mult { get; set; }

    [Name("status_effect")]
    public int status_effect { get; set; }

    [Name("cc_type")]
    public int cc_type { get; set; }

    [Name("cc_duration")]
    public float cc_duration { get; set; }

    [Name("cc_slow_amount")]
    public float cc_slow_amount { get; set; }

    [Name("dot_duration")]
    public float dot_duration { get; set; }

    [Name("dot_tick_interval")]
    public float dot_tick_interval { get; set; }

    [Name("dot_damage_per_tick")]
    public float dot_damage_per_tick { get; set; }

    [Name("mark_duration")]
    public float mark_duration { get; set; }

    [Name("mark_damage_mult")]
    public float mark_damage_mult { get; set; }

    [Name("chain_count")]
    public int chain_count { get; set; }

    [Name("chain_range")]
    public float chain_range { get; set; }

    [Name("chain_damage_reduction")]
    public float chain_damage_reduction { get; set; }

    // CSV의 //로 시작하는 주석 컬럼 (선택적)
    [Name("//support_name")]
    [Optional]
    public string support_name { get; set; }

    [Name("//description")]
    [Optional]
    public string description { get; set; }

    #region Helper Properties (Enum 변환 - 새 ID 체계)

    /// <summary>
    /// support_category를 SupportCategory로 변환
    /// 3703801=Projectile, 3703902=AOE, 3704003=StatusEffect, etc.
    /// </summary>
    public SupportCategory GetSupportCategory()
    {
        return support_category switch
        {
            3703801 => SupportCategory.Projectile,
            3703902 => SupportCategory.AOE,
            3704003 => SupportCategory.StatusEffect,
            3704104 => SupportCategory.Chain,
            3704205 => SupportCategory.SkillDamageUp,
            3704306 => SupportCategory.ShootingTypeChange,
            _ => SupportCategory.Projectile
        };
    }

    /// <summary>
    /// status_effect를 StatusEffectType으로 변환
    /// 3301600=None, 3301701=CC, 3301802=DOT, 3301903=Mark, 3302004=Chain
    /// </summary>
    public StatusEffectType GetStatusEffectType()
    {
        return status_effect switch
        {
            3301600 => StatusEffectType.None,
            3301701 => StatusEffectType.CC,
            3301802 => StatusEffectType.DOT,
            3301903 => StatusEffectType.Mark,
            3302004 => StatusEffectType.Chain,
            _ => StatusEffectType.None
        };
    }

    /// <summary>
    /// cc_type을 CCType으로 변환
    /// 3402100=None, 3402201=Stun, 3402302=Slow, etc.
    /// </summary>
    public CCType GetCCType()
    {
        return cc_type switch
        {
            3402100 => CCType.None,
            3402201 => CCType.Stun,
            3402302 => CCType.Slow,
            3402403 => CCType.Root,
            3402505 => CCType.Knockback,
            _ => CCType.None
        };
    }

    /// <summary>
    /// 체이닝 지원 스킬인지 확인
    /// </summary>
    public bool IsChainSupport => status_effect == 3302004;

    /// <summary>
    /// 표식 지원 스킬인지 확인
    /// </summary>
    public bool IsMarkSupport => status_effect == 3301903;

    /// <summary>
    /// DOT 지원 스킬인지 확인
    /// </summary>
    public bool IsDOTSupport => status_effect == 3301802;

    /// <summary>
    /// DOT 타입 반환 (새 CSV 구조에서는 DOT 여부만 판단, 기본 Burn 반환)
    /// </summary>
    public DOTType GetDOTType()
    {
        // 새 CSV 구조에서는 별도 DOT 타입 필드가 없음
        // DOT 서포트 스킬이면 기본 Burn 타입 반환
        return IsDOTSupport ? DOTType.Burn : DOTType.None;
    }

    /// <summary>
    /// Mark 타입 반환 (새 CSV 구조에서는 Mark 여부만 판단, 기본 Romance 반환)
    /// </summary>
    public MarkType GetMarkType()
    {
        // 새 CSV 구조에서는 별도 Mark 타입 필드가 없음
        // Mark 서포트 스킬이면 기본 Romance 타입 반환
        return IsMarkSupport ? MarkType.Romance : MarkType.None;
    }

    #endregion
}

/// <summary>
/// 서포트 스킬 카테고리 (새 ID 체계)
/// </summary>
public enum SupportCategory
{
    Projectile = 1,        // 발사체 변형 (3703801)
    AOE = 2,               // 범위 변형 (3703902)
    StatusEffect = 3,      // 상태이상 부여 (3704003)
    Chain = 4,             // 연쇄 효과 (3704104)
    SkillDamageUp = 5,     // 스킬 데미지 증가 (3704205)
    ShootingTypeChange = 6 // 발사 형식 변경 (3704306)
}
