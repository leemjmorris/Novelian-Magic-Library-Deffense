using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 강화 UI 패널
/// </summary>
public class EnhancementPanel : MonoBehaviour
{
    [Header("Enhancement Info UI")]
    [SerializeField] private TextMeshProUGUI enhancementLevelText;
    [SerializeField] private TextMeshProUGUI material1Text;
    [SerializeField] private TextMeshProUGUI material1CountText;
    [SerializeField] private TextMeshProUGUI material2Text;
    [SerializeField] private TextMeshProUGUI material2CountText;
    [SerializeField] private TextMeshProUGUI material3Text;
    [SerializeField] private TextMeshProUGUI material3CountText;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButton;

    [Header("Reference")]
    [SerializeField] private CharacterInfoPanel characterInfoPanel;
    [SerializeField] private GameObject characterInfoPanelObject;
    
    
    private int characterID;

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
    /// 승급 버튼 클릭 이벤트
    /// </summary>
    public void OnUpgradeButtonClicked()
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

        // 강화 실행
        if (CharacterEnhancementManager.Instance.TryEnhance(characterID))
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
        characterInfoPanel?.RefreshLevelUI();
        characterInfoPanel?.RefreshBookmarkUI();
        characterInfoPanelObject.SetActive(true);
    }
}
