using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeamSetupPanel : MonoBehaviour
{
    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup presetCanvasGroup;

    [Header("Tabs")]
    [SerializeField] private GameObject partyTab;
    [SerializeField] private GameObject characterTab;

    [Header("Prefabs")]
    [SerializeField] private GameObject characterSlotPrefab;
    [SerializeField] private GameObject deckslotPrefab;

    [Header("Containers")]
    [SerializeField] private Transform characterSlotContainer;

    [Header("Deck Slots")]
    [SerializeField] private List<DeckSlot> deckSlots = new List<DeckSlot>();

    [Header("Deck Unset Panel")]
    [SerializeField] private GameObject deckUnsetPanel;

    private List<DeckCharacterSlot> characterSlots = new List<DeckCharacterSlot>();
    private int selectedDeckSlotIndex = -1; // 현재 선택된 덱 슬롯
    private bool isInitialized = false;

    private void Start()
    {
        // 덱 슬롯 인덱스 설정
        for (int i = 0; i < deckSlots.Count; i++)
        {
            deckSlots[i].SetSlotIndex(i);
        }

        // 모든 캐릭터 데이터 가져오기
        var allCharacters = CSVLoader.Instance.GetTable<CharacterData>().GetAll();

        foreach (var characterData in allCharacters)
        {
            // 슬롯 인스턴스 생성
            GameObject slotObj = Instantiate(characterSlotPrefab, characterSlotContainer);
            DeckCharacterSlot slot = slotObj.GetComponent<DeckCharacterSlot>();

            // TeamSetupPanel 연결
            slot.SetPanel(this);

            // 캐릭터 ID로 초기화
            slot.Init(characterData.Character_ID);
            characterSlots.Add(slot);
        }

        isInitialized = true;

        // Start에서 첫 복원 실행
        RestoreDeckFromManager();
        RefreshAllSlots();
    }

    private void OnEnable()
    {
        // Start 이후에만 복원 (첫 실행 시 Start에서 처리)
        if (!isInitialized) return;

        // 패널 활성화 시 DeckManager에서 덱 복원
        RestoreDeckFromManager();

        // 모든 슬롯 정보 갱신
        RefreshAllSlots();
    }

    /// <summary>
    /// DeckManager에서 저장된 덱 복원
    /// </summary>
    private void RestoreDeckFromManager()
    {
        if (DeckManager.Instance == null) return;

        for (int i = 0; i < deckSlots.Count; i++)
        {
            int characterId = DeckManager.Instance.GetCharacterAtIndex(i);
            if (characterId > 0)
            {
                deckSlots[i].SetCharacter(characterId);
            }
            else
            {
                deckSlots[i].ClearSlot();
            }
        }

        Debug.Log($"[TeamSetupPanel] 덱 복원 완료. 현재 덱: {GetSetDeckCount()}/4");
    }

    /// <summary>
    /// 모든 슬롯 정보 갱신 (승급 등 외부 변경 반영)
    /// </summary>
    public void RefreshAllSlots()
    {
        // DeckSlot 정보 갱신
        foreach (var slot in deckSlots)
        {
            slot.RefreshCharacterInfo();
        }

        // DeckCharacterSlot 정보 갱신
        foreach (var slot in characterSlots)
        {
            slot.RefreshCharacterInfo();
        }

        Debug.Log("[TeamSetupPanel] 모든 슬롯 정보 갱신 완료");
    }

    /// <summary>
    /// 덱 슬롯 클릭 시 호출 (Inspector Button OnClick에서 연결)
    /// </summary>
    public void OnDeckSlotClicked(int slotIndex)
    {
        // 기존 선택된 슬롯의 프레임 비활성화
        if (selectedDeckSlotIndex >= 0 && selectedDeckSlotIndex < deckSlots.Count)
        {
            deckSlots[selectedDeckSlotIndex].SetSelected(false);
        }

        // 새 슬롯 선택 및 프레임 활성화
        selectedDeckSlotIndex = slotIndex;
        deckSlots[slotIndex].SetSelected(true);

        Debug.Log($"[TeamSetupPanel] 덱 슬롯 {slotIndex} 선택됨");

        // 캐릭터가 설정되어 있으면 해제 패널 표시, 아니면 캐릭터 탭 열기
        if (deckSlots[slotIndex].IsSet)
        {
            ShowDeckUnsetPanel();
        }
        else
        {
            OnTabCharacterButtonClicked();
        }
    }

    /// <summary>
    /// 캐릭터 선택 시 호출 (DeckCharacterSlot에서 호출)
    /// </summary>
    public void OnCharacterSelected(int characterId)
    {
        if (selectedDeckSlotIndex < 0 || selectedDeckSlotIndex >= deckSlots.Count)
        {
            Debug.LogWarning("[TeamSetupPanel] 선택된 덱 슬롯이 없습니다.");
            return;
        }

        // 이미 덱에 있는 캐릭터인지 확인
        int existingSlotIndex = GetSlotIndexByCharacterId(characterId);

        if (existingSlotIndex >= 0 && existingSlotIndex != selectedDeckSlotIndex)
        {
            // 기존 슬롯과 선택된 슬롯의 캐릭터 교환
            int existingCharacterId = deckSlots[selectedDeckSlotIndex].CharacterId;

            if (existingCharacterId > 0)
            {
                // 선택된 슬롯에 캐릭터가 있으면 교환
                deckSlots[existingSlotIndex].SetCharacter(existingCharacterId);
                Debug.Log($"[TeamSetupPanel] 슬롯 {existingSlotIndex}에 캐릭터 ID {existingCharacterId} 이동");
            }
            else
            {
                // 선택된 슬롯이 비어있으면 기존 슬롯 초기화
                deckSlots[existingSlotIndex].ClearSlot();
                Debug.Log($"[TeamSetupPanel] 슬롯 {existingSlotIndex} 초기화");
            }
        }

        // 선택된 덱 슬롯에 캐릭터 설정
        deckSlots[selectedDeckSlotIndex].SetCharacter(characterId);

        // 선택 프레임 비활성화 및 선택 초기화
        deckSlots[selectedDeckSlotIndex].SetSelected(false);
        selectedDeckSlotIndex = -1;

        Debug.Log($"[TeamSetupPanel] 캐릭터 ID {characterId} 설정 완료. 현재 덱: {GetSetDeckCount()}/4");
    }

    /// <summary>
    /// 캐릭터 ID로 슬롯 인덱스 찾기 (없으면 -1)
    /// </summary>
    private int GetSlotIndexByCharacterId(int characterId)
    {
        for (int i = 0; i < deckSlots.Count; i++)
        {
            if (deckSlots[i].IsSet && deckSlots[i].CharacterId == characterId)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 캐릭터가 이미 덱에 있는지 확인
    /// </summary>
    public bool IsCharacterInDeck(int characterId)
    {
        foreach (var slot in deckSlots)
        {
            if (slot.IsSet && slot.CharacterId == characterId)
                return true;
        }
        return false;
    }


    /// <summary>
    /// 캐릭터 탭 버튼 클릭
    /// </summary>
    public void OnTabCharacterButtonClicked()
    {
        partyTab.SetActive(false);
        characterTab.SetActive(true);
    }

    #region Deck Unset Panel

    /// <summary>
    /// 덱 해제 패널 표시
    /// </summary>
    private void ShowDeckUnsetPanel()
    {
        if (deckUnsetPanel != null)
        {
            deckUnsetPanel.SetActive(true);
            Debug.Log("[TeamSetupPanel] 덱 해제 패널 표시");
        }
    }

    /// <summary>
    /// 덱 해제 패널 숨기기
    /// </summary>
    private void HideDeckUnsetPanel()
    {
        if (deckUnsetPanel != null)
        {
            deckUnsetPanel.SetActive(false);
            Debug.Log("[TeamSetupPanel] 덱 해제 패널 숨김");
        }
    }

    /// <summary>
    /// 해제 버튼 클릭 (Inspector Button OnClick에서 연결)
    /// </summary>
    public void OnUnsetButtonClicked()
    {
        if (selectedDeckSlotIndex >= 0 && selectedDeckSlotIndex < deckSlots.Count)
        {
            int characterId = deckSlots[selectedDeckSlotIndex].CharacterId;

            // UI 슬롯 초기화
            deckSlots[selectedDeckSlotIndex].ClearSlot();

            // DeckManager에서도 해제
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.SetCharacterAtIndex(selectedDeckSlotIndex, 0);
            }

            Debug.Log($"[TeamSetupPanel] 슬롯 {selectedDeckSlotIndex}에서 캐릭터 ID {characterId} 해제 완료");
        }

        // 선택 프레임 비활성화 및 선택 초기화
        if (selectedDeckSlotIndex >= 0 && selectedDeckSlotIndex < deckSlots.Count)
        {
            deckSlots[selectedDeckSlotIndex].SetSelected(false);
        }
        selectedDeckSlotIndex = -1;

        // 패널 숨기기
        HideDeckUnsetPanel();
    }

    /// <summary>
    /// 취소 버튼 클릭 (Inspector Button OnClick에서 연결)
    /// </summary>
    public void OnCancelButtonClicked()
    {
        // 선택 프레임 비활성화 및 선택 초기화
        if (selectedDeckSlotIndex >= 0 && selectedDeckSlotIndex < deckSlots.Count)
        {
            deckSlots[selectedDeckSlotIndex].SetSelected(false);
        }
        selectedDeckSlotIndex = -1;

        // 패널 숨기기
        HideDeckUnsetPanel();

        Debug.Log("[TeamSetupPanel] 덱 해제 취소");
    }

    #endregion

    private void OnDisable()
    {
        if (IsDeckValid())
        {
            // 3개 이상이면 DeckManager에 저장
            SaveDeckToManager();
            Debug.Log("[TeamSetupPanel] 덱이 유효하므로 저장합니다.");
        }
        else
        {
            // 3개 미만이면 DeckManager 초기화
            if (DeckManager.Instance != null)
                DeckManager.Instance.ClearDeck();
            Debug.Log("[TeamSetupPanel] 덱이 3개 미만이므로 초기화합니다.");
        }

        // UI 슬롯은 항상 초기화 (다음에 OnEnable에서 복원됨)
        ClearAllDeckSlotsUI();
    }

    /// <summary>
    /// DeckManager에 현재 덱 저장
    /// </summary>
    private void SaveDeckToManager()
    {
        if (DeckManager.Instance == null) return;

        DeckManager.Instance.ClearDeck();
        for (int i = 0; i < deckSlots.Count; i++)
        {
            if (deckSlots[i].IsSet)
            {
                DeckManager.Instance.SetCharacterAtIndex(i, deckSlots[i].CharacterId);
            }
        }
    }

    /// <summary>
    /// UI 슬롯만 초기화 (DeckManager는 건드리지 않음)
    /// </summary>
    private void ClearAllDeckSlotsUI()
    {
        foreach (var slot in deckSlots)
        {
            slot.ClearSlot();
        }
    }

    /// <summary>
    /// 모든 덱 슬롯 초기화
    /// </summary>
    public void ClearAllDeckSlots()
    {
        foreach (var slot in deckSlots)
        {
            slot.ClearSlot();
        }

        if (DeckManager.Instance != null)
            DeckManager.Instance.ClearDeck();
    }

    /// <summary>
    /// 설정된 덱 슬롯 개수
    /// </summary>
    public int GetSetDeckCount()
    {
        int count = 0;
        foreach (var slot in deckSlots)
        {
            if (slot.IsSet) count++;
        }
        return count;
    }

    /// <summary>
    /// 덱이 유효한지 (3개 이상)
    /// </summary>
    public bool IsDeckValid()
    {
        return GetSetDeckCount() >= 3;
    }
}
