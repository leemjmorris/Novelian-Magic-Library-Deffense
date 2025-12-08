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
        public string lastUpdated;
        public CurrencySaveData currencies;
        public ProgressionData progression;
        public CharacterSaveData characters;
        public DeckData deck;
        public Dictionary<string, int> ingredients;
        public BookmarkSaveData bookmarks;
        public DispatchData dispatch;

        public UserData()
        {
            lastUpdated = DateTime.UtcNow.ToString("o");
            currencies = new CurrencySaveData();
            progression = new ProgressionData();
            characters = new CharacterSaveData();
            deck = new DeckData();
            ingredients = new Dictionary<string, int>();
            bookmarks = new BookmarkSaveData();
            dispatch = new DispatchData();
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

            // 기본 덱 설정
            data.deck.slot0 = 22007;
            data.deck.slot1 = 24013;
            data.deck.slot2 = 25017;
            data.deck.slot3 = -1;

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
        public int ap;             // 1607: 행동력
        public string apRecoveryTime; // AP 회복 시간

        public CurrencySaveData()
        {
            gold = 0;
            exp = 0;
            application = 0;
            recommendation = 0;
            magicStone = 0;
            ap = 30; // 기본 AP
            apRecoveryTime = DateTime.UtcNow.ToString("o");
        }
    }

    [Serializable]
    public class ProgressionData
    {
        public int highestClearedStage;
        public int playerLevel;
        public int playerExp;

        public ProgressionData()
        {
            highestClearedStage = 0;
            playerLevel = 1;
            playerExp = 0;
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

    [Serializable]
    public class DeckData
    {
        public int slot0;
        public int slot1;
        public int slot2;
        public int slot3;

        public DeckData()
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
        public string startTime;
        public string endTime;

        public DispatchStateData()
        {
            isActive = false;
            locationId = 0;
            hours = 0;
            startTime = "";
            endTime = "";
        }
    }
}
