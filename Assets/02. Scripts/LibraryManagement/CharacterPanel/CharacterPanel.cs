using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;

/// <summary>
/// 캐릭터 필터 타입
/// </summary>
public enum CharacterFilterType
{
    All = 0,            // 전체
    Romance = 1,        // 로맨스
    Comedy = 2,         // 코미디
    Adventure = 3,      // 모험
    Mystery = 4,        // 추리
    Horror = 5,         // 공포
    Acquired = 6,       // 획득
    NotAcquired = 7,    // 미획득
    LevelAsc = 8,       // 낮은레벨순
    LevelDesc = 9       // 높은레벨순
}

public class CharacterPanel : MonoBehaviour
{
    [SerializeField] private const int MAX_CHARACTERS = 20;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject characterSlotPrefab;
    [SerializeField] private GameObject characterInfoPanel;
    [SerializeField] private CharacterInfoPanel infoPanel;

    [Header("Filter")]
    [SerializeField] private GameObject filterObject;      // Filter UI 오브젝트 (활성화/비활성화용)
    [SerializeField] private TMP_Dropdown filterDropdown;

    private List<LibraryCharacterSlot> characterSlots = new List<LibraryCharacterSlot>();
    private CharacterFilterType currentFilter = CharacterFilterType.All;

    private async UniTaskVoid Start()
    {
        Debug.Log("[CharacterPanel] Start() called - Waiting for CSVLoader...");

        // 드롭다운 초기화
        InitializeFilterDropdown();

        // CSV 로딩 완료될 때까지 대기
        await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);

        Debug.Log("[CharacterPanel] CSVLoader ready - Getting CharacterTable...");

        CsvTable<CharacterData> characterTable = CSVLoader.Instance.GetTable<CharacterData>();

        // null 체크도 추가
        if (characterTable == null)
        {
            Debug.LogError("[CharacterPanel] CharacterTable is null after loading! CSV data may have failed to load.");
            return;
        }

        Debug.Log($"[CharacterPanel] CharacterTable loaded with {characterTable.Count} entries");

        // 보유 캐릭터가 앞에 오도록 정렬
        var sortedCharacters = characterTable.GetAll()
            .OrderByDescending(c => IsCharacterOwned(c.Character_ID)) // 보유 캐릭터 먼저
            .ThenBy(c => c.Character_ID) // 그 다음 ID순
            .ToList();

        int slotIndex = 0;
        foreach (var characterData in sortedCharacters)
        {
            if (slotIndex >= MAX_CHARACTERS) break;

            var slot = Instantiate(characterSlotPrefab, contentParent);
            var characterSlot = slot.GetComponent<LibraryCharacterSlot>();

            // InfoPanel 연결
            characterSlot.SetInfoPanel(infoPanel);

            // 테이블 데이터로 초기화
            characterSlot.InitSlot(characterData);

            characterSlots.Add(characterSlot);
            slotIndex++;
        }

