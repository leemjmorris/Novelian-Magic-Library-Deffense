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

    // 새 컬럼 추가 (Issue #362 - 보조스킬 밸런스 개편)
    [Name("buff_value_mult")]
    public float buff_value_mult { get; set; } = 1f;

    [Name("debuff_value_mult")]
    public float debuff_value_mult { get; set; } = 1f;

    [Name("channel_duration_mult")]
    public float channel_duration_mult { get; set; } = 1f;

    [Name("low_hp_bonus_damage_mult")]
    public float low_hp_bonus_damage_mult { get; set; } = 1f;

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
    /// Issue #362 - 새 SupportCategory 체계 적용
    /// </summary>
    public SupportCategory GetSupportCategory()
    {
        return support_category switch
        {
            3503801 => SupportCategory.Projectile,
            3503902 => SupportCategory.AOE,
            3504003 => SupportCategory.StatusEffect,
            3504104 => SupportCategory.Chain,
            3504205 => SupportCategory.SkillDamageUp,
            3504306 => SupportCategory.ShootingTypeChange,
            3504407 => SupportCategory.Universal,
            3504508 => SupportCategory.BuffEnhance,
            3504609 => SupportCategory.DOTEnhance,
            3504710 => SupportCategory.TrapMineEnhance,
            3504811 => SupportCategory.InstantEnhance,
            3504912 => SupportCategory.ChannelingEnhance,
            3505013 => SupportCategory.DebuffEnhance,
            _ => SupportCategory.Projectile
        };
    }

    /// <summary>
    /// status_effect를 StatusEffectType으로 변환
    /// Issue #362 - 새 ID 체계 적용
    /// 3201600=None, 3201701=CC, 3201802=DOT, 3201903=Mark, 3202004=Chain
    /// </summary>
    public StatusEffectType GetStatusEffectType()
    {
        return status_effect switch
        {
            3201600 => StatusEffectType.None,
            3201701 => StatusEffectType.CC,
            3201802 => StatusEffectType.DOT,
            3201903 => StatusEffectType.Mark,
            3202004 => StatusEffectType.Chain,
            _ => StatusEffectType.None
        };
    }

    /// <summary>
    /// CC 타입 반환
    /// Issue #362 - 새 CSV에서는 cc_type 컬럼이 제거됨
    /// cc_slow_amount > 0 이면 Slow, cc_duration > 0 이면 Stun
    /// </summary>
    public CCType GetCCType()
    {
        if (cc_slow_amount > 0) return CCType.Slow;
        if (cc_duration > 0) return CCType.Stun;
        return CCType.None;
    }

    /// <summary>
    /// 저체력 보너스 데미지 서포트인지 확인 (처형 등)
    /// </summary>
    public bool IsLowHpBonusSupport => low_hp_bonus_damage_mult > 1f;

    /// <summary>
    /// 버프 강화 서포트인지 확인
    /// </summary>
    public bool IsBuffEnhanceSupport => buff_value_mult > 1f;

    /// <summary>
    /// 디버프 강화 서포트인지 확인
    /// </summary>
    public bool IsDebuffEnhanceSupport => debuff_value_mult > 1f;

    /// <summary>
    /// 채널링 강화 서포트인지 확인
    /// </summary>
    public bool IsChannelEnhanceSupport => channel_duration_mult > 1f;

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
/// 서포트 스킬 카테고리 (Issue #362 - 새 ID 체계)
/// </summary>
public enum SupportCategory
{
    Projectile = 1,         // 발사체 변형 (3503801)
    AOE = 2,                // 범위 변형 (3503902)
    StatusEffect = 3,       // 상태이상 부여 (3504003)
    Chain = 4,              // 연쇄 효과 (3504104)
    SkillDamageUp = 5,      // 스킬 데미지 증가 (3504205)
    ShootingTypeChange = 6, // 발사 형식 변경 (3504306)
    Universal = 7,          // 범용 강화 (3504407)
    BuffEnhance = 8,        // 버프 강화 (3504508)
    DOTEnhance = 9,         // DOT 강화 (3504609)
    TrapMineEnhance = 10,   // 함정/지뢰 강화 (3504710)
    InstantEnhance = 11,    // 즉발 강화 (3504811)
    ChannelingEnhance = 12, // 채널링 강화 (3504912)
    DebuffEnhance = 13      // 디버프 강화 (3505013)
}
