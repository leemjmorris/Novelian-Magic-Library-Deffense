using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject raycastPanel;

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

    // 현재 로드 중인 캐릭터 ID (다른 캐릭터 선택 시 이전 로드 무시용)
    private int loadingCharacterId = 0;

    public void InitInfo(int characterID, int level, LibraryCharacterSlot slot = null)
    {
        CharacterID = characterID;
        currentSlot = slot;
        var characterData = CSVLoader.Instance.GetData<CharacterData>(CharacterID);
        characterNameText.text = $"{CSVLoader.Instance.GetData<StringTable>(characterData.Character_Name_ID)?.Text ?? "Unknown"}";

        // 캐릭터 아이콘 로드
        LoadCharacterSprite(characterData);

        RefreshLevelUI();
        RefreshBookmarkUI();
        enhancementPanel?.Initialize(CharacterID);
    }

    /// <summary>
    /// 캐릭터 스프라이트 로드
    /// </summary>
    private void LoadCharacterSprite(CharacterData characterData)
    {
        if (characterSprite == null || characterData == null) return;

        // 로드 중인 캐릭터 ID 기록 (다른 캐릭터 선택 시 이전 로드 무시용)
        loadingCharacterId = characterData.Character_ID;
        int expectedCharacterId = loadingCharacterId;

        string spriteKey = AddressableKey.Icon_Character;

        // Path_ID가 있으면 PathTable에서 개별 아이콘 키 조회
        if (characterData.Path_ID > 0)
        {
            var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
            if (pathData != null && !string.IsNullOrEmpty(pathData.Addressable_Key))
            {
                spriteKey = pathData.Addressable_Key;
            }
        }

        Addressables.LoadAssetAsync<Sprite>(spriteKey).Completed += handle =>
        {
            // 오브젝트가 파괴되었거나 다른 캐릭터로 변경된 경우 무시
            if (this == null || characterSprite == null) return;
            if (loadingCharacterId != expectedCharacterId) return;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                characterSprite.sprite = handle.Result;
            }
            else
            {
                Debug.LogWarning($"[CharacterInfoPanel] Failed to load character sprite: {spriteKey}");
            }
        };
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
                UpdateSlotText(i, bookmark.DisplayName);
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
        Debug.Log($"[CharacterInfoPanel] ShowPanel called - panel: {(panel != null ? panel.name : "NULL")}, raycastPanel: {(raycastPanel != null ? raycastPanel.name : "NULL")}");
        raycastPanel?.SetActive(true);
        panel.SetActive(true);
    }

    public void HidePanel()
    {
        currentSlot?.RefreshCharacterLevel();
        panel.SetActive(false);
        raycastPanel?.SetActive(false);
    }
}
