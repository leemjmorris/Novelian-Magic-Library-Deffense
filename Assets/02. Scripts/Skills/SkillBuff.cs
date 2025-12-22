using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 버프 - 캐릭터에게 일시적 스탯 증가 효과 적용
    /// </summary>
    public class SkillBuff : MonoBehaviour
    {
        #region Runtime Data
        private MainSkillData mainSkill;
        private SupportSkillData supportSkill;
        private Character targetCharacter;
        #endregion

        #region Buff Settings
        private float duration;
        private float radius;
        private BuffType buffType;
        private float buffValue;
        #endregion

        #region State
        private bool isApplied;
        #endregion

        #region Constants
        private const float DEFAULT_DURATION = 8f;
        private const float DEFAULT_RADIUS = 5f;
        private const float DEFAULT_BUFF_VALUE = 20f;
        #endregion

        public void Initialize(MainSkillData main, SupportSkillData support, Character caster)
        {
            mainSkill = main;
            supportSkill = support;
            targetCharacter = caster;

            duration = main.duration > 0 ? main.duration : DEFAULT_DURATION;
            radius = main.aoe_radius > 0 ? main.aoe_radius : DEFAULT_RADIUS;

            // 버프 타입 및 값 결정 (기본: 공격력 증가)
            buffType = BuffType.ATK_Damage_UP;
            buffValue = main.base_damage > 0 ? main.base_damage : DEFAULT_BUFF_VALUE;

            ApplyBuffAsync().Forget();
        }

        private async UniTaskVoid ApplyBuffAsync()
        {
            if (targetCharacter == null)
            {
                DestroyBuff();
                return;
            }

            // 버프 적용
            ApplyBuffEffect();
            isApplied = true;

            // 지속시간 대기
            await UniTask.Delay((int)(duration * 1000));

            // 버프 해제
            RemoveBuffEffect();

            DestroyBuff();
        }

        private void ApplyBuffEffect()
        {
            if (targetCharacter == null) return;

            switch (buffType)
            {
                case BuffType.ATK_Damage_UP:
                    targetCharacter.ApplyBuffModifier("damage", buffValue);
                    break;

                case BuffType.ATK_Speed_UP:
                    targetCharacter.ApplyBuffModifier("attackSpeed", buffValue);
                    break;

                case BuffType.ATK_Range_UP:
                    targetCharacter.ApplyBuffModifier("range", buffValue);
                    break;

                case BuffType.Critical_Chance_UP:
                    targetCharacter.ApplyBuffModifier("critChance", buffValue);
                    break;

                case BuffType.Critical_Damage_UP:
                    targetCharacter.ApplyBuffModifier("critMultiplier", buffValue);
                    break;

                case BuffType.Defense_UP:
                    targetCharacter.ApplyBuffModifier("defense", buffValue);
                    break;

                case BuffType.Move_Speed_UP:
                    targetCharacter.ApplyBuffModifier("moveSpeed", buffValue);
                    break;
            }
        }

        private void RemoveBuffEffect()
        {
            if (targetCharacter == null) return;

            switch (buffType)
            {
                case BuffType.ATK_Damage_UP:
                    targetCharacter.RemoveBuffModifier("damage", buffValue);
                    break;

                case BuffType.ATK_Speed_UP:
                    targetCharacter.RemoveBuffModifier("attackSpeed", buffValue);
                    break;

                case BuffType.ATK_Range_UP:
                    targetCharacter.RemoveBuffModifier("range", buffValue);
                    break;

                case BuffType.Critical_Chance_UP:
                    targetCharacter.RemoveBuffModifier("critChance", buffValue);
                    break;

                case BuffType.Critical_Damage_UP:
                    targetCharacter.RemoveBuffModifier("critMultiplier", buffValue);
                    break;

                case BuffType.Defense_UP:
                    targetCharacter.RemoveBuffModifier("defense", buffValue);
                    break;

                case BuffType.Move_Speed_UP:
                    targetCharacter.RemoveBuffModifier("moveSpeed", buffValue);
                    break;
            }
        }

        private void OnDestroy()
        {
            if (isApplied && targetCharacter != null)
            {
                RemoveBuffEffect();
            }
        }

        private void DestroyBuff()
        {
            Destroy(gameObject);
        }
    }
}
