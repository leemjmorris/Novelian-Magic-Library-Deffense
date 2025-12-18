public static class Tag
{
    public static readonly string Player = "Player";
    public static readonly string Monster = "Monster";
    public static readonly string Wall = "Wall";
    public static readonly string BossMonster = "BossMonster";
    public static readonly string Character = "Character";
    public static readonly string CharacterInfoPanel = "CharacterInfoPanel";
    public static readonly string Obstacle = "Obstacle";
}

public static class SceneName
{
    public static readonly string TitleScene = "TitleScene";
    public static readonly string LobbyScene = "LobbyScene";
    public static readonly string StageScene = "StageScene";
    public static readonly string GameScene = "GameScene";
    public static readonly string Inventory = "Inventory";
    public static readonly string LibraryManagementScene = "LibraryManagementScene";
    public static readonly string DispatchSystemScene = "DispatchSystemScene";
    public static readonly string BookMarkCraftScene = "BookMarkCraftScene";
    public static readonly string BossDungeonScene = "BossDungeonScene";  // Issue #476 - 도전던전 씬
}

public static class AddressableKey
{
    public static readonly string PathTable = "PathTable";
    public static readonly string Monster = "Monster";
    public static readonly string BossMonster = "BossMonster";
    public static readonly string Projectile = "Projectile";
    public static readonly string Skill = "Skill";

    //JML: CSV Addressable Keys
    public static readonly string BookmarkTable = "BookmarkTable";
    public static readonly string BookmarkCraftTable = "BookMarkCraftTable";
    public static readonly string BookmarkOptionTable = "BookmarkOptionTable";
    public static readonly string BookmarkStatListTable = "BookmarkStatListTable";
    public static readonly string BookmarkSkillListTable = "BookmarkSkillListTable";
    public static readonly string BookmarkSkillTable = "BookmarkSkillTable";
    public static readonly string CardLevelTable = "CardLevelTable";
    public static readonly string CardListTable = "CardListTable";
    public static readonly string CardTable = "CardTable";
    public static readonly string ItemTable = "ItemTable";
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
    public static readonly string MainSkillTable = "MainSkillTable";
    public static readonly string SupportSkillTable = "SupportSkillTable";
    public static readonly string SupportCompatibilityTable = "SupportCompatibilityTable";
    public static readonly string SkillLevelTable = "SkillLevelTable";
    public static readonly string PlayerLevelTable = "PlayerLevelTable";
    public static readonly string SkillTypeTable = "SkillTypeTable";
    public static readonly string SkillCardTable = "SkillCardTable"; 
    public static readonly string LayoutPresetTable = "LayoutPresetTable";  // Issue #420    
    public static readonly string PartySynergyTable = "PartySynergyTable";  // 파티 시너지 테이블
    public static readonly string PartySynergyEnhancementTable = "PartySynergyEnhancementTable";  // 파티 시너지 강화 테이블
    public static readonly string PartySynergyEffectTable = "PartySynergyEffectTable";

    // Issue #476 - 도전던전 테이블
    public static readonly string BossDungeonTable = "BossDungeonTable";
    
    // JML: Icon Addressable Keys
    public static readonly string Icon_Mystery = "Icon_Mystery";
    public static readonly string IconAdventure = "Icon_Adventure";
    public static readonly string IconRomance = "Icon_Romance";
    public static readonly string IconHorror = "Icon_Horror";
    public static readonly string IconComedy = "Icon_Comedy";
    public static readonly string Icon_Character = "ChaIcon";
    public static readonly string Icon_Plus = "Plus";
    public static readonly string Icon_LegendaryBookmark = "LegendaryBookmark";
    public static readonly string Icon_MythicBookmark = "MythicBookmark";
    public static readonly string Icon_NormalBookmark = "NormalBookmark";
    public static readonly string Icon_RareBookmark = "RareBookmark";
    public static readonly string Icon_UniqueBookmark = "UniqueBookmark";

    // SkillPrefabDatabase removed - migrated to SkillEffectDatabase

    /// <summary>
    /// JML: [Obsolete] 이 메서드는 더 이상 사용하지 않음 (Issue #447)
    /// 개별 캐릭터 프리팹 시스템으로 변경됨.
    /// CharacterPlacementManager.GetCharacterPrefabKey() 사용 권장.
    /// Character_ID → CharacterData.Path_ID → PathData.Addressable_Key → "Prefab_" + key
    /// </summary>
    [System.Obsolete("Use CharacterPlacementManager.GetCharacterPrefabKey() instead. Individual character prefabs are now loaded via PathTable.")]
    public static string GetCharacterKey(int characterId)
    {
        UnityEngine.Debug.LogWarning($"[AddressableKey] GetCharacterKey({characterId}) is deprecated. Use PathTable lookup with 'Prefab_' prefix instead.");
        return "Character";  // 구 버전 호환성을 위해 유지 (사용하지 말 것)
    }

