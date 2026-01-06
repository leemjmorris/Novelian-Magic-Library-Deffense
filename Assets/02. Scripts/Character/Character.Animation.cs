//LMJ : Character partial class - Animation Control
namespace Novelian.Combat
{
    using UnityEngine;

    /// <summary>
    /// 캐릭터 애니메이션 제어
    /// - 공격 애니메이션 재생
    /// - 스킬 애니메이션 트리거
    /// - Animator 파라미터 관리
    /// </summary>
    public partial class Character
    {
        #region Animation Constants

        // Animator Parameter Names
        private const string ANIM_ATTACK = "Attack";
        private const string ANIM_SKILL = "Skill";
        private const string ANIM_IS_ATTACKING = "IsAttacking";
        private const string ANIM_ATTACK_SPEED = "AttackSpeed";

        #endregion

        #region Animation Methods

        /// <summary>
        /// JML: 공격 애니메이션 재생
        /// TryAttack/TryUseActiveSkill에서 호출
        /// </summary>
        private void PlayAttackAnimation()
        {
            if (characterAnimator == null)
            {
                // Animator가 없으면 무시 (애니메이션 없는 캐릭터도 지원)
                return;
            }

            // Attack 트리거 설정
            if (HasAnimatorParameter(ANIM_ATTACK))
            {
                characterAnimator.SetTrigger(ANIM_ATTACK);
            }

            // 공격 속도에 따른 애니메이션 속도 조절
            if (HasAnimatorParameter(ANIM_ATTACK_SPEED))
            {
                float animSpeed = Mathf.Clamp(FinalAttackSpeed, 0.5f, 3f);
                characterAnimator.SetFloat(ANIM_ATTACK_SPEED, animSpeed);
            }
        }

        /// <summary>
        /// 스킬 애니메이션 재생 (액티브 스킬용)
        /// </summary>
        private void PlaySkillAnimation()
        {
            if (characterAnimator == null)
                return;

            if (HasAnimatorParameter(ANIM_SKILL))
            {
                characterAnimator.SetTrigger(ANIM_SKILL);
            }
        }

        /// <summary>
        /// 공격 중 상태 설정 (채널링 등)
        /// </summary>
        private void SetAttackingState(bool isAttacking)
        {
            if (characterAnimator == null)
                return;

            if (HasAnimatorParameter(ANIM_IS_ATTACKING))
            {
                characterAnimator.SetBool(ANIM_IS_ATTACKING, isAttacking);
            }
        }

        /// <summary>
        /// Animator 파라미터 존재 여부 확인
        /// </summary>
        private bool HasAnimatorParameter(string paramName)
        {
            if (characterAnimator == null)
                return false;

            foreach (AnimatorControllerParameter param in characterAnimator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 애니메이션 속도 배율 설정
        /// </summary>
        public void SetAnimationSpeed(float speedMultiplier)
        {
            if (characterAnimator == null)
                return;

            characterAnimator.speed = speedMultiplier;
        }

        /// <summary>
        /// 특정 애니메이션 클립 재생
        /// </summary>
        public void PlayAnimationClip(string stateName, int layer = 0)
        {
            if (characterAnimator == null)
                return;

            characterAnimator.Play(stateName, layer);
        }

        /// <summary>
        /// 애니메이션 리셋 (Idle 상태로)
        /// </summary>
        public void ResetAnimation()
        {
            if (characterAnimator == null)
                return;

            characterAnimator.Rebind();
            characterAnimator.Update(0f);
        }

        /// <summary>
        /// 승리 애니메이션 재생 (StageStateManager에서 호출)
        /// </summary>
        public void PlayVictoryAnimation()
        {
            if (characterAnimator == null)
                return;

            if (HasAnimatorParameter("Victory"))
            {
                characterAnimator.SetTrigger("Victory");
            }
            else if (HasAnimatorParameter("Win"))
            {
                characterAnimator.SetTrigger("Win");
            }

            GameLog.Log($"[Character] Playing victory animation");
        }

        /// <summary>
        /// 사망 애니메이션 재생 (StageStateManager에서 호출)
        /// </summary>
        public void PlayDieAnimation()
        {
            if (characterAnimator == null)
                return;

            if (HasAnimatorParameter("Die"))
            {
                characterAnimator.SetTrigger("Die");
            }
            else if (HasAnimatorParameter("Death"))
            {
                characterAnimator.SetTrigger("Death");
            }

            GameLog.Log($"[Character] Playing die animation");
        }

        #endregion
    }
}
