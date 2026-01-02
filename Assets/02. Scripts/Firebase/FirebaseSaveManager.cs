using UnityEngine;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Firebase.Data;
using System;
using System.Collections.Generic;

/// <summary>
/// Firebase Realtime Database 저장/로드 관리
/// 부분 업데이트 지원으로 효율적인 데이터 동기화
/// </summary>
public class FirebaseSaveManager : MonoBehaviour
{
    private const string LOG_PREFIX = "<color=#3EB489>[FirebaseSave]</color>";
    private const string USERS_PATH = "users";

    public static FirebaseSaveManager Instance { get; private set; }

    private DatabaseReference databaseRef;
    private UserData cachedUserData;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public UserData CachedData => cachedUserData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Firebase Database 초기화
    /// FirebaseManager.InitializeAsync() 이후에 호출해야 함
    /// </summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            Debug.Log($"{LOG_PREFIX} 이미 초기화됨");
            return;
        }

        databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        isInitialized = true;
        Debug.Log($"{LOG_PREFIX} 초기화 완료");
    }

    #region 전체 데이터 로드/저장

    /// <summary>
    /// 유저 데이터 로드 (없으면 기본값 생성)
    /// </summary>
    public async UniTask<UserData> LoadUserDataAsync(string userId)
    {
        if (!isInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않음");
            return null;
        }

        try
        {
            var snapshot = await databaseRef.Child(USERS_PATH).Child(userId).GetValueAsync();

            if (snapshot.Exists)
            {
                cachedUserData = ParseUserData(snapshot);
                Debug.Log($"{LOG_PREFIX} 유저 데이터 로드 완료");
            }
            else
            {
                // 신규 유저 - 기본값 생성
                cachedUserData = UserData.CreateDefault();
                await SaveAllAsync(userId);
                Debug.Log($"{LOG_PREFIX} 신규 유저 데이터 생성 완료");
            }

            return cachedUserData;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 데이터 로드 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 전체 유저 데이터 저장
    /// </summary>
    public async UniTask<bool> SaveAllAsync(string userId)
    {
        if (!isInitialized || cachedUserData == null)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않았거나 데이터 없음");
            return false;
        }

        try
        {
            cachedUserData.lastUpdated = DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(cachedUserData);

            // Dictionary는 JsonUtility로 직렬화 안되므로 수동 처리
            var updates = ConvertToFirebaseFormat(cachedUserData);
            await databaseRef.Child(USERS_PATH).Child(userId).SetValueAsync(updates);

            Debug.Log($"{LOG_PREFIX} 전체 데이터 저장 완료");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 전체 저장 실패: {e.Message}");
            return false;
        }
    }

    #endregion

    #region 부분 업데이트 메서드들

    /// <summary>
    /// 재화 저장
    /// </summary>
    public async UniTask SaveCurrenciesAsync(string userId, CurrencySaveData currencies)
    {
        if (!isInitialized) return;

        try
        {
            cachedUserData.currencies = currencies;
            var data = new Dictionary<string, object>
            {
                { "gold", currencies.gold },
                { "exp", currencies.exp },
                { "application", currencies.application },
                { "recommendation", currencies.recommendation },
                { "magicStone", currencies.magicStone },
                { "dungeonPass", currencies.dungeonPass },
                { "ap", currencies.ap },
                { "apLastSyncTimeMs", currencies.apLastSyncTimeMs },
                { "apRecoveryTime", currencies.apRecoveryTime } // 레거시 호환
            };

            await databaseRef.Child(USERS_PATH).Child(userId).Child("currencies").UpdateChildrenAsync(data);
            Debug.Log($"{LOG_PREFIX} 재화 저장 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 재화 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 진행도 저장 (스테이지, 레벨, 처치 몬스터 수)
    /// </summary>
    public async UniTask SaveProgressionAsync(string userId, ProgressionData progression)
    {
        if (!isInitialized) return;

        try
        {
            cachedUserData.progression = progression;
            var data = new Dictionary<string, object>
            {
                { "highestClearedStage", progression.highestClearedStage },
                { "playerLevel", progression.playerLevel },
                { "playerExp", progression.playerExp },
                { "bossDungeonProgress", progression.bossDungeonProgress }, // Issue #476
                { "totalKilledMonsters", progression.totalKilledMonsters }
            };

            await databaseRef.Child(USERS_PATH).Child(userId).Child("progression").UpdateChildrenAsync(data);
            Debug.Log($"{LOG_PREFIX} 진행도 저장 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 진행도 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 보유 캐릭터 저장
    /// </summary>
    public async UniTask SaveOwnedCharactersAsync(string userId, HashSet<int> ownedCharacters)
    {
        if (!isInitialized) return;

        try
        {
            var data = new Dictionary<string, object>();
            foreach (var charId in ownedCharacters)
            {
                data[charId.ToString()] = true;
                cachedUserData.characters.owned[charId.ToString()] = true;
            }

            await databaseRef.Child(USERS_PATH).Child(userId).Child("characters").Child("owned").SetValueAsync(data);
            Debug.Log($"{LOG_PREFIX} 보유 캐릭터 저장 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 보유 캐릭터 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 캐릭터 강화 레벨 저장
    /// </summary>
    public async UniTask SaveCharacterEnhancementAsync(string userId, int characterId, int level)
    {
        if (!isInitialized) return;

        try
        {
            cachedUserData.characters.enhancements[characterId.ToString()] = level;

            await databaseRef.Child(USERS_PATH).Child(userId)
                .Child("characters").Child("enhancements").Child(characterId.ToString())
                .SetValueAsync(level);

            Debug.Log($"{LOG_PREFIX} 캐릭터 강화 저장 완료 (ID: {characterId}, Lv: {level})");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 캐릭터 강화 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 덱 프리셋 전체 저장
    /// </summary>
    public async UniTask SaveDeckPresetsAsync(string userId, DeckData deckData)
    {
        if (!isInitialized) return;

        try
        {
            // 캐시 업데이트
            cachedUserData.deck = deckData;

            var data = new Dictionary<string, object>
            {
                { "currentPreset", deckData.currentPreset },
                { "preset0", ConvertPresetToDict(deckData.preset0) },
                { "preset1", ConvertPresetToDict(deckData.preset1) },
                { "preset2", ConvertPresetToDict(deckData.preset2) },
                { "preset3", ConvertPresetToDict(deckData.preset3) }
            };

            await databaseRef.Child(USERS_PATH).Child(userId).Child("deck").SetValueAsync(data);
            Debug.Log($"{LOG_PREFIX} 덱 프리셋 저장 완료 (현재 프리셋: {deckData.currentPreset + 1})");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 덱 프리셋 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 프리셋 데이터를 Dictionary로 변환
    /// </summary>
    private Dictionary<string, object> ConvertPresetToDict(DeckPresetData preset)
    {
        if (preset == null)
        {
            return new Dictionary<string, object>
            {
                { "slot0", -1 },
                { "slot1", -1 },
                { "slot2", -1 },
                { "slot3", -1 }
            };
        }

        return new Dictionary<string, object>
        {
            { "slot0", preset.slot0 },
            { "slot1", preset.slot1 },
            { "slot2", preset.slot2 },
            { "slot3", preset.slot3 }
        };
    }

    /// <summary>
    /// 덱 저장 (하위 호환용 - 현재 프리셋에 저장)
    /// </summary>
    public async UniTask SaveDeckAsync(string userId, List<int> deck)
    {
        if (!isInitialized) return;

        try
        {
            cachedUserData.deck.FromList(deck);

            // 전체 프리셋 데이터로 저장
            await SaveDeckPresetsAsync(userId, cachedUserData.deck);
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 덱 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 재료 저장
    /// </summary>
    public async UniTask SaveIngredientsAsync(string userId, Dictionary<int, int> ingredients)
    {
        if (!isInitialized) return;

        try
        {
            var data = new Dictionary<string, object>();
            cachedUserData.ingredients.Clear();

            foreach (var kvp in ingredients)
            {
                data[kvp.Key.ToString()] = kvp.Value;
                cachedUserData.ingredients[kvp.Key.ToString()] = kvp.Value;
            }

            await databaseRef.Child(USERS_PATH).Child(userId).Child("ingredients").SetValueAsync(data);
            Debug.Log($"{LOG_PREFIX} 재료 저장 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 재료 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 단일 재료 업데이트
    /// </summary>
    public async UniTask SaveSingleIngredientAsync(string userId, int ingredientId, int count)
    {
        if (!isInitialized) return;

        try
        {
            cachedUserData.ingredients[ingredientId.ToString()] = count;

            await databaseRef.Child(USERS_PATH).Child(userId)
                .Child("ingredients").Child(ingredientId.ToString())
                .SetValueAsync(count);

            Debug.Log($"{LOG_PREFIX} 재료 저장 완료 (ID: {ingredientId}, 수량: {count})");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 재료 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 책갈피 전체 저장
    /// </summary>
    public async UniTask SaveBookmarksAsync(string userId, BookmarkSaveData bookmarks)
    {
        if (!isInitialized) return;

        try
        {
            cachedUserData.bookmarks = bookmarks;
            var data = ConvertBookmarksToFirebaseFormat(bookmarks);

            await databaseRef.Child(USERS_PATH).Child(userId).Child("bookmarks").SetValueAsync(data);
            Debug.Log($"{LOG_PREFIX} 책갈피 저장 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 책갈피 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 파견 상태 저장
    /// </summary>
    public async UniTask SaveDispatchAsync(string userId, string dispatchType, DispatchStateData state)
    {
        if (!isInitialized) return;

        try
        {
            if (dispatchType == "combat")
                cachedUserData.dispatch.combat = state;
            else
                cachedUserData.dispatch.gathering = state;

            var data = new Dictionary<string, object>
            {
                { "isActive", state.isActive },
                { "locationId", state.locationId },
                { "hours", state.hours },
                { "startTimeMs", state.startTimeMs },
                { "endTimeMs", state.endTimeMs },
                { "startTime", state.startTime },
                { "endTime", state.endTime },
                { "presetIndex", state.presetIndex }
            };

            await databaseRef.Child(USERS_PATH).Child(userId).Child("dispatch").Child(dispatchType).SetValueAsync(data);
            Debug.Log($"{LOG_PREFIX} 파견({dispatchType}) 저장 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 파견 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 닉네임 저장
    /// </summary>
    public async UniTask SaveNicknameAsync(string userId, string nickname)
    {
        if (!isInitialized) return;

        try
        {
            cachedUserData.nickname = nickname;
            await databaseRef.Child(USERS_PATH).Child(userId).Child("nickname").SetValueAsync(nickname);
            Debug.Log($"{LOG_PREFIX} 닉네임 저장 완료: {nickname}");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 닉네임 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 파티 시너지 레벨 저장
    /// </summary>
    public async UniTask SavePartySynergyAsync(string userId, int partyId, int level)
    {
        if (!isInitialized) return;

        try
        {
            if (cachedUserData.partySynergies == null)
            {
                cachedUserData.partySynergies = new PartySynergySaveData();
            }

            cachedUserData.partySynergies.levels[partyId.ToString()] = level;

            await databaseRef.Child(USERS_PATH).Child(userId)
                .Child("partySynergies").Child("levels").Child(partyId.ToString())
                .SetValueAsync(level);

            Debug.Log($"{LOG_PREFIX} 파티 시너지 저장 완료 (PartyID: {partyId}, Lv: {level})");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 파티 시너지 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 프로필 사진/프레임 저장
    /// </summary>
    public async UniTask SaveProfileAsync(string userId, int pictureId, int frameId)
    {
        if (!isInitialized) return;

        try
        {
            if (cachedUserData.profile == null)
            {
                cachedUserData.profile = new ProfileSaveData();
            }

            cachedUserData.profile.equippedPictureId = pictureId;
            cachedUserData.profile.equippedFrameId = frameId;

            var data = new Dictionary<string, object>
            {
                { "equippedPictureId", pictureId },
                { "equippedFrameId", frameId },
                { "equippedTitleIndex", cachedUserData.profile.equippedTitleIndex }
            };

            await databaseRef.Child(USERS_PATH).Child(userId).Child("profile").SetValueAsync(data);
            Debug.Log($"{LOG_PREFIX} 프로필 저장 완료 (사진: {pictureId}, 프레임: {frameId})");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 프로필 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 칭호 저장
    /// </summary>
    public async UniTask SaveTitleAsync(string userId, int titleIndex)
    {
        if (!isInitialized) return;

        try
        {
            if (cachedUserData.profile == null)
            {
                cachedUserData.profile = new ProfileSaveData();
            }

            cachedUserData.profile.equippedTitleIndex = titleIndex;

            await databaseRef.Child(USERS_PATH).Child(userId).Child("profile").Child("equippedTitleIndex").SetValueAsync(titleIndex);
            Debug.Log($"{LOG_PREFIX} 칭호 저장 완료 (인덱스: {titleIndex})");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 칭호 저장 실패: {e.Message}");
        }
    }

    #endregion

    #region 데이터 변환 헬퍼

    private Dictionary<string, object> ConvertToFirebaseFormat(UserData data)
    {
        var result = new Dictionary<string, object>
        {
            { "nickname", data.nickname ?? "" },
            { "lastUpdated", data.lastUpdated },
            { "currencies", new Dictionary<string, object>
                {
                    { "gold", data.currencies.gold },
                    { "exp", data.currencies.exp },
                    { "application", data.currencies.application },
                    { "recommendation", data.currencies.recommendation },
                    { "magicStone", data.currencies.magicStone },
                    { "dungeonPass", data.currencies.dungeonPass },
                    { "ap", data.currencies.ap },
                    { "apLastSyncTimeMs", data.currencies.apLastSyncTimeMs },
                    { "apRecoveryTime", data.currencies.apRecoveryTime } // 레거시 호환
                }
            },
            { "progression", new Dictionary<string, object>
                {
                    { "highestClearedStage", data.progression.highestClearedStage },
                    { "playerLevel", data.progression.playerLevel },
                    { "playerExp", data.progression.playerExp },
                    { "bossDungeonProgress", data.progression.bossDungeonProgress }, // Issue #476
                    { "totalKilledMonsters", data.progression.totalKilledMonsters }
                }
            },
            { "characters", new Dictionary<string, object>
                {
                    { "owned", data.characters.owned },
                    { "enhancements", data.characters.enhancements }
                }
            },
            { "deck", new Dictionary<string, object>
                {
                    { "currentPreset", data.deck.currentPreset },
                    { "preset0", ConvertPresetToDict(data.deck.preset0) },
                    { "preset1", ConvertPresetToDict(data.deck.preset1) },
                    { "preset2", ConvertPresetToDict(data.deck.preset2) },
                    { "preset3", ConvertPresetToDict(data.deck.preset3) }
                }
            },
            { "ingredients", data.ingredients },
            { "bookmarks", ConvertBookmarksToFirebaseFormat(data.bookmarks) },
            { "dispatch", new Dictionary<string, object>
                {
                    { "combat", new Dictionary<string, object>
                        {
                            { "isActive", data.dispatch.combat.isActive },
                            { "locationId", data.dispatch.combat.locationId },
                            { "hours", data.dispatch.combat.hours },
                            { "startTime", data.dispatch.combat.startTime },
                            { "endTime", data.dispatch.combat.endTime },
                            { "presetIndex", data.dispatch.combat.presetIndex }
                        }
                    },
                    { "gathering", new Dictionary<string, object>
                        {
                            { "isActive", data.dispatch.gathering.isActive },
                            { "locationId", data.dispatch.gathering.locationId },
                            { "hours", data.dispatch.gathering.hours },
                            { "startTime", data.dispatch.gathering.startTime },
                            { "endTime", data.dispatch.gathering.endTime },
                            { "presetIndex", data.dispatch.gathering.presetIndex }
                        }
                    }
                }
            },
            { "partySynergies", new Dictionary<string, object>
                {
                    { "levels", data.partySynergies?.levels ?? new Dictionary<string, int>() }
                }
            },
            { "profile", new Dictionary<string, object>
                {
                    { "equippedPictureId", data.profile?.equippedPictureId ?? -1 },
                    { "equippedFrameId", data.profile?.equippedFrameId ?? -1 },
                    { "equippedTitleIndex", data.profile?.equippedTitleIndex ?? 0 }
                }
            }
        };

        return result;
    }

    private Dictionary<string, object> ConvertBookmarksToFirebaseFormat(BookmarkSaveData bookmarks)
    {
        var items = new Dictionary<string, object>();
        foreach (var kvp in bookmarks.items)
        {
            items[kvp.Key] = new Dictionary<string, object>
            {
                { "dataId", kvp.Value.dataId },
                { "name", kvp.Value.name },
                { "grade", kvp.Value.grade },
                { "type", kvp.Value.type },
                { "optionType", kvp.Value.optionType },
                { "optionValue", kvp.Value.optionValue },
                { "skillId", kvp.Value.skillId },
                { "createdTime", kvp.Value.createdTime },
                { "equippedCharacterId", kvp.Value.equippedCharacterId },
                { "equipSlotIndex", kvp.Value.equipSlotIndex }
            };
        }

        return new Dictionary<string, object>
        {
            { "nextId", bookmarks.nextId },
            { "items", items }
        };
    }

    private UserData ParseUserData(DataSnapshot snapshot)
    {
        var data = new UserData();

        // nickname
        if (snapshot.Child("nickname").Exists)
            data.nickname = snapshot.Child("nickname").Value?.ToString() ?? "";

        // lastUpdated
        if (snapshot.Child("lastUpdated").Exists)
            data.lastUpdated = snapshot.Child("lastUpdated").Value.ToString();

        // currencies
        var currenciesSnap = snapshot.Child("currencies");
        if (currenciesSnap.Exists)
        {
            data.currencies.gold = GetIntValue(currenciesSnap.Child("gold"));
            data.currencies.exp = GetIntValue(currenciesSnap.Child("exp"));
            data.currencies.application = GetIntValue(currenciesSnap.Child("application"));
            data.currencies.recommendation = GetIntValue(currenciesSnap.Child("recommendation"));
            data.currencies.magicStone = GetIntValue(currenciesSnap.Child("magicStone"));
            data.currencies.dungeonPass = GetIntValue(currenciesSnap.Child("dungeonPass"));
            data.currencies.ap = GetIntValue(currenciesSnap.Child("ap"));
            data.currencies.apLastSyncTimeMs = GetLongValue(currenciesSnap.Child("apLastSyncTimeMs"));
            if (currenciesSnap.Child("apRecoveryTime").Exists)
                data.currencies.apRecoveryTime = currenciesSnap.Child("apRecoveryTime").Value?.ToString() ?? "";
        }

        // progression
        var progressionSnap = snapshot.Child("progression");
        if (progressionSnap.Exists)
        {
            data.progression.highestClearedStage = GetIntValue(progressionSnap.Child("highestClearedStage"));
            data.progression.playerLevel = GetIntValue(progressionSnap.Child("playerLevel"));
            data.progression.playerExp = GetIntValue(progressionSnap.Child("playerExp"));
            data.progression.bossDungeonProgress = GetIntValue(progressionSnap.Child("bossDungeonProgress"), 1); // Issue #476: 기본값 1
            data.progression.totalKilledMonsters = GetLongValue(progressionSnap.Child("totalKilledMonsters"));
        }

        // characters
        var charactersSnap = snapshot.Child("characters");
        if (charactersSnap.Exists)
        {
            var ownedSnap = charactersSnap.Child("owned");
            if (ownedSnap.Exists)
            {
                foreach (var child in ownedSnap.Children)
                {
                    data.characters.owned[child.Key] = true;
                }
            }

            var enhancementsSnap = charactersSnap.Child("enhancements");
            if (enhancementsSnap.Exists)
            {
                foreach (var child in enhancementsSnap.Children)
                {
                    data.characters.enhancements[child.Key] = GetIntValue(child);
                }
            }
        }

        // deck (프리셋 시스템 지원 + 하위 호환 마이그레이션)
        var deckSnap = snapshot.Child("deck");
        if (deckSnap.Exists)
        {
            // 새 프리셋 형식 확인 (preset0이 존재하면 새 형식)
            if (deckSnap.Child("preset0").Exists)
            {
                // 새 프리셋 형식
                data.deck.currentPreset = GetIntValue(deckSnap.Child("currentPreset"));
                data.deck.preset0 = ParseDeckPreset(deckSnap.Child("preset0"));
                data.deck.preset1 = ParseDeckPreset(deckSnap.Child("preset1"));
                data.deck.preset2 = ParseDeckPreset(deckSnap.Child("preset2"));
                data.deck.preset3 = ParseDeckPreset(deckSnap.Child("preset3"));
            }
            else
            {
                // 레거시 형식 (slot0~3만 있는 경우) - 마이그레이션용
                data.deck.slot0 = GetIntValue(deckSnap.Child("slot0"));
                data.deck.slot1 = GetIntValue(deckSnap.Child("slot1"));
                data.deck.slot2 = GetIntValue(deckSnap.Child("slot2"));
                data.deck.slot3 = GetIntValue(deckSnap.Child("slot3"));
                // DeckManager.SetDeckFromFirebase에서 MigrateLegacyData 호출됨
            }
        }

        // ingredients
        var ingredientsSnap = snapshot.Child("ingredients");
        if (ingredientsSnap.Exists)
        {
            foreach (var child in ingredientsSnap.Children)
            {
                data.ingredients[child.Key] = GetIntValue(child);
            }
        }

        // bookmarks
        var bookmarksSnap = snapshot.Child("bookmarks");
        if (bookmarksSnap.Exists)
        {
            data.bookmarks.nextId = GetIntValue(bookmarksSnap.Child("nextId"));
            var itemsSnap = bookmarksSnap.Child("items");
            if (itemsSnap.Exists)
            {
                foreach (var child in itemsSnap.Children)
                {
                    var item = new BookmarkItemData
                    {
                        dataId = GetIntValue(child.Child("dataId")),
                        name = child.Child("name").Value?.ToString() ?? "",
                        grade = GetIntValue(child.Child("grade")),
                        type = GetIntValue(child.Child("type")),
                        optionType = GetIntValue(child.Child("optionType")),
                        optionValue = GetFloatValue(child.Child("optionValue")),
                        skillId = GetIntValue(child.Child("skillId")),
                        createdTime = child.Child("createdTime").Value?.ToString() ?? "",
                        equippedCharacterId = GetIntValue(child.Child("equippedCharacterId")),
                        equipSlotIndex = GetIntValue(child.Child("equipSlotIndex"))
                    };
                    data.bookmarks.items[child.Key] = item;
                }
            }
        }

        // dispatch
        var dispatchSnap = snapshot.Child("dispatch");
        if (dispatchSnap.Exists)
        {
            data.dispatch.combat = ParseDispatchState(dispatchSnap.Child("combat"));
            data.dispatch.gathering = ParseDispatchState(dispatchSnap.Child("gathering"));
        }

        // partySynergies
        var partySynergiesSnap = snapshot.Child("partySynergies");
        if (partySynergiesSnap.Exists)
        {
            var levelsSnap = partySynergiesSnap.Child("levels");
            if (levelsSnap.Exists)
            {
                foreach (var child in levelsSnap.Children)
                {
                    data.partySynergies.levels[child.Key] = GetIntValue(child);
                }
            }
        }

        // profile (프로필 사진/프레임/칭호)
        var profileSnap = snapshot.Child("profile");
        if (profileSnap.Exists)
        {
            data.profile.equippedPictureId = GetIntValue(profileSnap.Child("equippedPictureId"), -1);
            data.profile.equippedFrameId = GetIntValue(profileSnap.Child("equippedFrameId"), -1);
            data.profile.equippedTitleIndex = GetIntValue(profileSnap.Child("equippedTitleIndex"), 0);
        }

        return data;
    }

    private DispatchStateData ParseDispatchState(DataSnapshot snap)
    {
        var state = new DispatchStateData();
        if (snap.Exists)
        {
            state.isActive = snap.Child("isActive").Value != null && (bool)snap.Child("isActive").Value;
            state.locationId = GetIntValue(snap.Child("locationId"));
            state.hours = GetIntValue(snap.Child("hours"));
            state.startTimeMs = GetLongValue(snap.Child("startTimeMs"));
            state.endTimeMs = GetLongValue(snap.Child("endTimeMs"));
            state.startTime = snap.Child("startTime").Value?.ToString() ?? "";
            state.endTime = snap.Child("endTime").Value?.ToString() ?? "";
            state.presetIndex = GetIntValue(snap.Child("presetIndex"), -1); // 기본값 -1
        }
        return state;
    }

    private DeckPresetData ParseDeckPreset(DataSnapshot snap)
    {
        var preset = new DeckPresetData();
        if (snap.Exists)
        {
            preset.slot0 = GetIntValue(snap.Child("slot0"));
            preset.slot1 = GetIntValue(snap.Child("slot1"));
            preset.slot2 = GetIntValue(snap.Child("slot2"));
            preset.slot3 = GetIntValue(snap.Child("slot3"));
        }
        return preset;
    }

    private int GetIntValue(DataSnapshot snap)
    {
        if (snap.Value == null) return 0;
        return Convert.ToInt32(snap.Value);
    }

    // Issue #476: 기본값 매개변수 오버로드
    private int GetIntValue(DataSnapshot snap, int defaultValue)
    {
        if (snap.Value == null) return defaultValue;
        return Convert.ToInt32(snap.Value);
    }

    private float GetFloatValue(DataSnapshot snap)
    {
        if (snap.Value == null) return 0f;
        return Convert.ToSingle(snap.Value);
    }

    private long GetLongValue(DataSnapshot snap)
    {
        if (snap.Value == null) return 0L;
        return Convert.ToInt64(snap.Value);
    }

    #endregion

    #region 랭킹 시스템

    private const string LEADERBOARD_PATH = "leaderboard";

    /// <summary>
    /// 리더보드에 내 기록 업데이트
    /// </summary>
    public async UniTask UpdateLeaderboardAsync(string oderId, long totalKilledMonsters)
    {
        if (!isInitialized || string.IsNullOrEmpty(oderId)) return;

        try
        {
            var data = new Dictionary<string, object>
            {
                { "oderId", oderId },
                { "totalKilledMonsters", totalKilledMonsters },
                { "lastUpdated", DateTime.UtcNow.ToString("o") }
            };

            await databaseRef.Child(LEADERBOARD_PATH).Child(oderId).SetValueAsync(data);
            Debug.Log($"{LOG_PREFIX} 리더보드 업데이트 완료: {totalKilledMonsters}");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 리더보드 업데이트 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 내 랭킹 조회 (나보다 높은 유저 수 + 1)
    /// </summary>
    public async UniTask<int> GetMyRankAsync(long myKillCount)
    {
        if (!isInitialized) return 1;

        try
        {
            // 나보다 처치 수가 많은 유저 수 카운트
            var query = databaseRef.Child(LEADERBOARD_PATH)
                .OrderByChild("totalKilledMonsters")
                .StartAt(myKillCount + 1);

            var snapshot = await query.GetValueAsync();
            int higherCount = (int)snapshot.ChildrenCount;
            int rank = higherCount + 1;

            Debug.Log($"{LOG_PREFIX} 랭킹 조회: 내 킬={myKillCount}, 나보다 높은 유저={higherCount}명, 내 순위=#{rank}");

            return rank;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 랭킹 조회 실패: {e.Message}");
            return 1;
        }
    }

    #endregion

    #region 게임 종료 시 저장

    private void OnApplicationQuit()
    {
        if (isInitialized && cachedUserData != null && FirebaseManager.Instance?.CurrentUserId != null)
        {
            // 동기적으로 저장 시도 (Unity 종료 시에는 async가 제대로 작동 안 할 수 있음)
            try
            {
                cachedUserData.lastUpdated = DateTime.UtcNow.ToString("o");
                var updates = ConvertToFirebaseFormat(cachedUserData);
                databaseRef.Child(USERS_PATH).Child(FirebaseManager.Instance.CurrentUserId).SetValueAsync(updates);
                Debug.Log($"{LOG_PREFIX} 게임 종료 - 데이터 저장 요청");
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_PREFIX} 종료 시 저장 실패: {e.Message}");
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isInitialized && cachedUserData != null && FirebaseManager.Instance?.CurrentUserId != null)
        {
            // 앱이 백그라운드로 갈 때 저장
            SaveAllAsync(FirebaseManager.Instance.CurrentUserId).Forget();
            Debug.Log($"{LOG_PREFIX} 앱 일시정지 - 데이터 저장");
        }
    }

    #endregion

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
