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
        [Header("Runtime Data")]
        private MainSkillData mainSkill;
        private SupportSkillData supportSkill;

        [Header("AOE Settings")]
        private float radius;
        private float damage;
        private float duration;
        private float tickInterval = 1f;

        [Header("State")]
        private bool isInitialized;
        private bool isGround;      // 지속 장판
        private bool isLinear;      // 직선 이동
        private bool isFalling;     // 낙하 (운석 등)
        private Vector3 moveDirection;
        private Vector3 fallTarget; // 낙하 목표 위치
        private float moveSpeed;
        private float fallSpeed = 25f; // 낙하 속도

        [Header("Tick Tracking")]
        private HashSet<int> damagedTargets = new HashSet<int>();
        private float tickTimer;

        public void Initialize(MainSkillData main, SupportSkillData support, bool isGround = false, bool isLinear = false, Vector3 moveDirection = default, bool isFalling = false, Vector3 fallTarget = default)
        {
            mainSkill = main;
            supportSkill = support;
            this.isGround = isGround;
            this.isLinear = isLinear;
            this.moveDirection = moveDirection;
            this.isFalling = isFalling;
            this.fallTarget = fallTarget;

            // 기본 값 설정
            damage = SkillExecutor.CalculateDamage(main, support);
            radius = main.aoe_radius > 0 ? main.aoe_radius : 3f;
            duration = main.duration > 0 ? main.duration : 2f;

            // 직선 이동 속도
            if (isLinear)
            {
                moveSpeed = main.projectile_speed > 0 ? main.projectile_speed : 8f;
            }

            // 낙하 속도 (projectile_speed 사용하거나 기본값 25)
            if (isFalling)
            {
                fallSpeed = main.projectile_speed > 0 ? main.projectile_speed : 25f;
            }

            // 서포트 스킬 효과
            if (support != null)
            {
                // 범위 증가
                if (support.IsAreaUpSupport && support.scale_multiplier > 0)
                {
                    radius *= support.scale_multiplier;
                    transform.localScale *= support.scale_multiplier;
                }

                // 틱 데미지 설정
                if (support.HasTickDamage)
                {
                    tickInterval = support.tick_interval;
                }

                // Linger 서포트 - 잔류 효과
                if (support.IsLingerSupport && support.duration > 0)
                {
                    duration += support.duration;
                }
            }

            isInitialized = true;

            // 낙하 타입은 별도 처리
            if (isFalling)
            {
                StartFallingAsync().Forget();
            }
            else
            {
                // AOE 시작
                StartAOEAsync().Forget();
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

            // 착지 시 데미지
            ApplyDamageToTargetsInRadius();

            // 잠시 대기 후 삭제 (이펙트 재생 시간)
            await UniTask.Delay(1500);
            DestroyAOE();
        }

        private async UniTaskVoid StartAOEAsync()
        {
            float elapsed = 0f;

            // 즉시 데미지 (TargetAOE, LinearAOE의 경우)
            if (!isGround)
            {
                ApplyDamageToTargetsInRadius();
            }

            // 지속 장판 또는 이동 AOE
            while (elapsed < duration && isInitialized)
            {
                if (isLinear)
                {
                    // 직선 이동
                    transform.position += moveDirection * moveSpeed * Time.deltaTime;
                }

                if (isGround)
                {
                    // 틱 데미지
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

            // AOE 종료
            DestroyAOE();
        }

        private void ApplyDamageToTargetsInRadius()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

            foreach (var col in colliders)
            {
                if (!col.CompareTag(Tag.Monster) && !col.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = null;
                if (col.CompareTag(Tag.Monster))
                {
                    target = col.GetComponent<Monster>();
                }
                else if (col.CompareTag(Tag.BossMonster))
                {
                    target = col.GetComponent<BossMonster>();
                }

                if (target != null && target.IsAlive())
                {
                    target.TakeDamage(damage);
                }
            }
        }

        private void ApplyTickDamage()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

            foreach (var col in colliders)
            {
                if (!col.CompareTag(Tag.Monster) && !col.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = null;
                if (col.CompareTag(Tag.Monster))
                {
                    target = col.GetComponent<Monster>();
                }
                else if (col.CompareTag(Tag.BossMonster))
                {
                    target = col.GetComponent<BossMonster>();
                }

                if (target != null && target.IsAlive())
                {
                    // 틱 데미지 계산
                    float tickDamage = damage * 0.2f; // 기본 20%

                    if (supportSkill != null && supportSkill.tick_damage > 0)
                    {
                        tickDamage = supportSkill.tick_damage;
                    }

                    target.TakeDamage(tickDamage);

                    // DOT 서포트 적용 (Poison, Burn)
                    ApplyDOTEffects(target);
                }
            }
        }

        private void ApplyDOTEffects(ITargetable target)
        {
            if (supportSkill == null) return;

            if (supportSkill.IsDOTSupport)
            {
                DOTType dotType = supportSkill.IsPoisonSupport ? DOTType.Poison : DOTType.Burn;
                float dotDamage = supportSkill.tick_damage;
                float dotInterval = supportSkill.tick_interval;
                float dotDuration = supportSkill.duration;

                // Monster/BossMonster에 DOT 적용
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    var monster = target.GetTransform().GetComponent<Monster>();
                    monster?.ApplyDOT(dotType, dotDamage, dotInterval, dotDuration, null);
                }
                else if (target.GetTransform().CompareTag(Tag.BossMonster))
                {
                    var boss = target.GetTransform().GetComponent<BossMonster>();
                    boss?.ApplyDOT(dotType, dotDamage, dotInterval, dotDuration, null);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized || !isLinear) return;

            // 직선 AOE의 경우 충돌 시 데미지
            if (!other.CompareTag(Tag.Monster) && !other.CompareTag(Tag.BossMonster))
                return;

            int targetId = other.GetInstanceID();
            if (damagedTargets.Contains(targetId))
                return;

            ITargetable target = null;
            if (other.CompareTag(Tag.Monster))
            {
                target = other.GetComponent<Monster>();
            }
            else if (other.CompareTag(Tag.BossMonster))
            {
                target = other.GetComponent<BossMonster>();
            }

            if (target != null && target.IsAlive())
            {
                target.TakeDamage(damage);
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
