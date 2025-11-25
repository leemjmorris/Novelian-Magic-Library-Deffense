using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [SerializeField] private GameObject bookmarkEquipPanel;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Character Info Tabs")]
    [SerializeField] private Button story1Button;
    [SerializeField] private Button story2Button;
    [SerializeField] private Button story3Button;

    [Header("Bookmark Equip Buttons")]
    [SerializeField] private Button bookmarkSlot1Button;
    [SerializeField] private Button bookmarkSlot2Button;
    [SerializeField] private Button bookmarkSlot3Button;
    [SerializeField] private Button bookmarkSlot4Button;
    [SerializeField] private Button bookmarkSlot5Button;

    [Header("Bookmark Equip Texts")]
    [SerializeField] private TextMeshProUGUI bookmarkSlot1Text;
    [SerializeField] private TextMeshProUGUI bookmarkSlot2Text;
    [SerializeField] private TextMeshProUGUI bookmarkSlot3Text;
    [SerializeField] private TextMeshProUGUI bookmarkSlot4Text;
    [SerializeField] private TextMeshProUGUI bookmarkSlot5Text;

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButton;

    [Header("Character Name Text")]
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Header("Character Level Text")]
    [SerializeField] private TextMeshProUGUI characterLevelText;

    [Header("Character EXP Text")]
    [SerializeField] private TextMeshProUGUI characterExpText;

    [Header("Character EXP Slider")]
    [SerializeField] private Slider characterExpSlider;

    [Header("Character Sprite")]
    [SerializeField] private Image characterSprite;

    [Header("Enhancement Info UI")]
    [SerializeField] private TextMeshProUGUI enhancementLevelText;
    [SerializeField] private TextMeshProUGUI material1Text;
    [SerializeField] private TextMeshProUGUI material2Text;
    [SerializeField] private TextMeshProUGUI material3Text;
    [SerializeField] private TextMeshProUGUI bookmarkSlotText;

    public int CharacterID { get; private set; }
    private int selectedSlotIndex = 0;

    public void InitInfo(int characterID, int level)
    {
        CharacterID = characterID;
        var characterData = CSVLoader.Instance.GetData<CharacterData>(CharacterID);
        characterNameText.text = $"{characterData.Character_Name}";
        characterLevelText.text = $"Lv.{level}";

        RefreshBookmarkUI();
        RefreshEnhancementUI();
    }
    /// <summary>
    /// 책갈피 슬롯 버튼 클릭 (Inspector OnClick에서 호출)
    /// </summary>
    public void OnBookmarkSlotClicked(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        Debug.Log($"Slot {slotIndex} selected");
        ShowBookmarkEquipPanel();
    }

    /// <summary>
    /// 현재 선택된 슬롯 인덱스 가져오기
    /// </summary>
    public int GetSelectedSlotIndex()
    {
        return selectedSlotIndex;
    }

    /// <summary>
    /// BookMarkManager에서 장착된 책갈피 정보 가져와서 UI 갱신
    /// </summary>
    private void RefreshBookmarkUI()
    {
        for (int i = 0; i < 5; i++)
        {
            BookMark bookmark = BookMarkManager.Instance.GetCharacterBookmarkAtSlot(CharacterID, i);

            if (bookmark != null)
            {
                UpdateSlotText(i, bookmark.Name);
            }
            else
            {
                UpdateSlotText(i, $"책갈피 슬롯 {i + 1}"); // Display "슬롯 1", "슬롯 2", etc.
            }
        }
    }

    public void StoryButtonClicked()
    {

    }

    public void UpdateSlotText(int slotIndex, string bookmarkName)
    {
        switch (slotIndex)
        {
            case 0:
                bookmarkSlot1Text.text = bookmarkName;
                break;
            case 1:
                bookmarkSlot2Text.text = bookmarkName;
                break;
            case 2:
                bookmarkSlot3Text.text = bookmarkName;
                break;
            case 3:
                bookmarkSlot4Text.text = bookmarkName;
                break;
            case 4:
                bookmarkSlot5Text.text = bookmarkName;
                break;
            default:
                Debug.LogError("Invalid slot index");
                break;
        }
    }

    public void ShowBookmarkEquipPanel()
    {
        bookmarkEquipPanel.SetActive(true);
    }

    public void ShowPanel()
    {
        panel.SetActive(true);
    }

    public void HidePanel()
    {
        panel.SetActive(false);
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
        int currentLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(CharacterID);
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
            if (bookmarkSlotText != null) bookmarkSlotText.text = $"책갈피 슬롯: 5개";
            return;
        }

        // 강화 레벨 텍스트
        if (enhancementLevelText != null)
        {
            enhancementLevelText.text = $"Lv {currentLevel} → Lv {nextLevel}";
        }

        // 다음 강화 정보 가져오기
        EnhancementLevelData nextInfo = CharacterEnhancementManager.Instance.GetNextEnhancementInfo(CharacterID);
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

            material1Text.text = $"{mat1Name}: {mat1Current}/{mat1Required}";
            material1Text.color = mat1Enough ? Color.white : Color.red;
        }

        // 재료 2 표시
        if (material2Text != null)
        {
            string mat2Name = IngredientManager.Instance.GetIngredientName(nextInfo.Material_2_ID);
            int mat2Current = IngredientManager.Instance.GetIngredientCount(nextInfo.Material_2_ID);
            int mat2Required = nextInfo.Material_2_Count;
            bool mat2Enough = mat2Current >= mat2Required;

            material2Text.text = $"{mat2Name}: {mat2Current}/{mat2Required}";
            material2Text.color = mat2Enough ? Color.white : Color.red;
        }

        // 재료 3 표시
        if (material3Text != null)
        {
            string mat3Name = IngredientManager.Instance.GetIngredientName(nextInfo.Material_3_ID);
            int mat3Current = IngredientManager.Instance.GetIngredientCount(nextInfo.Material_3_ID);
            int mat3Required = nextInfo.Material_3_Count;
            bool mat3Enough = mat3Current >= mat3Required;

            material3Text.text = $"{mat3Name}: {mat3Current}/{mat3Required}";
            material3Text.color = mat3Enough ? Color.white : Color.red;
        }

        // 북마크 슬롯 표시
        if (bookmarkSlotText != null)
        {
            int currentSlots = CharacterEnhancementManager.Instance.GetBookmarkSlotCount(CharacterID);
            int nextSlots = nextInfo.Bookmark_Slots;
            bookmarkSlotText.text = $"책갈피 슬롯: {currentSlots} → {nextSlots}";
        }

        // 버튼 활성화/비활성화
        if (upgradeButton != null)
        {
            bool canEnhance = CharacterEnhancementManager.Instance.CanEnhance(CharacterID, out _);
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
        if (!CharacterEnhancementManager.Instance.CanEnhance(CharacterID, out string failReason))
        {
            Debug.LogWarning($"[Enhancement Failed] {failReason}");
            // TODO: 팝업 표시
            return;
        }

        // 강화 실행
        if (CharacterEnhancementManager.Instance.TryEnhance(CharacterID))
        {
            CharacterData charData = CSVLoader.Instance.GetData<CharacterData>(CharacterID);
            Debug.Log($"[Enhancement Success] {charData.Character_Name} 강화 완료!");

            // UI 갱신
            RefreshEnhancementUI();
            RefreshBookmarkUI();

            // TODO: 강화 성공 이펙트/사운드
        }
        else
        {
            Debug.LogError("Enhancement failed unexpectedly");
        }
    }
}
