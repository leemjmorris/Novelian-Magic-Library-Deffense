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
        RefreshEnhancementUI();
    }

    /// <summary>
    /// 강화 정보 UI 갱신
    /// </summary>
    private void RefreshEnhancementUI()
    {
        if (CharacterEnhancementManager.Instance == null)
        {
            Debug.LogWarning("CharacterEnhancementManager is not initialized");
            return;
        }

        // 현재 강화 레벨
        int currentLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(characterID);
        int nextLevel = currentLevel + 1;

        // 최대 레벨 체크
        if (currentLevel >= 10)
        {
            if (enhancementLevelText != null)
                enhancementLevelText.text = "최대 레벨 달성!";
            if (upgradeButton != null)
                upgradeButton.interactable = false;

            // 재료 텍스트 비활성화
            if (material1Text != null) material1Text.text = "-";
            if (material2Text != null) material2Text.text = "-";
            if (material3Text != null) material3Text.text = "-";
            return;
        }

        // 강화 레벨 텍스트
        if (enhancementLevelText != null)
        {
            enhancementLevelText.text = $"Lv {currentLevel} → Lv {nextLevel}";
        }

        // 다음 강화 정보 가져오기
        EnhancementLevelData nextInfo = CharacterEnhancementManager.Instance.GetNextEnhancementInfo(characterID);
        if (nextInfo == null)
        {
            Debug.LogError("Failed to get next enhancement info");
            return;
        }

        // 재료 1 표시
        if (material1Text != null)
        {
            string mat1Name = IngredientManager.Instance.GetIngredientName(nextInfo.Material_1_ID);
            int mat1Current = IngredientManager.Instance.GetIngredientCount(nextInfo.Material_1_ID);

            int mat1Required = nextInfo.Material_1_Count;
            bool mat1Enough = mat1Current >= mat1Required;

            material1Text.text = $"{mat1Name}";
            material1CountText.text = $"{mat1Current}/{mat1Required}";
            material1CountText.color = mat1Enough ? Color.white : Color.red;

            // 재료 1 아이콘 로드
            LoadMaterialIcon(nextInfo.Material_1_ID, material1Icon, 1).Forget();
        }

        // 재료 2 표시
        if (material2Text != null)
        {
            string mat2Name = IngredientManager.Instance.GetIngredientName(nextInfo.Material_2_ID);
            int mat2Current = IngredientManager.Instance.GetIngredientCount(nextInfo.Material_2_ID);
            int mat2Required = nextInfo.Material_2_Count;
            bool mat2Enough = mat2Current >= mat2Required;

            material2Text.text = $"{mat2Name}";
            material2CountText.text = $"{mat2Current}/{mat2Required}";
            material2CountText.color = mat2Enough ? Color.white : Color.red;

            // 재료 2 아이콘 로드
            LoadMaterialIcon(nextInfo.Material_2_ID, material2Icon, 2).Forget();
        }

        // 재료 3 표시
        if (material3Text != null)
        {
            string mat3Name = IngredientManager.Instance.GetIngredientName(nextInfo.Material_3_ID);
            int mat3Current = IngredientManager.Instance.GetIngredientCount(nextInfo.Material_3_ID);
            int mat3Required = nextInfo.Material_3_Count;
            bool mat3Enough = mat3Current >= mat3Required;

            material3Text.text = $"{mat3Name}";
            material3CountText.text = $"{mat3Current}/{mat3Required}";
            material3CountText.color = mat3Enough ? Color.white : Color.red;

            // 재료 3 아이콘 로드
            LoadMaterialIcon(nextInfo.Material_3_ID, material3Icon, 3).Forget();
        }

        goldText.text = $"소모 골드: {nextInfo.Material_4_Count}G";

        // 버튼 활성화/비활성화
        if (upgradeButton != null)
        {
            bool canEnhance = CharacterEnhancementManager.Instance.CanEnhance(characterID, out _);
            upgradeButton.interactable = canEnhance;
        }
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
            Debug.LogError("CharacterEnhancementManager is not initialized");
            return;
        }

        // 강화 가능 확인
        if (!CharacterEnhancementManager.Instance.CanEnhance(characterID, out string failReason))
        {
            Debug.LogWarning($"[Enhancement Failed] {failReason}");
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
            Debug.Log($"[Enhancement Success] {charName} 강화 완료!");

            // UI 갱신
            RefreshEnhancementUI();
            characterInfoPanel?.RefreshBookmarkUI();

            // TODO: 강화 성공 이펙트/사운드
        }
        else
        {
            Debug.LogError("Enhancement failed unexpectedly");
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
        Debug.Log($"[EnhancementPanel] LoadMaterialIcon - Slot {slotIndex}, MaterialID: {materialId}, IconImage: {(iconImage != null ? iconImage.name : "NULL")}");

        if (iconImage == null)
        {
            Debug.LogWarning($"[EnhancementPanel] Slot {slotIndex} - iconImage is NULL!");
            return;
        }

        // 이전에 로드한 아이콘 해제
        ReleaseMaterialIcon(slotIndex);

        // materialId가 0이면 재료 없음
        if (materialId == 0)
        {
            Debug.Log($"[EnhancementPanel] Slot {slotIndex} - materialId is 0, disabling icon");
            iconImage.enabled = false;
            return;
        }

        // IngredientData에서 Path_ID 가져오기
        var ingredientData = CSVLoader.Instance?.GetData<IngredientData>(materialId);
        if (ingredientData == null || ingredientData.Path_ID == 0)
        {
            Debug.LogWarning($"[EnhancementPanel] Slot {slotIndex} - IngredientData null or Path_ID is 0. IngredientData: {(ingredientData != null ? $"exists, Path_ID={ingredientData.Path_ID}" : "NULL")}");
            iconImage.enabled = false;
            return;
        }

        // PathData에서 Addressable_Key 가져오기
        var pathData = CSVLoader.Instance?.GetData<PathData>(ingredientData.Path_ID);
        if (pathData == null || string.IsNullOrEmpty(pathData.Addressable_Key) || pathData.Addressable_Key == "0")
        {
            Debug.LogWarning($"[EnhancementPanel] Slot {slotIndex} - PathData issue. PathData: {(pathData != null ? $"exists, Addressable_Key={pathData.Addressable_Key}" : "NULL")}");
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

            if (icon != null && iconImage != null)
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
            Debug.LogWarning($"[EnhancementPanel] Failed to load material icon: {iconPath}\n{e.Message}");
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
