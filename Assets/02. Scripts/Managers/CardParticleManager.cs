using UnityEngine;
using Coffee.UIExtensions;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// 카드 선택 패널 UI 파티클 관리자 (Issue #559)
    /// 패널이 열릴 때 배경 파티클 효과 재생
    /// </summary>
    public class CardParticleManager : MonoBehaviour
    {
        [Header("Background Effect")]
        [Tooltip("패널 열릴 때 재생되는 배경 파티클 (ConfettiRain)")]
        [SerializeField] private UIParticle backgroundEffect;

        [Header("Settings")]
        [SerializeField] private bool effectEnabled = true;

        private ParticleSystem[] backgroundParticles;

        private void Awake()
        {
            InitializeUIParticle(backgroundEffect, ref backgroundParticles);
            Debug.Log("[CardParticleManager] Initialized");
        }

        private void InitializeUIParticle(UIParticle uiParticle, ref ParticleSystem[] particleSystems)
        {
            if (uiParticle == null) return;

            uiParticle.Stop();
            particleSystems = uiParticle.GetComponentsInChildren<ParticleSystem>(true);

            // TimeScale=0에서도 재생되도록 설정
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    var main = ps.main;
                    main.useUnscaledTime = true;
                }
            }
        }

        /// <summary>
        /// 배경 파티클 재생 (패널 열릴 때 호출)
        /// </summary>
        public void PlayBackgroundEffect()
        {
            if (!effectEnabled || backgroundEffect == null) return;

            ResetAndPlayParticles(backgroundEffect, backgroundParticles);
            Debug.Log("[CardParticleManager] Background effect started");
        }

        /// <summary>
        /// 배경 파티클 중지 (패널 닫힐 때 호출)
        /// </summary>
        public void StopBackgroundEffect()
        {
            if (backgroundEffect == null) return;

            backgroundEffect.Stop();
            Debug.Log("[CardParticleManager] Background effect stopped");
        }

        private void ResetAndPlayParticles(UIParticle uiParticle, ParticleSystem[] particleSystems)
        {
            if (uiParticle == null) return;

            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    if (ps != null)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Clear(true);
                        ps.Play(true);
                    }
                }
            }

            uiParticle.Stop();
            uiParticle.Clear();
            uiParticle.Play();
        }
    }
}
