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
    public static readonly string StringTable = "StringTable";


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

    public static string GetCharacterKey(int characterId)
    {
        return $"Character_{characterId:D2}";
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