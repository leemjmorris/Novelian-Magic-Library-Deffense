using System;
using CsvHelper.Configuration.Attributes;
using Novelian.Combat;

/// <summary>
/// MainSkillTable.csv 데이터 클래스
/// 간소화된 새 스킬 시스템 - behavior_type 문자열 기반
/// </summary>
[Serializable]
public class MainSkillData
{
    [Name("skill_id")]
    public int skill_id { get; set; }

    [Name("//skill_name")]
    [Optional]
    public string skill_name { get; set; }

    [Name("behavior_type")]
    public string behavior_type { get; set; }

    [Name("base_damage")]
    public float base_damage { get; set; }

    [Name("cooldown")]
    public float cooldown { get; set; }

    [Name("range")]
    public float range { get; set; }

    [Name("projectile_speed")]
    public float projectile_speed { get; set; }

    [Name("aoe_radius")]
    public float aoe_radius { get; set; }

    [Name("duration")]
    public float duration { get; set; }

    [Name("//description")]
    [Optional]
    public string description { get; set; }

    #region Behavior Type Helpers

    /// <summary>
    /// behavior_type 문자열을 SkillBehaviorType enum으로 변환
    /// 신규 시스템: Projectile, BeamRay, AOE 3개 타입
    /// </summary>
    public SkillBehaviorType GetBehaviorType()
    {
        if (string.IsNullOrEmpty(behavior_type))
            return SkillBehaviorType.Unknown;

        return behavior_type switch
        {
            "Projectile" => SkillBehaviorType.Projectile,
            "BeamRay" => SkillBehaviorType.Beam,
            "AOE" => SkillBehaviorType.Visual_AOE,
            // Legacy 호환성 (이전 타입들도 지원)
            "SingleProjectile" => SkillBehaviorType.Projectile,
            "ExplosiveProjectile" => SkillBehaviorType.Projectile,
            "FallingProjectile" => SkillBehaviorType.Projectile,
            "TargetAOE" => SkillBehaviorType.Visual_AOE,
            "LinearAOE" => SkillBehaviorType.Visual_AOE,
            "GroundAOE" => SkillBehaviorType.Field,
            "MovingAOE" => SkillBehaviorType.Visual_AOE,
            "Barrier" => SkillBehaviorType.Shield,
            "Buff" => SkillBehaviorType.Shield,
            "Debuff" => SkillBehaviorType.Field,
            "Trap" => SkillBehaviorType.Field,
            "Instant" => SkillBehaviorType.Visual_AOE,
            _ => SkillBehaviorType.Unknown
        };
    }

    /// <summary>
    /// 투사체 스킬인지 확인 (신규 + Legacy 지원)
    /// </summary>
    public bool IsProjectileSkill => behavior_type == "Projectile" ||
                                     behavior_type == "SingleProjectile" ||
                                     behavior_type == "ExplosiveProjectile" ||
                                     behavior_type == "FallingProjectile";

    /// <summary>
    /// 빔 스킬인지 확인
    /// </summary>
    public bool IsBeamSkill => behavior_type == "BeamRay";

    /// <summary>
    /// AOE 스킬인지 확인 (신규 + Legacy 지원)
    /// </summary>
    public bool IsAOESkill => behavior_type == "AOE" ||
                              behavior_type == "TargetAOE" ||
                              behavior_type == "LinearAOE" ||
                              behavior_type == "GroundAOE" ||
                              behavior_type == "MovingAOE";

    /// <summary>
    /// 투사체 속도가 있는 스킬인지 확인
    /// </summary>
    public bool HasProjectileSpeed => projectile_speed > 0;

    /// <summary>
    /// 범위 효과가 있는 스킬인지 확인
    /// </summary>
    public bool HasAOERadius => aoe_radius > 0;

    /// <summary>
    /// 지속시간이 있는 스킬인지 확인
    /// </summary>
    public bool HasDuration => duration > 0;

