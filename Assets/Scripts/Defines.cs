public static class Tag
{
    public static readonly string Player = "Player";
    public static readonly string Monster = "Monster";
    public static readonly string Wall = "Wall";
    public static readonly string BossMonster = "BossMonster";
}

public static class AddressableKey
{
    public static readonly string Monster = "Monster";
    public static readonly string BossMonster = "BossMonster";
    public static readonly string Projectile = "Projectile";
    public static readonly string Skill = "Skill";

    //JML: CSV Addressable Keys
    public static readonly string ItemTable = "ItemTable";
    public static readonly string BookmarkCraftTable = "BookMarkCraftTable";
    public static readonly string BookmarkResultTable = "BookmarkResultTable";
    public static readonly string BookmarkOptionTable = "BookmarkOptionTable";
    public static readonly string BookmarkItemTable = "BookmarkItemTable";
    public static readonly string GradeTable = "GradeTable";

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
public enum UseType
{
    BookmarkCraft = 1,
    UserLevelUp = 2,
    ProductPurchase = 3
}