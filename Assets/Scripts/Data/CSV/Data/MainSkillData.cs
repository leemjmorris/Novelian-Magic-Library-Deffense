using System;
using CsvHelper.Configuration.Attributes;

/// <summary>
/// MainSkillTable.csv 데이터 클래스
/// 새로운 스킬 시스템 - 스킬 타입 ID 기반
/// </summary>
[Serializable]
public class MainSkillData
{
    [Name("skill_id")]
    public int skill_id { get; set; }

    [Name("skill_type_ID")]
    public int skill_type_ID { get; set; }

    [Name("element_type_ID")]
    public int element_type_ID { get; set; }

    [Name("base_damage")]
    public float base_damage { get; set; }

    // Issue #362 - 버프/디버프/CC/DOT/표식 컬럼 추가
    [Name("buff_type")]
    public int buff_type { get; set; }

    [Name("base_buff_value")]
    public float base_buff_value { get; set; }

    [Name("debuff_type")]
    public int debuff_type { get; set; }

    [Name("base_debuff_value")]
    public float base_debuff_value { get; set; }

    [Name("cc_type")]
    public int cc_type { get; set; }

    [Name("cc_duration")]
    public float cc_duration { get; set; }

    [Name("stun_use")]
    public bool stun_use { get; set; }

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

    [Name("cooldown")]
    public float cooldown { get; set; }

    [Name("cast_time")]
    public float cast_time { get; set; }

    [Name("range")]
    public float range { get; set; }

    [Name("projectile_speed")]
    public float projectile_speed { get; set; }

    [Name("projectile_count")]
    public int projectile_count { get; set; }

    [Name("skill_lifetime")]
    public float skill_lifetime { get; set; }

    [Name("pierce_count")]
    public int pierce_count { get; set; }

    [Name("is_homing")]
    public bool is_homing { get; set; }

    [Name("aoe_radius")]
    public float aoe_radius { get; set; }

    [Name("aoe_angle")]
    public float aoe_angle { get; set; }

    [Name("channel_duration")]
    public float channel_duration { get; set; }

    [Name("channel_tick_interval")]
    public float channel_tick_interval { get; set; }

    [Name("interruptible")]
    public bool interruptible { get; set; }

    // CSV의 //로 시작하는 주석 컬럼 (선택적)
    [Name("//skill_name")]
    [Optional]
    public string skill_name { get; set; }

    [Name("//description")]
    [Optional]
    public string description { get; set; }

    #region Helper Properties (Enum 변환 - 새 ID 체계)

    /// <summary>
    /// skill_type_ID를 SkillAssetType으로 변환
    /// 3000100=Projectile, 3000201=InstantSingle, 3000302=AOE, etc.
    /// </summary>
    public SkillAssetType GetSkillType()
    {
        return skill_type_ID switch
        {
            3000100 => SkillAssetType.Projectile,
            3000201 => SkillAssetType.InstantSingle,
            3000302 => SkillAssetType.AOE,
            3000403 => SkillAssetType.DOT,
            3000504 => SkillAssetType.Buff,
            3000605 => SkillAssetType.Debuff,
            3000706 => SkillAssetType.Channeling,
            3000807 => SkillAssetType.Trap,
            3000908 => SkillAssetType.Mine,
            _ => SkillAssetType.Projectile
        };
    }

    /// <summary>
    /// element_type_ID를 ElementType으로 변환
    /// 3101000=None, 3101101=Romance, 3101202=Comedy, etc.
    /// </summary>
    public ElementType GetElementType()
    {
        return element_type_ID switch
        {
            3101000 => ElementType.None,
            3101101 => ElementType.Romance,
            3101202 => ElementType.Comedy,
            3101303 => ElementType.Adventure,
            3101404 => ElementType.Mystery,
            3101505 => ElementType.Fear,
            _ => ElementType.None
        };
    }

