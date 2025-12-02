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

    #endregion
}