        Debug.Log($"[CharacterPanel] Created {slotIndex} character slots");
        characterInfoPanel.SetActive(false);
    }

    /// <summary>
    /// 캐릭터 보유 여부 확인 (정렬용)
    /// </summary>
    private bool IsCharacterOwned(int characterId)
    {
        if (CharacterOwnershipManager.Instance == null)
            return true; // Manager 없으면 전부 보유로 처리

        return CharacterOwnershipManager.Instance.IsOwned(characterId);
    }

    /// <summary>
    /// 필터 드롭다운 초기화
    /// </summary>
    private void InitializeFilterDropdown()
    {
        if (filterDropdown == null)
        {
            Debug.LogWarning("[CharacterPanel] FilterDropdown is not assigned!");
            return;
        }

        filterDropdown.ClearOptions();

        // 기획서 순서대로 옵션 추가
        var options = new List<string>
        {
            "전체",           // 0: All
            "로맨스",         // 1: Romance
            "코미디",         // 2: Comedy
            "모험",           // 3: Adventure
            "추리",           // 4: Mystery
            "공포",           // 5: Horror
            "획득",           // 6: Acquired
            "미획득",         // 7: NotAcquired
            "낮은레벨순(보유)",     // 8: LevelAsc
            "높은레벨순(보유)"      // 9: LevelDesc
        };

        filterDropdown.AddOptions(options);
        filterDropdown.value = 0;
        filterDropdown.onValueChanged.AddListener(OnFilterChanged);

        Debug.Log("[CharacterPanel] Filter dropdown initialized");
    }

    /// <summary>
    /// 필터 변경 이벤트 핸들러
    /// </summary>
    private void OnFilterChanged(int index)
    {
        currentFilter = (CharacterFilterType)index;
        ApplyFilter();
        Debug.Log($"[CharacterPanel] Filter changed to: {currentFilter}");
    }

    /// <summary>
    /// 현재 필터 적용
    /// </summary>
    private void ApplyFilter()
    {
        // 레벨순 정렬인 경우
        if (currentFilter == CharacterFilterType.LevelAsc || currentFilter == CharacterFilterType.LevelDesc)
        {
            ApplySorting();
            return;
        }

        // 일반 필터링
        foreach (var slot in characterSlots)
        {
            if (slot == null) continue;

            bool shouldShow = ShouldShowSlot(slot);
            slot.gameObject.SetActive(shouldShow);
        }
    }

    /// <summary>
    /// 레벨순 정렬 적용
    /// </summary>
    private void ApplySorting()
    {
        // 미획득 캐릭터 숨기고, 보유 캐릭터만 표시
        foreach (var slot in characterSlots)
        {
            if (slot != null)
                slot.gameObject.SetActive(slot.IsOwned);
        }

        // 보유 캐릭터만 정렬
        var ownedSlots = characterSlots.Where(s => s != null && s.IsOwned);

        var sortedSlots = currentFilter == CharacterFilterType.LevelAsc
            ? ownedSlots.OrderBy(s => s.EnhancementLevel).ThenBy(s => s.CharacterID).ToList()
            : ownedSlots.OrderByDescending(s => s.EnhancementLevel).ThenBy(s => s.CharacterID).ToList();

        for (int i = 0; i < sortedSlots.Count; i++)
        {
            sortedSlots[i].transform.SetSiblingIndex(i);
        }

        Debug.Log($"[CharacterPanel] Sorted by {currentFilter}");
    }

    /// <summary>
    /// 필터 조건에 따라 슬롯 표시 여부 결정
    /// </summary>
    private bool ShouldShowSlot(LibraryCharacterSlot slot)
    {
        switch (currentFilter)
        {
            case CharacterFilterType.All:
                return true;

            case CharacterFilterType.Romance:
                return slot.CharacterGenre == Genre.Romance;

            case CharacterFilterType.Comedy:
                return slot.CharacterGenre == Genre.Comedy;

            case CharacterFilterType.Adventure:
                return slot.CharacterGenre == Genre.Adventure;

            case CharacterFilterType.Mystery:
                return slot.CharacterGenre == Genre.Mystery;

            case CharacterFilterType.Horror:
                return slot.CharacterGenre == Genre.Horror;

            case CharacterFilterType.Acquired:
                return slot.IsOwned;

            case CharacterFilterType.NotAcquired:
                return !slot.IsOwned;

            default:
                return true;
        }
    }

    /// <summary>
    /// 패널 활성화 시 필터 UI 표시
    /// </summary>
    private void OnEnable()
    {
        if (filterObject != null)
        {
            filterObject.SetActive(true);
        }
    }

    /// <summary>
    /// 패널 비활성화 시 필터 초기화 및 필터 UI 숨김
    /// </summary>
    private void OnDisable()
    {
        ResetFilter();

        if (filterObject != null)
        {
            filterObject.SetActive(false);
        }
    }

    /// <summary>
    /// 필터를 전체로 초기화하고 모든 슬롯 표시
    /// </summary>
    public void ResetFilter()
    {
        currentFilter = CharacterFilterType.All;

        if (filterDropdown != null)
        {
            filterDropdown.SetValueWithoutNotify(0); // 이벤트 발생 없이 값만 변경
        }

        // 모든 슬롯 활성화 및 원래 순서로 복원
        for (int i = 0; i < characterSlots.Count; i++)
        {
            if (characterSlots[i] != null)
            {
                characterSlots[i].gameObject.SetActive(true);
                characterSlots[i].transform.SetSiblingIndex(i);
            }
        }

        Debug.Log("[CharacterPanel] Filter reset to All");
    }

    private void OnDestroy()
    {
        if (filterDropdown != null)
        {
            filterDropdown.onValueChanged.RemoveListener(OnFilterChanged);
        }
    }
}
