using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using TMPro;

/// <summary>
/// 인벤토리 UI의 메인 컨트롤러
/// IngredientManager로부터 재료 정보를 받아와 UI에 표시
/// </summary>
public class InventoryController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform inventoryGridParent;     // 아이템 슬롯들의 부모 Transform
    [SerializeField] private GameObject itemSlotPrefab;         // 아이템 슬롯 프리팹
    [SerializeField] private ScrollRect scrollRect;             // 스크롤 뷰 (상하 스와이프용)
    [SerializeField] private Button backButton;                 // 뒤로가기 버튼
    [SerializeField] private TMP_Dropdown sortDropdown;         // 정렬 드롭다운

    [Header("Item Detail Popup")]
    [SerializeField] private GameObject itemDetailPopup;                    // 아이템 정보 팝업 패널
    [SerializeField] private Image popupItemIcon;                           // 팝업 아이템 아이콘
    [SerializeField] private TextMeshProUGUI popupItemNameText;             // 팝업 아이템 이름
    [SerializeField] private TextMeshProUGUI popupItemDescriptionText;      // 팝업 아이템 설명
    [SerializeField] private TextMeshProUGUI popupItemCurrentCountText;     // 팝업 현재 보유량
    [SerializeField] private Button popupCloseButton;                       // 팝업 닫기 버튼 (배경 클릭)


    /// <summary>
    /// 정렬 방식 열거형
    /// </summary>
    public enum SortType
    {
        Newest = 0,     // 최신순
        Name = 1        // 이름순(가나다)
    }

    // 아이템 슬롯 풀
    private List<InventoryItemSlot> itemSlots = new List<InventoryItemSlot>();

    // 현재 표시 중인 아이템 정보 리스트
    private List<InventoryItemInfo> displayedItems = new List<InventoryItemInfo>();

    // 현재 정렬 방식
    private SortType currentSortType = SortType.Name;

    private void Start()
    {
        InitializeUI();

        // IngredientManager가 없으면 생성 (테스트용)
        if (IngredientManager.Instance == null)
        {
            GameObject managerObj = new GameObject("IngredientManager");
            managerObj.AddComponent<IngredientManager>();
            Debug.LogWarning("[InventoryController] IngredientManager가 없어서 생성했습니다. (테스트용)");
        }

        LoadInventoryData();
    }

    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // 팝업 초기 상태: 비활성화
        if (itemDetailPopup != null)
            itemDetailPopup.SetActive(false);

        // 뒤로가기 버튼
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

        // 팝업 닫기 버튼 (배경 클릭)
        if (popupCloseButton != null)
            popupCloseButton.onClick.AddListener(CloseItemDetailPopup);

        // 정렬 드롭다운 초기화
        if (sortDropdown != null)
        {
            sortDropdown.ClearOptions();
            sortDropdown.AddOptions(new List<string> { "최신순", "이름순(가나다)" });
            sortDropdown.value = (int)currentSortType;
            sortDropdown.onValueChanged.AddListener(OnSortDropdownValueChanged);
            Debug.Log("[InventoryController] 정렬 드롭다운 초기화 완료");
        }
    }

    /// <summary>
    /// IngredientManager로부터 인벤토리 데이터 로드 및 UI 갱신
    /// </summary>
    public void LoadInventoryData()
    {
        if (IngredientManager.Instance == null)
        {
            Debug.LogError("[InventoryController] IngredientManager 인스턴스가 없습니다!");
            return;
        }

        if (CSVLoader.Instance == null)
        {
            Debug.LogError("[InventoryController] CSVLoader 인스턴스가 없습니다!");
            return;
        }

        // 기존 아이템 정보 초기화
        displayedItems.Clear();

        // IngredientManager에서 모든 재료 정보 가져오기
        // (IngredientManager는 Dictionary<int, int>로 관리하므로 모든 ID를 순회해야 함)
        // 실제 구현에서는 IngredientManager에 GetAllIngredients() 같은 메서드가 필요할 수 있음
        // 여기서는 ItemTable의 모든 Material 타입 아이템을 체크하는 방식 사용

        var allItemData = CSVLoader.Instance.GetTable<ItemData>();
        if (allItemData == null)
        {
            Debug.LogError("[InventoryController] ItemData 테이블을 불러올 수 없습니다!");
            return;
        }

        // ItemTable에서 Material 타입 아이템들을 가져와서 보유 수량 확인
        foreach (var itemData in allItemData.GetAll())
        {
            // Material 타입만 인벤토리에 표시 (또는 Inventory 플래그가 true인 아이템)
            if (itemData.Item_Type == ItemType.Material && itemData.Inventory)
            {
                int currentCount = IngredientManager.Instance.GetIngredientCount(itemData.Item_ID);

                // 보유 수량이 1개 이상인 아이템만 표시
                if (currentCount > 0)
                {
                    var itemInfo = new InventoryItemInfo(itemData.Item_ID, currentCount);
                    displayedItems.Add(itemInfo);
                }
            }
        }

        //TODO JML: 2차 빌드 후 삭제 예정 - 임시 책갈피 추가
        if (TempBookMarkManager.Instance != null)
        {
            var bookmarks = TempBookMarkManager.Instance.GetAllBookmarks();
            foreach (var bookmark in bookmarks)
            {
                int instanceId = TempBookMarkManager.Instance.GetNextInstanceId();
                var bookmarkInfo = new InventoryItemInfo(bookmark, instanceId);
                displayedItems.Add(bookmarkInfo);
            }
        }

        // 아이템 정렬: 현재 정렬 방식에 따라
        ApplySorting();

        // UI 갱신
        RefreshInventoryUI();
    }

    /// <summary>
    /// 인벤토리 UI 갱신
    /// 아이템 리스트를 기반으로 슬롯 생성 및 배치
    /// </summary>
    private void RefreshInventoryUI()
    {
        // 기존 슬롯 모두 제거
        ClearAllSlots();

        // 필요한 총 슬롯 개수 계산
        int totalSlotsNeeded = 0;
        foreach (var item in displayedItems)
        {
            // 아이템이 차지하는 슬롯 개수 (최대 스택 수를 넘으면 여러 슬롯 사용)
            totalSlotsNeeded += item.GetRequiredSlotCount();
        }

        // 슬롯 생성
        int currentSlotIndex = 0;
        foreach (var item in displayedItems)
        {
            int requiredSlots = item.GetRequiredSlotCount();

            for (int i = 0; i < requiredSlots; i++)
            {
                var slot = GetOrCreateSlot(currentSlotIndex);
                slot.SetItem(item, i);
                slot.OnItemLongPressed = OnItemSlotLongPressed;
                currentSlotIndex++;
            }
        }

        Debug.Log($"[InventoryController] 인벤토리 UI 갱신 완료: {displayedItems.Count}종 아이템, {totalSlotsNeeded}개 슬롯");
    }

    /// <summary>
    /// 슬롯 가져오기 또는 새로 생성
    /// </summary>
    private InventoryItemSlot GetOrCreateSlot(int index)
    {
        // 기존 슬롯이 있으면 재사용
        if (index < itemSlots.Count)
        {
            return itemSlots[index];
        }

        // 새 슬롯 생성
        GameObject slotObj = Instantiate(itemSlotPrefab, inventoryGridParent);
        var slot = slotObj.GetComponent<InventoryItemSlot>();

        if (slot == null)
        {
            Debug.LogError("[InventoryController] itemSlotPrefab에 InventoryItemSlot 컴포넌트가 없습니다!");
            slot = slotObj.AddComponent<InventoryItemSlot>();
        }

        itemSlots.Add(slot);
        return slot;
    }

    /// <summary>
    /// 모든 슬롯 제거
    /// </summary>
    private void ClearAllSlots()
    {
        foreach (var slot in itemSlots)
        {
            slot.ClearSlot();
        }
    }

    #region Item Detail Popup

    /// <summary>
    /// 아이템 슬롯 길게 누름 이벤트 처리
    /// </summary>
    private void OnItemSlotLongPressed(InventoryItemInfo itemInfo)
    {
        if (itemInfo == null) return;

        ShowItemDetailPopup(itemInfo);
    }

    /// <summary>
    /// 아이템 상세 정보 팝업 표시
    /// </summary>
    private void ShowItemDetailPopup(InventoryItemInfo itemInfo)
    {
        if (itemDetailPopup == null) return;

        // 팝업 활성화
        itemDetailPopup.SetActive(true);

        // 아이템 아이콘 로드 (비동기)
        LoadPopupIcon(itemInfo).Forget();

        // 아이템 이름
        if (popupItemNameText != null)
        {
            popupItemNameText.text = itemInfo.ItemName;
        }

        // 아이템 설명
        if (popupItemDescriptionText != null)
        {
            string description = GetItemDescription(itemInfo);
            popupItemDescriptionText.text = description;
        }

        // 현재 보유량
        if (popupItemCurrentCountText != null)
        {
            popupItemCurrentCountText.text = $"현재 보유량: {itemInfo.CurrentCount}";
        }

        Debug.Log($"[InventoryController] 아이템 정보 팝업 표시: {itemInfo.ItemName}");
    }

    /// <summary>
    /// 팝업 아이콘 로드 (비동기)
    /// </summary>
    private async UniTaskVoid LoadPopupIcon(InventoryItemInfo itemInfo)
    {
        if (popupItemIcon == null) return;

        if (!string.IsNullOrEmpty(itemInfo.IconPath))
        {
            try
            {
                Sprite icon = await Addressables.LoadAssetAsync<Sprite>(itemInfo.IconPath).Task;
                if (icon != null && popupItemIcon != null)
                {
                    popupItemIcon.sprite = icon;
                    popupItemIcon.enabled = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[InventoryController] 팝업 아이콘 로드 실패: {itemInfo.IconPath}\n{e.Message}");
            }
        }
    }

    /// <summary>
    /// 아이템 설명 가져오기
    /// ItemTable의 Description 필드 사용
    /// </summary>
    private string GetItemDescription(InventoryItemInfo itemInfo)
    {
        // InventoryItemInfo에서 직접 가져오기
        if (!string.IsNullOrEmpty(itemInfo.Description))
        {
            return itemInfo.Description;
        }

        // Description이 없으면 기본 메시지
        return "아이템 설명이 없습니다.";
    }

    /// <summary>
    /// 아이템 상세 정보 팝업 닫기
    /// </summary>
    public void CloseItemDetailPopup()
    {
        if (itemDetailPopup != null)
        {
            itemDetailPopup.SetActive(false);
            Debug.Log("[InventoryController] 아이템 정보 팝업 닫기");
        }
    }

    #endregion

    #region Sorting

    /// <summary>
    /// 정렬 드롭다운 값 변경 이벤트
    /// </summary>
    private void OnSortDropdownValueChanged(int value)
    {
        currentSortType = (SortType)value;
        Debug.Log($"[InventoryController] 정렬 방식 변경: {currentSortType}");

        // 정렬 적용 및 UI 갱신
        ApplySorting();
        RefreshInventoryUI();
    }

    /// <summary>
    /// 현재 정렬 방식에 따라 아이템 리스트 정렬
    /// </summary>
    private void ApplySorting()
    {
        switch (currentSortType)
        {
            case SortType.Newest:
                // 최신순: ItemID가 높을수록 최근에 추가된 아이템 (역순 정렬)
                displayedItems = displayedItems.OrderByDescending(item => item.ItemID).ToList();
                Debug.Log("[InventoryController] 최신순 정렬 완료");
                break;

            case SortType.Name:
                // 이름순(가나다): 한글 이름 기준 오름차순
                displayedItems = displayedItems.OrderBy(item => item.ItemName).ToList();
                Debug.Log("[InventoryController] 이름순 정렬 완료");
                break;
        }
    }

    #endregion

    #region Button Events

    /// <summary>
    /// 뒤로가기 버튼 클릭 이벤트
    /// </summary>
    private void OnBackButtonClicked()
    {
        // TODO: 로딩 화면 표시 후 로비 씬으로 이동
        Debug.Log("[InventoryController] 뒤로가기 버튼 클릭 - 로비로 이동");
        SceneManager.LoadScene("LobbyScene");
    }

    #endregion

    /// <summary>
    /// 외부에서 인벤토리 갱신이 필요할 때 호출
    /// 예: BookMarkCraft에서 재료 사용 후
    /// </summary>
    public void RefreshInventory()
    {
        LoadInventoryData();
    }
}
