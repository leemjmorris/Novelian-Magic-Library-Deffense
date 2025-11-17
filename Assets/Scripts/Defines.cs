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

    // Character Addressable Keys (ID-based)
    // 나중에 CSV 연동 시: CSVLoader.Get<CharacterTableData>(characterId).AddressableKey
    public static string GetCharacterKey(int characterId)
    {
        return $"Character_{characterId}";
    }

    public static string GetCardSpriteKey(int characterId)
    {
        return $"CardSprite_{characterId}";
    }
}