    /// <summary>
    /// 투사체 스킬인지 확인
    /// </summary>
    public bool IsProjectileSkill => skill_type_ID == 3000100;

    /// <summary>
    /// 범위 스킬인지 확인
    /// </summary>
    public bool IsAOESkill => skill_type_ID == 3000302;

    /// <summary>
    /// DOT 스킬인지 확인
    /// </summary>
    public bool IsDOTSkill => skill_type_ID == 3000403;

    /// <summary>
    /// 채널링 스킬인지 확인
    /// </summary>
    public bool IsChannelingSkill => skill_type_ID == 3000706;

    /// <summary>
    /// 함정/지뢰 스킬인지 확인
    /// </summary>
    public bool IsTrapSkill => skill_type_ID == 3000807 || skill_type_ID == 3000908;

    /// <summary>
    /// 버프 스킬인지 확인
    /// </summary>
    public bool IsBuffSkill => skill_type_ID == 3000504;

    /// <summary>
    /// 디버프 스킬인지 확인
    /// </summary>
    public bool IsDebuffSkill => skill_type_ID == 3000605;

    /// <summary>
    /// CC 타입 반환 (3302100=None, 3302201=Stun, 3302302=Slow)
    /// </summary>
    public CCType GetCCType()
    {
        return cc_type switch
        {
            3302100 => CCType.None,
            3302201 => CCType.Stun,
            3302302 => CCType.Slow,
            _ => CCType.None
        };
    }

    /// <summary>
    /// 버프 타입 반환
    /// </summary>
    public BuffType GetBuffType()
    {
        return buff_type switch
        {
            3604400 => BuffType.None,
            3604501 => BuffType.ATK_Damage_UP,
            3604602 => BuffType.ATK_Speed_UP,
            3604703 => BuffType.ATK_Range_UP,
            3604804 => BuffType.Critical_Damage_UP,
            3604905 => BuffType.Battle_Exp_UP,
            _ => BuffType.None
        };
    }

    /// <summary>
    /// 디버프 타입 반환
    /// </summary>
    public DeBuffType GetDeBuffType()
    {
        return debuff_type switch
        {
            3605000 => DeBuffType.None,
            3605101 => DeBuffType.ATK_Damage_Down,
            3605202 => DeBuffType.ATK_Speed_Down,
            3605303 => DeBuffType.Take_Damage_UP,
            _ => DeBuffType.None
        };
    }

    /// <summary>
    /// CC 효과가 있는 스킬인지 확인
    /// stun_use=true 이거나 cc_duration > 0 이면 CC 효과 있음
    /// </summary>
    public bool HasCCEffect => stun_use || (cc_type != 3302100 && cc_duration > 0);

    /// <summary>
    /// DOT 효과가 있는 스킬인지 확인
    /// </summary>
    public bool HasDOTEffect => dot_duration > 0 && dot_damage_per_tick > 0;

    /// <summary>
    /// 표식 효과가 있는 스킬인지 확인
    /// </summary>
    public bool HasMarkEffect => mark_duration > 0 && mark_damage_mult > 0;

    /// <summary>
    /// 버프 효과가 있는 스킬인지 확인
    /// </summary>
    public bool HasBuffEffect => buff_type != 3604400 && base_buff_value > 0;

    /// <summary>
    /// 디버프 효과가 있는 스킬인지 확인
    /// </summary>
    public bool HasDebuffEffect => debuff_type != 3605000 && base_debuff_value > 0;

    /// <summary>
    /// 속성 타입 기반 표식 타입 반환
    /// </summary>
    public MarkType GetElementBasedMarkType()
    {
        return element_type_ID switch
        {
            3101101 => MarkType.Romance,    // 로맨스
            3101202 => MarkType.Comedy,     // 코미디
            3101303 => MarkType.Adventure,  // 모험
            3101404 => MarkType.Mystery,    // 추리
            3101505 => MarkType.Fear,       // 공포
            _ => MarkType.None
        };
    }

    #endregion
}
