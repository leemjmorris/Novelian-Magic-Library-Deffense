using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Data;
using Cysharp.Threading.Tasks;

/// <summary>
/// JML: Bookmark Manager (Singleton)
/// 책갈피 매니저 (싱글톤)
/// </summary>
public class BookMarkManager : MonoBehaviour
{
    private static BookMarkManager instance;
    public static BookMarkManager Instance => instance;

    // JML: Owned Bookmarks
    // 보유한 모든 책갈피
    private List<BookMark> ownedBookmarks = new List<BookMark>();

    // JML: 책갈피 추가 이벤트 (UI 갱신용)
    public event System.Action<BookMark> OnBookmarkAdded;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// JML: Add Bookmark (called after crafting or from librarian management system)
    /// 책갈피 추가 (제작 완료 후 or 사서 관리 시스템에서 호출)
    /// </summary>
    public void AddBookmark(BookMark bookmark)
    {
        if (bookmark == null)
        {
            GameLog.LogError("[BookMarkManager] null 책갈피를 추가할 수 없습니다!");
            return;
        }

        ownedBookmarks.Add(bookmark);
        GameLog.Log($"[BookMarkManager] 책갈피 추가: {bookmark}");

        // JML: 이벤트 발생 (UI 갱신용)
        OnBookmarkAdded?.Invoke(bookmark);

        // Firebase에 저장
        SaveToFirebase();
    }

    /// <summary>
    /// JML: Remove Bookmark (for deletion)
    /// 책갈피 제거 (삭제 시 사용)
    /// </summary>
    public bool RemoveBookmark(int uniqueID)
    {
        for (int i = 0; i < ownedBookmarks.Count; i++)
        {
            if (ownedBookmarks[i].UniqueID == uniqueID)
            {
                if (ownedBookmarks[i].IsEquipped)
                {
                    GameLog.LogWarning($"[BookMarkManager] 장착된 책갈피는 제거할 수 없습니다: {ownedBookmarks[i]}");
                    return false;
                }

                GameLog.Log($"[BookMarkManager] 책갈피 제거: {ownedBookmarks[i]}");
                ownedBookmarks.RemoveAt(i);
                SaveToFirebase();
                return true;
            }
        }

        GameLog.LogWarning($"[BookMarkManager] 존재하지 않는 책갈피 ID: {uniqueID}");
        return false;
    }

    /// <summary>
    /// 현재 책갈피 상태를 Firebase에 저장
    /// </summary>
    private void SaveToFirebase()
    {
        if (FirebaseSaveManager.Instance == null || !FirebaseSaveManager.Instance.IsInitialized)
            return;

        if (FirebaseManager.Instance == null || string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserId))
            return;

