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

    [Header("Stage Select Panel")]
    [SerializeField] private GameObject stageSelectPanel; // 스테이지 선택 패널
    [SerializeField] private GameObject lobbyWindow; // 로비 메인 Window

    [Header("Inventory Panel")]
    [SerializeField] private GameObject inventoryPanel; // 인벤토리 패널

    [Header("Menu Layout")]
    [SerializeField] private GameObject menuLayout; // 메뉴 레이아웃 (토글용)

    [Header("Profile Panel")]
    [SerializeField] private GameObject profileDetailPanel; // 프로필 상세 패널 (버튼 누르면 열림)

    [Header("User Nickname")]
    [SerializeField] private TextMeshProUGUI nicknameText; // 로비 화면에 표시되는 닉네임 (UserText (1))

    [Header("Shop Panel")]
    [SerializeField] private GameObject shopPanel; // 상점 패널

    [Header("Boss Dungeon Panel (Issue #476)")]
    [SerializeField] private GameObject bossDungeonSelectPanel; // 도전던전 선택 패널

    [Header("Account UI Panel")]
    [SerializeField] private GameObject accountUIPanel; // 계정/환경설정 패널

    private void OnEnable()
    {
        // Issue #602: 로비 진입 시 TimeScale 스택 리셋
        TimeManager.Instance?.ResetTimeScale();

        // Lobby BGM 재생 (다른 씬에서 돌아올 때 크로스페이드)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.CrossfadeBGM("BGM_Lobby", 1f);
        }

        InitializeAP();
        RefreshNickname();

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }

        // 메뉴 레이아웃 활성화 (항상 보이는 상태로 시작)
        if (menuLayout != null)
        {
            menuLayout.SetActive(true);
        }

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

        goldText.text = CurrencyManager.FormatCurrency(gold);
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
            goldText.text = CurrencyManager.FormatCurrency(newAmount);
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
        // 메뉴 레이아웃이 열려있으면 먼저 닫기
        if (menuLayout != null && menuLayout.activeSelf)
        {
            menuLayout.SetActive(false);
        }

        // 씬 전환 대신 패널 토글 방식으로 변경
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
        if (lobbyWindow != null)
        {
            lobbyWindow.SetActive(false);
        }
    }

    public void OnTrainingGroundButton()
    {
        LoadSceneWithFadeOnly(SceneName.TrainingScene).Forget();
    }

    /// <summary>
    /// 인벤토리 패널에서 뒤로가기 버튼 클릭 시 호출
    /// 로비 메인 화면으로 복귀
    /// </summary>
    public void OnInventoryBackButton()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        if (lobbyWindow != null)
        {
            lobbyWindow.SetActive(true);
        }
    }

    // 오른쪽 버튼들
    /// <summary>
    /// 설정 버튼 클릭 시 Account UI 패널 활성화
    /// </summary>
    public void OnSettingsButton()
    {
        if (accountUIPanel != null)
        {
            accountUIPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 메뉴 버튼 클릭 시 MenuLayout 토글
    /// </summary>
    public void OnMenuButton()
    {
        if (menuLayout != null)
        {
            menuLayout.SetActive(!menuLayout.activeSelf);
        }
    }

    

    public void OnShopButton()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }
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
        // 메뉴 레이아웃이 열려있으면 먼저 닫기
        if (menuLayout != null && menuLayout.activeSelf)
        {
            menuLayout.SetActive(false);
        }

        // 씬 전환 대신 패널 토글 방식으로 변경
        if (stageSelectPanel != null)
        {
            stageSelectPanel.SetActive(true);
        }
        if (lobbyWindow != null)
        {
            lobbyWindow.SetActive(false);
        }
    }

    /// <summary>
    /// 스테이지 선택 패널에서 홈 버튼 클릭 시 호출
    /// 로비 메인 화면으로 복귀
    /// </summary>
    public void OnStageSelectHomeButton()
    {
        if (stageSelectPanel != null)
        {
            stageSelectPanel.SetActive(false);
        }
        if (lobbyWindow != null)
        {
            lobbyWindow.SetActive(true);
        }
    }

    // 하단 버튼들
    public void OnDispatchButton()
    {
        LoadSceneWithFadeOnly(SceneName.DispatchSystemScene).Forget();
    }

    /// <summary>
    /// 파견 완료 상태 확인 및 Red Dot 표시
    /// DispatchStateHelper 유틸리티 클래스 사용
    /// </summary>
    private void CheckDispatchState()
    {
        if (dispatchRedDot == null)
        {
            return;
        }

        // DispatchStateHelper를 사용한 파견 완료 체크
        bool isCompleted = DispatchStateHelper.IsDispatchCompleted();

        if (isCompleted != dispatchRedDot.activeSelf)
        {
            dispatchRedDot.SetActive(isCompleted);
            if (isCompleted)
            {
                Debug.Log("[LobbyUI] ✅ 파견 완료 - 로비에 Red Dot 표시");
            }
        }
    }

    public void OnCraftButton()
    {
        // Issue #576 - 책갈피 해금 체크
        if (StageProgressManager.Instance != null &&
            !StageProgressManager.Instance.IsBookmarkUnlocked())
        {
            int requiredStage = StageProgressManager.Instance.GetBookmarkUnlockStage();
            string message = string.Format(WarningText.BookmarkLocked, requiredStage);
            WarningUIManager.Instance.ShowWarning(message);
            return;
        }

        LoadSceneWithFadeOnly(SceneName.BookMarkCraftScene).Forget();
    }

    public void OnLibrarianManageButton()
    {
        LoadSceneWithFadeOnly(SceneName.LibraryManagementScene).Forget();
    }

    /// <summary>
    /// Issue #476 - 도전던전 버튼 클릭 시 BossDungeonSelectPanel 활성화
    /// Issue #576 - 5스테이지 클리어 후 해금
    /// </summary>
    public void OnChallengeDungeonButton()
    {
        // Issue #576 - 도전던전 해금 체크
        if (StageProgressManager.Instance != null &&
            !StageProgressManager.Instance.IsBossDungeonUnlocked())
        {
            int requiredStage = StageProgressManager.Instance.GetBossDungeonUnlockStage();
            string message = string.Format(WarningText.BossDungeonLocked, requiredStage);
            WarningUIManager.Instance.ShowWarning(message);
            return;
        }

        // 메뉴 레이아웃이 열려있으면 먼저 닫기
        if (menuLayout != null && menuLayout.activeSelf)
        {
            menuLayout.SetActive(false);
        }

        // 도전던전 선택 패널 열기
        if (bossDungeonSelectPanel != null)
        {
            bossDungeonSelectPanel.SetActive(true);
        }
        if (lobbyWindow != null)
        {
            lobbyWindow.SetActive(false);
        }
    }

    /// <summary>
    /// Issue #476 - 도전던전 선택 패널에서 홈 버튼 클릭 시 호출
    /// 로비 메인 화면으로 복귀
    /// </summary>
    public void OnBossDungeonHomeButton()
    {
        if (bossDungeonSelectPanel != null)
        {
            bossDungeonSelectPanel.SetActive(false);
        }
        if (lobbyWindow != null)
        {
            lobbyWindow.SetActive(true);
        }
    }

    /// <summary>
    /// 프로필 버튼 클릭 시 프로필 상세 패널 활성화
    /// </summary>
    public void OnProfileButton()
    {
        Debug.Log("[LobbyUI] OnProfileButton 호출됨!");
        if (profileDetailPanel != null)
        {
            profileDetailPanel.SetActive(true);
            Debug.Log("[LobbyUI] profileDetailPanel 활성화됨");
        }
        else
        {
            Debug.LogWarning("[LobbyUI] profileDetailPanel이 null입니다!");
        }
    }

    /// <summary>
    /// 프로필 상세 패널 닫기 버튼
    /// </summary>
    public void OnProfileCloseButton()
    {
        if (profileDetailPanel != null)
        {
            profileDetailPanel.SetActive(false);
        }
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
    /// 로비 화면에 닉네임 표시
    /// Firebase에서 캐시된 닉네임을 UserText (1)에 표시
    /// </summary>
    public void RefreshNickname()
    {
        if (nicknameText == null) return;

        if (FirebaseSaveManager.Instance != null && FirebaseSaveManager.Instance.CachedData != null)
        {
            string nickname = FirebaseSaveManager.Instance.CachedData.nickname;
            nicknameText.text = string.IsNullOrEmpty(nickname) ? "Guest" : nickname;
        }
        else
        {
            nicknameText.text = "Guest";
        }
    }
}

