using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

/// <summary>
/// 캐릭터 강화 UI 패널
/// </summary>
public class EnhancementPanel : MonoBehaviour
{
    [Header("Enhancement Info UI")]
    [SerializeField] private TextMeshProUGUI enhancementLevelText;
    [SerializeField] private TextMeshProUGUI material1Text;
    [SerializeField] private TextMeshProUGUI material1CountText;
    [SerializeField] private Image material1Icon;
    [SerializeField] private TextMeshProUGUI material2Text;
    [SerializeField] private TextMeshProUGUI material2CountText;
    [SerializeField] private Image material2Icon;
    [SerializeField] private TextMeshProUGUI material3Text;
    [SerializeField] private TextMeshProUGUI material3CountText;
    [SerializeField] private Image material3Icon;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButton;

    [Header("Reference")]
    [SerializeField] private CharacterInfoPanel characterInfoPanel;
    [SerializeField] private GameObject characterInfoPanelObject;
    [SerializeField] private GameObject raycastPanel;

    private int characterID;

    // Addressables 메모리 관리
    private AsyncOperationHandle<Sprite> material1IconHandle;
    private AsyncOperationHandle<Sprite> material2IconHandle;
    private AsyncOperationHandle<Sprite> material3IconHandle;
    private bool hasMaterial1Icon = false;
    private bool hasMaterial2Icon = false;
    private bool hasMaterial3Icon = false;

    // 현재 로드 중인 캐릭터 ID (다른 캐릭터 선택 시 이전 로드 무시용)
    private int loadingCharacterId = 0;

    private void OnEnable()
    {
        raycastPanel?.SetActive(true);
    }

    /// <summary>
    /// 캐릭터 ID 설정 및 UI 초기화
    /// </summary>
    public void Initialize(int characterID)
    {
        this.characterID = characterID;
        this.loadingCharacterId = characterID;
        InitializeAsync().Forget();
    }

    /// <summary>
    /// 비동기 초기화 (Firebase 데이터 로드 대기)
    /// </summary>
    private async UniTaskVoid InitializeAsync()
    {
        // Firebase 데이터 로드 완료 대기 (최대 3초 타임아웃)
        float timeout = 3f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (IngredientManager.Instance != null && IngredientManager.Instance.IsDataLoaded)
                break;

            await UniTask.Delay(100);
            elapsed += 0.1f;
        }

