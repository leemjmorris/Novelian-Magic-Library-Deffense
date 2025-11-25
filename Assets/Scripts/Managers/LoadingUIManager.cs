using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.AddressableAssets;
using System.Threading;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LCB: 로딩 UI 매니저 - 모든 씬에서 유지되는 DontDestroyOnLoad 오브젝트
    /// LCB: 씬 전환 중 로딩 진행률, 팁 메시지 등을 표시
    /// </summary>
    public class LoadingUIManager : MonoBehaviour
    {
        private static LoadingUIManager instance;
        public static LoadingUIManager Instance
        {
            get
            {
                return instance;
            }
        }

        // LCB: UI 컴포넌트들 (Inspector에서 할당)
        [Header("UI Components (Assign in Inspector)")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Image progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI loadingTipText;

        [Header("Loading Tips")]
        private string[] loadingTips = new string[] // LCB: 로딩 중 표시할 팁 목록
        {
            "Tip\n도서관을 지키기 위해\n사서들을 배치하세요!",
            "Tip\n책갈피를 장착하면\n캐릭터가 더 강해집니다.",
            "Tip\n스킬을 잘 조합하면\n시너지 효과를 얻을 수 있습니다.",
            "Tip\n몬스터의 장르를 확인하고\n전략을 세우세요.",
            "Tip\n행동력을 효율적으로\n사용하세요!",
            "Tip\nUI를 때리지 마세요.\n이미 충분히 맞았습니다.",
        };

        [Header("Settings")]
        [SerializeField] private float minimumDisplayTime = 1.0f; // LCB: 최소 표시 시간 (너무 빠른 깜빡임 방지)
        [SerializeField] private int smoothSteps = 100; // LCB: 부드러운 진행률 업데이트를 위한 단계 수

        private bool isShowing = false;
        private CancellationTokenSource loadingCts; // LCB: 로딩 취소용 토큰
        private float currentProgress = 0f; // LCB: 현재 표시 중인 진행률

        private void Awake()
        {
            // LCB: 중복 인스턴스 방지
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // LCB: 시작 시 로딩 패널 비활성화
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        /// <summary>
        /// LCB: 로딩 UI 표시 (랜덤 팁과 함께)
        /// LCB: FadeController에서 씬 로딩 시작 시 호출됨
        /// </summary>
        public void Show()
        {
            if (loadingPanel == null) return;

            loadingPanel.SetActive(true);
            isShowing = true;

            // LCB: 랜덤 로딩 팁 설정
            if (loadingTips != null && loadingTips.Length > 0 && loadingTipText != null)
            {
                int randomIndex = Random.Range(0, loadingTips.Length);
                loadingTipText.text = loadingTips[randomIndex]; // LCB: 팁 배열에서 랜덤 선택
            }

            // LCB: 진행률 초기화 (0%부터 시작)
            SetProgress(0f);

            Debug.Log("[LoadingUIManager] Loading UI shown");
        }

        /// <summary>
        /// LCB: 로딩 UI 숨기기
        /// LCB: 최소 표시 시간을 보장하여 너무 빨리 사라지지 않도록 함
        /// </summary>
        public async UniTask Hide()
        {
            if (loadingPanel == null || !isShowing) return;

            // LCB: 최소 표시 시간 보장 (UX 개선)
            await UniTask.Delay((int)(minimumDisplayTime * 1000));

            loadingPanel.SetActive(false);
            isShowing = false;

            Debug.Log("[LoadingUIManager] Loading UI hidden");
        }

        /// <summary>
        /// LCB: 로딩 진행률 업데이트 (0.0 ~ 1.0)
        /// LCB: 진행률 바와 퍼센트 텍스트 동시 업데이트
        /// </summary>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress); // LCB: 0~1 범위로 제한
            currentProgress = progress; // LCB: 현재 진행률 저장

            if (progressBar != null)
            {
                progressBar.fillAmount = progress; // LCB: Fill Image 업데이트
            }

            if (progressText != null)
            {
                progressText.text = $"Loading... {(progress * 100):F0}%"; // LCB: 퍼센트 표시
            }
        }

        /// <summary>
        /// LCB: UniTaskManager 패턴 - 1부터 100%까지 1%씩 순차적으로 증가하는 로딩
        /// LCB: 실제 씬 로딩과 무관하게 일정 시간동안 진행률을 증가시킴
        /// </summary>
        /// <param name="durationMs">전체 로딩 애니메이션 시간 (밀리초)</param>
        /// <param name="ct">취소 토큰</param>
        public async UniTask FakeLoadAsync(int durationMs, CancellationToken ct = default)
        {
            int stepDelay = durationMs / 100; // LCB: 100단계로 나누기 (1%씩)
            SetProgress(0f); // LCB: 0%부터 시작

            // LCB: 1%부터 100%까지 1%씩 증가
            for (int i = 1; i <= 100; i++)
            {
                ct.ThrowIfCancellationRequested(); // LCB: 취소 요청 확인

                float progress = i / 100f; // LCB: 1%, 2%, 3%, ..., 100%
                SetProgress(progress);

                await UniTask.Delay(stepDelay, cancellationToken: ct); // LCB: 각 1%마다 딜레이
            }
        }

        /// <summary>
        /// LCB: 목표 진행률까지 부드럽게 보간하는 방식
        /// LCB: 실제 AsyncOperation.progress를 추적하되 값 변화를 부드럽게 만듦
        /// LCB: SetProgress()를 직접 호출하는 것보다 시각적으로 부드러움
        /// </summary>
        /// <param name="targetProgress">목표 진행률 (0.0 ~ 1.0)</param>
        /// <param name="duration">보간 시간 (초)</param>
        /// <param name="ct">취소 토큰</param>
        public async UniTask SmoothSetProgressAsync(float targetProgress, float duration = 0.3f, CancellationToken ct = default)
        {
            float startProgress = currentProgress; // LCB: 현재 진행률에서 시작
            targetProgress = Mathf.Clamp01(targetProgress); // LCB: 0~1 범위로 제한

            float elapsed = 0f;

            // LCB: duration 시간 동안 부드럽게 보간
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested(); // LCB: 취소 요청 확인

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration); // LCB: 0~1 사이의 보간 비율
                float newProgress = Mathf.Lerp(startProgress, targetProgress, t); // LCB: 선형 보간

                SetProgress(newProgress);

                await UniTask.Yield(cancellationToken: ct); // LCB: 다음 프레임까지 대기
            }

            // LCB: 최종 값 보장
            SetProgress(targetProgress);
        }

        /// <summary>
        /// LCB: 여러 단계의 로딩을 순차적으로 진행 (UniTaskManager 패턴)
        /// LCB: 예: 씬 로딩(0-30%), 리소스 로딩(30-70%), 초기화(70-100%)
        /// </summary>
        /// <param name="stages">각 단계의 (목표진행률, 지속시간ms) 배열</param>
        /// <param name="ct">취소 토큰</param>
        public async UniTask LoadInStagesAsync((float targetProgress, int durationMs)[] stages, CancellationToken ct = default)
        {
            SetProgress(0f); // LCB: 0%부터 시작

            foreach (var (targetProgress, durationMs) in stages)
            {
                ct.ThrowIfCancellationRequested(); // LCB: 취소 요청 확인

                float startProgress = currentProgress;
                float target = Mathf.Clamp01(targetProgress);
                int stepDelay = Mathf.Max(1, durationMs / smoothSteps);

                // LCB: 현재 진행률에서 목표 진행률까지 부드럽게 증가
                for (int i = 1; i <= smoothSteps; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    float t = i / (float)smoothSteps; // LCB: 0~1 보간 비율
                    float progress = Mathf.Lerp(startProgress, target, t); // LCB: 선형 보간
                    SetProgress(progress);

                    await UniTask.Delay(stepDelay, cancellationToken: ct);
                }

                SetProgress(target); // LCB: 각 단계 최종 값 보장
            }
        }

        /// <summary>
        /// LCB: 현재 로딩 UI가 표시 중인지 확인
        /// </summary>
        public bool IsShowing => isShowing;

        /// <summary>
        /// LCB: 로딩 UI 진행률을 표시한 후 페이드 효과와 함께 씬을 전환합니다
        /// LCB: 실행 순서: LoadingUI 표시 → 진행률 1-100% 순차적으로 → FadeController로 씬 전환
        /// </summary>
        /// <param name="addressableKey">Addressable 씬 키</param>
        /// <param name="loadingDurationMs">로딩 진행률 표시 시간 (기본: 5000ms = 5초)</param>
        /// <param name="ct">취소 토큰</param>
        public async UniTask LoadSceneWithProgress(
            string addressableKey,
            int loadingDurationMs = 3000,
            CancellationToken ct = default
        )
        {
            try
            {
                // Step 1: LoadingUI 표시
                Show();

                // Step 2: 진행률 0-100% 애니메이션
                await FakeLoadAsync(loadingDurationMs, ct);

                // Step 3: 100% 상태를 잠깐 보여줌 (선택적)
                await UniTask.Delay(200, cancellationToken: ct);

                // Step 4: LoadingUI 숨기기
                await Hide();

                // Step 5: FadeController로 씬 전환
                if (Core.FadeController.Instance != null)
                {
                    await Core.FadeController.Instance.LoadSceneWithFade(addressableKey);
                }
                else
                {
                    Debug.LogWarning("[LoadingUIManager] FadeController not available, loading scene directly");
                    await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(addressableKey);
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[LoadingUIManager] LoadSceneWithProgress was cancelled");
                await Hide(); // 취소되어도 UI는 숨김
                throw;
            }
        }

        private void OnDestroy()
        {
            // LCB: 진행 중인 로딩 취소
            loadingCts?.Cancel();
            loadingCts?.Dispose();
        }
    }
}
