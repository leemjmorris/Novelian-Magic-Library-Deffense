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
    [SerializeField] private GameObject deckPanel;

    private List<DeckCharacterSlot> characterSlots = new List<DeckCharacterSlot>();
    private int selectedDeckSlotIndex = -1;
    private bool isInitialized = false;

    // 임시 덱 시스템 (저장하기 전까지 임시 저장)
    private List<int> tempDeck = new List<int>();
    private bool isSaved = false;

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

        // 패널 열릴 때: 저장된 덱을 임시 덱에 복사
        isSaved = false;
        LoadTempDeckFromManager();

        RefreshUI();
    }

    void OnDisable()
    {
        // 패널 닫힐 때: 저장 안 했으면 임시 덱 초기화
        if (!isSaved)
        {
            tempDeck.Clear();
            Debug.Log("[TeamSetupPanel] 저장하지 않고 패널 닫힘 - 임시 덱 초기화");
        }
        deckPanel.SetActive(false);
    }

    /// <summary>
    /// DeckManager의 저장된 덱을 임시 덱에 복사
    /// </summary>
    private void LoadTempDeckFromManager()
    {
        tempDeck.Clear();
        if (DeckManager.Instance != null)
        {
            tempDeck = DeckManager.Instance.GetDeck();
        }
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
    /// 캐릭터 선택 팝업에서 캐릭터 선택 (임시 덱에 저장)
    /// </summary>
    private void OnCharacterSelected(int characterID)
    {
        characterSelectionPopup.SetActive(false);
        if (selectedDeckSlotIndex < 0)
        {
            Debug.LogWarning("[TeamSetupPanel] 슬롯 인덱스가 유효하지 않습니다.");
            return;
        }

        // 임시 덱에 캐릭터 설정 (DeckManager가 아닌 tempDeck에 저장)
        bool success = SetTempDeckCharacterAtIndex(selectedDeckSlotIndex, characterID);

        if (success)
        {
            Debug.Log($"[TeamSetupPanel] 임시 덱 슬롯 {selectedDeckSlotIndex}에 캐릭터 ID {characterID} 설정 (저장 전)");

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
            Debug.LogWarning($"[TeamSetupPanel] 임시 덱 슬롯 {selectedDeckSlotIndex}에 캐릭터 설정 실패");
        }
    }

    /// <summary>
    /// 임시 덱의 특정 인덱스에 캐릭터 설정
    /// </summary>
    private bool SetTempDeckCharacterAtIndex(int index, int characterID)
    {
        const int MAX_DECK_SIZE = 4;
        if (index < 0 || index >= MAX_DECK_SIZE)
        {
            return false;
        }

        // 이미 다른 슬롯에 같은 캐릭터가 있으면 제거
        int existingIndex = tempDeck.IndexOf(characterID);
        if (existingIndex >= 0 && existingIndex != index)
        {
            tempDeck[existingIndex] = -1;
        }

        // 리스트 크기 확장
        while (tempDeck.Count <= index)
        {
            tempDeck.Add(-1);
        }

        tempDeck[index] = characterID;
        return true;
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

        // 임시 덱의 유효한 캐릭터 수 확인
        int validCount = GetTempDeckValidCount();
        int minSize = DeckManager.Instance.GetMinDeckSize();

        if (validCount < minSize)
        {
            Debug.LogWarning($"[TeamSetupPanel] 덱에 최소 {minSize}명이 필요합니다! (현재: {validCount}명)");
            // TODO: UI 경고 메시지 표시
            return;
        }

        // 임시 덱을 실제 DeckManager에 저장
        SaveTempDeckToManager();
        isSaved = true;

        Debug.Log($"[TeamSetupPanel] 덱 저장 완료! 캐릭터 수: {validCount}");

        // TODO: 저장 완료 UI 피드백
    }

    /// <summary>
    /// 임시 덱을 DeckManager에 저장
    /// </summary>
    private void SaveTempDeckToManager()
    {
        if (DeckManager.Instance == null) return;

        DeckManager.Instance.ClearDeck();
        for (int i = 0; i < tempDeck.Count; i++)
        {
            if (tempDeck[i] > 0)
            {
                DeckManager.Instance.SetCharacterAtIndex(i, tempDeck[i]);
            }
        }
    }

    /// <summary>
    /// 임시 덱의 유효한 캐릭터 수 반환
    /// </summary>
    private int GetTempDeckValidCount()
    {
        int count = 0;
        foreach (int id in tempDeck)
        {
            if (id > 0) count++;
        }
        return count;
    }

    void RefreshUI()
    {
        RefreshDeckSlots();
        RefreshSynergyInfo();
    }

    /// <summary>
    /// 덱 슬롯 UI 갱신 (임시 덱 기준으로 표시)
    /// </summary>
    void RefreshDeckSlots()
    {
        if (deckSlots == null) return;

        for (int i = 0; i < deckSlots.Length; i++)
        {
            // 임시 덱에서 캐릭터 ID 가져오기 (DeckManager가 아닌 tempDeck 사용)
            int characterID = GetTempDeckCharacterAtIndex(i);

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

    /// <summary>
    /// 임시 덱에서 특정 인덱스의 캐릭터 ID 가져오기
    /// </summary>
    private int GetTempDeckCharacterAtIndex(int index)
    {
        if (index < 0 || index >= tempDeck.Count)
        {
            return -1;
        }
        return tempDeck[index];
    }

    void RefreshSynergyInfo()
    {
        // 임시 덱 기준으로 시너지 정보 표시
        int deckCount = GetTempDeckValidCount();

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