        // 타임아웃 시에도 UI 갱신 시도 (0으로 표시될 수 있음)
        RefreshEnhancementUI();
    }

    /// <summary>
    /// 강화 정보 UI 갱신
    /// </summary>
    private void RefreshEnhancementUI()
    {
        if (CharacterEnhancementManager.Instance == null)
        {
            GameLog.LogWarning("CharacterEnhancementManager is not initialized");
            return;
        }

        // 현재 강화 레벨
        int currentLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(characterID);
        int nextLevel = currentLevel + 1;
        bool isMaxLevel = currentLevel >= 10;

        // 강화 레벨 텍스트
        if (enhancementLevelText != null)
        {
            enhancementLevelText.text = isMaxLevel ? "최대 레벨 달성!" : $"Lv {currentLevel} → Lv {nextLevel}";
        }

        // 최대 레벨일 경우 버튼 비활성화
        if (isMaxLevel && upgradeButton != null)
        {
            upgradeButton.interactable = false;
        }

        // 강화 정보 가져오기 (최대 레벨이면 현재 레벨 정보, 아니면 다음 레벨 정보)
        EnhancementLevelData enhancementInfo = isMaxLevel
            ? CharacterEnhancementManager.Instance.GetCurrentEnhancementInfo(characterID)
            : CharacterEnhancementManager.Instance.GetNextEnhancementInfo(characterID);

        if (enhancementInfo == null)
        {
            GameLog.LogError("Failed to get enhancement info");
            // 정보가 없어도 빈 값으로 표시
            DisplayMaterialInfo(1, 0, 0, material1Text, material1CountText, material1Icon);
            DisplayMaterialInfo(2, 0, 0, material2Text, material2CountText, material2Icon);
            DisplayMaterialInfo(3, 0, 0, material3Text, material3CountText, material3Icon);
            if (goldText != null) goldText.text = "소모 골드: 0G";
            return;
        }

        // 재료 1 표시
        DisplayMaterialInfo(1, enhancementInfo.Material_1_ID, enhancementInfo.Material_1_Count, material1Text, material1CountText, material1Icon);

        // 재료 2 표시
        DisplayMaterialInfo(2, enhancementInfo.Material_2_ID, enhancementInfo.Material_2_Count, material2Text, material2CountText, material2Icon);

        // 재료 3 표시
        DisplayMaterialInfo(3, enhancementInfo.Material_3_ID, enhancementInfo.Material_3_Count, material3Text, material3CountText, material3Icon);

        // 골드 표시
        if (goldText != null)
            goldText.text = $"소모 골드: {enhancementInfo.Material_4_Count}G";

        // 버튼 활성화/비활성화 (최대 레벨이면 이미 비활성화됨)
        if (!isMaxLevel && upgradeButton != null)
        {
            bool canEnhance = CharacterEnhancementManager.Instance.CanEnhance(characterID, out _);
            upgradeButton.interactable = canEnhance;
        }
    }

    /// <summary>
    /// 재료 정보 표시 헬퍼 메서드
    /// </summary>
    private void DisplayMaterialInfo(int slotIndex, int materialId, int requiredCount,
        TextMeshProUGUI nameText, TextMeshProUGUI countText, Image iconImage)
    {
        // 재료 ID가 0이면 빈 값으로 표시
        if (materialId == 0)
        {
            if (nameText != null) nameText.text = "-";
            if (countText != null)
            {
                countText.text = "-";
                countText.color = Color.white;
            }
            if (iconImage != null) iconImage.enabled = false;
            return;
        }

        string matName = IngredientManager.Instance.GetIngredientName(materialId);
        int matCurrent = IngredientManager.Instance.GetIngredientCount(materialId);
        bool matEnough = matCurrent >= requiredCount;

        if (nameText != null)
            nameText.text = matName;

        if (countText != null)
        {
            countText.text = $"{matCurrent}/{requiredCount}";
            countText.color = matEnough ? Color.white : Color.red;
        }

        // 아이콘 로드
        LoadMaterialIcon(materialId, iconImage, slotIndex).Forget();
    }

    /// <summary>
    /// 승급 버튼 클릭 이벤트 (Unity Button용 래퍼)
    /// </summary>
    public void OnUpgradeButtonClicked()
    {
        OnUpgradeButtonClickedAsync().Forget();
    }

    /// <summary>
    /// 승급 버튼 클릭 이벤트 (async - Firebase 저장 완료 대기)
    /// </summary>
    private async UniTaskVoid OnUpgradeButtonClickedAsync()
    {
        if (CharacterEnhancementManager.Instance == null)
        {
            GameLog.LogError("CharacterEnhancementManager is not initialized");
            return;
        }

        // 강화 가능 확인
        if (!CharacterEnhancementManager.Instance.CanEnhance(characterID, out string failReason))
        {
            GameLog.LogWarning($"[Enhancement Failed] {failReason}");
            // TODO: 팝업 표시
            return;
        }

        // 버튼 비활성화 (중복 클릭 방지)
        if (upgradeButton != null)
            upgradeButton.interactable = false;

        // 강화 실행 (Firebase 저장 완료 대기)
        bool success = await CharacterEnhancementManager.Instance.TryEnhanceAsync(characterID);

        // 버튼 다시 활성화
        if (upgradeButton != null)
            upgradeButton.interactable = true;

        if (success)
        {
            CharacterData charData = CSVLoader.Instance.GetData<CharacterData>(characterID);
            string charName = CSVLoader.Instance.GetData<StringTable>(charData.Character_Name_ID)?.Text ?? "Unknown";
            GameLog.Log($"[Enhancement Success] {charName} 강화 완료!");

            // UI 갱신
            RefreshEnhancementUI();
            characterInfoPanel?.RefreshBookmarkUI();

            // TODO: 강화 성공 이펙트/사운드
        }
        else
        {
            GameLog.LogError("Enhancement failed unexpectedly");
        }
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        raycastPanel?.SetActive(false);
        characterInfoPanel?.RefreshLevelUI();
        characterInfoPanel?.RefreshBookmarkUI();
        characterInfoPanelObject.SetActive(true);
    }

    /// <summary>
    /// 재료 아이콘 로드 (비동기)
    /// </summary>
    private async UniTaskVoid LoadMaterialIcon(int materialId, Image iconImage, int slotIndex)
    {
        GameLog.Log($"[EnhancementPanel] LoadMaterialIcon - Slot {slotIndex}, MaterialID: {materialId}, IconImage: {(iconImage != null ? iconImage.name : "NULL")}");

        if (iconImage == null)
        {
            GameLog.LogWarning($"[EnhancementPanel] Slot {slotIndex} - iconImage is NULL!");
            return;
        }

        // 로드 시작 시 현재 캐릭터 ID 기록
        int expectedCharacterId = loadingCharacterId;

        // 이전에 로드한 아이콘 해제
        ReleaseMaterialIcon(slotIndex);

        // materialId가 0이면 재료 없음
        if (materialId == 0)
        {
            GameLog.Log($"[EnhancementPanel] Slot {slotIndex} - materialId is 0, disabling icon");
            iconImage.enabled = false;
            return;
        }

        // IngredientData에서 Path_ID 가져오기
        var ingredientData = CSVLoader.Instance?.GetData<IngredientData>(materialId);
        if (ingredientData == null || ingredientData.Path_ID == 0)
        {
            GameLog.LogWarning($"[EnhancementPanel] Slot {slotIndex} - IngredientData null or Path_ID is 0. IngredientData: {(ingredientData != null ? $"exists, Path_ID={ingredientData.Path_ID}" : "NULL")}");
            iconImage.enabled = false;
            return;
        }

        // PathData에서 Addressable_Key 가져오기
        var pathData = CSVLoader.Instance?.GetData<PathData>(ingredientData.Path_ID);
        if (pathData == null || string.IsNullOrEmpty(pathData.Addressable_Key) || pathData.Addressable_Key == "0")
        {
            GameLog.LogWarning($"[EnhancementPanel] Slot {slotIndex} - PathData issue. PathData: {(pathData != null ? $"exists, Addressable_Key={pathData.Addressable_Key}" : "NULL")}");
            iconImage.enabled = false;
            return;
        }

        string iconPath = pathData.Addressable_Key;

        // 유효한 Addressable 키인지 확인
        if (string.IsNullOrWhiteSpace(iconPath) || iconPath == "0")
        {
            iconImage.enabled = false;
            return;
        }

        try
        {
            var handle = Addressables.LoadAssetAsync<Sprite>(iconPath);
            Sprite icon = await handle.Task;

            // 오브젝트가 파괴되었거나 다른 캐릭터로 변경된 경우 무시
            if (this == null || iconImage == null) return;
            if (loadingCharacterId != expectedCharacterId)
            {
                // 이전 캐릭터의 로드 결과는 해제
                if (handle.IsValid())
                    Addressables.Release(handle);
                return;
            }

            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;

                // 핸들 저장
                switch (slotIndex)
                {
                    case 1:
                        material1IconHandle = handle;
                        hasMaterial1Icon = true;
                        break;
                    case 2:
                        material2IconHandle = handle;
                        hasMaterial2Icon = true;
                        break;
                    case 3:
                        material3IconHandle = handle;
                        hasMaterial3Icon = true;
                        break;
                }
            }
        }
        catch (System.Exception e)
        {
            GameLog.LogWarning($"[EnhancementPanel] Failed to load material icon: {iconPath}\n{e.Message}");
            if (iconImage != null)
                iconImage.enabled = false;
        }
    }

    /// <summary>
    /// 재료 아이콘 메모리 해제
    /// </summary>
    private void ReleaseMaterialIcon(int slotIndex)
    {
        switch (slotIndex)
        {
            case 1:
                if (hasMaterial1Icon && material1IconHandle.IsValid())
                {
                    Addressables.Release(material1IconHandle);
                    hasMaterial1Icon = false;
                }
                break;
            case 2:
                if (hasMaterial2Icon && material2IconHandle.IsValid())
                {
                    Addressables.Release(material2IconHandle);
                    hasMaterial2Icon = false;
                }
                break;
            case 3:
                if (hasMaterial3Icon && material3IconHandle.IsValid())
                {
                    Addressables.Release(material3IconHandle);
                    hasMaterial3Icon = false;
                }
                break;
        }
    }

    /// <summary>
    /// 모든 재료 아이콘 메모리 해제
    /// </summary>
    private void ReleaseAllMaterialIcons()
    {
        ReleaseMaterialIcon(1);
        ReleaseMaterialIcon(2);
        ReleaseMaterialIcon(3);
    }

    private void OnDestroy()
    {
        ReleaseAllMaterialIcons();
    }
}
