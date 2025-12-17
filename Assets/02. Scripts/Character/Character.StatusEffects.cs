//LMJ : Character partial class - Status Effect Application (CC, DOT, Mark, Debuff)
namespace Novelian.Combat
{
    using UnityEngine;

    /// <summary>
    /// 캐릭터 상태이상 적용
    /// - 서포트 스킬 효과 적용 (슬로우, 스턴, DOT 등)
    /// - 메인 스킬 자체 효과 적용 (CC, DOT, 표식, 디버프)
    /// </summary>
    public partial class Character
    {
        #region Support Skill Status Effects

        /// <summary>
        /// 서포트 스킬의 상태이상 효과 적용
        /// </summary>
        /// <param name="target">대상</param>
        private void ApplyStatusEffect(ITargetable target)
        {
            if (supportData == null || target == null || !target.IsAlive())
                return;

            var effectType = supportData.GetStatusEffectType();
            if (effectType == StatusEffectType.None)
                return;

            // 보스에 대한 CC 면역 체크
            bool isBoss = target.GetTransform().CompareTag(Tag.BossMonster);

            switch (effectType)
            {
                case StatusEffectType.CC:
                    // CC 타입에 따라 분기 (Slow/Stun)
                    var ccType = supportData.GetCCType();
                    if (ccType == CCType.Slow)
                    {
                        ApplySlow(target, supportData.cc_slow_amount, supportData.cc_duration);
                    }
                    else if (ccType == CCType.Stun)
                    {
                        if (!isBoss)
                        {
                            ApplyStun(target, supportData.cc_duration);
                        }
                        else
                        {
                            Debug.Log($"[Character] 보스 CC 면역: Stun");
                        }
                    }
                    break;

                case StatusEffectType.DOT:
                    ApplyDOT(target, supportData.dot_damage_per_tick, supportData.dot_duration, supportData.dot_tick_interval);
                    break;

                case StatusEffectType.Mark:
                    ApplyMark(target, supportData.mark_damage_mult, supportData.mark_duration);
                    break;

                case StatusEffectType.Chain:
                    // 체인은 타겟팅에서 처리 (BuildChainTargets)
                    break;

                default:
                    Debug.LogWarning($"[Character] Unknown status effect type: {effectType}");
                    break;
            }
        }

        #endregion

        #region Main Skill Status Effects

        /// <summary>
        /// 메인 스킬의 자체 효과 적용 (CSV 데이터 기반)
        /// AOE/DOT/디버프 스킬의 내장 효과
        /// </summary>
        /// <param name="target">대상</param>
        /// <param name="skillData">스킬 데이터</param>
        private void ApplyMainSkillEffectsToTarget(ITargetable target, MainSkillData skillData)
        {
            if (target == null || skillData == null || !target.IsAlive())
                return;

            bool isBoss = target.GetTransform().CompareTag(Tag.BossMonster);

            // CC 효과 적용 (스턴, 슬로우 등)
            if (skillData.HasCCEffect)
            {
                // 보스는 기본적으로 CC 면역 (stun_use 등으로 판단)
                if (!isBoss)
                {
                    ApplyMainSkillCC(target, skillData);
                }
                else
                {
                    Debug.Log($"[Character] 보스 CC 면역: {skillData.skill_name}");
                }
            }

            // DOT 효과 적용 (지속 피해)
            if (skillData.HasDOTEffect)
            {
                ApplyDOT(target, skillData.dot_damage_per_tick, skillData.dot_duration, skillData.dot_tick_interval);
            }

            // 표식 효과 적용 (데미지 증폭)
            if (skillData.HasMarkEffect)
            {
                ApplyMark(target, skillData.mark_damage_mult, skillData.mark_duration);
            }

            // 디버프 효과 적용
            if (skillData.HasDebuffEffect)
            {
                var debuffType = skillData.GetDeBuffType();
                ApplyDebuff(target, debuffType, skillData.base_debuff_value, skillData.cc_duration);
            }
        }

        /// <summary>
        /// 메인 스킬의 CC 효과 적용 (cc_type ID 기반)
        /// </summary>
        private void ApplyMainSkillCC(ITargetable target, MainSkillData skillData)
        {
            var ccType = skillData.GetCCType();

            switch (ccType)
            {
                case CCType.Slow:
                    ApplySlow(target, skillData.cc_slow_amount / 100f, skillData.cc_duration);
                    break;
                case CCType.Stun:
                    ApplyStun(target, skillData.cc_duration);
                    break;
                case CCType.None:
                default:
                    // stun_use가 true면 스턴 적용
                    if (skillData.stun_use && skillData.cc_duration > 0)
                    {
                        ApplyStun(target, skillData.cc_duration);
                    }
                    break;
            }
        }

        #endregion

        #region Status Effect Implementation

        /// <summary>
        /// 슬로우 적용 - Monster는 ApplySlow, BossMonster는 ApplyCC 사용
        /// </summary>
        private void ApplySlow(ITargetable target, float slowPercent, float duration)
        {
            if (target.GetTransform().CompareTag(Tag.Monster))
            {
                Monster monster = target.GetTransform().GetComponent<Monster>();
                monster?.ApplySlow(slowPercent, duration);
            }
            else if (target.GetTransform().CompareTag(Tag.BossMonster))
            {
                // BossMonster는 ApplyCC로 슬로우 적용 (면역이지만 효과는 표시)
                BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                boss?.ApplyCC(CCType.Slow, duration, slowPercent, null);
            }
            Debug.Log($"[Character] Applied Slow: {slowPercent * 100}% for {duration}s");
        }

