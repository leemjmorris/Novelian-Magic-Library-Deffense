using System.Threading;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using TMPro;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    [Header("Warning Panel")]
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private CanvasGroup warningCanvasGroup;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float displayDuration = 1.5f;

    private const string FEATURE_NOT_READY_MESSAGE = "준비 중인 기능입니다";
    private CancellationTokenSource warningCts;

    // 왼쪽 버튼들
    public void OnPassTicketButton()
    {
        ShowWarningAsync(FEATURE_NOT_READY_MESSAGE).Forget();
    }

    public void OnNoticeButton()
    {
        ShowWarningAsync(FEATURE_NOT_READY_MESSAGE).Forget();
    }

    public void OnAttendanceButton()
    {
        ShowWarningAsync(FEATURE_NOT_READY_MESSAGE).Forget();
    }

    public void OnQuestButton()
    {
        ShowWarningAsync(FEATURE_NOT_READY_MESSAGE).Forget();
    }

    public void OnInventoryButton()
    {
        ShowWarningAsync(FEATURE_NOT_READY_MESSAGE).Forget();
    }

    // 오른쪽 버튼들
    public void OnSettingsButton()
    {
        ShowWarningAsync(FEATURE_NOT_READY_MESSAGE).Forget();
    }

    public void OnShopButton()
    {
        ShowWarningAsync(FEATURE_NOT_READY_MESSAGE).Forget();
    }

    public void OnSpecialDealButton()
    {
        ShowWarningAsync(FEATURE_NOT_READY_MESSAGE).Forget();
    }

    public void OnMailButton()
    {
        ShowWarningAsync(FEATURE_NOT_READY_MESSAGE).Forget();
    }

    // 중앙 버튼
    public void OnBattleButton()
    {
        LoadSceneWithLoadingUI(sceneName.StageScene).Forget();
    }

    // 하단 버튼들
    public void OnDispatchButton()
    {
        LoadSceneWithFadeOnly(sceneName.DispatchSystemScene).Forget();
    }

    public void OnCraftButton()
    {
        LoadSceneWithFadeOnly(sceneName.BookMarkCraftScene).Forget();
    }

    public void OnLibrarianManageButton()
    {
        LoadSceneWithFadeOnly(sceneName.LibraryManagementScene).Forget();
    }

    public void OnChallengeDungeonButton()
    {
        ShowWarningAsync(FEATURE_NOT_READY_MESSAGE).Forget();
    }


    /// <summary>
    /// LCB: 로딩 UI 없이 페이드 효과만으로 씬을 전환하는 메서드
    /// LCB: 페이드 아웃 → 씬 로드 → 페이드 인
    /// </summary>
    private async UniTaskVoid LoadSceneWithFadeOnly(string sceneName)
    {
        // 매니저가 없으면 직접 씬 로드 (fallback)
        if (FadeController.Instance == null)
        {
            Debug.LogWarning("FadeController not available, loading scene directly");
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            return;
        }

        // Step 1: 페이드 아웃 (화면 어두워짐)
        FadeController.Instance.fadePanel.SetActive(true);
        await FadeController.Instance.FadeOut(0.5f);

        // Step 2: 씬 로드
        await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

        // Step 3: 페이드 인 (새 씬 밝아짐)
        await FadeController.Instance.FadeIn(0.5f);

        // Step 4: 페이드 패널 비활성화
        FadeController.Instance.fadePanel.SetActive(false);
    }

    /// <summary>
    /// LCB: 로딩 UI를 표시하면서 씬을 전환하는 공통 메서드
    /// LCB: 로딩 UI → 페이드 아웃 → 씬 로드 → 페이드 인
    /// </summary>
    private async UniTaskVoid LoadSceneWithLoadingUI(string sceneName)
    {
        // 매니저가 없으면 직접 씬 로드 (fallback)
        if (LoadingUIManager.Instance == null || FadeController.Instance == null)
        {
            Debug.LogWarning("LoadingUIManager or FadeController not available, loading scene directly");
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            return;
        }

        // Step 1: 로딩 UI 표시 및 진행률 애니메이션 (Inspector의 LOADING_DURATION_MS 사용)
        LoadingUIManager.Instance.Show();
        await LoadingUIManager.Instance.FakeLoadAsync();

        // Step 2: 100% 상태 잠깐 보여주기
        await UniTask.Delay(200);

        // Step 3: 페이드 아웃 (화면 어두워짐)
        FadeController.Instance.fadePanel.SetActive(true);
        await FadeController.Instance.FadeOut(0.5f);

        // Step 4: 로딩 UI 숨기기
        await LoadingUIManager.Instance.Hide();

        // Step 5: 씬 로드
        await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

        // Step 6: 페이드 인 (새 씬 밝아짐)
        await FadeController.Instance.FadeIn(0.5f);

        // Step 7: 페이드 패널 비활성화
        FadeController.Instance.fadePanel.SetActive(false);
    }

    /// <summary>
    /// 경고 메시지를 페이드 인/아웃으로 표시
    /// </summary>
    private async UniTaskVoid ShowWarningAsync(string message)
    {
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
    /// CanvasGroup 알파값을 페이드
    /// </summary>
    private async UniTask FadeCanvasGroupAsync(float from, float to, float duration, CancellationToken token)
    {
        float elapsed = 0f;
        warningCanvasGroup.alpha = from;

        while (elapsed < duration)
        {
            token.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            warningCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            await UniTask.Yield(token);
        }

        warningCanvasGroup.alpha = to;
    }
}

