using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeamSetupPanel : MonoBehaviour
{
    [Header("Deck Slots (4개)")]
    [SerializeField] private DeckSlot[] deckSlots;

    [Header("Character Selection Popup")]
    [SerializeField] private GameObject characterSelectionPopup;
    [SerializeField] private Transform characterListParent;
    [SerializeField] private GameObject characterSlotPrefab;

    [Header("Party Synergy")]
    [SerializeField] private TextMeshProUGUI partyNameText;
    [SerializeField] private TextMeshProUGUI synergyDescText;

    private List<DeckCharacterSlot> characterSlots = new List<DeckCharacterSlot>();
    private int selectedDeckSlotIndex = -1;
    private bool isInitialized = false;

    void Start()
    {
        InitializeDeckSlots();

        if (characterSelectionPopup != null)
        {
            characterSelectionPopup.SetActive(false);
        }
    }

    void OnEnable()
    {
        if (!isInitialized && CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            InitializeCharacterList();
            isInitialized = true;
        }

        RefreshUI();
    }

    void Update()
    {
        if (!isInitialized && CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            InitializeCharacterList();
            isInitialized = true;
        }
    }

    void InitializeCharacterList()
    {
        var allCharacters = CSVLoader.Instance.GetTable<CharacterData>()?.GetAll();

        if (allCharacters == null || allCharacters.Count == 0)
        {
            Debug.LogError("[TeamSetupPanel] CharacterData를 불러올 수 없습니다!");
            return;
        }

        foreach (var characterData in allCharacters)
        {
            GameObject slotObj = Instantiate(characterSlotPrefab, characterListParent);
            DeckCharacterSlot slot = slotObj.GetComponent<DeckCharacterSlot>();

            if (slot != null)
            {
                slot.SetCharacter(characterData);
                Button button = slotObj.GetComponentInChildren<Button>();
                if (button != null)
                {
                    int characterID = characterData.Character_ID;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnCharacterSelected(characterID));
                }
                characterSlots.Add(slot);
            }
        }
    }

    void InitializeDeckSlots()
    {
        if (deckSlots == null || deckSlots.Length == 0) return;

        for (int i = 0; i < deckSlots.Length; i++)
        {
            int slotIndex = i;
            deckSlots[i].Initialize(slotIndex, OnDeckSlotClicked);
        }
    }

    /// <summary>
    /// 덱 슬롯 클릭 → 캐릭터 선택 팝업 열기
    /// </summary>
    public void OnDeckSlotClicked(int slotIndex)
    {
        selectedDeckSlotIndex = slotIndex;

        if (characterSelectionPopup != null)
        {
            characterSelectionPopup.SetActive(true);
        }

        Debug.Log($"[TeamSetupPanel] 슬롯 {slotIndex} 클릭 → 캐릭터 선택 팝업 열림");
    }

    /// <summary>
    /// 캐릭터 선택 팝업에서 캐릭터 선택 (수정됨!)
    /// </summary>
    private void OnCharacterSelected(int characterID)
    {
        characterSelectionPopup.SetActive(false);
        if (DeckManager.Instance == null || selectedDeckSlotIndex < 0)
        {
            Debug.LogWarning("[TeamSetupPanel] DeckManager 또는 슬롯 인덱스가 유효하지 않습니다.");
            return;
        }

        // 선택한 슬롯에 캐릭터 설정 (순서 유지!)
        bool success = DeckManager.Instance.SetCharacterAtIndex(selectedDeckSlotIndex, characterID);

        if (success)
        {
            Debug.Log($"[TeamSetupPanel] 슬롯 {selectedDeckSlotIndex}에 캐릭터 ID {characterID} 설정 완료");

            // 팝업 닫기
            if (characterSelectionPopup != null)
            {
                characterSelectionPopup.SetActive(false);
            }

            // UI 갱신
            RefreshUI();
        }
        else
        {
            Debug.LogWarning($"[TeamSetupPanel] 슬롯 {selectedDeckSlotIndex}에 캐릭터 설정 실패");
        }
    }

    /// <summary>
    /// 팝업 닫기 버튼 (Inspector에서 연결)
    /// </summary>
    public void CloseCharacterSelectionPopup()
    {
        if (characterSelectionPopup != null)
        {
            characterSelectionPopup.SetActive(false);
        }
        selectedDeckSlotIndex = -1;
    }

    /// <summary>
    /// Inspector Button OnClick에 연결
    /// </summary>
    public void OnSaveButtonClicked()
    {
        if (DeckManager.Instance == null) return;

        if (!DeckManager.Instance.IsDeckValid())
        {
            int minSize = DeckManager.Instance.GetMinDeckSize();
            int currentCount = DeckManager.Instance.GetDeckCount();
            Debug.LogWarning($"[TeamSetupPanel] 덱에 최소 {minSize}명이 필요합니다! (현재: {currentCount}명)");

            // TODO: UI 경고 메시지 표시
            return;
        }

        Debug.Log($"[TeamSetupPanel] 덱 저장 완료! 캐릭터 수: {DeckManager.Instance.GetDeckCount()}");

        // TODO: 저장 완료 UI 피드백
    }

    void RefreshUI()
    {
        RefreshDeckSlots();
        RefreshSynergyInfo();
    }

    /// <summary>
    /// 덱 슬롯 UI 갱신 (수정됨!)
    /// </summary>
    void RefreshDeckSlots()
    {
        if (DeckManager.Instance == null || deckSlots == null) return;

        for (int i = 0; i < deckSlots.Length; i++)
        {
            int characterID = DeckManager.Instance.GetCharacterAtIndex(i);

            if (characterID > 0) // 유효한 캐릭터
            {
                CharacterData data = CSVLoader.Instance.GetData<CharacterData>(characterID);
                if (data != null)
                {
                    deckSlots[i].SetCharacter(data);
                }
                else
                {
                    Debug.LogWarning($"[TeamSetupPanel] 캐릭터 ID {characterID}의 데이터를 찾을 수 없습니다.");
                    deckSlots[i].ClearSlot();
                }
            }
            else // 빈 슬롯
            {
                deckSlots[i].ClearSlot();
            }
        }
    }

    void RefreshSynergyInfo()
    {
        if (DeckManager.Instance == null) return;

        int deckCount = DeckManager.Instance.GetDeckCount();

        if (partyNameText != null)
        {
            partyNameText.text = deckCount > 0 ? "파티 이름 A" : "-";
        }

        if (synergyDescText != null)
        {
            if (deckCount >= 2)
            {
                synergyDescText.text = "공격력 +10%\n5초마다 경계 회복 +3";
            }
            else
            {
                synergyDescText.text = "시너지 효과 없음\n(캐릭터 2명 이상 필요)";
            }
        }
    }
}
