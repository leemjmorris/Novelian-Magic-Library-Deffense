//LMJ : Character partial class - Stats, Buffs, Bookmarks, and Star Tier System
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using NovelianMagicLibraryDefense.Managers;

    /// <summary>
    /// 캐릭터 스탯 관리
    /// - 스탯 버프 적용
    /// - 책갈피 시스템
    /// - 성급 시스템
    /// - 전역 스텟 버프
    /// </summary>
    public partial class Character
    {
        #region Star Tier System (Issue #349)

        /// <summary>
        /// JML: 캐릭터 성급 (1~3성, 최대 3성)
        /// 중복 캐릭터 카드 선택 시 성급 업그레이드
        /// </summary>
        private int starTier = 1;
        public const int MAX_STAR_TIER = 3;

        /// <summary>
        /// JML: 현재 성급 반환
        /// </summary>
        public int GetStarTier() => starTier;

        /// <summary>
        /// JML: 최종 성급인지 확인
        /// </summary>
        public bool IsFinalStarTier() => starTier >= MAX_STAR_TIER;

        /// <summary>
        /// JML: 성급 업그레이드 (중복 캐릭터 카드 선택 시)
        /// CardLevelTable.csv의 value_change 값 적용 (배수 방식)
        /// </summary>
        /// <returns>업그레이드 성공 여부</returns>
        public bool UpgradeStarTier()
        {
            if (starTier >= MAX_STAR_TIER)
            {
                Debug.LogWarning($"[Character] 이미 최종 성급입니다! (현재: {starTier}성)");
                return false;
            }

            int oldStarTier = starTier;
            starTier++;

            // CSV에서 성급 배율 가져오기
            float newTierMultiplier = GetStarTierMultiplierFromCSV(starTier);
            float oldTierMultiplier = GetStarTierMultiplierFromCSV(oldStarTier);

            // 증가분 계산 (새 배율 - 이전 배율)
            float upgradeBonus = newTierMultiplier - oldTierMultiplier;

            // 모든 스탯에 버프 적용
            ApplyStatBuff(StatType.Damage, upgradeBonus);
            ApplyStatBuff(StatType.AttackSpeed, upgradeBonus);

            Debug.Log($"[Character] 성급 업그레이드! {oldStarTier}성 → {starTier}성 (배율: {oldTierMultiplier} → {newTierMultiplier}, 증가분: +{upgradeBonus * 100:F0}%)");
            return true;
        }

        /// <summary>
        /// JML: CardLevelTable.csv에서 캐릭터별 성급 배율 가져오기
        /// 캐릭터 ID를 기반으로 CardLevelTable의 해당 행 조회
        /// </summary>
        /// <param name="tier">성급 (1~3)</param>
        /// <returns>배율 값 (1.0, 1.2, 1.5 등)</returns>
        private float GetStarTierMultiplierFromCSV(int tier)
        {
            // CardLevelTable ID 계산
            // 캐릭터 순번 = (characterId % 100) - 1 (021001 → 0, 021002 → 1, ...)
            // 기본 ID = 25025 (그림 Tier1)
            // 각 캐릭터당 3행씩 (Tier 1~3)
            int characterIndex = GetCharacterIndexFromId(characterId);
            int cardLevelId = 25025 + (characterIndex * 3) + (tier - 1);

            var cardLevelTable = CSVLoader.Instance?.GetTable<CardLevelData>();
            if (cardLevelTable == null)
            {
                Debug.LogWarning("[Character] CardLevelTable이 로드되지 않았습니다. 기본값 사용.");
                return GetDefaultTierMultiplier(tier);
            }

            var cardLevelData = cardLevelTable.GetId(cardLevelId);
            if (cardLevelData == null)
            {
                Debug.LogWarning($"[Character] CardLevelTable에서 ID {cardLevelId}를 찾을 수 없습니다. 기본값 사용.");
                return GetDefaultTierMultiplier(tier);
            }

            Debug.Log($"[Character] CSV 성급 배율 조회: CharacterId={characterId}, Tier={tier}, CardLevelId={cardLevelId}, value_change={cardLevelData.value_change}");
            return cardLevelData.value_change;
        }

        /// <summary>
        /// JML: 캐릭터 ID에서 순번 추출 (CardLevelTable 매핑용)
        /// 021001 → 0, 021002 → 1, ... 025020 → 19
        /// </summary>
        private int GetCharacterIndexFromId(int charId)
        {
            // CharacterTable 순서 기준 (0번부터)
            // Horror: 021001~021004 (인덱스 0~3)
            // Romance: 022005~022008 (인덱스 4~7)
            // Adventure: 023009~023012 (인덱스 8~11)
            // Comedy: 024013~024016 (인덱스 12~15)
            // Mystery: 025017~025020 (인덱스 16~19)
            int lastTwoDigits = charId % 100;
            return lastTwoDigits - 1; // 01 → 0, 02 → 1, ...
        }

        /// <summary>
        /// JML: CSV 로드 실패 시 기본 성급 배율
        /// </summary>
        private float GetDefaultTierMultiplier(int tier)
        {
            return tier switch
            {
                1 => 1.0f,
                2 => 1.2f,
                3 => 1.5f,
                _ => 1.0f
            };
        }

        #endregion

        #region Global Stat Buff System (Issue #349)

        /// <summary>
        /// JML: 외부에서 스텟 버프 적용 (StageManager에서 호출)
        /// 전역 스텟 카드 선택 시 모든 캐릭터에 적용
        /// </summary>
        /// <param name="statType">스텟 타입 (StatType enum)</param>
        /// <param name="value">증가 값 (% 단위, 예: 0.1 = 10%)</param>
        public void ApplyStatBuff(StatType statType, float value)
        {
            // value는 비율(0.1 = 10%), modifier는 % 단위로 저장
            float percentValue = value * 100f;

            switch (statType)
            {
                case StatType.Damage:
                case StatType.TotalDamage:
                    damageModifier += percentValue;
                    Debug.Log($"[Character] 데미지 버프 +{percentValue}% (총 {damageModifier}%)");
                    break;

                case StatType.AttackSpeed:
                    attackSpeedModifier += percentValue;
                    Debug.Log($"[Character] 공격속도 버프 +{percentValue}% (총 {attackSpeedModifier}%)");
                    break;

                case StatType.ProjectileSpeed:
                    projectileSpeedModifier += percentValue;
                    Debug.Log($"[Character] 투사체속도 버프 +{percentValue}% (총 {projectileSpeedModifier}%)");
                    break;

                case StatType.Range:
                    rangeModifier += percentValue;
                    Debug.Log($"[Character] 사거리 버프 +{percentValue}% (총 {rangeModifier}%)");
                    break;

                case StatType.CritChance:
                    critChanceModifier += percentValue;
                    Debug.Log($"[Character] 치명타 확률 버프 +{percentValue}% (총 {critChanceModifier}%)");
                    break;

                case StatType.CritMultiplier:
                    critMultiplierModifier += percentValue;
                    Debug.Log($"[Character] 치명타 배율 버프 +{percentValue}% (총 {critMultiplierModifier}%)");
                    break;

                case StatType.BonusDamage:
                    bonusDamageModifier += percentValue;
                    Debug.Log($"[Character] 추가 데미지 버프 +{percentValue}% (총 {bonusDamageModifier}%)");
                    break;

                case StatType.HealthRegen:
                    healthRegenModifier += percentValue;
                    Debug.Log($"[Character] 체력 회복 버프 +{percentValue}% (총 {healthRegenModifier}%)");
                    break;

                default:
                    Debug.LogWarning($"[Character] 알 수 없는 StatType: {statType}");
                    break;
            }
        }

        /// <summary>
        /// 일시적 버프 적용 (지속시간 후 해제)
        /// </summary>
        public void ApplyTemporaryBuff(StatType statType, float value, float duration)
        {
            // 버프 적용
            ApplyStatBuff(statType, value);
            Debug.Log($"[Character] Temporary buff applied: {statType} +{value * 100}% for {duration}s");

            // 지속시간 후 해제
            RemoveBuffAfterDurationAsync(statType, value, duration).Forget();
        }

        private async UniTaskVoid RemoveBuffAfterDurationAsync(StatType statType, float value, float duration)
        {
            await UniTask.Delay((int)(duration * 1000));
            ApplyStatBuff(statType, -value); // 버프 해제 (음수로 제거)
            Debug.Log($"[Character] Temporary buff expired: {statType} -{value * 100}%");
        }

        #endregion

        #region Bookmark System (Issue #320)

        /// <summary>
        /// JML: CharacterPlacementManager에서 호출하여 characterId 설정
        /// </summary>
        public void SetCharacterId(int id)
        {
            characterId = id;
        }

        /// <summary>
        /// JML: characterId 가져오기 (설정되지 않으면 프리팹 이름에서 파싱)
        /// Issue #349: 외부 접근용으로 public 변경
        /// </summary>
        public int GetCharacterId()
        {
            if (characterId > 0) return characterId;

            // Fallback: 프리팹 이름에서 파싱 "Character_01_Slot0" → 1
            string objName = gameObject.name;
            if (objName.Contains("Character_"))
            {
                string idPart = objName.Replace("Character_", "").Split('_')[0];
                if (int.TryParse(idPart, out int parsed))
                {
                    return parsed;
                }
            }
            return -1;
        }

        /// <summary>
        /// JML: 장착된 책갈피 적용 (스탯 + 스킬)
        /// 책갈피가 없으면 프리팹 기본값 사용 (하위 호환성)
        /// </summary>
        private void ApplyBookmarksIfAvailable()
        {
            int charId = GetCharacterId();
            Debug.Log($"[Character] ApplyBookmarksIfAvailable 시작 - CharacterID: {charId}");

            if (charId <= 0)
            {
                Debug.LogWarning($"[Character] CharacterID가 유효하지 않음: {charId}");
                return;
            }

            if (BookMarkManager.Instance == null)
            {
                Debug.LogWarning("[Character] BookMarkManager.Instance가 null입니다!");
                return;
            }

            var bookmarks = BookMarkManager.Instance.GetEquippedBookmarksForCharacter(charId);
            Debug.Log($"[Character] GetEquippedBookmarksForCharacter({charId}) 결과: {(bookmarks != null ? bookmarks.Count.ToString() : "null")}개");

            if (bookmarks == null || bookmarks.Count == 0)
            {
                Debug.Log($"[Character] CharacterID {charId}에 장착된 책갈피가 없습니다.");
                return;
            }

            Debug.Log($"[Character] 책갈피 {bookmarks.Count}개 적용 시작 (CharacterID: {charId})");

            foreach (var bookmark in bookmarks)
            {
                if (bookmark.Type == BookmarkType.Stat)
                {
                    ApplyStatBookmark(bookmark);
                }
                else if (bookmark.Type == BookmarkType.Skill)
                {
                    // JML: 스킬 책갈피 적용
                    ApplySkillBookmark(bookmark);
                }
            }
        }

        /// <summary>
        /// JML: 스탯 책갈피 적용 (OptionType에 따라 스탯 변형)
        /// BookmarkOptionTable.csv Option_Type 기준
        /// </summary>
        private void ApplyStatBookmark(BookMark bookmark)
        {
            // OptionValue는 소수 (0.02 = 2%), modifier는 정수 퍼센트 (2 = 2%)
            float percentValue = bookmark.OptionValue * 100f;

            switch (bookmark.OptionType)
            {
                case 1: // 공격력 증가
                    damageModifier += percentValue;
                    Debug.Log($"[Character] 스탯 책갈피: 공격력 +{percentValue:F0}% (총 {damageModifier:F0}%)");
                    break;
                case 2: // 공격 속도 증가
                    attackSpeedModifier += percentValue;
                    Debug.Log($"[Character] 스탯 책갈피: 공격속도 +{percentValue:F0}% (총 {attackSpeedModifier:F0}%)");
                    break;
                case 3: // 치명타 배율 증가
                    critMultiplierModifier += percentValue;
                    Debug.Log($"[Character] 스탯 책갈피: 치명타 배율 +{percentValue:F0}% (총 {critMultiplierModifier:F0}%)");
                    break;
                case 4: // 치명타 확률 증가
                    critChanceModifier += percentValue;
                    Debug.Log($"[Character] 스탯 책갈피: 치명타 확률 +{percentValue:F0}% (총 {critChanceModifier:F0}%)");
                    break;
                case 5: // 치명타 피해 증가 (배율에 누적)
                    critMultiplierModifier += percentValue;
                    Debug.Log($"[Character] 스탯 책갈피: 치명타 피해 +{percentValue:F0}% (총 {critMultiplierModifier:F0}%)");
                    break;
                case 6: // 속성 강화 (GenreType별)
                    ApplyGenreModifier(bookmark);
                    break;
                case 7: // 쿨타임 감소
                    cooldownModifier += percentValue;
                    Debug.Log($"[Character] 스탯 책갈피: 쿨타임 -{percentValue:F0}% (총 {cooldownModifier:F0}%)");
                    break;
                case 8: // 시전 속도 감소
                    castTimeModifier += percentValue;
                    Debug.Log($"[Character] 스탯 책갈피: 시전속도 -{percentValue:F0}% (총 {castTimeModifier:F0}%)");
                    break;
                // case 9: 제거됨 (발사체 속도)
                // case 10, 11, 12: RewardHelper에서 처리 (아이템/골드/파견시간)
                // case 13, 14, 15: 보류 (적 디버프)
                case 16: // 보스 데미지 증가
                    bossDamageModifier += percentValue;
                    Debug.Log($"[Character] 스탯 책갈피: 보스 데미지 +{percentValue:F0}% (총 {bossDamageModifier:F0}%)");
                    break;
                default:
                    // 10, 11, 12는 RewardHelper에서 처리하므로 경고 안 함
                    if (bookmark.OptionType < 10 || bookmark.OptionType > 12)
                    {
                        Debug.LogWarning($"[Character] 미구현 OptionType: {bookmark.OptionType}");
                    }
                    break;
            }
        }

        /// <summary>
        /// JML: 속성(장르) 강화 적용 - Option_Name_ID로 GenreType 구분
        /// </summary>
        private void ApplyGenreModifier(BookMark bookmark)
        {
            // BookmarkOptionTable.csv Option_Name_ID 기준:
            // 80340420: 로맨스, 80340421: 모험, 80340422: 코미디, 80340423: 추리, 80340424: 공포
            GenreType genre = GetGenreFromBookmark(bookmark);

            if (!genreModifiers.ContainsKey(genre))
                genreModifiers[genre] = 0f;

            genreModifiers[genre] += bookmark.OptionValue;
            Debug.Log($"[Character] 스탯 책갈피: {genre} 속성 강화 +{bookmark.OptionValue * 100f:F0}% (총 {genreModifiers[genre] * 100f:F0}%)");
        }

        /// <summary>
        /// JML: BookMark의 Option_Name_ID로 GenreType 결정
        /// BookmarkTable → BookmarkOptionTable → Option_Name_ID 순서로 조회
        /// </summary>
        private GenreType GetGenreFromBookmark(BookMark bookmark)
        {
            // 1. BookmarkTable에서 책갈피 데이터 조회
            var bookmarkData = CSVLoader.Instance?.GetData<BookmarkData>(bookmark.BookmarkDataID);
            if (bookmarkData == null) return GenreType.Horror;

            // 2. BookmarkOptionTable에서 옵션 데이터 조회
            var optionData = CSVLoader.Instance?.GetData<BookmarkOptionData>(bookmarkData.Option_ID);
            if (optionData == null) return GenreType.Horror;

            // 3. Option_Name_ID로 속성 구분 (StringTable 기준)
            return optionData.Option_Name_ID switch
            {
                80340382 => GenreType.Romance,   // 로맨스 속성 강화
                80340383 => GenreType.Adventure, // 모험 속성 강화
                80340384 => GenreType.Comedy,    // 코미디 속성 강화
                80340385 => GenreType.Mystery,   // 추리 속성 강화
                80340386 => GenreType.Horror,    // 공포 속성 강화
                _ => GenreType.Horror
            };
        }

        /// <summary>
        /// JML: 스킬 책갈피 적용 (MainSkill → activeSkillId)
        /// </summary>
        private void ApplySkillBookmark(BookMark bookmark)
        {
            if (bookmark.SkillID <= 0) return;

            // MainSkillData 확인
            var mainSkill = CSVLoader.Instance?.GetData<MainSkillData>(bookmark.SkillID);
            if (mainSkill != null)
            {
                if (activeSkillId == 0) // 비어있을 때만 설정
                {
                    activeSkillId = bookmark.SkillID;
                    Debug.Log($"[Character] 스킬 책갈피: activeSkillId = {bookmark.SkillID} ({mainSkill.skill_name})");
                }
            }
        }

        #endregion

        #region Genre System (상성 시스템)

        /// <summary>
        /// 캐릭터 장르 반환 (상성 계산용)
        /// CharacterData의 Genre를 CSVLoader에서 조회
        /// </summary>
        public Genre GetGenre()
        {
            int charId = GetCharacterId();
            if (charId <= 0 || CSVLoader.Instance == null)
            {
                return Genre.Horror; // 기본값
            }

            var charData = CSVLoader.Instance.GetData<CharacterData>(charId);
            if (charData == null)
            {
                return Genre.Horror; // 기본값
            }

            return charData.Genre;
        }

        /// <summary>
        /// 파티 시너지 장르 버프 적용
        /// PartySynergyManager에서 호출
        /// </summary>
        /// <param name="genre">강화할 장르</param>
        /// <param name="value">버프 값 (0.05 = 5%)</param>
        public void ApplyGenreSynergyBuff(Genre genre, float value)
        {
            // Genre → GenreType 변환 (같은 값이므로 직접 캐스팅)
            GenreType genreType = (GenreType)(int)genre;

            if (!genreModifiers.ContainsKey(genreType))
                genreModifiers[genreType] = 0f;

            genreModifiers[genreType] += value;
            Debug.Log($"[Character] 파티 시너지: {genreType} 속성 강화 +{value * 100f:F0}% (총 {genreModifiers[genreType] * 100f:F0}%)");
        }

        /// <summary>
        /// 특정 장르에 대한 현재 modifier 값 반환 (디버그용)
        /// </summary>
        public float GetGenreModifier(Genre genre)
        {
            GenreType genreType = (GenreType)(int)genre;
            return genreModifiers.TryGetValue(genreType, out float value) ? value : 0f;
        }

        #endregion

        #region Public Stat Getters (Issue #424 - ChaCard UI용)

        /// <summary>
        /// JML: UI용 최종 공격력 반환 (Issue #424)
        /// Character.SkillData.cs의 FinalDamage 참조
        /// </summary>
        public float GetDisplayDamage()
        {
            if (basicAttackData == null) return 0f;
            return FinalDamage;
        }

        /// <summary>
        /// JML: UI용 최종 공격속도 반환 (Issue #424)
        /// Character.SkillData.cs의 FinalAttackSpeed 참조
        /// </summary>
        public float GetDisplayAttackSpeed()
        {
            if (basicAttackData == null) return 1f;
            return FinalAttackSpeed;
        }

        /// <summary>
        /// JML: UI용 치명타 확률 반환 (Issue #424)
        /// 기본값 5% + modifier
        /// </summary>
        public float GetDisplayCritChance()
        {
            return 5f + critChanceModifier; // 기본 5% + 추가 %
        }

        /// <summary>
        /// JML: UI용 치명타 배율 반환 (Issue #424)
        /// 기본값 150% + modifier
        /// </summary>
        public float GetDisplayCritMultiplier()
        {
            return 150f + critMultiplierModifier; // 기본 150% + 추가 %
        }

        /// <summary>
        /// JML: UI용 장착 스킬 이름 반환 (Issue #424)
        /// </summary>
        public string GetDisplaySkillName()
        {
            if (supportData != null)
            {
                return supportData.support_name;
            }
            return "없음";
        }

        #endregion
    }
}
