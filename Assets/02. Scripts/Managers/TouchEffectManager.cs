using UnityEngine;
using Coffee.UIExtensions;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// 터치/클릭 입력 시 시각적 피드백 이펙트를 표시하는 매니저
    /// UIParticle (ParticleEffectForUGUI) 기반으로 UI Canvas 내에서 파티클 렌더링
    /// DontDestroyOnLoad 싱글톤 패턴으로 모든 씬에서 동일하게 동작
    /// </summary>
    public class TouchEffectManager : MonoBehaviour
    {
        private static TouchEffectManager instance;
        public static TouchEffectManager Instance => instance;

        [Header("Effect Settings")]
        [SerializeField] private UIParticle tapEffect;
        [SerializeField] private bool effectEnabled = true;

        [Header("Canvas Reference")]
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private RectTransform canvasRectTransform;

        private Camera uiCamera;
        private ParticleSystem[] childParticleSystems;
        private bool isEventSubscribed;

        private void Awake()
        {
            // Singleton 패턴 (DontDestroyOnLoad)
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // DontDestroyOnLoad를 위해 root로 이동
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            // Canvas 참조 설정
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
            }

            if (parentCanvas != null)
            {
                canvasRectTransform = parentCanvas.GetComponent<RectTransform>();

                // Screen Space - Overlay가 아닌 경우 카메라 참조 필요
                if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    uiCamera = parentCanvas.worldCamera;
                }
            }

            // UIParticle 초기 상태 설정
            if (tapEffect != null)
            {
                tapEffect.Stop();
                // 자식 ParticleSystem 캐싱
                childParticleSystems = tapEffect.GetComponentsInChildren<ParticleSystem>(true);

                // TimeScale=0에서도 재생되도록 설정
                foreach (var ps in childParticleSystems)
                {
                    if (ps != null)
                    {
                        var main = ps.main;
                        main.useUnscaledTime = true;
                    }
                }
            }

            // Awake에서 이벤트 구독 (OnEnable/OnDisable 대신)
            SubscribeToInputEvents();

            GameLog.Log("[TouchEffectManager] Initialized successfully");
        }

        private void SubscribeToInputEvents()
        {
            if (isEventSubscribed) return;

            InputManager.OnShortPress += HandleTap;
            isEventSubscribed = true;
        }

        private void UnsubscribeFromInputEvents()
        {
            if (!isEventSubscribed) return;

            InputManager.OnShortPress -= HandleTap;
            isEventSubscribed = false;
        }

        /// <summary>
        /// 터치/클릭 이벤트 처리
        /// </summary>
        /// <param name="screenPosition">스크린 좌표</param>
        private void HandleTap(Vector2 screenPosition)
        {
            if (!effectEnabled || tapEffect == null)
            {
                return;
            }

            // 스크린 좌표를 Canvas 로컬 좌표로 변환
            if (TryConvertScreenToCanvasPosition(screenPosition, out Vector2 localPosition))
            {
                // UIParticle 위치 설정 및 재생
                tapEffect.rectTransform.anchoredPosition = localPosition;

                // 연타 시에도 즉시 재생되도록 모든 ParticleSystem 리셋
                ResetAndPlayParticles();
            }
        }

        /// <summary>
        /// 모든 파티클 시스템을 완전히 리셋하고 재생
        /// </summary>
        private void ResetAndPlayParticles()
        {
            // 자식 ParticleSystem들도 함께 리셋
            if (childParticleSystems != null)
            {
                foreach (var ps in childParticleSystems)
                {
                    if (ps != null)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Clear(true);
                        ps.Play(true);
                    }
                }
            }

            // UIParticle도 리셋
            tapEffect.Stop();
            tapEffect.Clear();
            tapEffect.Play();
        }

        /// <summary>
        /// 스크린 좌표를 Canvas 로컬 좌표로 변환
        /// </summary>
        private bool TryConvertScreenToCanvasPosition(Vector2 screenPosition, out Vector2 localPosition)
        {
            localPosition = Vector2.zero;

            if (canvasRectTransform == null)
            {
                return false;
            }

            // RectTransformUtility를 사용하여 스크린 좌표를 로컬 좌표로 변환
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPosition,
                uiCamera, // Screen Space - Overlay인 경우 null
                out localPosition
            );
        }

        /// <summary>
        /// 이펙트 활성화/비활성화 설정
        /// </summary>
        public void SetEffectEnabled(bool enabled)
        {
            effectEnabled = enabled;
            GameLog.Log($"[TouchEffectManager] Effect enabled: {enabled}");
        }

        /// <summary>
        /// 현재 이펙트 활성화 상태 반환
        /// </summary>
        public bool IsEffectEnabled()
        {
            return effectEnabled;
        }

        private void OnDestroy()
        {
            if (instance != this) return;

            UnsubscribeFromInputEvents();
            instance = null;
            GameLog.Log("[TouchEffectManager] Destroyed");
        }
    }
}
