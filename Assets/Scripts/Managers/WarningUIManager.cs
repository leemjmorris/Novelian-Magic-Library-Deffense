using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// 모든 씬에서 사용 가능한 경고/알림 UI 매니저
    /// BootScene에서 초기화되어 DontDestroyOnLoad로 유지됨
    /// </summary>
    public class WarningUIManager : MonoBehaviour
    {
        private static WarningUIManager instance;
        public static WarningUIManager Instance
        {
            get
            {
                return instance;
            }
        }

        [Header("Warning Panel (Assign in Inspector)")]
        [SerializeField] private GameObject warningPanel;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float displayDuration = 1.5f;

        private CanvasGroup warningCanvasGroup;
        private TextMeshProUGUI warningText;
        private CancellationTokenSource warningCts;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // 자식에서 컴포넌트 자동 탐색
            if (warningPanel != null)
            {
                warningCanvasGroup = warningPanel.GetComponent<CanvasGroup>();
                warningText = warningPanel.GetComponentInChildren<TextMeshProUGUI>();
                warningPanel.SetActive(false);
            }

            Debug.Log("[WarningUIManager] Initialized");
        }

        /// <summary>
        /// 경고 메시지 표시 (기본 메시지)
        /// </summary>
        public void ShowWarning()
        {
            ShowWarningAsync(WarningText.FeatureNotReady).Forget();
        }

        /// <summary>
        /// 경고 메시지 표시 (커스텀 메시지)
        /// </summary>
        public void ShowWarning(string message)
        {
            ShowWarningAsync(message).Forget();
        }

        /// <summary>
        /// 경고 메시지를 페이드 인/아웃으로 표시
        /// </summary>
        private async UniTaskVoid ShowWarningAsync(string message)
        {
            if (warningPanel == null || warningCanvasGroup == null || warningText == null)
            {
                Debug.LogWarning("[WarningUIManager] Warning panel references not assigned!");
                return;
            }

            // 기존 경고 애니메이션 취소
            warningCts?.Cancel();
            warningCts?.Dispose();
            warningCts = new CancellationTokenSource();
            var token = warningCts.Token;

            try
            {
                // 텍스트 설정 & 패널 활성화
                warningText.text = message;
                warningCanvasGroup.alpha = 0f;
                warningPanel.SetActive(true);

                // 페이드 인
                await FadeCanvasGroupAsync(0f, 1f, fadeDuration, token);

                // 대기
                await UniTask.Delay((int)(displayDuration * 1000), cancellationToken: token);

                // 페이드 아웃
                await FadeCanvasGroupAsync(1f, 0f, fadeDuration, token);

                // 패널 비활성화
                warningPanel.SetActive(false);
            }
            catch (System.OperationCanceledException)
            {
                // 취소됨 - 새 경고가 시작되므로 무시
            }
        }

        /// <summary>
        /// CanvasGroup 알파값 페이드
        /// </summary>
        private async UniTask FadeCanvasGroupAsync(float from, float to, float duration, CancellationToken token)
        {
            float elapsed = 0f;
            warningCanvasGroup.alpha = from;

            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                warningCanvasGroup.alpha = Mathf.Lerp(from, to, t);
                await UniTask.Yield(token);
            }

            warningCanvasGroup.alpha = to;
        }

        private void OnDestroy()
        {
            warningCts?.Cancel();
            warningCts?.Dispose();
        }
    }
}
