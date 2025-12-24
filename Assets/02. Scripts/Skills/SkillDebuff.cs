using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 디버프 - 적에게 일시적 스탯 감소 효과 적용
    /// </summary>
    public class SkillDebuff : MonoBehaviour
    {
        #region Runtime Data
        private MainSkillData mainSkill;
        private SupportSkillData supportSkill;
        private ITargetable target;
        #endregion

        #region Debuff Settings
        private float duration;
        private float radius;
        private DeBuffType debuffType;
        private float debuffValue;
        #endregion

        #region Constants
        private const float DEFAULT_DURATION = 5f;
        private const float DEFAULT_RADIUS = 3f;
        private const float DEFAULT_DEBUFF_VALUE = 20f;
        #endregion

        public void Initialize(MainSkillData main, SupportSkillData support, ITargetable targetable)
        {
            mainSkill = main;
            supportSkill = support;
            target = targetable;

            duration = main.duration > 0 ? main.duration : DEFAULT_DURATION;
            radius = main.aoe_radius > 0 ? main.aoe_radius : DEFAULT_RADIUS;

            // 디버프 타입 및 값 결정 (기본: 방어력 감소)
            debuffType = DeBuffType.Defense_DOWN;
            debuffValue = main.base_damage > 0 ? main.base_damage : DEFAULT_DEBUFF_VALUE;

            // 서포트 스킬에서 디버프율 가져오기
            if (support != null && support.debuff_rate > 0)
            {
                debuffValue = support.debuff_rate * 100f;
            }

            ApplyDebuffAsync().Forget();
        }

        private async UniTaskVoid ApplyDebuffAsync()
        {
            // 범위 내 적에게 디버프 적용
            if (radius > 0)
            {
                ApplyDebuffToTargetsInRadius();
            }
            else if (target != null)
            {
                ApplyDebuffToTarget(target);
            }

            // 지속시간 대기
            await UniTask.Delay((int)(duration * 1000));

            DestroyDebuff();
        }

        private void ApplyDebuffToTargetsInRadius()
        {
            var targets = TargetableUtils.GetTargetsInRadius(transform.position, radius);

            foreach (var t in targets)
            {
                ApplyDebuffToTarget(t);
            }
        }

        private void ApplyDebuffToTarget(ITargetable targetable)
        {
            if (targetable == null || !targetable.IsAlive()) return;

            Transform targetTransform = targetable.GetTransform();

            // Monster에게 디버프 적용
            if (targetTransform.CompareTag(Tag.Monster))
            {
                Monster monster = targetTransform.GetComponent<Monster>();
                if (monster != null)
                {
                    ApplyDebuffToMonster(monster);
                }
            }
            // BossMonster에게 디버프 적용
            else if (targetTransform.CompareTag(Tag.BossMonster))
            {
                BossMonster boss = targetTransform.GetComponent<BossMonster>();
                if (boss != null)
                {
                    ApplyDebuffToBoss(boss);
                }
            }
        }

        private void ApplyDebuffToMonster(Monster monster)
        {
            switch (debuffType)
            {
                case DeBuffType.Defense_DOWN:
                    // 방어력 감소는 받는 피해 증가로 처리
                    monster.ApplyDebuff(DeBuffType.Take_Damage_UP, debuffValue, duration);
                    break;

                case DeBuffType.Move_Speed_DOWN:
                    monster.ApplySlow(debuffValue / 100f, duration);
                    break;

                case DeBuffType.ATK_Damage_DOWN:
                    monster.ApplyDebuff(DeBuffType.ATK_Damage_Down, debuffValue, duration);
                    break;

                case DeBuffType.Vulnerability:
                    monster.ApplyDebuff(DeBuffType.Take_Damage_UP, debuffValue, duration);
                    break;

                case DeBuffType.ATK_Speed_DOWN:
                    monster.ApplyDebuff(DeBuffType.ATK_Speed_Down, debuffValue, duration);
                    break;
            }
        }

        private void ApplyDebuffToBoss(BossMonster boss)
        {
            switch (debuffType)
            {
                case DeBuffType.Defense_DOWN:
                    // 방어력 감소는 받는 피해 증가로 처리
                    boss.ApplyDebuff(DeBuffType.Take_Damage_UP, debuffValue, duration);
                    break;

                case DeBuffType.Move_Speed_DOWN:
                    boss.ApplyDebuff(DeBuffType.ATK_Speed_Down, debuffValue, duration);
                    break;

                case DeBuffType.ATK_Damage_DOWN:
                    boss.ApplyDebuff(DeBuffType.ATK_Damage_Down, debuffValue, duration);
                    break;

                case DeBuffType.Vulnerability:
                    boss.ApplyDebuff(DeBuffType.Take_Damage_UP, debuffValue, duration);
                    break;

                case DeBuffType.ATK_Speed_DOWN:
                    boss.ApplyDebuff(DeBuffType.ATK_Speed_Down, debuffValue, duration);
                    break;
            }
        }

        private void DestroyDebuff()
        {
            Destroy(gameObject);
        }
    }
}
