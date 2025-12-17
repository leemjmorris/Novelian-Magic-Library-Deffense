using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Managers;

/// <summary>
/// 가챠 패널 UI 컨트롤러 (가챠 로직 포함)
/// - 1회/10회 뽑기 버튼 처리
/// - 재화 소비: 지원서 우선, 부족 시 골드 사용
/// - 중복 캐릭터: 정수로 변환
/// - 결과 패널 표시/숨기기
/// </summary>
public class GachaPanelController : MonoBehaviour
{
    [Header("Result Panels")]
    [SerializeField] private GameObject singleSummonPanel;
    [SerializeField] private GameObject tenSummonPanel;

    [Header("Result Slots")]
    [SerializeField] private GachaResultSlot singleSlot;
    [SerializeField] private GachaResultSlot[] tenSlots = new GachaResultSlot[10];

    [Header("Buttons")]
    [SerializeField] private Button gachaX1Button;
    [SerializeField] private Button gachaX10Button;

    [Header("Cost Text (Optional)")]
    [SerializeField] private TextMeshProUGUI gachaX1CostText;
    [SerializeField] private TextMeshProUGUI gachaX10CostText;

    [Header("Close Buttons")]
    [SerializeField] private Button singlePanelCloseButton;
    [SerializeField] private Button tenPanelCloseButton;

    [Header("Animation Settings")]
    [SerializeField] private float slotRevealDelay = 0.15f;

    // 뽑기 비용
    private const int PULL_COST_APPLICATION = 1;      // 지원서 1개당 1회
    private const int PULL_COST_GOLD = 5000;          // 골드 5000당 1회

    // 정수 ID 베이스 (CHARACTER_POOL 인덱스 기반으로 계산)
    private const int ESSENCE_ID_BASE = 30001;

    // 전체 캐릭터 풀 (20종, 균등 확률 5%)
    private static readonly int[] CHARACTER_POOL = {
        21001, 21002, 21003, 21004,  // Horror
        22005, 22006, 22007, 22008,  // Romance
        23009, 23010, 23011, 23012,  // Adventure
        24013, 24014, 24015, 24016,  // Comedy
        25017, 25018, 25019, 25020   // Mystery
    };

    private bool isProcessing = false;

    private void Start()
    {
        // 버튼 리스너 등록
        if (gachaX1Button != null)
            gachaX1Button.onClick.AddListener(OnGachaX1Button);

        if (gachaX10Button != null)
            gachaX10Button.onClick.AddListener(OnGachaX10Button);

        if (singlePanelCloseButton != null)
            singlePanelCloseButton.onClick.AddListener(CloseSinglePanel);

        if (tenPanelCloseButton != null)
            tenPanelCloseButton.onClick.AddListener(CloseTenPanel);

        // 결과 패널 초기 비활성화
        if (singleSummonPanel != null)
            singleSummonPanel.SetActive(false);

        if (tenSummonPanel != null)
            tenSummonPanel.SetActive(false);

        // 비용 텍스트 초기화
        UpdateCostText();
    }

    private void OnEnable()
    {
        UpdateCostText();
        UpdateButtonStates();
    }

    private void OnDestroy()
    {
        if (gachaX1Button != null)
            gachaX1Button.onClick.RemoveListener(OnGachaX1Button);

        if (gachaX10Button != null)
            gachaX10Button.onClick.RemoveListener(OnGachaX10Button);

        if (singlePanelCloseButton != null)
            singlePanelCloseButton.onClick.RemoveListener(CloseSinglePanel);

        if (tenPanelCloseButton != null)
            tenPanelCloseButton.onClick.RemoveListener(CloseTenPanel);
    }

    #region Button Handlers

    /// <summary>
    /// 1회 뽑기 버튼 클릭
    /// </summary>
    public void OnGachaX1Button()
    {
        if (isProcessing) return;
        PerformSinglePullAsync().Forget();
    }

    /// <summary>
    /// 10회 뽑기 버튼 클릭
    /// </summary>
    public void OnGachaX10Button()
    {
        if (isProcessing) return;
        PerformTenPullAsync().Forget();
    }

    #endregion

    #region Gacha Logic

    /// <summary>
    /// 1회 뽑기 실행
    /// </summary>
    private async UniTaskVoid PerformSinglePullAsync()
    {
        isProcessing = true;
        SetButtonsInteractable(false);

        if (!TrySpendCost(1))
        {
            isProcessing = false;
            UpdateButtonStates();
            return;
        }

        var result = ProcessPull();
        await ShowSingleResult(result);

        isProcessing = false;
        UpdateButtonStates();
        UpdateCostText();
    }

