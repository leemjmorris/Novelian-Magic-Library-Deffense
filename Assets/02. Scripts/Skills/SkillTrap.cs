using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 트랩 - 적이 밟으면 발동
    /// </summary>
    public class SkillTrap : MonoBehaviour
    {
        #region Runtime Data
        private MainSkillData mainSkill;
        private SupportSkillData supportSkill;
        #endregion

        #region Trap Settings
        private float damage;
        private float radius;
        private float lifetime;
        #endregion

        #region State
        private bool isInitialized;
        private bool isTriggered;
        #endregion

        #region Constants
        private const float DEFAULT_RADIUS = 2f;
        private const float DEFAULT_LIFETIME = 10f;
        private const int DESTROY_DELAY_MS = 500;
        #endregion

        public void Initialize(MainSkillData main, SupportSkillData support)
        {
            mainSkill = main;
            supportSkill = support;

            damage = SkillExecutor.CalculateDamage(main, support);
            radius = main.aoe_radius > 0 ? main.aoe_radius : DEFAULT_RADIUS;
            lifetime = main.duration > 0 ? main.duration : DEFAULT_LIFETIME;

            isInitialized = true;
            isTriggered = false;

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
            if (!TargetableUtils.IsValidEnemy(other)) return;

            TriggerTrap();
        }

        private void TriggerTrap()
        {
            isTriggered = true;

            // 범위 내 모든 적에게 데미지 + CC 효과
            TargetableUtils.ApplyDamageInRadius(transform.position, radius, damage, ApplyCCEffects);

            DestroyTrapDelayed().Forget();
        }

        private void ApplyCCEffects(ITargetable target)
        {
            if (supportSkill == null || !supportSkill.IsCCSupport) return;

            Transform targetTransform = target.GetTransform();

            // 몬스터만 CC 효과 적용 (보스는 면역)
            if (!targetTransform.CompareTag(Tag.Monster)) return;

            Monster monster = targetTransform.GetComponent<Monster>();
            if (monster == null) return;

            // CC 타입에 따라 효과 적용
            CCType ccType = supportSkill.GetCCType();
            switch (ccType)
            {
                case CCType.Slow:
                    monster.ApplySlow(supportSkill.slow_rate, supportSkill.duration);
                    break;
                case CCType.Stun:
                    monster.ApplyDizzy(supportSkill.duration);
                    break;
            }
        }

        private async UniTaskVoid DestroyTrapDelayed()
        {
            await UniTask.Delay(DESTROY_DELAY_MS);
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
