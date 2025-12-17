using UnityEngine;

/// <summary>
/// 인벤토리에 표시될 아이템 정보를 담는 클래스
/// ItemTable의 데이터와 현재 보유 수량을 함께 관리
/// </summary>
public class InventoryItemInfo
{
    // IngredientTable에서 가져온 기본 정보
    public int ItemID { get; private set; }
    public string ItemName { get; private set; }
    public UseType ItemType { get; private set; }
    public Grade ItemGrade { get; private set; }
    public int MaxStackCount { get; private set; }
    public int MaxTotalCount { get; private set; }
    public string IconPath { get; private set; }        // 아이콘 경로
    public string Description { get; private set; }     // 아이템 설명

    // 현재 보유 수량
    public int CurrentCount { get; private set; }

    /// <summary>
    /// IngredientTable 데이터를 기반으로 인벤토리 아이템 정보 생성
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <param name="currentCount">현재 보유 수량</param>
    public InventoryItemInfo(int itemId, int currentCount)
    {
        // IngredientTable에서 데이터 로드
        var ingredientData = CSVLoader.Instance.GetData<IngredientData>(itemId);

        if (ingredientData == null)
        {
            Debug.LogError($"[InventoryItemInfo] 존재하지 않는 재료 ID: {itemId}");
            return;
        }

        ItemID = ingredientData.Ingredient_ID;
        ItemName = CSVLoader.Instance.GetData<StringTable>(ingredientData.Ingredient_Name_ID)?.Text ?? "Unknown";
        ItemType = ingredientData.Use_Type;

        // GradeTable에서 등급 정보 가져오기
        if (ingredientData.Grade_ID > 0)
        {
            var gradeData = CSVLoader.Instance.GetData<GradeData>(ingredientData.Grade_ID);
            ItemGrade = gradeData?.Grade_Type ?? Grade.Common;
        }
        else
        {
            // Grade_ID가 0이면 기본값 사용 (에러 방지)
            ItemGrade = Grade.Common;
        }

        MaxStackCount = Mathf.Max(1, ingredientData.Max_Stack); // 한 슬롯당 최대 스택 개수
        MaxTotalCount = Mathf.Max(MaxStackCount, ingredientData.Max_Count); // 전체 최대 보유 가능 개수
        IconPath = GetIconPath(ingredientData.Ingredient_ID);           // 아이템 ID 기반 아이콘 경로
        Description = GetDescription(ingredientData.Ingredient_ID);     // 아이템 ID 기반 설명
        CurrentCount = currentCount;
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
        return AddressableKey.GetItemIconKey(itemId);
    }

    /// <summary>
    /// 아이템 ID 기반 설명 반환 (하드코딩)
    /// </summary>
    private string GetDescription(int itemId)
    {
        return itemId switch
        {
            // 기존 아이템
            101611 => "게임 내 기본 화폐입니다.",
            103622 => "사서의 성장에 필요한 경험치입니다.",
            102113 => "희미한 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            102214 => "응축된 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            102315 => "비범한 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            102416 => "고대의 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            102517 => "신성한 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            102118 => "책갈피 제작에 필요한\n마법의 잉크입니다.",
            // 파견 보상 아이템
            10101 => "희미한 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            10102 => "응축된 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            10103 => "비범한 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            10104 => "신성한 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            10105 => "고대의 마력이 깃든 종이\n책갈피 제작에 사용됩니다.",
            10106 => "책갈피 제작에 필요한\n마법의 잉크입니다.",
            10207 => "로맨스 장르의 페이지입니다.\n책갈피 제작에 사용됩니다.",
            10208 => "코미디 장르의 페이지입니다.\n책갈피 제작에 사용됩니다.",
            10209 => "모험 장르의 페이지입니다.\n책갈피 제작에 사용됩니다.",
            10210 => "공포 장르의 페이지입니다.\n책갈피 제작에 사용됩니다.",
            10211 => "추리 장르의 페이지입니다.\n책갈피 제작에 사용됩니다.",
            10313 => "책갈피를 고정하는 클립입니다.\n책갈피 제작에 사용됩니다.",
            10114 => "마법이 깃든 룬석입니다.\n책갈피 강화에 사용됩니다.",
            _ => "아이템 설명이 없습니다."
        };
    }
}
