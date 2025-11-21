using UnityEngine;

/// <summary>
/// 인벤토리에 표시될 아이템 정보를 담는 클래스
/// ItemTable의 데이터와 현재 보유 수량을 함께 관리
/// </summary>
public class InventoryItemInfo
{
    // ItemTable에서 가져온 기본 정보
    public int ItemID { get; private set; }
    public string ItemName { get; private set; }
    public ItemType ItemType { get; private set; }
    public Grade ItemGrade { get; private set; }
    public int MaxStackCount { get; private set; }
    public int MaxTotalCount { get; private set; }
    public string IconPath { get; private set; }        // 아이콘 경로
    public string Description { get; private set; }     // 아이템 설명

    // 현재 보유 수량
    public int CurrentCount { get; private set; }

    /// <summary>
    /// ItemTable 데이터를 기반으로 인벤토리 아이템 정보 생성
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <param name="currentCount">현재 보유 수량</param>
    public InventoryItemInfo(int itemId, int currentCount)
    {
        // ItemTable에서 데이터 로드
        var itemData = CSVLoader.Instance.GetData<ItemData>(itemId);

        if (itemData == null)
        {
            Debug.LogError($"[InventoryItemInfo] 존재하지 않는 아이템 ID: {itemId}");
            return;
        }

        ItemID = itemData.Item_ID;
        ItemName = itemData.Item_Name;
        ItemType = itemData.Item_Type;
        ItemGrade = itemData.Item_Grade;
        MaxStackCount =Mathf.Max(1, itemData.Max_Stack); // 한 슬롯당 최대 스택 개수
        MaxTotalCount = Mathf.Max(MaxStackCount, itemData.Max_Count); // 전체 최대 보유 가능 개수
        IconPath = GetIconPath(itemData.Item_ID);           // 아이템 ID 기반 아이콘 경로
        Description = GetDescription(itemData.Item_ID);     // 아이템 ID 기반 설명
        CurrentCount = currentCount;
    }

    // TODO JML: 2차 빌드 후 삭제 예정 - BookMark 전용 생성자
    /// <summary>
    /// BookMark 객체로부터 인벤토리 아이템 정보 생성 (임시)
    /// </summary>
    /// <param name="bookmark">제작된 책갈피 객체</param>
    /// <param name="instanceId">고유 인스턴스 ID</param>
    public InventoryItemInfo(BookMark bookmark, int instanceId)
    {
        ItemID = 900000 + instanceId; // 임시 고유 ID (900001, 900002, ...)
        ItemName = $"{bookmark.GetName()} #{instanceId}";
        ItemType = ItemType.Special; // Special 타입으로 구분
        ItemGrade = bookmark.GetGrade();
        MaxStackCount = 1; // 책갈피는 1개씩만
        MaxTotalCount = 1;
        IconPath = "";
        Description = GetBookmarkDescription(bookmark);
        CurrentCount = 1;
    }

    //TODO JML: 2차 빌드 후 삭제 예정 - BookMark 설명 생성
    /// <summary>
    /// BookMark의 정보를 설명 문자열로 변환
    /// </summary>
    private string GetBookmarkDescription(BookMark bookmark)
    {
        string gradeText = bookmark.GetGrade() switch
        {
            Grade.Common => "커먼",
            Grade.Rare => "레어",
            Grade.Unique => "유니크",
            Grade.Legendary => "레전더리",
            Grade.Mythic => "신화",
            _ => "알 수 없음"
        };

        return $"등급: {gradeText}\n" +
               $"옵션 타입: {bookmark.GetOptionType()}\n" +
               $"옵션 값: {bookmark.GetOptionValue()}";
    }

    /// <summary>
    /// 아이템 수량 업데이트
    /// </summary>
    public void UpdateCount(int newCount)
    {
        CurrentCount = Mathf.Clamp(newCount, 0, MaxTotalCount);
    }

    /// <summary>
    /// 현재 아이템이 몇 개의 슬롯을 차지하는지 계산
    /// 예: MaxStackCount=999, CurrentCount=1009 -> 2슬롯 필요
    /// </summary>
    public int GetRequiredSlotCount()
    { 
        if (CurrentCount <= 0 || MaxStackCount <= 0)
            return 0;

        // 올림 나누기: (a + b - 1) / b
        return (CurrentCount + MaxStackCount - 1) / MaxStackCount;
        //return Mathf.CeilToInt((float)CurrentCount / MaxStackCount);
    }

    /// <summary>
    /// 특정 슬롯 인덱스에 표시될 수량 반환
    /// </summary>
    /// <param name="slotIndex">슬롯 인덱스 (0부터 시작)</param>
    public int GetCountForSlot(int slotIndex)
    {
        if (slotIndex < 0 || MaxStackCount <= 0)
            return 0;

        int startIndex = slotIndex * MaxStackCount;
        int remainingCount = CurrentCount - startIndex;

        if (remainingCount <= 0)
            return 0;

        return Mathf.Min(remainingCount, MaxStackCount);
    }

    /// <summary>
    /// 디버그용 문자열 출력
    /// </summary>
    public override string ToString()
    {
        return $"[{ItemID}] {ItemName} (등급: {ItemGrade}, 타입: {ItemType}) - {CurrentCount}/{MaxTotalCount}개";
    }

    /// <summary>
    /// 아이템 ID 기반 아이콘 경로 반환 (Addressables 키 사용)
    /// </summary>
    private string GetIconPath(int itemId)
    {
        // AddressableKey 사용
        return AddressableKey.GetItemIconKey(itemId);
    }

    /// <summary>
    /// 아이템 ID 기반 설명 반환 (하드코딩)
    /// </summary>
    private string GetDescription(int itemId)
    {
        return itemId switch
        {
            101611 => "게임 내 기본 화폐입니다.",
            103622 => "사서의 성장에 필요한 경험치입니다.",
            102113 => "희미한 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            102214 => "응축된 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            102315 => "비범한 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            102416 => "고대의 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            102517 => "신성한 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            102118 => "책갈피 제작에 필요한\n마법의 잉크입니다.",
            _ => "아이템 설명이 없습니다."
        };
    }
}