        var data = ToBookmarkSaveData();
        FirebaseSaveManager.Instance.SaveBookmarksAsync(
            FirebaseManager.Instance.CurrentUserId,
            data
        ).Forget();
    }

    /// <summary>
    /// JML: Get All Bookmarks
    /// 모든 책갈피 가져오기
    /// </summary>
    public List<BookMark> GetAllBookmarks()
    {
        return new List<BookMark>(ownedBookmarks);
    }

    /// <summary>
    /// JML: Get Bookmark by UniqueID
    /// 특정 UniqueID의 책갈피 가져오기
    /// </summary>
    public BookMark GetBookmark(int uniqueID)
    {
        for (int i = 0; i < ownedBookmarks.Count; i++)
        {
            if (ownedBookmarks[i].UniqueID == uniqueID)
            {
                return ownedBookmarks[i];
            }
        }
        return null;
    }

    /// <summary>
    /// JML: Get Unequipped Bookmarks (for librarian management UI)
    /// 미장착 책갈피만 가져오기 (사서 관리 UI에서 사용)
    /// </summary>
    public List<BookMark> GetUnequippedBookmarks()
    {
        List<BookMark> result = new List<BookMark>();
        for (int i = 0; i < ownedBookmarks.Count; i++)
        {
            if (!ownedBookmarks[i].IsEquipped)
            {
                result.Add(ownedBookmarks[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// JML: Get Equipped Bookmarks for specific librarian (for librarian management UI)
    /// 특정 사서에게 장착된 책갈피 가져오기 (사서 관리 UI에서 사용)
    /// </summary>
    public List<BookMark> GetEquippedBookmarks(int librarianID)
    {
        List<BookMark> result = new List<BookMark>();
        for (int i = 0; i < ownedBookmarks.Count; i++)
        {
            if (ownedBookmarks[i].IsEquipped && ownedBookmarks[i].EquippedLibrarianID == librarianID)
            {
                result.Add(ownedBookmarks[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// JML: Filter by Grade
    /// 등급별 필터링
    /// </summary>
    public List<BookMark> GetBookmarksByGrade(Grade grade)
    {
        List<BookMark> result = new List<BookMark>();
        for (int i = 0; i < ownedBookmarks.Count; i++)
        {
            if (ownedBookmarks[i].Grade == grade)
            {
                result.Add(ownedBookmarks[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// JML: Filter by Option Type
    /// 옵션 타입별 필터링
    /// </summary>
    public List<BookMark> GetBookmarksByOptionType(int optionType)
    {
        List<BookMark> result = new List<BookMark>();
        for (int i = 0; i < ownedBookmarks.Count; i++)
        {
            if (ownedBookmarks[i].OptionType == optionType)
            {
                result.Add(ownedBookmarks[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// JML: Filter by Bookmark Type (Stat/Skill)
    /// 북마크 타입별 필터링 (스탯/스킬)
    /// </summary>
    public List<BookMark> GetBookmarksByType(BookmarkType type)
    {
        List<BookMark> result = new List<BookMark>();
        for (int i = 0; i < ownedBookmarks.Count; i++)
        {
            if (ownedBookmarks[i].Type == type)
            {
                result.Add(ownedBookmarks[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// JML: Get Stat Bookmarks Only
    /// 스탯 북마크만 가져오기
    /// </summary>
    public List<BookMark> GetStatBookmarks()
    {
        return GetBookmarksByType(BookmarkType.Stat);
    }

    /// <summary>
    /// JML: Get Skill Bookmarks Only
    /// 스킬 북마크만 가져오기
    /// </summary>
    public List<BookMark> GetSkillBookmarks()
    {
        return GetBookmarksByType(BookmarkType.Skill);
    }

    #region Character Bookmark Equipment Management

    // 캐릭터별 장착된 책갈피 배열 (5개 슬롯)
    private Dictionary<int, BookMark[]> characterBookmarks = new Dictionary<int, BookMark[]>();

    /// <summary>
    /// 캐릭터의 책갈피 배열 가져오기 (없으면 생성)
    /// </summary>
    private BookMark[] GetOrCreateBookmarkArray(int characterID)
    {
        if (!characterBookmarks.ContainsKey(characterID))
        {
            characterBookmarks[characterID] = new BookMark[5];
        }
        return characterBookmarks[characterID];
    }

    /// <summary>
    /// 책갈피를 캐릭터에 장착 (이미 장착된 경우 교체)
    /// </summary>
    public bool EquipBookmarkToCharacter(int characterID, BookMark bookmark, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 5)
        {
            GameLog.LogError($"[BookMarkManager] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }

        if (bookmark == null)
        {
            GameLog.LogError("[BookMarkManager] null 책갈피는 장착할 수 없습니다.");
            return false;
        }

        // 다른 곳에 이미 장착된 책갈피인지 확인
        if (bookmark.IsEquipped)
        {
            GameLog.LogWarning($"[BookMarkManager] 책갈피 {bookmark.Name}는 이미 다른 곳에 장착되어 있습니다.");
            return false;
        }

        BookMark[] bookmarks = GetOrCreateBookmarkArray(characterID);

        // 기존 책갈피가 있으면 먼저 해제
        if (bookmarks[slotIndex] != null)
        {
            BookMark oldBookmark = bookmarks[slotIndex];
            oldBookmark.Unequip();
            GameLog.Log($"[BookMarkManager] 슬롯 {slotIndex}에서 기존 책갈피 해제: {oldBookmark.Name}");
        }

        // 새 책갈피 장착
        bookmarks[slotIndex] = bookmark;
        bookmark.Equip(characterID, slotIndex);

        GameLog.Log($"[BookMarkManager] 책갈피 장착: {bookmark.Name} (슬롯 {slotIndex})");
        SaveToFirebase();
        return true;
    }

    /// <summary>
    /// 특정 캐릭터의 특정 슬롯의 책갈피 가져오기
    /// </summary>
    public BookMark GetCharacterBookmarkAtSlot(int characterID, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 5) return null;

        BookMark[] bookmarks = GetOrCreateBookmarkArray(characterID);
        return bookmarks[slotIndex];
    }

    /// <summary>
    /// LCB: Unequip bookmark from character slot
    /// 캐릭터 슬롯에서 책갈피 해제
    /// </summary>
    public bool UnequipBookmarkFromCharacter(int characterID, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 5)
        {
            GameLog.LogError($"[BookMarkManager] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }

        BookMark[] bookmarks = GetOrCreateBookmarkArray(characterID);

        // LCB: Check if there's a bookmark in the slot (슬롯에 책갈피가 있는지 확인)
        if (bookmarks[slotIndex] == null)
        {
            GameLog.LogWarning($"[BookMarkManager] 슬롯 {slotIndex}에 장착된 책갈피가 없습니다.");
            return false;
        }

        BookMark bookmarkToUnequip = bookmarks[slotIndex];

        // LCB: Unequip the bookmark (책갈피 해제)
        bookmarkToUnequip.Unequip();
        bookmarks[slotIndex] = null;

        GameLog.Log($"[BookMarkManager] 책갈피 해제: {bookmarkToUnequip.Name} (슬롯 {slotIndex})");
        SaveToFirebase();
        return true;
    }

    /// <summary>
    /// 캐릭터에게 실제로 장착된 책갈피만 가져오기 (null 제외)
    /// 스탯/스킬 적용 시 사용
    /// </summary>
    public List<BookMark> GetEquippedBookmarksForCharacter(int characterID)
    {
        GameLog.Log($"[BookMarkManager] GetEquippedBookmarksForCharacter({characterID}) 호출");
        GameLog.Log($"[BookMarkManager] characterBookmarks 딕셔너리 키 목록: [{string.Join(", ", characterBookmarks.Keys)}]");

        BookMark[] bookmarks = GetOrCreateBookmarkArray(characterID);
        List<BookMark> result = new List<BookMark>();

        for (int i = 0; i < bookmarks.Length; i++)
        {
            if (bookmarks[i] != null)
            {
                result.Add(bookmarks[i]);
                GameLog.Log($"[BookMarkManager] 슬롯 {i}: {bookmarks[i].Name} (Type: {bookmarks[i].Type}, SkillID: {bookmarks[i].SkillID})");
            }
        }

        GameLog.Log($"[BookMarkManager] CharacterID {characterID}에 장착된 책갈피: {result.Count}개");
        return result;
    }

    /// <summary>
    /// JML: 캐릭터에 장착된 특정 타입 책갈피 개수 확인
    /// </summary>
    public int GetEquippedBookmarkCountByType(int characterID, BookmarkType type)
    {
        BookMark[] bookmarks = GetOrCreateBookmarkArray(characterID);
        int count = 0;

        for (int i = 0; i < bookmarks.Length; i++)
        {
            if (bookmarks[i] != null && bookmarks[i].Type == type)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// JML: 책갈피 타입별 최대 장착 개수
    /// Stat: 4개, Skill: 1개
    /// </summary>
    public int GetMaxBookmarkCountByType(BookmarkType type)
    {
        return type switch
        {
            BookmarkType.Stat => 4,
            BookmarkType.Skill => 1,
            _ => 0
        };
    }

    /// <summary>
    /// JML: 해당 타입 책갈피를 더 장착할 수 있는지 확인
    /// </summary>
    public bool CanEquipBookmarkType(int characterID, BookmarkType type)
    {
        int currentCount = GetEquippedBookmarkCountByType(characterID, type);
        int maxCount = GetMaxBookmarkCountByType(type);
        return currentCount < maxCount;
    }

    #endregion

    #region Firebase Save/Load

    /// <summary>
    /// Firebase 데이터로 책갈피 설정 (BootScene에서 호출)
    /// </summary>
    public void SetBookmarksFromFirebase(BookmarkSaveData data)
    {
        if (data == null) return;

        // 기존 데이터 초기화
        ownedBookmarks.Clear();
        characterBookmarks.Clear();

        // 다음 고유 ID 설정
        BookMark.SetNextUniqueID(data.nextId);

        // 책갈피 복원
        if (data.items != null)
        {
            foreach (var kvp in data.items)
            {
                if (int.TryParse(kvp.Key, out int uniqueId))
                {
                    var item = kvp.Value;

                    // 생성 시간 파싱
                    DateTime createdTime = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(item.createdTime))
                    {
                        DateTime.TryParse(item.createdTime, out createdTime);
                    }

                    // 책갈피 생성
                    var bookmark = new BookMark(
                        uniqueId,
                        item.dataId,
                        item.name,
                        (Grade)item.grade,
                        (BookmarkType)item.type,
                        item.optionType,
                        item.optionValue,
                        item.skillId,
                        createdTime,
                        item.equippedCharacterId,
                        item.equipSlotIndex
                    );

                    ownedBookmarks.Add(bookmark);

                    // 장착 상태 복원
                    if (item.equippedCharacterId >= 0 && item.equipSlotIndex >= 0)
                    {
                        BookMark[] bookmarks = GetOrCreateBookmarkArray(item.equippedCharacterId);
                        if (item.equipSlotIndex < bookmarks.Length)
                        {
                            bookmarks[item.equipSlotIndex] = bookmark;
                            GameLog.Log($"<color=#3EB489>[BookMarkManager]</color> 장착 상태 복원: CharacterID={item.equippedCharacterId}, 슬롯={item.equipSlotIndex}, 책갈피={bookmark.Name}, SkillID={bookmark.SkillID}");
                        }
                    }
                }
            }
        }

        GameLog.Log($"<color=#3EB489>[BookMarkManager]</color> Firebase에서 책갈피 로드: {ownedBookmarks.Count}개");
        GameLog.Log($"<color=#3EB489>[BookMarkManager]</color> characterBookmarks 딕셔너리 키 목록: [{string.Join(", ", characterBookmarks.Keys)}]");
    }

    /// <summary>
    /// 모든 책갈피를 Firebase 저장용 데이터로 변환
    /// </summary>
    public BookmarkSaveData ToBookmarkSaveData()
    {
        var data = new BookmarkSaveData();
        data.items = new Dictionary<string, BookmarkItemData>();

        int maxId = 0;
        foreach (var bookmark in ownedBookmarks)
        {
            var itemData = new BookmarkItemData
            {
                dataId = bookmark.BookmarkDataID,
                name = bookmark.Name,
                grade = (int)bookmark.Grade,
                type = (int)bookmark.Type,
                optionType = bookmark.OptionType,
                optionValue = bookmark.OptionValue,
                skillId = bookmark.SkillID,
                createdTime = bookmark.CreatedTime.ToString("o"),
                equippedCharacterId = bookmark.EquippedLibrarianID,
                equipSlotIndex = bookmark.EquipSlotIndex
            };

            data.items[bookmark.UniqueID.ToString()] = itemData;

            if (bookmark.UniqueID > maxId)
                maxId = bookmark.UniqueID;
        }

        data.nextId = maxId + 1;
        return data;
    }

    #endregion

    // 사용법
    // // 캐릭터 ID로 장착된 모든 책갈피 가져오기 (null 제외)
    // List<BookMark> equippedBookmarks = BookMarkManager.Instance.GetEquippedBookmarksForCharacter(characterID);

    // // 스탯/스킬 적용
    // for (int i = 0; i<equippedBookmarks.Count; i++)
    // {
    //     BookMark bookmark = equippedBookmarks[i];
        
    //     if (bookmark.Type == BookmarkType.Stat)
    //     {
    //         // 스탯 적용 로직
    //     }
    //     else if (bookmark.Type == BookmarkType.Skill)
    //     {
    //         // 스킬 적용 로직
    //     }
    // }

    #region Reward Modifier Methods (Option_Type 10, 11, 12)

    /// <summary>
    /// 아이템 획득 확률 modifier 합계 (Option_Type 10)
    /// 모든 캐릭터에 장착된 책갈피의 합계
    /// </summary>
    public float GetItemDropModifier()
    {
        return GetTotalModifierByOptionType(10);
    }

    /// <summary>
    /// 골드 획득량 modifier 합계 (Option_Type 11)
    /// 모든 캐릭터에 장착된 책갈피의 합계
    /// </summary>
    public float GetGoldModifier()
    {
        return GetTotalModifierByOptionType(11);
    }

    /// <summary>
    /// 파견 시간 감소 modifier 합계 (Option_Type 12)
    /// 모든 캐릭터에 장착된 책갈피의 합계
    /// </summary>
    public float GetDispatchTimeModifier()
    {
        return GetTotalModifierByOptionType(12);
    }

    /// <summary>
    /// 특정 OptionType의 모든 장착된 책갈피 modifier 합계
    /// </summary>
    private float GetTotalModifierByOptionType(int optionType)
    {
        float total = 0f;

        // 모든 캐릭터에 장착된 책갈피 순회
        foreach (var kvp in characterBookmarks)
        {
            BookMark[] bookmarks = kvp.Value;
            for (int i = 0; i < bookmarks.Length; i++)
            {
                if (bookmarks[i] != null &&
                    bookmarks[i].Type == BookmarkType.Stat &&
                    bookmarks[i].OptionType == optionType)
                {
                    total += bookmarks[i].OptionValue;
                }
            }
        }

        return total;
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// JML: Add Test Stat Bookmark
    /// 테스트용 스탯 책갈피 추가
    /// </summary>
    [ContextMenu("테스트 스탯 책갈피 추가")]
    private void AddTestStatBookmark()
    {
        var testBookmark = new BookMark(
            bookmarkDataID: 1001,
            name: "테스트 스탯 책갈피",
            grade: Grade.Common,
            optionType: 1,
            optionValue: 10.5f
        );
        AddBookmark(testBookmark);
    }

    /// <summary>
    /// JML: Add Test Skill Bookmark
    /// 테스트용 스킬 책갈피 추가
    /// </summary>
    [ContextMenu("테스트 스킬 책갈피 추가")]
    private void AddTestSkillBookmark()
    {
        var testBookmark = new BookMark(
            bookmarkDataID: 1111,
            name: "체인 러브 쇼크",
            grade: Grade.Common,
            optionType: 1,
            optionValue: 1,
            skillID: 3121
        );
        AddBookmark(testBookmark);
    }

    /// <summary>
    /// JML: Print All Bookmarks
    /// 보유 책갈피 목록 출력
    /// </summary>
    [ContextMenu("보유 책갈피 목록 출력")]
    private void PrintAllBookmarks()
    {
        GameLog.Log($"=== 보유 책갈피 목록 ({ownedBookmarks.Count}개) ===");
        for (int i = 0; i < ownedBookmarks.Count; i++)
        {
            GameLog.Log(ownedBookmarks[i].ToString());
        }
    }

    #endregion
}