    /// <summary>
    /// 10회 뽑기 실행
    /// </summary>
    private async UniTaskVoid PerformTenPullAsync()
    {
        isProcessing = true;
        SetButtonsInteractable(false);

        if (!TrySpendCost(10))
        {
            isProcessing = false;
            UpdateButtonStates();
            return;
        }

        var results = new List<GachaResult>();
        for (int i = 0; i < 10; i++)
        {
            results.Add(ProcessPull());
        }

        await ShowTenResults(results);

        isProcessing = false;
        UpdateButtonStates();
        UpdateCostText();
    }

    /// <summary>
    /// 재화 소비 시도 (지원서 우선 → 골드 대체)
    /// </summary>
    private bool TrySpendCost(int pullCount)
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[GachaPanelController] CurrencyManager가 없습니다!");
            return false;
        }

        int appCost = pullCount * PULL_COST_APPLICATION;
        int goldCost = pullCount * PULL_COST_GOLD;

        // 1순위: 지원서
        if (CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.APPLICATION_ID, appCost))
        {
            CurrencyManager.Instance.SpendCurrency(CurrencyManager.APPLICATION_ID, appCost);
            Debug.Log($"[Gacha] 지원서 {appCost}개 소비");
            return true;
        }

        // 2순위: 골드
        if (CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.GOLD_ID, goldCost))
        {
            CurrencyManager.Instance.SpendCurrency(CurrencyManager.GOLD_ID, goldCost);
            Debug.Log($"[Gacha] 골드 {goldCost} 소비");
            return true;
        }

        // 둘 다 부족
        if (WarningUIManager.Instance != null)
            WarningUIManager.Instance.ShowWarning("재화가 부족합니다");
        Debug.LogWarning("[Gacha] 재화 부족!");
        return false;
    }

    /// <summary>
    /// 단일 뽑기 처리 (캐릭터 선택 + 중복 처리)
    /// </summary>
    private GachaResult ProcessPull()
    {
        // 균등 확률로 캐릭터 선택
        int randomIndex = Random.Range(0, CHARACTER_POOL.Length);
        int characterId = CHARACTER_POOL[randomIndex];

        // 보유 여부 확인
        bool isNew = !CharacterOwnershipManager.Instance.IsOwned(characterId);

        if (isNew)
        {
            // 신규 캐릭터 → 해금
            CharacterOwnershipManager.Instance.UnlockCharacter(characterId);
            Debug.Log($"[Gacha] 신규 캐릭터 획득: {characterId}");
            return new GachaResult(characterId, true, 0);
        }
        else
        {
            // 중복 캐릭터 → 정수 지급 (캐릭터 풀 인덱스 기반)
            int characterIndex = System.Array.IndexOf(CHARACTER_POOL, characterId);
            int essenceId = ESSENCE_ID_BASE + characterIndex;  // 30001 + index
            IngredientManager.Instance.AddIngredient(essenceId, 1);
            Debug.Log($"[Gacha] 중복 캐릭터! 정수 지급: {essenceId}");
            return new GachaResult(characterId, false, essenceId);
        }
    }

    /// <summary>
    /// 재화 충분 여부 확인
    /// </summary>
    private bool HasEnoughCurrency(int pullCount)
    {
        if (CurrencyManager.Instance == null) return false;

        int appCost = pullCount * PULL_COST_APPLICATION;
        int goldCost = pullCount * PULL_COST_GOLD;

        return CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.APPLICATION_ID, appCost) ||
               CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.GOLD_ID, goldCost);
    }

    /// <summary>
    /// 현재 사용될 재화 타입 확인 (true: 지원서, false: 골드)
    /// </summary>
    private bool WillUseApplication(int pullCount)
    {
        if (CurrencyManager.Instance == null) return false;

        int appCost = pullCount * PULL_COST_APPLICATION;
        return CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.APPLICATION_ID, appCost);
    }

    #endregion

    #region Result Display

    /// <summary>
    /// 1회 뽑기 결과 표시
    /// </summary>
    private async UniTask ShowSingleResult(GachaResult result)
    {
        if (singleSummonPanel == null || singleSlot == null)
        {
            Debug.LogError("[GachaPanelController] singleSummonPanel 또는 singleSlot이 설정되지 않았습니다!");
            return;
        }

        // 슬롯 초기화
        singleSlot.gameObject.SetActive(false);

        // 패널 열기
        singleSummonPanel.SetActive(true);

        // 잠시 대기 후 슬롯 표시
        await UniTask.Delay(100);

        // 결과 표시
        await singleSlot.Initialize(result);
        singleSlot.gameObject.SetActive(true);

        Debug.Log($"[Gacha] 1회 뽑기 결과: {result.GetCharacterName()} (신규: {result.IsNew})");
    }

    /// <summary>
    /// 10회 뽑기 결과 표시
    /// </summary>
    private async UniTask ShowTenResults(List<GachaResult> results)
    {
        if (tenSummonPanel == null || tenSlots == null || tenSlots.Length < 10)
        {
            Debug.LogError("[GachaPanelController] tenSummonPanel 또는 tenSlots이 올바르게 설정되지 않았습니다!");
            return;
        }

        // 모든 슬롯 초기 비활성화
        foreach (var slot in tenSlots)
        {
            if (slot != null)
                slot.gameObject.SetActive(false);
        }

        // 패널 열기
        tenSummonPanel.SetActive(true);

        // 잠시 대기
        await UniTask.Delay(100);

        // 순차적으로 슬롯 표시 (연출)
        for (int i = 0; i < Mathf.Min(results.Count, tenSlots.Length); i++)
        {
            if (tenSlots[i] != null)
            {
                await tenSlots[i].Initialize(results[i]);
                tenSlots[i].gameObject.SetActive(true);

                // 연출용 딜레이
                await UniTask.Delay((int)(slotRevealDelay * 1000));
            }
        }

        // 결과 로그
        int newCount = 0;
        int dupeCount = 0;
        foreach (var result in results)
        {
            if (result.IsNew) newCount++;
            else dupeCount++;
        }
        Debug.Log($"[Gacha] 10회 뽑기 결과: 신규 {newCount}명, 중복 {dupeCount}명");
    }

    #endregion

    #region Panel Control

    /// <summary>
    /// 1회 결과 패널 닫기
    /// </summary>
    public void CloseSinglePanel()
    {
        if (singleSummonPanel != null)
        {
            singleSummonPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 10회 결과 패널 닫기
    /// </summary>
    public void CloseTenPanel()
    {
        if (tenSummonPanel != null)
        {
            tenSummonPanel.SetActive(false);
        }
    }

    #endregion

    #region UI Update

    /// <summary>
    /// 버튼 활성화/비활성화
    /// </summary>
    private void SetButtonsInteractable(bool interactable)
    {
        if (gachaX1Button != null)
            gachaX1Button.interactable = interactable;

        if (gachaX10Button != null)
            gachaX10Button.interactable = interactable;
    }

    /// <summary>
    /// 버튼 상태 업데이트 (재화에 따라)
    /// </summary>
    private void UpdateButtonStates()
    {
        if (gachaX1Button != null)
            gachaX1Button.interactable = HasEnoughCurrency(1);

        if (gachaX10Button != null)
            gachaX10Button.interactable = HasEnoughCurrency(10);
    }

    /// <summary>
    /// 비용 텍스트 업데이트
    /// </summary>
    private void UpdateCostText()
    {
        if (gachaX1CostText != null)
        {
            if (WillUseApplication(1))
                gachaX1CostText.text = $"지원서 x{PULL_COST_APPLICATION}";
            else
                gachaX1CostText.text = $"{CurrencyManager.FormatCurrency(PULL_COST_GOLD)} 골드";
        }

        if (gachaX10CostText != null)
        {
            if (WillUseApplication(10))
                gachaX10CostText.text = $"지원서 x{PULL_COST_APPLICATION * 10}";
            else
                gachaX10CostText.text = $"{CurrencyManager.FormatCurrency(PULL_COST_GOLD * 10)} 골드";
        }
    }

    #endregion
}

/// <summary>
/// 가챠 결과 데이터
/// </summary>
public class GachaResult
{
    public int CharacterId { get; private set; }
    public bool IsNew { get; private set; }
    public int EssenceId { get; private set; }  // 중복 시에만 유효 (0이면 신규)

    public GachaResult(int characterId, bool isNew, int essenceId)
    {
        CharacterId = characterId;
        IsNew = isNew;
        EssenceId = essenceId;
    }

    /// <summary>
    /// 캐릭터 이름 조회
    /// </summary>
    public string GetCharacterName()
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            return $"Character_{CharacterId}";

        var characterData = CSVLoader.Instance.GetData<CharacterData>(CharacterId);
        if (characterData == null)
            return $"Character_{CharacterId}";

        var stringData = CSVLoader.Instance.GetData<StringTable>(characterData.Character_Name_ID);
        return stringData?.Text ?? $"Character_{CharacterId}";
    }

    /// <summary>
    /// 정수 이름 조회 (중복 시)
    /// </summary>
    public string GetEssenceName()
    {
        if (EssenceId == 0 || IngredientManager.Instance == null)
            return "";

        return IngredientManager.Instance.GetIngredientName(EssenceId);
    }
}
