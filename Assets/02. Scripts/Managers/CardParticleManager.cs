using UnityEngine;
using Coffee.UIExtensions;
using AssetKits.ParticleImage;
using AssetKits.ParticleImage.Enumerations;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// 카드 선택 패널 UI 파티클 관리자 (Issue #559)
    /// 패널이 열릴 때 배경 파티클 효과 재생
    /// UIParticle 또는 ParticleImage 둘 다 지원
    /// </summary>
    public class CardParticleManager : MonoBehaviour
    {
        private enum BackgroundEffectType
        {
            UIParticle,
            ParticleImage
        }

        [Header("Background Effect")]
        [Tooltip("배경 파티클 타입 선택")]
        [SerializeField] private BackgroundEffectType backgroundEffectType = BackgroundEffectType.UIParticle;

        [Tooltip("UIParticle 사용 시 (Coffee.UIExtensions)")]
        [SerializeField] private UIParticle backgroundEffectUIParticle;

        [Tooltip("ParticleImage 사용 시 (AssetKits.ParticleImage)")]
        [SerializeField] private ParticleImage backgroundEffectParticleImage;

        [Header("Settings")]
        [SerializeField] private bool effectEnabled = true;

        private ParticleSystem[] backgroundParticles;

        private void Awake()
        {
            if (backgroundEffectType == BackgroundEffectType.UIParticle)
            {
                InitializeUIParticle(backgroundEffectUIParticle, ref backgroundParticles);
            }
            else // ParticleImage
            {
                InitializeParticleImage(backgroundEffectParticleImage);
            }
            GameLog.Log($"[CardParticleManager] Initialized - Type: {backgroundEffectType}");
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

        private void InitializeParticleImage(ParticleImage particleImage)
        {
            if (particleImage == null) return;

            // TimeScale=0에서도 재생되도록 설정
            particleImage.timeScale = TimeScale.Unscaled;
            particleImage.Stop(true);
        }

        /// <summary>
        /// 배경 파티클 재생 (패널 열릴 때 호출)
        /// </summary>
        public void PlayBackgroundEffect()
        {
            if (!effectEnabled) return;

            if (backgroundEffectType == BackgroundEffectType.UIParticle)
            {
                if (backgroundEffectUIParticle == null) return;
                ResetAndPlayUIParticle(backgroundEffectUIParticle, backgroundParticles);
            }
            else // ParticleImage
            {
                if (backgroundEffectParticleImage == null) return;
                ResetAndPlayParticleImage(backgroundEffectParticleImage);
            }

            GameLog.Log($"[CardParticleManager] Background effect started - Type: {backgroundEffectType}");
        }

        /// <summary>
        /// 배경 파티클 중지 (패널 닫힐 때 호출)
        /// </summary>
        public void StopBackgroundEffect()
        {
            if (backgroundEffectType == BackgroundEffectType.UIParticle)
            {
                if (backgroundEffectUIParticle == null) return;
                backgroundEffectUIParticle.Stop();
            }
            else // ParticleImage
            {
                if (backgroundEffectParticleImage == null) return;
                backgroundEffectParticleImage.Stop(true);
            }

            GameLog.Log("[CardParticleManager] Background effect stopped");
        }

        private void ResetAndPlayUIParticle(UIParticle uiParticle, ParticleSystem[] particleSystems)
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

        private void ResetAndPlayParticleImage(ParticleImage particleImage)
        {
            if (particleImage == null) return;

            particleImage.Stop(true);
            particleImage.Clear();
            particleImage.Play();
        }
    }
}
