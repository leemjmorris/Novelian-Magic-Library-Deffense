using System;
using System.Collections.Generic;

namespace Firebase.Data
{
    /// <summary>
    /// Firebase Realtime Database에 저장되는 유저 데이터 구조
    /// </summary>
    [Serializable]
    public class UserData
    {
        public string nickname;
        public string lastUpdated;
        public CurrencySaveData currencies;
        public ProgressionData progression;
        public CharacterSaveData characters;
        public DeckData deck;
        public Dictionary<string, int> ingredients;
        public BookmarkSaveData bookmarks;
        public DispatchData dispatch;
        public PartySynergySaveData partySynergies;
        public ProfileSaveData profile;

        public UserData()
        {
            nickname = "";
            lastUpdated = DateTime.UtcNow.ToString("o");
            currencies = new CurrencySaveData();
            progression = new ProgressionData();
            characters = new CharacterSaveData();
            deck = new DeckData();
            ingredients = new Dictionary<string, int>();
            bookmarks = new BookmarkSaveData();
            dispatch = new DispatchData();
            partySynergies = new PartySynergySaveData();
            profile = new ProfileSaveData();
        }

        /// <summary>
        /// 기본값으로 초기화된 새 유저 데이터 생성
        /// </summary>
        public static UserData CreateDefault()
        {
            var data = new UserData();

            // 기본 캐릭터 지급
            data.characters.owned = new Dictionary<string, bool>
            {
                { "22007", true },
                { "24013", true },
                { "25017", true }
            };

            // 기본 덱 설정 (프리셋 0에 설정)
            data.deck.currentPreset = 0;
            data.deck.preset0.slot0 = 22007;
            data.deck.preset0.slot1 = 24013;
            data.deck.preset0.slot2 = 25017;
            data.deck.preset0.slot3 = -1;

            return data;
        }
    }

    [Serializable]
    public class CurrencySaveData
    {
        public int gold;           // 1601: 골드
        public int exp;            // 1602: 경험치
        public int application;    // 1603: 지원서
        public int recommendation; // 1604: 추천서
        public int magicStone;     // 1605: 마석
        public int dungeonPass;    // 1606: 던전 출입증
        public int ap;             // 1607: 행동력
        public long apLastSyncTimeMs; // AP 마지막 동기화 시간 (서버 시간, 밀리초)
        public long dungeonPassLastSyncTimeMs; // 던전 출입증 마지막 동기화 시간 (서버 시간, 밀리초)

        // 하위 호환용 (마이그레이션용, deprecated)
        public string apRecoveryTime;

        public CurrencySaveData()
        {
            gold = 0;
            exp = 0;
            application = 0;
            recommendation = 0;
            magicStone = 0;
            dungeonPass = 5; // 기본 던전 출입증 5개
            ap = 30; // 기본 AP
            apLastSyncTimeMs = 0;
            dungeonPassLastSyncTimeMs = 0;
            apRecoveryTime = "";
        }
    }

    [Serializable]
    public class ProgressionData
    {
        public int highestClearedStage;
        public int playerLevel;
        public int playerExp;
        public int bossDungeonProgress; // Issue #476: 도전던전 해금된 최대 층
        public long totalKilledMonsters; // 총 처치한 몬스터 수 (랭킹용)
        public Dictionary<string, int> stageRanks; // Issue #645: 스테이지별 클리어 랭크 (stageNumber → rankIndex: 0=S, 1=A, 2=B, 3=F)
        public Dictionary<string, bool> bossDungeonAttempted; // Issue #645: 도전던전 시도 기록 (floorIndex → true: 시도했으나 미클리어)

        public ProgressionData()
        {
            highestClearedStage = 0;
            playerLevel = 1;
            playerExp = 0;
            bossDungeonProgress = 1; // 1층부터 시작
            totalKilledMonsters = 0;
            stageRanks = new Dictionary<string, int>();
            bossDungeonAttempted = new Dictionary<string, bool>();
        }
    }

    [Serializable]
    public class CharacterSaveData
    {
        public Dictionary<string, bool> owned;
        public Dictionary<string, int> enhancements;

