using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

/// <summary>
/// 파티 시너지 강화 패널
/// - 시너지 강화 UI 표시
/// - Close 버튼으로 패널 닫기
/// </summary>
public class PartySynergyEnhancementPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Synergy Info")]
    [SerializeField] private TextMeshProUGUI synergyNameText;

    [Header("Character Slots")]
    [SerializeField] private PartySlot[] characterSlots;

    [Header("Required Level")]
    [SerializeField] private TextMeshProUGUI requiredLevelText;

    [Header("Current Level Panel")]
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI currentEffectText;

    [Header("Next Level Panel")]
    [SerializeField] private TextMeshProUGUI nextLevelText;
    [SerializeField] private TextMeshProUGUI nextEffectText;

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Image materialIcon;
    [SerializeField] private TextMeshProUGUI materialCountText;
    [SerializeField] private TextMeshProUGUI goldText;

    private PartySynergyData currentSynergyData;
    private PartySynergyEnhancementData currentEnhancementData;

    // Issue 1: 연속 클릭 방지 플래그
    private bool isUpgrading = false;

    // Issue 2: Addressables 메모리 관리
    private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<Sprite> currentIconHandle;
    private bool hasLoadedIcon = false;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }
    }

    private void OnEnable()
    {
        // Issue 4: 재화/재료 변경 이벤트 구독
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }
        if (IngredientManager.Instance != null)
        {
            IngredientManager.Instance.OnIngredientChanged += OnIngredientChanged;
        }
    }

    private void OnDisable()
    {
        // Issue 4: 이벤트 구독 해제
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }
        if (IngredientManager.Instance != null)
        {
            IngredientManager.Instance.OnIngredientChanged -= OnIngredientChanged;
        }
    }

    /// <summary>
    /// 재화 변경 시 UI 갱신
    /// </summary>
    private void OnCurrencyChanged(int currencyId, int newAmount)
    {
        // 골드 변경 시에만 갱신
        if (currencyId == CurrencyManager.GOLD_ID && currentSynergyData != null && currentEnhancementData != null)
        {
            UpdateGoldInfo(currentEnhancementData);
            int currentLevel = PartySynergyManager.Instance != null ? PartySynergyManager.Instance.GetSynergyLevel(currentSynergyData.Party_ID) : 1;
            UpdateButtonInteractable(currentSynergyData, currentLevel);
        }
    }

    /// <summary>
    /// 재료 변경 시 UI 갱신
    /// </summary>
    private void OnIngredientChanged(int ingredientId, int newAmount)
    {
        // 현재 필요한 재료가 변경된 경우에만 갱신
        if (currentSynergyData != null && currentEnhancementData != null && currentEnhancementData.Material_ID == ingredientId)
        {
            UpdateMaterialInfo(currentEnhancementData);
            int currentLevel = PartySynergyManager.Instance != null ? PartySynergyManager.Instance.GetSynergyLevel(currentSynergyData.Party_ID) : 1;
            UpdateButtonInteractable(currentSynergyData, currentLevel);
        }
    }

    /// <summary>
    /// 시너지 데이터로 패널 초기화
    /// </summary>
    public void Initialize(PartySynergyData data)
    {
        if (data == null)
        {
            Debug.LogError("[PartySynergyEnhancementPanel] PartySynergyData is null!");
            return;
        }

        currentSynergyData = data;

        // 시너지 이름 표시
        if (synergyNameText != null)
        {
            var stringData = CSVLoader.Instance?.GetData<StringTable>(data.Party_Name_ID);
            synergyNameText.text = stringData?.Text ?? $"Party_{data.Party_ID}";
        }

        // 캐릭터 슬롯 초기화
        InitializeCharacterSlots(data);

        // 시너지 레벨 정보 업데이트
        int currentSynergyLevel = PartySynergyManager.Instance?.GetSynergyLevel(data.Party_ID) ?? 1;
        UpdateRequiredLevelText(data, currentSynergyLevel);
        UpdateSynergyLevelInfo(data, currentSynergyLevel);

        // 업그레이드 버튼 정보 업데이트
        UpdateUpgradeButton(data, currentSynergyLevel);

        Debug.Log($"[PartySynergyEnhancementPanel] Initialized - PartyID: {data.Party_ID}, PartySize: {data.Party_Size}");
    }

    private void InitializeCharacterSlots(PartySynergyData data)
    {
        if (characterSlots == null) return;

        int[] charIds = { data.Req_Char_1_ID, data.Req_Char_2_ID, data.Req_Char_3_ID, data.Req_Char_4_ID };

        for (int i = 0; i < characterSlots.Length && i < data.Party_Size; i++)
        {
            characterSlots[i]?.Init(charIds[i]);
        }
    }

    private void UpdateRequiredLevelText(PartySynergyData data, int currentSynergyLevel)
    {
        if (requiredLevelText == null) return;

        // 다음 강화에 필요한 ID 가져오기
        int nextUpgradeId = GetNextUpgradeId(data, currentSynergyLevel);
        if (nextUpgradeId == 0)
        {
            requiredLevelText.text = "최대 레벨";
            return;
        }

        // 강화 데이터 조회
        var enhancementData = CSVLoader.Instance?.GetData<PartySynergyEnhancementData>(nextUpgradeId);
        if (enhancementData == null)
        {
            requiredLevelText.text = "";
            return;
        }

        // 필요 레벨 추출 (401 → 1, 402 → 2, 404 → 4 등)
        int requiredLevel = enhancementData.Need_Party_Characters_Lv % 100;

        // 캐릭터 레벨 체크
        bool allMeetLevel = CheckAllCharactersMeetLevel(data, requiredLevel);

        // 텍스트 설정 (Rich Text)
        if (allMeetLevel)
        {
            requiredLevelText.text = $"필요 캐릭터 레벨: Lv {requiredLevel}";
        }
        else
        {
            requiredLevelText.text = $"필요 캐릭터 레벨: <color=red>Lv {requiredLevel}</color>";
        }
    }

    private bool CheckAllCharactersMeetLevel(PartySynergyData data, int requiredLevel)
    {
        if (CharacterEnhancementManager.Instance == null) return false;

        int[] charIds = { data.Req_Char_1_ID, data.Req_Char_2_ID, data.Req_Char_3_ID, data.Req_Char_4_ID };

        for (int i = 0; i < data.Party_Size; i++)
        {
            int charLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(charIds[i]);
            if (charLevel < requiredLevel)
            {
                return false;
            }
        }
        return true;
    }

    private void UpdateSynergyLevelInfo(PartySynergyData data, int currentLevel)
    {
        // 시너지 이름 가져오기
        var synergyNameData = CSVLoader.Instance?.GetData<StringTable>(data.Party_Name_ID);
        string synergyName = synergyNameData?.Text ?? $"Party_{data.Party_ID}";

        // 현재 레벨 정보
        var (currentEffectId, currentEffectValue) = GetEffectData(data, currentLevel);
        string currentEffectName = GetEffectName(currentEffectId);

        if (currentLevelText != null)
        {
            currentLevelText.text = $"Lv {currentLevel} {synergyName}";
        }
        if (currentEffectText != null)
        {
            currentEffectText.text = $"{currentEffectName} +{currentEffectValue}%";
        }

        // 다음 레벨 정보
        int nextLevel = currentLevel + 1;
        if (nextLevel > 5)
        {
            // 최대 레벨
            if (nextLevelText != null) nextLevelText.text = "최대 레벨";
            if (nextEffectText != null) nextEffectText.text = "";
            return;
        }

        var (nextEffectId, nextEffectValue) = GetEffectData(data, nextLevel);
        string nextEffectName = GetEffectName(nextEffectId);

        if (nextLevelText != null)
        {
            nextLevelText.text = $"Lv {nextLevel} {synergyName}";
        }
        if (nextEffectText != null)
        {
            nextEffectText.text = $"{nextEffectName} +{nextEffectValue}%";
        }
    }

    private (int effectId, float effectValue) GetEffectData(PartySynergyData data, int level)
    {
        return level switch
        {
            1 => (data.Effect_1_ID, data.Effect_1_Value),
            2 => (data.Effect_2_ID, data.Effect_2_Value),
            3 => (data.Effect_3_ID, data.Effect_3_Value),
            4 => (data.Effect_4_ID, data.Effect_4_Value),
            5 => (data.Effect_5_ID, data.Effect_5_Value),
            _ => (0, 0f)
        };
    }

    private string GetEffectName(int effectId)
    {
        var effectData = CSVLoader.Instance?.GetData<PartySynergyEffectData>(effectId);
        if (effectData == null) return "시너지 효과";

        var stringData = CSVLoader.Instance?.GetData<StringTable>(effectData.Party_Effect_Name_ID);
        return stringData?.Text ?? "시너지 효과";
    }

    private int GetNextUpgradeId(PartySynergyData data, int currentLevel)
    {
        return currentLevel switch
        {
            1 => data.Party_Upgrade_Lv2_ID,
            2 => data.Party_Upgrade_Lv3_ID,
            3 => data.Party_Upgrade_Lv4_ID,
            4 => data.Party_Upgrade_Lv5_ID,
            _ => 0 // 최대 레벨
        };
    }

    /// <summary>
    /// 패널 열기
    /// </summary>
    public void ShowPanel()
    {
        // 먼저 게임 오브젝트 자체를 활성화
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // panel이 지정되어 있으면 추가로 활성화
        if (panel != null && !panel.activeSelf)
        {
            panel.SetActive(true);
        }

        Debug.Log($"[PartySynergyEnhancementPanel] ShowPanel - gameObject.active: {gameObject.activeSelf}, panel: {(panel != null ? panel.activeSelf.ToString() : "null")}");
    }

    /// <summary>
    /// 패널 닫기
    /// </summary>
    public void HidePanel()
    {
        // panel이 지정되어 있으면 비활성화
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // 게임 오브젝트 자체도 비활성화
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }

        Debug.Log($"[PartySynergyEnhancementPanel] HidePanel - gameObject.active: {gameObject.activeSelf}");
    }

    /// <summary>
    /// Close 버튼 클릭 시 호출
    /// </summary>
    private void OnCloseButtonClicked()
    {
        Debug.Log("[PartySynergyEnhancementPanel] Close button clicked");
        HidePanel();
    }

    /// <summary>
    /// 현재 시너지 데이터 반환
    /// </summary>
    public PartySynergyData GetCurrentSynergyData()
    {
        return currentSynergyData;
    }

    #region Upgrade Button

    /// <summary>
    /// 업그레이드 버튼 클릭 이벤트 (Unity Button용 래퍼)
    /// </summary>
    private void OnUpgradeButtonClicked()
    {
        OnUpgradeButtonClickedAsync().Forget();
    }

    /// <summary>
    /// 업그레이드 버튼 클릭 이벤트 (async - Firebase 저장 완료 대기)
    /// </summary>
    private async UniTaskVoid OnUpgradeButtonClickedAsync()
    {
        // Issue 1: 연속 클릭 방지
        if (isUpgrading)
        {
            Debug.Log("[PartySynergyEnhancementPanel] Upgrade already in progress");
            return;
        }

        if (currentSynergyData == null || currentEnhancementData == null)
        {
            Debug.LogWarning("[PartySynergyEnhancementPanel] No enhancement data available");
            return;
        }

        // 강화 시작 - 버튼 즉시 비활성화
        isUpgrading = true;
        if (upgradeButton != null)
        {
            upgradeButton.interactable = false;
        }

        // 재료 소모
        int materialId = currentEnhancementData.Material_ID;
        int materialCount = currentEnhancementData.Material_Count;
        bool materialSuccess = IngredientManager.Instance != null && IngredientManager.Instance.RemoveIngredient(materialId, materialCount);

        if (!materialSuccess)
        {
            Debug.LogWarning("[PartySynergyEnhancementPanel] Failed to consume materials");
            isUpgrading = false;
            RefreshUI(); // 버튼 상태 복원
            return;
        }

        // 골드 소모
        int goldCount = currentEnhancementData.Gold_Count;
        bool goldSuccess = CurrencyManager.Instance != null && CurrencyManager.Instance.SpendCurrency(CurrencyManager.GOLD_ID, goldCount);

        if (!goldSuccess)
        {
            // 재료 롤백 (골드 소모 실패 시)
            if (IngredientManager.Instance != null)
            {
                IngredientManager.Instance.AddIngredient(materialId, materialCount);
            }
            Debug.LogWarning("[PartySynergyEnhancementPanel] Failed to consume gold");
            isUpgrading = false;
            RefreshUI(); // 버튼 상태 복원
            return;
        }

        // 시너지 레벨 증가 (Firebase 저장 완료 대기)
        int currentLevel = PartySynergyManager.Instance != null ? PartySynergyManager.Instance.GetSynergyLevel(currentSynergyData.Party_ID) : 1;
        int newLevel = currentLevel + 1;
        if (PartySynergyManager.Instance != null)
        {
            await PartySynergyManager.Instance.SetSynergyLevelAsync(currentSynergyData.Party_ID, newLevel);
        }

        Debug.Log($"[PartySynergyEnhancementPanel] Upgrade successful! Level: {currentLevel} -> {newLevel}");

        // UI 갱신 후 플래그 해제
        RefreshUI();
        isUpgrading = false;
    }

    /// <summary>
    /// UI 갱신 (강화 후)
    /// </summary>
    private void RefreshUI()
    {
        if (currentSynergyData == null) return;

        int currentLevel = PartySynergyManager.Instance?.GetSynergyLevel(currentSynergyData.Party_ID) ?? 1;

        // 필요 캐릭터 레벨 업데이트
        UpdateRequiredLevelText(currentSynergyData, currentLevel);

        // 시너지 레벨 정보 업데이트
        UpdateSynergyLevelInfo(currentSynergyData, currentLevel);

        // 업그레이드 버튼 정보 업데이트
        UpdateUpgradeButton(currentSynergyData, currentLevel);
    }

    /// <summary>
    /// 업그레이드 버튼 정보 업데이트
    /// </summary>
    private void UpdateUpgradeButton(PartySynergyData data, int currentLevel)
    {
        // 다음 업그레이드 ID 가져오기
        int nextUpgradeId = GetNextUpgradeId(data, currentLevel);

        // 최대 레벨이면 버튼 비활성화
        if (nextUpgradeId == 0)
        {
            if (upgradeButton != null)
            {
                upgradeButton.interactable = false;
            }
            if (materialIcon != null)
            {
                materialIcon.enabled = false;
            }
            if (materialCountText != null)
            {
                materialCountText.text = "MAX";
            }
            if (goldText != null)
            {
                goldText.text = "";
            }
            return;
        }

        // 강화 데이터 조회
        currentEnhancementData = CSVLoader.Instance?.GetData<PartySynergyEnhancementData>(nextUpgradeId);
        if (currentEnhancementData == null)
        {
            Debug.LogWarning($"[PartySynergyEnhancementPanel] Enhancement data not found for ID: {nextUpgradeId}");
            return;
        }

        // 재료 정보 업데이트
        UpdateMaterialInfo(currentEnhancementData);

        // 골드 정보 업데이트
        UpdateGoldInfo(currentEnhancementData);

        // 버튼 활성화 조건 체크
        UpdateButtonInteractable(data, currentLevel);
    }

    /// <summary>
    /// 재료 아이콘 및 수량 정보 업데이트
    /// </summary>
    private void UpdateMaterialInfo(PartySynergyEnhancementData enhancementData)
    {
        int materialId = enhancementData.Material_ID;
        int requiredCount = enhancementData.Material_Count;

        // 보유 수량 조회
        int ownedCount = IngredientManager.Instance?.GetIngredientCount(materialId) ?? 0;

        // 재료 아이콘 로드
        LoadMaterialIcon(materialId).Forget();

        // 수량 텍스트 설정
        if (materialCountText != null)
        {
            bool hasEnough = ownedCount >= requiredCount;
            if (hasEnough)
            {
                materialCountText.text = $"{ownedCount}/{requiredCount}";
            }
            else
            {
                materialCountText.text = $"<color=red>{ownedCount}/{requiredCount}</color>";
            }
        }
    }

    /// <summary>
    /// 재료 아이콘 로드 (비동기)
    /// </summary>
    private async UniTaskVoid LoadMaterialIcon(int materialId)
    {
        if (materialIcon == null) return;

        // Issue 2: 이전에 로드한 아이콘 해제
        ReleaseCurrentIcon();

        // IngredientData에서 Path_ID 가져오기
        var ingredientData = CSVLoader.Instance?.GetData<IngredientData>(materialId);
        if (ingredientData == null || ingredientData.Path_ID == 0)
        {
            materialIcon.enabled = false;
            return;
        }

        // PathData에서 Addressable_Key 가져오기
        var pathData = CSVLoader.Instance?.GetData<PathData>(ingredientData.Path_ID);
        if (pathData == null || string.IsNullOrEmpty(pathData.Addressable_Key))
        {
            materialIcon.enabled = false;
            return;
        }

        string iconPath = pathData.Addressable_Key;

        try
        {
            // 키가 존재하는지 확인
            var locationsHandle = Addressables.LoadResourceLocationsAsync(iconPath);
            var locations = await locationsHandle.Task;

            if (locations != null && locations.Count > 0)
            {
                // Issue 2: 핸들을 저장하여 나중에 해제할 수 있도록 함
                currentIconHandle = Addressables.LoadAssetAsync<Sprite>(iconPath);
                Sprite icon = await currentIconHandle.Task;

                if (icon != null && materialIcon != null)
                {
                    materialIcon.sprite = icon;
                    materialIcon.enabled = true;
                    hasLoadedIcon = true;
                }
            }
            else
            {
                materialIcon.enabled = false;
            }

            Addressables.Release(locationsHandle);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PartySynergyEnhancementPanel] Failed to load material icon: {iconPath}\n{e.Message}");
            materialIcon.enabled = false;
        }
    }

    /// <summary>
    /// Issue 2: 현재 로드된 아이콘 해제
    /// </summary>
    private void ReleaseCurrentIcon()
    {
        if (hasLoadedIcon && currentIconHandle.IsValid())
        {
            Addressables.Release(currentIconHandle);
            hasLoadedIcon = false;
        }
    }

    /// <summary>
    /// 골드 비용 정보 업데이트
    /// </summary>
    private void UpdateGoldInfo(PartySynergyEnhancementData enhancementData)
    {
        int requiredGold = enhancementData.Gold_Count;

        // 보유 골드 조회
        int ownedGold = CurrencyManager.Instance?.GetCurrency(CurrencyManager.GOLD_ID) ?? 0;

        if (goldText != null)
        {
            bool hasEnough = ownedGold >= requiredGold;
            if (hasEnough)
            {
                goldText.text = $"{requiredGold} G";
            }
            else
            {
                goldText.text = $"<color=red>{requiredGold} G</color>";
            }
        }
    }

    /// <summary>
    /// 버튼 활성화 조건 체크
    /// </summary>
    private void UpdateButtonInteractable(PartySynergyData data, int currentLevel)
    {
        if (upgradeButton == null || currentEnhancementData == null) return;

        // 조건 1: 캐릭터 레벨 충족
        int requiredLevel = currentEnhancementData.Need_Party_Characters_Lv % 100;
        bool meetsLevelRequirement = CheckAllCharactersMeetLevel(data, requiredLevel);

        // 조건 2: 재료 충족
        int materialId = currentEnhancementData.Material_ID;
        int requiredMaterial = currentEnhancementData.Material_Count;
        int ownedMaterial = IngredientManager.Instance?.GetIngredientCount(materialId) ?? 0;
        bool hasMaterial = ownedMaterial >= requiredMaterial;

        // 조건 3: 골드 충족
        int requiredGold = currentEnhancementData.Gold_Count;
        int ownedGold = CurrencyManager.Instance?.GetCurrency(CurrencyManager.GOLD_ID) ?? 0;
        bool hasGold = ownedGold >= requiredGold;

        // 모든 조건 충족 시 버튼 활성화
        upgradeButton.interactable = meetsLevelRequirement && hasMaterial && hasGold;

        Debug.Log($"[PartySynergyEnhancementPanel] Upgrade button: Level={meetsLevelRequirement}, Material={hasMaterial}, Gold={hasGold}");
    }

    #endregion

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(OnUpgradeButtonClicked);
        }

        // Issue 2: 아이콘 메모리 해제
        ReleaseCurrentIcon();
    }
}
