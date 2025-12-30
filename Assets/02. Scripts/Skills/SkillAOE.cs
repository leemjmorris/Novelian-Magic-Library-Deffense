using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 AOE - 범위 데미지, 지속 장판, 직선 이동 AOE 처리
    /// </summary>
    public class SkillAOE : MonoBehaviour
    {
        #region Runtime Data
        private MainSkillData mainSkill;
        private SupportSkillData supportSkill;
        private Character casterCharacter;
        private bool isCritical;
        #endregion

        #region AOE Settings
        private float radius;
        private float damage;
        private float duration;
        private float tickInterval;
        #endregion

        #region State
        private bool isInitialized;
        private bool isGround;
        private bool isLinear;
        private bool isFalling;
        private Vector3 moveDirection;
        private Vector3 fallTarget;
        private float moveSpeed;
        private float fallSpeed;
        #endregion

        #region Tick Tracking
        private HashSet<int> damagedTargets = new HashSet<int>();
        private float tickTimer;
        #endregion

        #region Constants
        private const float DEFAULT_RADIUS = 3f;
        private const float DEFAULT_DURATION = 2f;
        private const float DEFAULT_TICK_INTERVAL = 1f;
        private const float DEFAULT_MOVE_SPEED = 8f;
        private const float DEFAULT_FALL_SPEED = 25f;
        private const float DEFAULT_TICK_DAMAGE_RATIO = 0.2f;
        private const int FALLING_LAND_DELAY_MS = 1500;
        private const float DEFAULT_CC_DURATION = 3f;
        #endregion

        public void Initialize(MainSkillData main, SupportSkillData support, bool isGround = false, bool isLinear = false, Vector3 moveDirection = default, bool isFalling = false, Vector3 fallTarget = default, Character caster = null)
        {
            mainSkill = main;
            supportSkill = support;
            casterCharacter = caster;
            this.isGround = isGround;
            this.isLinear = isLinear;
            this.moveDirection = moveDirection;
            this.isFalling = isFalling;
            this.fallTarget = fallTarget;

            // 기본 값 설정 - Character 스탯 적용
            damage = SkillExecutor.CalculateDamage(main, support, caster, out isCritical);
            radius = main.aoe_radius > 0 ? main.aoe_radius : DEFAULT_RADIUS;
            duration = main.duration > 0 ? main.duration : DEFAULT_DURATION;
            tickInterval = DEFAULT_TICK_INTERVAL;

            // 이동/낙하 속도 설정 - Character 스탯 적용
            float baseSpeed = main.projectile_speed > 0 ? main.projectile_speed : DEFAULT_MOVE_SPEED;
            float baseFallSpeed = main.projectile_speed > 0 ? main.projectile_speed : DEFAULT_FALL_SPEED;

            if (caster != null)
            {
                float speedModifier = 1f + (caster.GetProjectileSpeedModifier() / 100f);
                moveSpeed = baseSpeed * speedModifier;
                fallSpeed = baseFallSpeed * speedModifier;
            }
            else
            {
                moveSpeed = baseSpeed;
                fallSpeed = baseFallSpeed;
            }

            // 서포트 스킬 효과 적용
            ApplySupportEffects(support);

            isInitialized = true;

            // AOE 시작
            if (isFalling)
            {
                StartFallingAsync().Forget();
            }
            else
            {
                StartAOEAsync().Forget();
            }
        }

        private void ApplySupportEffects(SupportSkillData support)
        {
            if (support == null) return;

            // 범위 증가 (Enhance 서포트의 scale_multiplier 사용)
            if (support.IsEnhanceSupport && support.scale_multiplier > 0)
            {
                radius *= support.scale_multiplier;
                transform.localScale *= support.scale_multiplier;
            }

            // 틱 데미지 설정
            if (support.HasTickDamage)
            {
                tickInterval = support.tick_interval;
            }
        }

        /// <summary>
        /// 낙하 AOE (운석, 폭격 등)
        /// </summary>
        private async UniTaskVoid StartFallingAsync()
        {
            // 낙하 목표까지 이동
            while (isInitialized && transform.position.y > fallTarget.y + 0.5f)
            {
                Vector3 dir = (fallTarget - transform.position).normalized;
                transform.position += dir * fallSpeed * Time.deltaTime;
                await UniTask.Yield();
            }

            // 착지 시 데미지 - 상성 시스템 적용 (Issue #531)
            if (casterCharacter != null)
            {
                TargetableUtils.ApplyDamageInRadius(transform.position, radius, damage, isCritical, casterCharacter.GetGenre());
            }
            else
            {
                TargetableUtils.ApplyDamageInRadius(transform.position, radius, damage);
            }

            // 이펙트 재생 대기 후 삭제
            await UniTask.Delay(FALLING_LAND_DELAY_MS);
            DestroyAOE();
        }

        private async UniTaskVoid StartAOEAsync()
        {
            float elapsed = 0f;

            // 즉시 데미지 (TargetAOE, LinearAOE의 경우) - 상성 시스템 적용 (Issue #531)
            if (!isGround)
            {
                if (casterCharacter != null)
                {
                    TargetableUtils.ApplyDamageInRadius(transform.position, radius, damage, isCritical, casterCharacter.GetGenre());
                }
                else
                {
                    TargetableUtils.ApplyDamageInRadius(transform.position, radius, damage);
                }
            }

            // 지속 장판 또는 직선 이동 AOE
            while (elapsed < duration && isInitialized)
            {
                // 직선 이동 (LinearAOE)
                if (isLinear)
                {
                    transform.position += moveDirection * moveSpeed * Time.deltaTime;
                }

                // 지속 장판 틱 데미지 (GroundAOE)
                if (isGround)
                {
                    tickTimer += Time.deltaTime;
                    if (tickTimer >= tickInterval)
                    {
                        tickTimer = 0f;
                        ApplyTickDamage();
                    }
                }

                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }

            DestroyAOE();
        }

        private void ApplyTickDamage()
        {
            float tickDamage = CalculateTickDamage();

            // 상성 시스템 적용 (Issue #531)
            if (casterCharacter != null)
            {
                TargetableUtils.ApplyDamageInRadius(transform.position, radius, tickDamage, casterCharacter.GetGenre(), target =>
                {
                    ApplyStatusEffects(target);
                });
            }
            else
            {
                TargetableUtils.ApplyDamageInRadius(transform.position, radius, tickDamage, target =>
                {
                    ApplyStatusEffects(target);
                });
            }
        }

        /// <summary>
        /// 상태효과 적용 (CC, DOT)
        /// </summary>
        private void ApplyStatusEffects(ITargetable target)
        {
            if (supportSkill == null) return;

            // CC 효과 적용
            if (supportSkill.IsCCSupport)
            {
                ApplyCCEffects(target);
            }

            // DOT 효과 적용
            if (supportSkill.IsDOTSupport)
            {
                ApplyDOTEffects(target);
            }
        }

        /// <summary>
        /// CC 효과 적용 (Slow/Stun)
        /// </summary>
        private void ApplyCCEffects(ITargetable target)
        {
            if (supportSkill == null || !supportSkill.IsCCSupport) return;

            Transform targetTransform = target.GetTransform();
            if (targetTransform == null) return;

            float ccDuration = supportSkill.duration > 0 ? supportSkill.duration : DEFAULT_CC_DURATION;
            CCType ccType = supportSkill.GetCCType();

            if (targetTransform.CompareTag(Tag.Monster))
            {
                Monster monster = targetTransform.GetComponent<Monster>();
                if (monster != null)
                {
                    if (ccType == CCType.Slow)
                    {
                        monster.ApplySlow(supportSkill.slow_rate, ccDuration);
                    }
                    else
                    {
                        monster.ApplyDizzy(ccDuration);
                    }
                }
            }
            else if (targetTransform.CompareTag(Tag.BossMonster))
            {
                BossMonster boss = targetTransform.GetComponent<BossMonster>();
                if (boss != null)
                {
                    boss.ApplyStun(ccDuration);
                }
            }
        }

        private float CalculateTickDamage()
        {
            if (supportSkill != null && supportSkill.tick_damage > 0)
            {
                return supportSkill.tick_damage;
            }

            return damage * DEFAULT_TICK_DAMAGE_RATIO;
        }

        private void ApplyDOTEffects(ITargetable target)
        {
            if (supportSkill == null || !supportSkill.IsDOTSupport) return;

            DOTType dotType = DOTType.Poison; // DOT 서포트 통합으로 기본값 사용
            float dotDamage = supportSkill.tick_damage;
            float dotInterval = supportSkill.tick_interval;
            float dotDuration = supportSkill.duration;

            Transform targetTransform = target.GetTransform();

            if (targetTransform.CompareTag(Tag.Monster))
            {
                targetTransform.GetComponent<Monster>()?.ApplyDOT(dotType, dotDamage, dotInterval, dotDuration, null);
            }
            else if (targetTransform.CompareTag(Tag.BossMonster))
            {
                targetTransform.GetComponent<BossMonster>()?.ApplyDOT(dotType, dotDamage, dotInterval, dotDuration, null);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized || !isLinear) return;
            if (!TargetableUtils.IsValidEnemy(other)) return;

            int targetId = other.GetInstanceID();
            if (damagedTargets.Contains(targetId)) return;

            ITargetable target = TargetableUtils.GetTargetable(other);
            if (target != null && target.IsAlive())
            {
                // 상성 시스템 적용 (Issue #531)
                if (casterCharacter != null)
                {
                    target.TakeDamage(damage, isCritical, casterCharacter.GetGenre());
                }
                else
                {
                    target.TakeDamage(damage, isCritical);
                }
                damagedTargets.Add(targetId);
            }
        }

        private void DestroyAOE()
        {
            isInitialized = false;
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