        public CharacterSaveData()
        {
            owned = new Dictionary<string, bool>();
            enhancements = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// 단일 프리셋의 덱 데이터
    /// </summary>
    [Serializable]
    public class DeckPresetData
    {
        public int slot0;
        public int slot1;
        public int slot2;
        public int slot3;

        public DeckPresetData()
        {
            slot0 = -1;
            slot1 = -1;
            slot2 = -1;
            slot3 = -1;
        }

        public List<int> ToList()
        {
            return new List<int> { slot0, slot1, slot2, slot3 };
        }

        public void FromList(List<int> list)
        {
            slot0 = list.Count > 0 ? list[0] : -1;
            slot1 = list.Count > 1 ? list[1] : -1;
            slot2 = list.Count > 2 ? list[2] : -1;
            slot3 = list.Count > 3 ? list[3] : -1;
        }

        public bool IsEmpty()
        {
            return slot0 <= 0 && slot1 <= 0 && slot2 <= 0 && slot3 <= 0;
        }

        public int GetValidCount()
        {
            int count = 0;
            if (slot0 > 0) count++;
            if (slot1 > 0) count++;
            if (slot2 > 0) count++;
            if (slot3 > 0) count++;
            return count;
        }

        public List<int> GetValidCharacters()
        {
            var list = new List<int>();
            if (slot0 > 0) list.Add(slot0);
            if (slot1 > 0) list.Add(slot1);
            if (slot2 > 0) list.Add(slot2);
            if (slot3 > 0) list.Add(slot3);
            return list;
        }

        public void Clear()
        {
            slot0 = -1;
            slot1 = -1;
            slot2 = -1;
            slot3 = -1;
        }

        public void CopyFrom(DeckPresetData other)
        {
            if (other == null) return;
            slot0 = other.slot0;
            slot1 = other.slot1;
            slot2 = other.slot2;
            slot3 = other.slot3;
        }
    }

    /// <summary>
    /// 덱 데이터 (4개 프리셋 지원)
    /// </summary>
    [Serializable]
    public class DeckData
    {
        public int currentPreset;  // 현재 선택된 프리셋 인덱스 (0~3)
        public DeckPresetData preset0;
        public DeckPresetData preset1;
        public DeckPresetData preset2;
        public DeckPresetData preset3;

        // 하위 호환용 (기존 데이터 마이그레이션용, deprecated)
        public int slot0;
        public int slot1;
        public int slot2;
        public int slot3;

        public DeckData()
        {
            currentPreset = 0;
            preset0 = new DeckPresetData();
            preset1 = new DeckPresetData();
            preset2 = new DeckPresetData();
            preset3 = new DeckPresetData();

            // 하위 호환용 기본값
            slot0 = -1;
            slot1 = -1;
            slot2 = -1;
            slot3 = -1;
        }

        /// <summary>
        /// 현재 선택된 프리셋 데이터 반환
        /// </summary>
        public DeckPresetData GetCurrentPreset()
        {
            return GetPreset(currentPreset);
        }

        /// <summary>
        /// 특정 인덱스의 프리셋 데이터 반환
        /// </summary>
        public DeckPresetData GetPreset(int index)
        {
            return index switch
            {
                0 => preset0,
                1 => preset1,
                2 => preset2,
                3 => preset3,
                _ => preset0
            };
        }

        /// <summary>
        /// 특정 인덱스의 프리셋 데이터 설정
        /// </summary>
        public void SetPreset(int index, DeckPresetData data)
        {
            switch (index)
            {
                case 0: preset0 = data; break;
                case 1: preset1 = data; break;
                case 2: preset2 = data; break;
                case 3: preset3 = data; break;
            }
        }

        /// <summary>
        /// 하위 호환용: 현재 프리셋을 List로 반환
        /// </summary>
        public List<int> ToList()
        {
            return GetCurrentPreset().ToList();
        }

        /// <summary>
        /// 하위 호환용: List로부터 현재 프리셋 설정
        /// </summary>
        public void FromList(List<int> list)
        {
            GetCurrentPreset().FromList(list);
        }

        /// <summary>
        /// 기존 데이터(slot0~3)가 있는지 확인 (마이그레이션 필요 여부)
        /// </summary>
        public bool HasLegacyData()
        {
            return slot0 > 0 || slot1 > 0 || slot2 > 0 || slot3 > 0;
        }

        /// <summary>
        /// 기존 데이터를 preset0으로 마이그레이션
        /// </summary>
        public void MigrateLegacyData()
        {
            if (!HasLegacyData()) return;

            preset0.slot0 = slot0;
            preset0.slot1 = slot1;
            preset0.slot2 = slot2;
            preset0.slot3 = slot3;
            currentPreset = 0;

            // 마이그레이션 후 레거시 데이터 초기화
            slot0 = -1;
            slot1 = -1;
            slot2 = -1;
            slot3 = -1;
        }
    }

    [Serializable]
    public class BookmarkSaveData
    {
        public int nextId;
        public Dictionary<string, BookmarkItemData> items;

        public BookmarkSaveData()
        {
            nextId = 1;
            items = new Dictionary<string, BookmarkItemData>();
        }
    }

    [Serializable]
    public class BookmarkItemData
    {
        public int dataId;
        public string name;
        public int grade;
        public int type;
        public int optionType;
        public float optionValue;
        public int skillId;
        public string createdTime;
        public int equippedCharacterId;
        public int equipSlotIndex;

        public BookmarkItemData()
        {
            dataId = 0;
            name = "";
            grade = 0;
            type = 0;
            optionType = 0;
            optionValue = 0f;
            skillId = -1;
            createdTime = DateTime.UtcNow.ToString("o");
            equippedCharacterId = -1;
            equipSlotIndex = -1;
        }
    }

    [Serializable]
    public class DispatchData
    {
        public DispatchStateData combat;
        public DispatchStateData gathering;

        public DispatchData()
        {
            combat = new DispatchStateData();
            gathering = new DispatchStateData();
        }
    }

    [Serializable]
    public class DispatchStateData
    {
        public bool isActive;
        public int locationId;
        public int hours;
        public long startTimeMs;  // 파견 시작 시간 (서버 시간, 밀리초)
        public long endTimeMs;    // 파견 종료 시간 (서버 시간, 밀리초)

        // 하위 호환용 (마이그레이션용, deprecated)
        public string startTime;
        public string endTime;
        public int presetIndex; // 파견에 사용 중인 프리셋 인덱스 (0~3, -1은 미사용)

        public DispatchStateData()
        {
            isActive = false;
            locationId = 0;
            hours = 0;
            startTimeMs = 0;
            endTimeMs = 0;
            startTime = "";
            endTime = "";
        }

        /// <summary>
        /// 레거시 데이터(string)가 있는지 확인
        /// </summary>
        public bool HasLegacyData()
        {
            return !string.IsNullOrEmpty(startTime) || !string.IsNullOrEmpty(endTime);
        }

        /// <summary>
        /// 레거시 데이터를 새 형식으로 마이그레이션
        /// ServerTimeManager가 초기화된 후 호출해야 함
        /// </summary>
        public void MigrateLegacyData()
        {
            if (!HasLegacyData()) return;

            // 레거시 string 시간을 파싱하여 밀리초로 변환
            if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime, out DateTime startDt))
            {
                startTimeMs = new DateTimeOffset(startDt.ToUniversalTime()).ToUnixTimeMilliseconds();
            }

            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime, out DateTime endDt))
            {
                endTimeMs = new DateTimeOffset(endDt.ToUniversalTime()).ToUnixTimeMilliseconds();
            }

            // 마이그레이션 후 레거시 데이터 초기화
            startTime = "";
            endTime = "";
            presetIndex = -1;
        }
    }

    [Serializable]
    public class PartySynergySaveData
    {
        public Dictionary<string, int> levels; // Party_ID → Level

        public PartySynergySaveData()
        {
            levels = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// 프로필 사진/프레임/칭호 저장 데이터
    /// </summary>
    [Serializable]
    public class ProfileSaveData
    {
        public int equippedPictureId;  // 장착된 프로필 사진 ID (캐릭터 ID 기반)
        public int equippedFrameId;    // 장착된 프레임 ID (1~12)
        public int equippedTitleIndex; // 장착된 칭호 인덱스 (Dropdown 옵션 인덱스)

        public ProfileSaveData()
        {
            equippedPictureId = -1;  // -1 = 장착 안 함
            equippedFrameId = -1;    // -1 = 장착 안 함
            equippedTitleIndex = 0;  // 0 = 첫 번째 칭호 (기본값)
        }
    }
}
