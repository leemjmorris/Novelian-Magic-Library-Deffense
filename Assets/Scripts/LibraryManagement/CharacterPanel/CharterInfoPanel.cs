using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [SerializeField] private GameObject bookmarkEquipPanel;
    [SerializeField] private GameObject enhancementPanelObject;

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

    [Header("Enhancement Panel")]
    [SerializeField] private EnhancementPanel enhancementPanel;

    [Header("Character Name Text")]
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Header("Character Level Text")]
    [SerializeField] private TextMeshProUGUI characterLevelText;
    [SerializeField] private TextMeshProUGUI characterSliderLevelText;

    [Header("Character Level Slider")]
    [SerializeField] private Slider characterLevelSlider;

    [Header("Character Sprite")]
    [SerializeField] private Image characterSprite;

    [Header("Up Button")]
    [SerializeField] private Button upButton;
    public int CharacterID { get; private set; }
    private int selectedSlotIndex = 0;
    private LibraryCharacterSlot currentSlot;

    public void InitInfo(int characterID, int level, LibraryCharacterSlot slot = null)
    {
        CharacterID = characterID;
        currentSlot = slot;
        var characterData = CSVLoader.Instance.GetData<CharacterData>(CharacterID);
        characterNameText.text = $"{CSVLoader.Instance.GetData<StringTable>(characterData.Character_Name_ID)?.Text ?? "Unknown"}";

        RefreshLevelUI();
        RefreshBookmarkUI();
        enhancementPanel?.Initialize(CharacterID);
    }

    /// <summary>
    /// 강화 레벨 UI 갱신
    /// </summary>
    public void RefreshLevelUI()
    {
        int enhancementLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(CharacterID);

        characterLevelText.text = $"Lv {enhancementLevel}";
        characterSliderLevelText.text = $"{enhancementLevel}/10";
        characterLevelSlider.value = enhancementLevel / 10f;
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
    public void RefreshBookmarkUI()
    {
        int availableSlots = CharacterEnhancementManager.Instance.GetBookmarkSlotCount(CharacterID);

        for (int i = 0; i < 5; i++)
        {
            Button slotButton = GetSlotButton(i);
            slotButton.interactable = (i < availableSlots);

            BookMark bookmark = BookMarkManager.Instance.GetCharacterBookmarkAtSlot(CharacterID, i);

            if (bookmark != null)
            {
                UpdateSlotText(i, bookmark.Name);
            }
            else
            {
                UpdateSlotText(i, $"책갈피 슬롯 {i + 1}");
            }
        }
    }

    /// <summary>
    /// 슬롯 인덱스에 해당하는 버튼 반환
    /// </summary>
    private Button GetSlotButton(int slotIndex)
    {
        return slotIndex switch
        {
            0 => bookmarkSlot1Button,
            1 => bookmarkSlot2Button,
            2 => bookmarkSlot3Button,
            3 => bookmarkSlot4Button,
            4 => bookmarkSlot5Button,
            _ => null
        };
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

    public void UpButton()
    {
        enhancementPanelObject.SetActive(true);
        enhancementPanel?.Initialize(CharacterID);
        gameObject.SetActive(false);
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
        currentSlot?.RefreshCharacterLevel();
        panel.SetActive(false);
    }
}