    /// <summary>
    /// JML: Character_ID로 프리팹 Addressable 키 조회 (Issue #447)
    /// Character_ID → CharacterData.Path_ID → PathData.Addressable_Key → "Prefab_" + key
    /// </summary>
    public static string GetCharacterPrefabKey(int characterId)
    {
        if (CSVLoader.Instance == null)
        {
            UnityEngine.Debug.LogError("[AddressableKey] CSVLoader.Instance is null");
            return null;
        }

        var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
        if (characterData == null)
        {
            UnityEngine.Debug.LogError($"[AddressableKey] CharacterData not found for ID: {characterId}");
            return null;
        }

        var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
        if (pathData == null)
        {
            UnityEngine.Debug.LogError($"[AddressableKey] PathData not found for Path_ID: {characterData.Path_ID}");
            return null;
        }

        return $"Prefab_{pathData.Addressable_Key}";
    }

    public static string GetCardSpriteKey(int characterId)
    {
        return $"CardSprite_{characterId}";
    }

    public static string GetItemIconKey(int itemId)
    {
        return $"ItemIcon_{itemId}";
    }

    /// <summary>
    /// Ingredient_ID로 재료 아이콘 어드레서블 키 반환
    /// 예: 10101 → "IngredientIcon_10101"
    /// </summary>
    public static string GetIngredientIconKey(int ingredientId)
    {
        return $"IngredientIcon_{ingredientId}";
    }

    /// <summary>
    /// JML: Monster_ID로 어드레서블 키 조회
    /// Monster_ID → MonsterData.Path_ID → PathData.Addressable_Key
    /// </summary>
    public static string GetMonsterAddressableKey(int monsterId)
    {
        if (CSVLoader.Instance == null)
        {
            UnityEngine.Debug.LogError("[AddressableKey] CSVLoader.Instance is null");
            return null;
        }

        var monsterData = CSVLoader.Instance.GetData<MonsterData>(monsterId);
        if (monsterData == null)
        {
            UnityEngine.Debug.LogError($"[AddressableKey] MonsterData not found for ID: {monsterId}");
            return null;
        }

        var pathData = CSVLoader.Instance.GetData<PathData>(monsterData.Path_ID);
        if (pathData == null)
        {
            UnityEngine.Debug.LogError($"[AddressableKey] PathData not found for Path_ID: {monsterData.Path_ID}");
            return null;
        }

        return pathData.Addressable_Key;
    }

    /// <summary>
    /// JML: Grade에 따른 책갈피 아이콘 어드레서블 키 반환
    /// </summary>
    public static string GetBookmarkIconKey(Grade grade)
    {
        return grade switch
        {
            Grade.Common => Icon_NormalBookmark,
            Grade.Rare => Icon_RareBookmark,
            Grade.Unique => Icon_UniqueBookmark,
            Grade.Legendary => Icon_LegendaryBookmark,
            Grade.Mythic => Icon_MythicBookmark,
            _ => Icon_NormalBookmark
        };
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
/// <summary>
/// 책갈피 옵션 타입 (BookmarkOptionTable.csv Option_Type 기준)
/// </summary>
public enum OptionType
{
    None = 0,
    AttackPower = 1,            // 공격력 증가
    AttackSpeed = 2,            // 공격 속도 증가
    CritMultiplier = 3,         // 치명타 배율 증가
    CritChance = 4,             // 치명타 확률 증가
    CritDamage = 5,             // 치명타 피해 증가
    GenreBonus = 6,             // 속성 강화 (로맨스/모험/코미디/추리/공포)
    CooldownReduction = 7,      // 쿨타임 감소
    CastTimeReduction = 8,      // 시전 속도 감소
    // 09: 제거됨 (발사체 속도)
    ItemDropRate = 10,          // 아이템 획득 확률 증가
    GoldBonus = 11,             // 골드 획득량 증가
    DispatchTimeReduction = 12, // 파견 시간 감소
    EnemyAttackReduction = 13,  // 적 공격력 감소 (보류)
    EnemySpeedReduction = 14,   // 적 공격 속도 감소 (보류)
    EnemyMoveReduction = 15,    // 적 이동 속도 감소 (보류)
    BossDamage = 16             // 보스 데미지 증가
}
public enum UseType
{
    BookmarkCraft = 1,
    UserLevelUp = 2,
    ProductPurchase = 3,
    CharacterEssence = 4    // 캐릭터 정수 (가챠 중복 시 지급)
}

public enum BookmarkType
{
    None = 0,
    Stat = 1,
    Skill = 2,
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

/// <summary>
/// 경고/알림 메시지 텍스트 상수
/// </summary>
public static class WarningText
{
    public const string FeatureNotReady = "준비 중인 기능입니다";
    public const string MainSkillBookmarkLimitReached = "메인 스킬 책갈피는 1개만 장착 가능합니다";
    public const string StatBookmarkLimitReached = "스탯 책갈피는 4개까지만 장착 가능합니다";
}

