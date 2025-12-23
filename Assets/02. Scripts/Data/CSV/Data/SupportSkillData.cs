using System;
using CsvHelper.Configuration.Attributes;
using Novelian.Combat;

/// <summary>
/// SupportSkillTable.csv 데이터 클래스
/// 간소화된 새 서포트 스킬 시스템 - 명확한 컬럼명 기반
/// </summary>
[Serializable]
public class SupportSkillData
{
    [Name("support_id")]
    public int support_id { get; set; }

    [Name("//support_name")]
    [Optional]
    public string support_name { get; set; }

    [Name("support_type")]
    public string support_type { get; set; }

    /// <summary>횟수 (연쇄 횟수, 분열 개수, 다중발사 개수 등)</summary>
    [Name("count")]
    public int count { get; set; }

    /// <summary>연쇄 시 데미지 유지율 (0.8 = 80%)</summary>
    [Name("chain_decay")]
    public float chain_decay { get; set; }

    /// <summary>폭발/데미지 배율</summary>
    [Name("explosion_ratio")]
    public float explosion_ratio { get; set; }

    /// <summary>잔류 데미지 비율</summary>
    [Name("linger_ratio")]
    public float linger_ratio { get; set; }

    /// <summary>둔화율 (0.3 = 30% 감소)</summary>
    [Name("slow_rate")]
    public float slow_rate { get; set; }

    /// <summary>디버프율 (방어력 감소 등)</summary>
    [Name("debuff_rate")]
    public float debuff_rate { get; set; }

    /// <summary>틱당 데미지 (독, 화상, 끌어당김 등)</summary>
    [Name("tick_damage")]
    public float tick_damage { get; set; }

    /// <summary>틱 주기 (초)</summary>
    [Name("tick_interval")]
    public float tick_interval { get; set; }

    /// <summary>범위 (연쇄 탐지 범위, 끌어당김 범위 등)</summary>
    [Name("range")]
    public float range { get; set; }

    /// <summary>거리 (넉백 거리 등)</summary>
    [Name("distance")]
    public float distance { get; set; }

    /// <summary>속도 (끌어당김 속도, 이동 AOE 속도 등)</summary>
    [Name("speed")]
    public float speed { get; set; }

    /// <summary>확산 각도 (분열, 다중발사 등)</summary>
    [Name("spread_angle")]
    public float spread_angle { get; set; }

    /// <summary>유도 강도 (0~1)</summary>
    [Name("homing_strength")]
    public float homing_strength { get; set; }

    /// <summary>크기 배율 (범위증가, 분열 시 크기 등)</summary>
    [Name("scale_multiplier")]
    public float scale_multiplier { get; set; }

    /// <summary>지속시간 (초)</summary>
    [Name("duration")]
    public float duration { get; set; }

    [Name("//description")]
    [Optional]
    public string description { get; set; }

    #region Helper Properties

    // ============================================
    // 신규 시스템: 8개 서포트 타입
    // Pierce, Homing, MultiShot, CC, DOT, Enhance, Bounce, Split
    // ============================================

    // === 투사체 전용 서포트 (5개) ===
    /// <summary>관통 서포트인지 확인</summary>
    public bool IsPierceSupport => support_type == "Pierce";

    /// <summary>유도 서포트인지 확인</summary>
    public bool IsHomingSupport => support_type == "Homing";

    /// <summary>다중발사 서포트인지 확인</summary>
    public bool IsMultiShotSupport => support_type == "MultiShot";

    /// <summary>바운스(벽 튕김) 서포트인지 확인</summary>
    public bool IsBounceSupport => support_type == "Bounce";

    /// <summary>분열 서포트인지 확인</summary>
    public bool IsSplitSupport => support_type == "Split";

    // === 범용 서포트 (3개) ===
    /// <summary>CC(군중 제어) 서포트인지 확인 - Slow, Stun 통합</summary>
    public bool IsCCSupport => support_type == "CC" || support_type == "Slow" || support_type == "Stun";

    /// <summary>도트(지속 데미지) 서포트인지 확인</summary>
    public bool IsDOTSupport => support_type == "DOT";

    /// <summary>강화(Enhance) 서포트인지 확인 - AreaUp, DamageUp, CooldownDown 통합</summary>
    public bool IsEnhanceSupport => support_type == "Enhance" ||
                                    support_type == "AreaUp" ||
                                    support_type == "DamageUp" ||
                                    support_type == "CooldownDown";

    // === 복합 체크 ===
    /// <summary>투사체 전용 서포트인지 확인</summary>
    public bool IsProjectileOnlySupport => IsPierceSupport || IsHomingSupport ||
                                           IsMultiShotSupport || IsBounceSupport || IsSplitSupport;

    /// <summary>틱 데미지가 있는 서포트인지 확인</summary>
    public bool HasTickDamage => tick_damage > 0 && tick_interval > 0;

    /// <summary>지속시간이 있는 서포트인지 확인</summary>
    public bool HasDuration => duration > 0;

