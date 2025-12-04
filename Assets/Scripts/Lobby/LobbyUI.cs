using Cysharp.Threading.Tasks;
using Dispatch;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using TMPro;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    [Header("Currency Display")]
    [SerializeField] private TextMeshProUGUI apText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI premiumText;
    private int maxAP = 30;

    [Header("Dispatch Red Dot")]
    [SerializeField] private GameObject dispatchRedDot; // 파견 버튼 Red Dot
    [SerializeField] private float dispatchCheckInterval = 1f; // 파견 상태 확인 주기 (초)

    private bool isDispatchCompleted = false; // 파견 완료 상태 캐싱

    private void OnEnable()
    {
        InitializeAP();

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }

        // 파견 완료 플래그 초기화
        isDispatchCompleted = false;

        // 파견 완료 상태 주기적 확인 시작
        InvokeRepeating(nameof(CheckDispatchState), 0f, dispatchCheckInterval);
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }

        // 주기적 확인 중지
        CancelInvoke(nameof(CheckDispatchState));
    }

    private void InitializeAP()
    {
        // CurrencyTable에서 최대 AP 조회
        if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            var currencyData = CSVLoader.Instance.GetData<CurrencyData>(CurrencyManager.AP_ID);
            if (currencyData != null && currencyData.Currency_Max_Count > 0)
            {
                maxAP = currencyData.Currency_Max_Count;
            }
        }

        UpdateAPText();
        UpdateGoldText();
        UpdatePremiumText();
    }

    private void UpdateAPText()
    {
        if (apText == null) return;

        int currentAP = 0;
        if (CurrencyManager.Instance != null)
        {
            currentAP = CurrencyManager.Instance.GetCurrency(CurrencyManager.AP_ID);
        }

        apText.text = $"{currentAP}/{maxAP}";
    }

    private void UpdateGoldText()
    {
        if (goldText == null) return;

        int gold = 0;
        if (CurrencyManager.Instance != null)
        {
            gold = CurrencyManager.Instance.GetCurrency(CurrencyManager.GOLD_ID);
        }

        goldText.text = $"{gold}";
    }

    private void UpdatePremiumText()
    {
        if (premiumText == null) return;

        int magicStone = 0;
        if (CurrencyManager.Instance != null)
        {
            magicStone = CurrencyManager.Instance.GetCurrency(CurrencyManager.MAGIC_STONE_ID);
        }

        premiumText.text = $"{magicStone}";
    }

    private void OnCurrencyChanged(int currencyId, int newAmount)
    {
        if (currencyId == CurrencyManager.AP_ID && apText != null)
        {
            apText.text = $"{newAmount}/{maxAP}";
        }
        else if (currencyId == CurrencyManager.GOLD_ID && goldText != null)
        {
            goldText.text = $"{newAmount} G";
        }
        else if (currencyId == CurrencyManager.MAGIC_STONE_ID && premiumText != null)
        {
            premiumText.text = $"{newAmount}";
        }
    }

    // 왼쪽 버튼들
    public void OnPassTicketButton()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
    }

    public void OnNoticeButton()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
    }

    public void OnAttendanceButton()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
    }

    public void OnQuestButton()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
    }

    public void OnInventoryButton()
    {
        LoadSceneWithFadeOnly(SceneName.Inventory).Forget();
    }

    // 오른쪽 버튼들
    public void OnSettingsButton()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
    }

    public void OnShopButton()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
    }

    public void OnSpecialDealButton()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
    }

    public void OnMailButton()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
    }

    // 중앙 버튼
    public void OnBattleButton()
    {
        LoadSceneWithLoadingUI(SceneName.StageScene).Forget();
    }

    // 하단 버튼들
    public void OnDispatchButton()
    {
        LoadSceneWithFadeOnly(SceneName.DispatchSystemScene).Forget();
    }

    /// <summary>
    /// 파견 완료 상태 확인 및 Red Dot 표시 (최적화: 완료되면 더 이상 체크 안 함)
    /// DispatchStateHelper 유틸리티 클래스 사용
    /// </summary>
    private void CheckDispatchState()
    {
        // Red Dot이 할당되지 않았으면 무시
        if (dispatchRedDot == null)
        {
            return;
        }

        // 이미 파견 완료 상태라면 더 이상 체크하지 않음 (최적화)
        if (isDispatchCompleted)
        {
            return;
        }

        // DispatchStateHelper를 사용한 간단한 파견 완료 체크
        bool isCompleted = DispatchStateHelper.IsDispatchCompleted();

        if (isCompleted)
        {
            // 파견 완료 상태라면 Red Dot 활성화 후 체크 중지
            dispatchRedDot.SetActive(true);
            isDispatchCompleted = true; // 플래그 설정으로 더 이상 체크 안 함
            CancelInvoke(nameof(CheckDispatchState)); // 주기적 확인 중지
            Debug.Log("[LobbyUI] ✅ 파견 완료 - 로비에 Red Dot 표시 (체크 중지)");
        }
        else
        {
            // 파견 중이거나 저장된 상태가 없으면 Red Dot 비활성화
            if (dispatchRedDot.activeSelf)
            {
                dispatchRedDot.SetActive(false);
            }
        }
    }

    public void OnCraftButton()
    {
        LoadSceneWithFadeOnly(SceneName.BookMarkCraftScene).Forget();
    }

    public void OnLibrarianManageButton()
    {
        LoadSceneWithFadeOnly(SceneName.LibraryManagementScene).Forget();
    }

    public void OnChallengeDungeonButton()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
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
}

