using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 트랩 - 적이 밟으면 발동
    /// </summary>
    public class SkillTrap : MonoBehaviour
    {
        [Header("Runtime Data")]
        private MainSkillData mainSkill;
        private SupportSkillData supportSkill;

        [Header("Trap Settings")]
        private float damage;
        private float radius;
        private float lifetime;

        [Header("State")]
        private bool isInitialized;
        private bool isTriggered;

        public void Initialize(MainSkillData main, SupportSkillData support)
        {
            mainSkill = main;
            supportSkill = support;

            damage = SkillExecutor.CalculateDamage(main, support);
            radius = main.aoe_radius > 0 ? main.aoe_radius : 2f;
            lifetime = main.duration > 0 ? main.duration : 10f;

            isInitialized = true;
            isTriggered = false;

            // 수명 타이머 시작
            StartLifetimeAsync().Forget();
        }

        private async UniTaskVoid StartLifetimeAsync()
        {
            await UniTask.Delay((int)(lifetime * 1000));

            if (!isTriggered && isInitialized)
            {
                DestroyTrap();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized || isTriggered) return;

            // 몬스터만 트랩 발동
            if (!other.CompareTag(Tag.Monster) && !other.CompareTag(Tag.BossMonster))
                return;

            TriggerTrap();
        }

        private void TriggerTrap()
        {
            isTriggered = true;


            // 범위 내 모든 적에게 데미지
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

                    // CC 효과 적용
                    ApplyCCEffects(target);
                }
            }

            // 트랩 파괴 (약간의 딜레이 후)
            DestroyTrapDelayed().Forget();
        }

        private void ApplyCCEffects(ITargetable target)
        {
            if (supportSkill == null) return;

            // 슬로우
            if (supportSkill.IsSlowSupport)
            {
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    var monster = target.GetTransform().GetComponent<Monster>();
                    monster?.ApplySlow(supportSkill.slow_rate, supportSkill.duration);
                }
            }

            // 스턴/빙결
            if (supportSkill.IsStunSupport || supportSkill.IsFreezeSupport)
            {
                // 보스는 스턴 면역
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    var monster = target.GetTransform().GetComponent<Monster>();
                    monster?.ApplyDizzy(supportSkill.duration);
                }
            }

            // 넉백
            if (supportSkill.IsKnockbackSupport)
            {
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    var monster = target.GetTransform().GetComponent<Monster>();
                    monster?.ApplyKnockback(transform.position, supportSkill.distance);
                }
            }
        }

        private async UniTaskVoid DestroyTrapDelayed()
        {
            await UniTask.Delay(500); // 0.5초 후 파괴
            DestroyTrap();
        }

        private void DestroyTrap()
        {
            isInitialized = false;
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