    #region Legacy Compatibility - Deprecated (이전 시스템 호환성)
    /// <summary>[Deprecated] 지속형 장판 스킬인지 확인 - AOE로 통합됨</summary>
    [Obsolete("Use IsAOESkill instead. Ground skills are now part of AOE.")]
    public bool IsGroundSkill => behavior_type == "GroundAOE" || (behavior_type == "AOE" && duration > 0);

    /// <summary>[Deprecated] 폭발형 투사체인지 확인 - Projectile로 통합됨</summary>
    [Obsolete("Use IsProjectileSkill with aoe_radius > 0 check instead.")]
    public bool IsExplosiveProjectile => behavior_type == "ExplosiveProjectile" || (behavior_type == "Projectile" && aoe_radius > 0);

    /// <summary>[Deprecated] 방어막 스킬인지 확인 - 제거됨</summary>
    [Obsolete("Barrier type has been removed from the new skill system.")]
    public bool IsBarrierSkill => behavior_type == "Barrier";

    /// <summary>[Deprecated] 버프 스킬인지 확인 - 제거됨</summary>
    [Obsolete("Buff type has been removed from the new skill system.")]
    public bool IsBuffSkill => behavior_type == "Buff";

    /// <summary>[Deprecated] 디버프 스킬인지 확인 - 제거됨</summary>
    [Obsolete("Debuff type has been removed from the new skill system.")]
    public bool IsDebuffSkill => behavior_type == "Debuff";

    /// <summary>[Deprecated] 트랩 스킬인지 확인 - AOE로 통합됨</summary>
    [Obsolete("Use IsAOESkill instead. Trap skills are now part of AOE.")]
    public bool IsTrapSkill => behavior_type == "Trap";

    /// <summary>[Deprecated] 즉발 스킬인지 확인 - AOE로 통합됨</summary>
    [Obsolete("Use IsAOESkill instead. Instant skills are now part of AOE.")]
    public bool IsInstantSkill => behavior_type == "Instant";
    #endregion

    #endregion

    #region Compatibility Helpers

    /// <summary>
    /// SupportCompatibilityData.IsCompatibleWith()와 호환되는 스킬 타입 반환
    /// </summary>
    public string GetSkillType() => behavior_type;

    #endregion

    #region Legacy Compatibility (Character.StatusEffects.cs)
    // 이전 시스템과의 호환성을 위한 스텁 프로퍼티들
    // 새 시스템에서는 이 효과들이 SupportSkillData로 분리됨

    /// <summary>CC 효과 여부 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public bool HasCCEffect => false;

    /// <summary>DOT 효과 여부 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public bool HasDOTEffect => false;

    /// <summary>마크 효과 여부 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public bool HasMarkEffect => false;

    /// <summary>디버프 효과 여부 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public bool HasDebuffEffect => false;

    /// <summary>DOT 틱당 데미지 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public float dot_damage_per_tick => 0f;

    /// <summary>DOT 지속시간 - duration 사용</summary>
    public float dot_duration => duration;

    /// <summary>DOT 틱 간격 - 기본값 1초</summary>
    public float dot_tick_interval => 1f;

    /// <summary>스킬 지속시간 - duration 사용</summary>
    public float skill_lifetime => duration;

    /// <summary>마크 데미지 배율 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public float mark_damage_mult => 1f;

    /// <summary>마크 지속시간 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public float mark_duration => 0f;

    /// <summary>기본 디버프 값 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public float base_debuff_value => 0f;

    /// <summary>CC 지속시간 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public float cc_duration => 0f;

    /// <summary>CC 슬로우 양 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public float cc_slow_amount => 0f;

    /// <summary>스턴 사용 여부 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public bool stun_use => false;

    /// <summary>CC 타입 반환 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public Novelian.Combat.CCType GetCCType() => Novelian.Combat.CCType.None;

    /// <summary>디버프 타입 반환 - 새 시스템에서는 서포트 스킬에서 처리</summary>
    public Novelian.Combat.DeBuffType GetDeBuffType() => Novelian.Combat.DeBuffType.None;

    #endregion
}