        /// <summary>
        /// 스턴 적용 - Monster는 ApplyDizzy, BossMonster는 ApplyCC 사용 (면역)
        /// </summary>
        private void ApplyStun(ITargetable target, float duration)
        {
            if (target.GetTransform().CompareTag(Tag.Monster))
            {
                Monster monster = target.GetTransform().GetComponent<Monster>();
                monster?.ApplyDizzy(duration); // Monster는 ApplyDizzy 사용
            }
            else if (target.GetTransform().CompareTag(Tag.BossMonster))
            {
                // BossMonster는 CC 면역 (ApplyCC는 효과만 표시)
                BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                boss?.ApplyCC(CCType.Stun, duration, 0f, null);
            }
            Debug.Log($"[Character] Applied Stun: {duration}s");
        }

        /// <summary>
        /// 넉백 적용 - Monster만 지원, BossMonster는 면역
        /// </summary>
        private void ApplyKnockback(ITargetable target, Vector3 sourcePos, float force)
        {
            if (target.GetTransform().CompareTag(Tag.Monster))
            {
                Monster monster = target.GetTransform().GetComponent<Monster>();
                monster?.ApplyKnockback(sourcePos, force); // sourcePosition 전달
            }
            // BossMonster는 넉백 면역 (메서드 없음)
            Debug.Log($"[Character] Applied Knockback: force={force}");
        }

        /// <summary>
        /// 당기기 적용 - Monster만 지원, BossMonster는 면역
        /// </summary>
        private void ApplyPull(ITargetable target, Vector3 pullTowards, float force)
        {
            if (target.GetTransform().CompareTag(Tag.Monster))
            {
                Monster monster = target.GetTransform().GetComponent<Monster>();
                // Pull: 대상을 당기는 방향으로 넉백 (역방향)
                monster?.ApplyKnockback(pullTowards, -force);
            }
            // BossMonster는 면역
            Debug.Log($"[Character] Applied Pull: force={force}");
        }

        /// <summary>
        /// 속박 적용 - Monster의 ApplyRoot 사용
        /// </summary>
        private void ApplyRoot(ITargetable target, float duration)
        {
            if (target.GetTransform().CompareTag(Tag.Monster))
            {
                Monster monster = target.GetTransform().GetComponent<Monster>();
                monster?.ApplyRoot(duration);
            }
            // BossMonster는 면역
            Debug.Log($"[Character] Applied Root: {duration}s");
        }

        /// <summary>
        /// 공포 적용 - 현재는 스턴으로 대체
        /// </summary>
        private void ApplyFear(ITargetable target, float duration)
        {
            ApplyStun(target, duration);
            Debug.Log($"[Character] Applied Fear (as Stun): {duration}s");
        }

        /// <summary>
        /// DOT 적용 - (DOTType, damagePerTick, tickInterval, duration, effectPrefab)
        /// </summary>
        private void ApplyDOT(ITargetable target, float damagePerTick, float duration, float tickInterval)
        {
            if (target.GetTransform().CompareTag(Tag.Monster))
            {
                Monster monster = target.GetTransform().GetComponent<Monster>();
                monster?.ApplyDOT(DOTType.Burn, damagePerTick, tickInterval, duration, null);
            }
            else if (target.GetTransform().CompareTag(Tag.BossMonster))
            {
                BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                boss?.ApplyDOT(DOTType.Burn, damagePerTick, tickInterval, duration, null);
            }
            Debug.Log($"[Character] Applied DOT: {damagePerTick} damage every {tickInterval}s for {duration}s");
        }

        /// <summary>
        /// 표식 적용 - (MarkType, duration, damageMultiplier, effectPrefab)
        /// </summary>
        private void ApplyMark(ITargetable target, float damageMultiplier, float duration)
        {
            if (target.GetTransform().CompareTag(Tag.Monster))
            {
                Monster monster = target.GetTransform().GetComponent<Monster>();
                monster?.ApplyMark(MarkType.Romance, duration, damageMultiplier, null);
            }
            else if (target.GetTransform().CompareTag(Tag.BossMonster))
            {
                BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                boss?.ApplyMark(MarkType.Romance, duration, damageMultiplier, null);
            }
            Debug.Log($"[Character] Applied Mark: {damageMultiplier}x damage for {duration}s");
        }

        /// <summary>
        /// 디버프 적용 - (DeBuffType, value, duration, effectPrefab)
        /// </summary>
        private void ApplyDebuff(ITargetable target, DeBuffType debuffType, float debuffValue, float duration)
        {
            if (target.GetTransform().CompareTag(Tag.Monster))
            {
                Monster monster = target.GetTransform().GetComponent<Monster>();
                monster?.ApplyDebuff(debuffType, debuffValue, duration, null);
            }
            else if (target.GetTransform().CompareTag(Tag.BossMonster))
            {
                BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                boss?.ApplyDebuff(debuffType, debuffValue, duration, null);
            }
            Debug.Log($"[Character] Applied Debuff: {debuffType} -{debuffValue}% for {duration}s");
        }

        #endregion
    }
}
