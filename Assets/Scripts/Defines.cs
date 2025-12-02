public static class Tag
{
    public static readonly string Player = "Player";
    public static readonly string Monster = "Monster";
    public static readonly string Wall = "Wall";
    public static readonly string BossMonster = "BossMonster";
    public static readonly string CharacterInfoPanel = "CharacterInfoPanel";
    public static readonly string Obstacle = "Obstacle";
}

public static class sceneName
{
    public static readonly string LobbyScene = "LobbyScene";
    public static readonly string StageScene = "StageScene";
    public static readonly string GameScene = "GameScene (JML)";
    public static readonly string Inventory = "Inventory";
    public static readonly string LibraryManagementScene = "LibraryManagementScene(LCB)";
    public static readonly string DispatchSystemScene = "DispatchSystemScene";
    public static readonly string BookMarkCraftScene = "BookMarkCraftScene";
}

public static class AddressableKey
{
    public static readonly string Monster = "Monster";
    public static readonly string BossMonster = "BossMonster";
    public static readonly string Projectile = "Projectile";
    public static readonly string Skill = "Skill";

    //JML: CSV Addressable Keys
    public static readonly string BookmarkTable = "BookmarkTable";
    public static readonly string BookmarkCraftTable = "BookMarkCraftTable";
    public static readonly string BookmarkOptionTable = "BookmarkOptionTable";
    public static readonly string BookmarkListTable = "BookmarkListTable";
    public static readonly string BookmarkSkillTable = "BookmarkSkillTable";
    public static readonly string CurrencyTable = "CurrencyTable";
    public static readonly string GradeTable = "GradeTable";
    public static readonly string IngredientTable = "IngredientTable";
    public static readonly string CharacterTable = "CharacterTable";
    public static readonly string LevelTable = "LevelTable";
    public static readonly string SkillTable = "SkillTable";
    public static readonly string EnhancementLevelTable = "EnhancementLevelTable";
    public static readonly string CharacterEnhancementTable = "CharacterEnhancementTable";
    public static readonly string StageTable = "StageTable";
    public static readonly string WaveTable = "WaveTable";
    public static readonly string MonsterLevelTable = "MonsterLevelTable";
    public static readonly string MonsterTable = "MonsterTable";
    public static readonly string RewardTable = "RewardTable";
    public static readonly string RewardGroupTable = "RewardGroupTable";
    public static readonly string StringTable = "StringTable";
    public static readonly string DispatchCategoryTable = "DispatchCategoryTable";
    public static readonly string DispatchLocationTable = "DispatchLocationTable";
    public static readonly string DispatchRewardTable = "DispatchRewardTable";
    public static readonly string DispatchTimeTable = "DispatchTimeTable";
    
    // JML: Icon Addressable Keys
    public static readonly string Icon_Mystery = "Icon_Mystery";
    public static readonly string IconAdventure = "Icon_Adventure";
    public static readonly string IconRomance = "Icon_Romance";
    public static readonly string IconHorror = "Icon_Horror";
    public static readonly string IconComedy = "Icon_Comedy";
    public static readonly string Icon_Character = "ChaIcon";
    public static readonly string Icon_Plus = "Plus";

    // 새 스킬 시스템 CSV 테이블
    public static readonly string MainSkillTable = "MainSkillTable";
    public static readonly string SupportSkillTable = "SupportSkillTable";
    public static readonly string SkillLevelTable = "SkillLevelTable";

    // 스킬 Prefab 데이터베이스
    public static readonly string SkillPrefabDatabase = "SkillPrefabDatabase";

    // 인게임 카드 시스템 CSV 테이블
    public static readonly string CardTable = "CardTable";
    public static readonly string CardLevelTable = "CardLevelTable";
    public static readonly string CardListTable = "CardListTable";
    public static readonly string PlayerLevelTable = "PlayerLevelTable";

    // JML: 범용 프리팹 방식 - 단일 키 반환 (Issue #320)
    public static string GetCharacterKey(int characterId)
    {
        return "Character";  // 모든 캐릭터가 동일한 프리팹 사용
    }

    public static string GetCardSpriteKey(int characterId)
    {
        return $"CardSprite_{characterId}";
    }

    public static string GetItemIconKey(int itemId)
    {
        return $"ItemIcon_{itemId}";
    }
}

/// <summary>
/// Skill Type Enum: 1=Attack, 2=Buff, 3=Debuff
/// </summary>
public enum SkillType
{
    Attack = 1,
    Buff = 2,
    Debuff = 3
}

/// <summary>
/// Attack Range Enum: 1=Single, 2=Area, 3=Wide
/// </summary>
public enum AttackRange
{
    Single = 1,
    Area = 2,
    Wide = 3
}

/// <summary>
/// Item Type Enum: 1=Consumable, 2=Equipment, 3=Material
/// </summary>
public enum ItemType
{
    Currency = 1,
    Material = 2,
    Special = 3,
}

public enum Grade
{
    Common = 1,
    Rare = 2,
    Unique = 3,
    Legendary = 4,
    Mythic = 5,
}
public enum Genre
{
    Horror = 1,
    Romance = 2,
    Adventure = 3,
    Comedy = 4,
    Mystery = 5
}
public enum OptionType
{
    AttackPower = 1,
    AttackSkill = 2,
}
public enum UseType
{
    BookmarkCraft = 1,
    UserLevelUp = 2,
    ProductPurchase = 3
}

public enum BookmarkType
{
    None = 0,
    Stat = 1,
    Skill = 2,
    SubSkill = 3,   // JML: 보조스킬 (추후 구현)
    All = 99        // JML: 전체 필터용
}

public enum CurrencyType
{
    FreeCurrency = 1,
    PaidCurrency = 2,
    SpecialCurrency = 3
}

public enum DispatchType
{
    Combat = 1,     // 전투형
    Collection = 2  // 채집형
}

public enum DispatchLocation
{
    NightmareWarehouse = 1,     // 악몽의 창고
    FateWarehouse = 2,          // 운명의 창고
    LaughterWarehouse = 3,      // 웃음의 창고
    TruthWarehouse = 4,         // 진실의 창고
    UnknownWarehouse = 5,       // 미지의 창고
    MagicLibraryOrganization = 6,   // 마도 서고 정돈
    MagicBarrierInspection = 7,     // 마력 장벽 유지 검사
    SpellbookCoverRestoration = 8,  // 마도서 표지 복원
    SealStabilityCheck = 9,         // 봉인구 안정성 확인
    MagicResiduePurification = 10   // 마력 잔재 정화
}

/// <summary>
/// 인게임 스텟 카드 타입 (CardLevelTable 기준)
/// Issue #349 - 카드 선택 UI 로직 개선
/// </summary>
public enum StatType
{
    Damage = 0,             // 공격력 증가 (25001~25003)
    CritMultiplier = 1,     // 치명타 배율 증가 (25004~25006)
    AttackSpeed = 2,        // 공격속도 증가 (25007~25009)
    CritChance = 3,         // 치명타 확률 증가 (25010~25012)
    ProjectileSpeed = 4,    // 투사체 발사 속도 증가 (25013~25015)
    TotalDamage = 5,        // 총 공격력 증가 (25016~25018)
    BonusDamage = 6,        // 추가 데미지 추가 (25019~25021)
    HealthRegen = 7,        // 체력 회복 (25022~25024)
    Range = 8               // 사거리 증가 (25085~25087)
}

