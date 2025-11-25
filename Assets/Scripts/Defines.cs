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
    Skill = 2
}

public enum CurrencyType
{
    FreeCurrency = 1,
    PaidCurrency = 2,
    SpecialCurrency = 3
}