using System.Collections.Generic;
using System.Linq;
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

    [Header("Tab Buttons")]
    [SerializeField] private Button partySynergyButton;
    [SerializeField] private Button characterSelectionButton;

    [Header("Active Synergy Display")]
    [SerializeField] private GameObject activeSynergyPanel;
    [SerializeField] private TextMeshProUGUI activeSynergyTitleText;  // "파티 시너지" 고정
    [SerializeField] private TextMeshProUGUI activeSynergyText;        // "제목\n\n설명" 형식

    [Header("Prefabs")]
    [SerializeField] private GameObject characterSlotPrefab;
    [SerializeField] private GameObject deckslotPrefab;

    [Header("Containers")]
    [SerializeField] private Transform characterSlotContainer;

    [Header("Deck Slots")]
    [SerializeField] private List<DeckSlot> deckSlots = new List<DeckSlot>();

    [Header("Preset Marks")]
    [SerializeField] private List<PresetMark> presetMarks = new List<PresetMark>();

    [Header("Deck Unset Panel")]
    [SerializeField] private GameObject deckUnsetPanel;
    [SerializeField] private GameObject raycastPanel;

    private List<DeckCharacterSlot> characterSlots = new List<DeckCharacterSlot>();
    private List<PartySynergyData> allSynergies = new List<PartySynergyData>();
    private int selectedDeckSlotIndex = -1; // 현재 선택된 덱 슬롯
    private bool isInitialized = false;

    private void Start()
    {
        // 탭 버튼 리스너 등록
        if (partySynergyButton != null)
            partySynergyButton.onClick.AddListener(OnTabPartySynergyButtonClicked);
        if (characterSelectionButton != null)
            characterSelectionButton.onClick.AddListener(OnTabCharacterButtonClicked);

        // 시너지 데이터 로드
        LoadSynergyData();

        // 덱 슬롯 인덱스 설정
        for (int i = 0; i < deckSlots.Count; i++)
        {
            deckSlots[i].SetSlotIndex(i);
        }

        // 프리셋 마크 초기화
        InitializePresetMarks();

        // DeckManager 프리셋 변경 이벤트 구독
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnPresetChanged += OnPresetChangedFromManager;
        }

        // 모든 캐릭터 데이터 가져오기
        var allCharacters = CSVLoader.Instance.GetTable<CharacterData>().GetAll();

        foreach (var characterData in allCharacters)
        {
            // 보유 캐릭터만 슬롯 생성
            if (CharacterOwnershipManager.Instance != null &&
                !CharacterOwnershipManager.Instance.IsOwned(characterData.Character_ID))
            {
                continue; // 미보유 캐릭터는 건너뛰기
            }

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
        RefreshActiveSynergies();
    }

    private void OnEnable()
    {
        // Start 이후에만 복원 (첫 실행 시 Start에서 처리)
        if (!isInitialized) return;

        // 프리셋 마크 상태 갱신
        RefreshPresetMarks();

        // 패널 활성화 시 DeckManager에서 덱 복원
        RestoreDeckFromManager();

        // 모든 슬롯 정보 갱신
        RefreshAllSlots();

        // 시너지 상태 갱신
        RefreshActiveSynergies();
    }

    /// <summary>
    /// DeckManager에서 저장된 덱 복원
    /// </summary>
    private void RestoreDeckFromManager()
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogWarning("[TeamSetupPanel] DeckManager.Instance가 null입니다!");
            return;
        }

        // DeckManager의 현재 덱 상태 확인
        var deckData = DeckManager.Instance.GetDeck();
        Debug.Log($"[TeamSetupPanel] DeckManager 덱 데이터: [{string.Join(", ", deckData)}]");

        for (int i = 0; i < deckSlots.Count; i++)
        {
            int characterId = DeckManager.Instance.GetCharacterAtIndex(i);
            Debug.Log($"[TeamSetupPanel] 슬롯 {i} 복원 시도: characterId={characterId}");

            if (characterId > 0)
            {
                deckSlots[i].SetCharacter(characterId);
                Debug.Log($"[TeamSetupPanel] 슬롯 {i}에 캐릭터 {characterId} 설정 완료");
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
        Debug.Log($"[TeamSetupPanel] OnDeckSlotClicked 호출됨 - slotIndex: {slotIndex}, 이전 selectedDeckSlotIndex: {selectedDeckSlotIndex}");

        // 기존 선택된 슬롯의 프레임 비활성화
        if (selectedDeckSlotIndex >= 0 && selectedDeckSlotIndex < deckSlots.Count)
        {
            deckSlots[selectedDeckSlotIndex].SetSelected(false);
        }

        // 새 슬롯 선택 및 프레임 활성화
        selectedDeckSlotIndex = slotIndex;
        deckSlots[slotIndex].SetSelected(true);

        Debug.Log($"[TeamSetupPanel] 덱 슬롯 {slotIndex} 선택됨, selectedDeckSlotIndex: {selectedDeckSlotIndex}");

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
        Debug.Log($"[TeamSetupPanel] OnCharacterSelected 호출됨 - characterId: {characterId}, selectedDeckSlotIndex: {selectedDeckSlotIndex}");

        if (selectedDeckSlotIndex < 0 || selectedDeckSlotIndex >= deckSlots.Count)
        {
            Debug.LogWarning($"[TeamSetupPanel] 선택된 덱 슬롯이 없습니다. selectedDeckSlotIndex: {selectedDeckSlotIndex}");
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
        int targetSlot = selectedDeckSlotIndex;
        deckSlots[targetSlot].SetCharacter(characterId);

        Debug.Log($"[TeamSetupPanel] 슬롯 {targetSlot}에 캐릭터 설정 후 - IsSet: {deckSlots[targetSlot].IsSet}, CharacterId: {deckSlots[targetSlot].CharacterId}");

        // 선택 프레임 비활성화 및 선택 초기화
        deckSlots[targetSlot].SetSelected(false);
        selectedDeckSlotIndex = -1;

        Debug.Log($"[TeamSetupPanel] 캐릭터 ID {characterId} 설정 완료. 현재 덱: {GetSetDeckCount()}/4");

        // 덱 변경 시 시너지 즉시 갱신
        RefreshActiveSynergies();
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
        if (partyTab != null)
            partyTab.SetActive(false);
        if (characterTab != null)
            characterTab.SetActive(true);
        if (activeSynergyPanel != null)
            activeSynergyPanel.SetActive(false);

        // 버튼 상태 업데이트
        if (partySynergyButton != null)
            partySynergyButton.interactable = true;
        if (characterSelectionButton != null)
            characterSelectionButton.interactable = false;

        Debug.Log("[TeamSetupPanel] 캐릭터 선택 탭 활성화");
    }

    /// <summary>
    /// 파티 시너지 탭 버튼 클릭
    /// </summary>
    public void OnTabPartySynergyButtonClicked()
    {
        if (partyTab != null)
            partyTab.SetActive(true);
        if (characterTab != null)
            characterTab.SetActive(false);

        // 버튼 상태 업데이트
        if (partySynergyButton != null)
            partySynergyButton.interactable = false;
        if (characterSelectionButton != null)
            characterSelectionButton.interactable = true;

        // 활성 시너지 새로고침
        RefreshActiveSynergies();

        Debug.Log("[TeamSetupPanel] 파티 시너지 탭 활성화");
    }

    #region Deck Unset Panel

    /// <summary>
    /// 덱 해제 패널 표시
    /// </summary>
    private void ShowDeckUnsetPanel()
    {
        if (deckUnsetPanel != null)
        {
            // Unity SerializeField는 ?.가 제대로 동작하지 않으므로 명시적 null 체크
            if (raycastPanel != null)
                raycastPanel.SetActive(true);
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
            // Unity SerializeField는 ?.가 제대로 동작하지 않으므로 명시적 null 체크
            if (raycastPanel != null)
                raycastPanel.SetActive(false);
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

        // 덱 변경 시 시너지 즉시 갱신
        RefreshActiveSynergies();
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

    #region Preset Management

    /// <summary>
    /// 프리셋 마크 초기화
    /// </summary>
    private void InitializePresetMarks()
    {
        for (int i = 0; i < presetMarks.Count; i++)
        {
            if (presetMarks[i] != null)
            {
                presetMarks[i].Initialize(i, this);
            }
        }

        // 현재 프리셋에 맞게 UI 업데이트
        RefreshPresetMarks();
        Debug.Log($"[TeamSetupPanel] 프리셋 마크 초기화 완료. 현재 프리셋: {GetCurrentPresetIndex() + 1}");
    }

    /// <summary>
    /// 프리셋 마크 클릭 시 호출 (PresetMark에서 호출)
    /// </summary>
    public void OnPresetMarkClicked(int presetIndex)
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogWarning("[TeamSetupPanel] DeckManager.Instance가 null입니다.");
            return;
        }

        int currentPreset = DeckManager.Instance.GetCurrentPresetIndex();
        if (presetIndex == currentPreset)
        {
            Debug.Log($"[TeamSetupPanel] 이미 프리셋 {presetIndex + 1}이 선택되어 있습니다.");
            return;
        }

        // 현재 덱 상태를 먼저 저장
        SaveCurrentDeckToPreset();

        // 프리셋 전환
        DeckManager.Instance.SwitchPreset(presetIndex);

        Debug.Log($"[TeamSetupPanel] 프리셋 {currentPreset + 1} → {presetIndex + 1} 전환");
    }

    /// <summary>
    /// DeckManager에서 프리셋 변경 이벤트 수신
    /// </summary>
    private void OnPresetChangedFromManager(int newPresetIndex)
    {
        // UI 업데이트
        RefreshPresetMarks();
        RestoreDeckFromManager();
        RefreshAllSlots();
        RefreshActiveSynergies();

        Debug.Log($"[TeamSetupPanel] 프리셋 변경 이벤트 수신: {newPresetIndex + 1}");
    }

    /// <summary>
    /// 프리셋 마크 UI 갱신
    /// </summary>
    private void RefreshPresetMarks()
    {
        int currentPreset = GetCurrentPresetIndex();

        for (int i = 0; i < presetMarks.Count; i++)
        {
            if (presetMarks[i] != null)
            {
                presetMarks[i].SetSelected(i == currentPreset);
            }
        }
    }

    /// <summary>
    /// 현재 선택된 프리셋 인덱스 반환
    /// </summary>
    private int GetCurrentPresetIndex()
    {
        if (DeckManager.Instance != null)
        {
            return DeckManager.Instance.GetCurrentPresetIndex();
        }
        return 0;
    }

    /// <summary>
    /// 현재 UI 덱 상태를 현재 프리셋에 저장
    /// </summary>
    private void SaveCurrentDeckToPreset()
    {
        if (DeckManager.Instance == null) return;

        for (int i = 0; i < deckSlots.Count; i++)
        {
            int characterId = deckSlots[i].IsSet ? deckSlots[i].CharacterId : -1;
            DeckManager.Instance.SetCharacterAtIndex(i, characterId);
        }

        Debug.Log($"[TeamSetupPanel] 현재 덱을 프리셋 {GetCurrentPresetIndex() + 1}에 저장");
    }

    #endregion

    private void OnDisable()
    {
        // JML: 덱 상태 디버그 로그
        Debug.Log($"[TeamSetupPanel] OnDisable - 현재 덱 슬롯 수: {GetSetDeckCount()}, deckSlots.Count: {deckSlots.Count}");
        for (int i = 0; i < deckSlots.Count; i++)
        {
            Debug.Log($"[TeamSetupPanel] Slot[{i}] - IsSet: {deckSlots[i].IsSet}, CharacterId: {deckSlots[i].CharacterId}");
        }

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
            Debug.Log($"[TeamSetupPanel] 덱이 3개 미만({GetSetDeckCount()}개)이므로 초기화합니다.");
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

    #region Active Synergy Display

    /// <summary>
    /// 시너지 데이터 로드
    /// </summary>
    private void LoadSynergyData()
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            Debug.LogWarning("[TeamSetupPanel] CSVLoader가 초기화되지 않았습니다.");
            return;
        }

        var synergyTable = CSVLoader.Instance.GetTable<PartySynergyData>();
        if (synergyTable == null)
        {
            Debug.LogWarning("[TeamSetupPanel] PartySynergyTable이 null입니다.");
            return;
        }

        allSynergies = synergyTable.GetAll()
            .Where(s => s.Party_Name_ID != 0)
            .OrderBy(s => s.Party_ID)
            .ToList();

        Debug.Log($"[TeamSetupPanel] 시너지 데이터 로드 완료: {allSynergies.Count}개");
    }

    /// <summary>
    /// 현재 덱 기준 활성 시너지 새로고침
    /// </summary>
    private void RefreshActiveSynergies()
    {
        var activeSynergies = GetActiveSynergies();

        if (activeSynergies.Count == 0)
        {
            DisplayNoActiveSynergy();
        }
        else
        {
            // 첫 번째 활성 시너지 표시
            DisplayActiveSynergy(activeSynergies[0]);
        }
    }

    /// <summary>
    /// 현재 덱 기준 활성화된 시너지 목록 반환
    /// </summary>
    private List<PartySynergyData> GetActiveSynergies()
    {
        var activeSynergies = new List<PartySynergyData>();

        if (DeckManager.Instance == null)
        {
            Debug.LogWarning("[TeamSetupPanel] DeckManager.Instance is null");
            return activeSynergies;
        }

        // 현재 UI 덱 슬롯에서 캐릭터 ID 가져오기
        var deckCharacterIds = new List<int>();
        foreach (var slot in deckSlots)
        {
            if (slot.IsSet && slot.CharacterId > 0)
            {
                deckCharacterIds.Add(slot.CharacterId);
            }
        }

        if (deckCharacterIds.Count < 4)
        {
            Debug.Log($"[TeamSetupPanel] 덱에 캐릭터가 {deckCharacterIds.Count}명뿐입니다. 시너지 활성화에는 4명 필요.");
            return activeSynergies;
        }

        var deckSet = new HashSet<int>(deckCharacterIds);

        foreach (var synergy in allSynergies)
        {
            bool isActive = deckSet.Contains(synergy.Req_Char_1_ID)
                         && deckSet.Contains(synergy.Req_Char_2_ID)
                         && deckSet.Contains(synergy.Req_Char_3_ID)
                         && deckSet.Contains(synergy.Req_Char_4_ID);

            if (isActive)
            {
                activeSynergies.Add(synergy);
                Debug.Log($"[TeamSetupPanel] 시너지 활성화됨! Party_ID: {synergy.Party_ID}");
            }
        }

        return activeSynergies;
    }

    /// <summary>
    /// 활성 시너지 없음 표시 (텍스트만 갱신, 패널 표시는 탭에서 관리)
    /// </summary>
    private void DisplayNoActiveSynergy()
    {
        if (activeSynergyTitleText != null)
            activeSynergyTitleText.text = "파티 시너지";
        if (activeSynergyText != null)
            activeSynergyText.text = "활성화된 시너지 없음\n\n4명의 캐릭터를 같은 파티로 편성하세요";

        Debug.Log("[TeamSetupPanel] 활성화된 시너지 없음");
    }

    /// <summary>
    /// 활성 시너지 표시 (텍스트만 갱신, 패널 표시는 탭에서 관리)
    /// </summary>
    private void DisplayActiveSynergy(PartySynergyData synergy)
    {
        // 제목 (고정)
        if (activeSynergyTitleText != null)
            activeSynergyTitleText.text = "파티 시너지";

        // 시너지 이름 + 효과 설명 (제목\n\n설명 형식)
        if (activeSynergyText != null)
        {
            // 시너지 이름
            var nameString = CSVLoader.Instance.GetData<StringTable>(synergy.Party_Name_ID);
            string synergyName = nameString?.Text ?? $"시너지_{synergy.Party_ID}";

            // 효과 설명 (Lv1 기준)
            var (effectId, effectValue) = GetEffectByLevel(synergy, 1);
            string effectDescription = GetEffectDescription(effectId, effectValue);

            activeSynergyText.text = $"{synergyName}\n\n{effectDescription}";
        }

        Debug.Log($"[TeamSetupPanel] 시너지 표시: {activeSynergyText?.text}");
    }

    /// <summary>
    /// 레벨에 따른 효과 ID와 값 반환 (확장성 고려)
    /// </summary>
    private (int effectId, float effectValue) GetEffectByLevel(PartySynergyData data, int level = 1)
    {
        return level switch
        {
            1 => (data.Effect_1_ID, data.Effect_1_Value),
            2 => (data.Effect_2_ID, data.Effect_2_Value),
            3 => (data.Effect_3_ID, data.Effect_3_Value),
            4 => (data.Effect_4_ID, data.Effect_4_Value),
            5 => (data.Effect_5_ID, data.Effect_5_Value),
            _ => (data.Effect_1_ID, data.Effect_1_Value)
        };
    }

    /// <summary>
    /// 효과 설명 텍스트 생성
    /// </summary>
    private string GetEffectDescription(int effectId, float effectValue)
    {
        var effectData = CSVLoader.Instance.GetData<PartySynergyEffectData>(effectId);
        if (effectData == null)
        {
            Debug.LogWarning($"[TeamSetupPanel] PartySynergyEffectData not found for ID: {effectId}");
            return $"효과 {effectId}: {effectValue}";
        }

        var descriptionString = CSVLoader.Instance.GetData<StringTable>(effectData.Party_Effect_Description_ID);
        if (descriptionString == null)
        {
            Debug.LogWarning($"[TeamSetupPanel] StringTable not found for ID: {effectData.Party_Effect_Description_ID}");
            return $"효과: {effectValue}";
        }

        // 효과값 포맷팅 (정수면 소수점 제거)
        string valueText = effectValue % 1 == 0 ? ((int)effectValue).ToString() : effectValue.ToString("F1");

        // 플레이스홀더 {0}이 있으면 대체, 없으면 개행 후 +N% 형식으로 표시
        string description = descriptionString.Text;
        if (description.Contains("{0}"))
        {
            return string.Format(description, valueText + "%");
        }
        else
        {
            return $"{description}\n+{valueText}%";
        }
    }

    #endregion

    private void OnDestroy()
    {
        // 버튼 리스너 해제
        if (partySynergyButton != null)
            partySynergyButton.onClick.RemoveListener(OnTabPartySynergyButtonClicked);
        if (characterSelectionButton != null)
            characterSelectionButton.onClick.RemoveListener(OnTabCharacterButtonClicked);

        // DeckManager 이벤트 구독 해제
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnPresetChanged -= OnPresetChangedFromManager;
        }
    }
}