    /// <summary>횟수 기반 서포트인지 확인</summary>
    public bool HasCount => count > 0;

    #region Legacy Compatibility - Deprecated (이전 시스템 호환성)
    /// <summary>[Deprecated] 연쇄 서포트 - 제거됨</summary>
    [Obsolete("Chain support has been removed. Use Pierce or Split instead.")]
    public bool IsChainSupport => support_type == "Chain";

    /// <summary>[Deprecated] 둔화 서포트 - CC로 통합됨</summary>
    [Obsolete("Use IsCCSupport instead. Slow is now part of CC.")]
    public bool IsSlowSupport => support_type == "Slow" || support_type == "CC";

    /// <summary>[Deprecated] 기절 서포트 - CC로 통합됨</summary>
    [Obsolete("Use IsCCSupport instead. Stun is now part of CC.")]
    public bool IsStunSupport => support_type == "Stun" || support_type == "CC";

    /// <summary>[Deprecated] 넉백 서포트 - 제거됨</summary>
    [Obsolete("Knockback support has been removed from the new skill system.")]
    public bool IsKnockbackSupport => support_type == "Knockback";

    /// <summary>[Deprecated] 끌어당김 서포트 - 제거됨</summary>
    [Obsolete("PullIn support has been removed from the new skill system.")]
    public bool IsPullInSupport => support_type == "PullIn";

    /// <summary>[Deprecated] 범위증가 서포트 - Enhance로 통합됨</summary>
    [Obsolete("Use IsEnhanceSupport instead. AreaUp is now part of Enhance.")]
    public bool IsAreaUpSupport => support_type == "AreaUp" || support_type == "Enhance";

    /// <summary>[Deprecated] 데미지증가 서포트 - Enhance로 통합됨</summary>
    [Obsolete("Use IsEnhanceSupport instead. DamageUp is now part of Enhance.")]
    public bool IsDamageUpSupport => support_type == "DamageUp" || support_type == "Enhance";

    /// <summary>[Deprecated] 쿨다운감소 서포트 - Enhance로 통합됨</summary>
    [Obsolete("Use IsEnhanceSupport instead. CooldownDown is now part of Enhance.")]
    public bool IsCooldownDownSupport => support_type == "CooldownDown" || support_type == "Enhance";

    /// <summary>[Deprecated] 잔류 서포트 - 제거됨</summary>
    [Obsolete("Linger support has been removed from the new skill system.")]
    public bool IsLingerSupport => support_type == "Linger";

    /// <summary>[Deprecated] 이동 서포트 - 제거됨</summary>
    [Obsolete("Moving support has been removed from the new skill system.")]
    public bool IsMovingSupport => support_type == "Moving";

    /// <summary>[Deprecated] 확장 서포트 - 제거됨</summary>
    [Obsolete("Expand support has been removed from the new skill system.")]
    public bool IsExpandSupport => support_type == "Expand";
    #endregion

    #endregion

    #region Status Effect Helpers (for Character.StatusEffects.cs compatibility)

    /// <summary>
    /// 상태효과 타입 반환 (CC, DOT 등)
    /// </summary>
    public StatusEffectType GetStatusEffectType()
    {
        if (IsCCSupport) return StatusEffectType.CC;
        if (IsDOTSupport) return StatusEffectType.DOT;
        if (IsChainSupport) return StatusEffectType.Chain;
        return StatusEffectType.None;
    }

    /// <summary>
    /// CC 타입 반환
    /// </summary>
    public CCType GetCCType()
    {
        return support_type switch
        {
            "Slow" => CCType.Slow,
            "Stun" => CCType.Stun,
            _ => CCType.None
        };
    }

    // 호환성을 위한 프로퍼티 별칭
    /// <summary>CC 슬로우 비율 (slow_rate와 동일)</summary>
    public float cc_slow_amount => slow_rate;

    /// <summary>CC 지속시간 (duration과 동일)</summary>
    public float cc_duration => duration;

    /// <summary>DOT 틱당 데미지 (tick_damage와 동일)</summary>
    public float dot_damage_per_tick => tick_damage;

    /// <summary>DOT 지속시간 (duration과 동일)</summary>
    public float dot_duration => duration;

    /// <summary>DOT 틱 간격 (tick_interval과 동일)</summary>
    public float dot_tick_interval => tick_interval;

    /// <summary>표식 데미지 배율 (explosion_ratio를 사용)</summary>
    public float mark_damage_mult => explosion_ratio;

    /// <summary>표식 지속시간 (duration과 동일)</summary>
    public float mark_duration => duration;

    /// <summary>디버프 값 배율</summary>
    public float debuff_value_mult => debuff_rate;

    // Character.Targeting.cs 호환성
    /// <summary>체인 횟수 (count와 동일)</summary>
    public int chain_count => count;

    /// <summary>체인 범위 (range와 동일)</summary>
    public float chain_range => range;

    #endregion
}
