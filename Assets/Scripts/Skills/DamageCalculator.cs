using UnityEngine;

/// <summary>
/// 데미지 계산기 클래스
/// 새로운 데미지 공식 적용:
/// 1. 단일: (기본 데미지) × (레벨 배율) × (보조 스킬 배율)
/// 2. 다중: (단일 타격 데미지) × (기본 투사체 수 + 추가 투사체 수)
/// 3. 표식: (단일 타격 데미지) × (1 + 표식 배율)
/// 4. 관통/체이닝: n번째 타격 데미지 = (단일 타격 데미지) × (1 - 감소율)^n
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// 단일 타격 데미지 계산
    /// 공식: (기본 데미지) × (레벨 배율) × (보조 스킬 배율)
    /// </summary>
    /// <param name="baseDamage">스킬 기본 데미지</param>
    /// <param name="levelMult">레벨 배율 (SkillLevelData.damage_mult)</param>
    /// <param name="supportMult">보조 스킬 배율 (SupportSkillData.damage_mult)</param>
    /// <returns>단일 타격 데미지</returns>
    public static float CalculateSingleDamage(float baseDamage, float levelMult = 1f, float supportMult = 1f)
    {
        return baseDamage * levelMult * supportMult;
    }

    /// <summary>
    /// 다중 투사체 총 데미지 계산
    /// 공식: (단일 타격 데미지) × (기본 투사체 수 + 추가 투사체 수)
    /// </summary>
    /// <param name="singleDamage">단일 타격 데미지</param>
    /// <param name="baseProjectileCount">기본 투사체 수</param>
    /// <param name="additionalProjectiles">추가 투사체 수</param>
    /// <returns>다중 투사체 총 데미지</returns>
    public static float CalculateMultiProjectileDamage(float singleDamage, int baseProjectileCount, int additionalProjectiles = 0)
    {
        int totalProjectiles = baseProjectileCount + additionalProjectiles;
        return singleDamage * totalProjectiles;
    }

    /// <summary>
    /// 표식 적용 데미지 계산
    /// 공식: (단일 타격 데미지) × (1 + 표식 배율)
    /// </summary>
    /// <param name="singleDamage">단일 타격 데미지</param>
    /// <param name="markDamageMult">표식 데미지 배율</param>
    /// <returns>표식 적용 데미지</returns>
    public static float CalculateMarkDamage(float singleDamage, float markDamageMult)
    {
        return singleDamage * (1f + markDamageMult);
    }

    /// <summary>
    /// 관통/체이닝 n번째 타격 데미지 계산
    /// 공식: (단일 타격 데미지) × (1 - 감소율)^n
    /// </summary>
    /// <param name="singleDamage">단일 타격 데미지</param>
    /// <param name="reductionRate">감소율 (0~1, 예: 0.3 = 30%)</param>
    /// <param name="hitCount">튕긴/관통한 횟수 (0부터 시작)</param>
    /// <returns>n번째 타격 데미지</returns>
    public static float CalculatePierceChainDamage(float singleDamage, float reductionRate, int hitCount)
    {
        if (hitCount <= 0) return singleDamage;

        // (1 - 감소율)^n
        float multiplier = Mathf.Pow(1f - reductionRate, hitCount);
        return singleDamage * multiplier;
    }

    /// <summary>
    /// 저체력 보너스 데미지 계산 (처형 서포트 등)
    /// Issue #362 - 새 서포트 시스템
    /// 공식: 대상 체력이 threshold% 이하일 때 damage × bonusMult
    /// </summary>
    /// <param name="baseDamage">기본 데미지</param>
    /// <param name="targetCurrentHp">대상 현재 체력</param>
    /// <param name="targetMaxHp">대상 최대 체력</param>
    /// <param name="bonusMult">보너스 배율 (예: 1.8 = 80% 추가 데미지)</param>
    /// <param name="hpThreshold">체력 임계치 (기본 0.3 = 30%)</param>
    /// <returns>최종 데미지</returns>
    public static float CalculateLowHpBonusDamage(
        float baseDamage,
        float targetCurrentHp,
        float targetMaxHp,
        float bonusMult,
        float hpThreshold = 0.3f)
    {
        if (targetMaxHp <= 0) return baseDamage;

        float hpRatio = targetCurrentHp / targetMaxHp;

        // 체력이 임계치 이하면 보너스 데미지 적용
        if (hpRatio <= hpThreshold)
        {
            return baseDamage * bonusMult;
        }

        return baseDamage;
    }

    /// <summary>
    /// 버프/디버프 효과량 계산
    /// Issue #362 - 새 서포트 시스템
    /// 공식: baseValue × buffValueMult (또는 debuffValueMult)
    /// </summary>
    /// <param name="baseValue">기본 효과량</param>
    /// <param name="valueMult">효과량 배율</param>
    /// <returns>최종 효과량</returns>
    public static float CalculateBuffDebuffValue(float baseValue, float valueMult)
    {
        return baseValue * valueMult;
    }

    /// <summary>
    /// 채널링 지속시간 계산
    /// Issue #362 - 새 서포트 시스템
    /// </summary>
    /// <param name="baseDuration">기본 채널링 지속시간</param>
    /// <param name="channelDurationMult">채널링 지속시간 배율</param>
    /// <returns>최종 채널링 지속시간</returns>
    public static float CalculateChannelDuration(float baseDuration, float channelDurationMult)
    {
        return baseDuration * channelDurationMult;
    }

    /// <summary>
    /// 전체 데미지 계산 (모든 요소 종합)
    /// </summary>
    /// <param name="skillData">메인 스킬 데이터</param>
    /// <param name="levelData">스킬 레벨 데이터 (null이면 레벨1)</param>
    /// <param name="supportData">보조 스킬 데이터 (null이면 미적용)</param>
    /// <param name="hasMarkEffect">표식이 적용된 적인지</param>
    /// <param name="pierceOrChainCount">관통/체이닝 횟수 (0이면 첫 타격)</param>
    /// <returns>최종 데미지</returns>
    public static float CalculateFinalDamage(
        MainSkillData skillData,
        SkillLevelData levelData = null,
        SupportSkillData supportData = null,
        bool hasMarkEffect = false,
        int pierceOrChainCount = 0)
    {
        if (skillData == null) return 0f;

        // 1. 기본 단일 데미지 계산
        float levelMult = levelData?.damage_mult ?? 1f;
        float supportMult = supportData?.damage_mult ?? 1f;
        float singleDamage = CalculateSingleDamage(skillData.base_damage, levelMult, supportMult);

        // 2. 표식 효과 적용
        if (hasMarkEffect && supportData != null && supportData.IsMarkSupport)
        {
            singleDamage = CalculateMarkDamage(singleDamage, supportData.mark_damage_mult);
        }

        // 3. 관통/체이닝 감소 적용
        if (pierceOrChainCount > 0 && supportData != null)
        {
            float reductionRate = supportData.chain_damage_reduction / 100f; // % -> 소수
            if (reductionRate > 0)
            {
                singleDamage = CalculatePierceChainDamage(singleDamage, reductionRate, pierceOrChainCount);
            }
        }

        return singleDamage;
    }

    /// <summary>
    /// 다중 투사체 총 데미지 계산 (CSV 데이터 기반)
    /// </summary>
    /// <param name="skillData">메인 스킬 데이터</param>
    /// <param name="levelData">스킬 레벨 데이터 (null이면 레벨1)</param>
    /// <param name="supportData">보조 스킬 데이터 (null이면 미적용)</param>
    /// <returns>다중 투사체 총 데미지</returns>
    public static float CalculateTotalMultiProjectileDamage(
        MainSkillData skillData,
        SkillLevelData levelData = null,
        SupportSkillData supportData = null)
    {
        if (skillData == null) return 0f;

        // 단일 데미지 계산
        float levelMult = levelData?.damage_mult ?? 1f;
        float supportMult = supportData?.damage_mult ?? 1f;
        float singleDamage = CalculateSingleDamage(skillData.base_damage, levelMult, supportMult);

        // 투사체 개수 계산
        int baseCount = skillData.projectile_count;
        int additionalCount = (levelData?.projectile_add ?? 0) + (supportData?.add_projectiles ?? 0);

        return CalculateMultiProjectileDamage(singleDamage, baseCount, additionalCount);
    }

    /// <summary>
    /// 데미지 정보 로그 출력 (디버그용)
    /// </summary>
    public static void LogDamageBreakdown(
        MainSkillData skillData,
        SkillLevelData levelData = null,
        SupportSkillData supportData = null)
    {
        if (skillData == null)
        {
            Debug.LogWarning("[DamageCalculator] skillData is null");
            return;
        }

        float baseDamage = skillData.base_damage;
        float levelMult = levelData?.damage_mult ?? 1f;
        float supportMult = supportData?.damage_mult ?? 1f;
        float singleDamage = CalculateSingleDamage(baseDamage, levelMult, supportMult);

        Debug.Log($"[DamageCalculator] Breakdown for Skill {skillData.skill_id}:\n" +
                  $"  Base Damage: {baseDamage}\n" +
                  $"  Level Multiplier: {levelMult}x\n" +
                  $"  Support Multiplier: {supportMult}x\n" +
                  $"  = Single Damage: {singleDamage}");

        if (skillData.projectile_count > 1 || (supportData?.add_projectiles ?? 0) > 0)
        {
            int totalProjectiles = skillData.projectile_count + (levelData?.projectile_add ?? 0) + (supportData?.add_projectiles ?? 0);
            Debug.Log($"  Projectile Count: {skillData.projectile_count} + {(levelData?.projectile_add ?? 0)} + {(supportData?.add_projectiles ?? 0)} = {totalProjectiles}\n" +
                      $"  = Total Multi-Projectile Damage: {singleDamage * totalProjectiles}");
        }

        if (supportData != null && supportData.IsMarkSupport)
        {
            float markDamage = CalculateMarkDamage(singleDamage, supportData.mark_damage_mult);
            Debug.Log($"  Mark Multiplier: +{supportData.mark_damage_mult * 100}%\n" +
                      $"  = Mark Damage: {markDamage}");
        }

        if (supportData != null && supportData.chain_damage_reduction > 0)
        {
            Debug.Log($"  Chain/Pierce Reduction: {supportData.chain_damage_reduction}% per hit\n" +
                      $"  Hit 1: {singleDamage:F1}\n" +
                      $"  Hit 2: {CalculatePierceChainDamage(singleDamage, supportData.chain_damage_reduction / 100f, 1):F1}\n" +
                      $"  Hit 3: {CalculatePierceChainDamage(singleDamage, supportData.chain_damage_reduction / 100f, 2):F1}");
        }
    }
}
