using System.Collections.Generic;
using UnityEngine;

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
            Debug.LogError("[BookMarkManager] null 책갈피를 추가할 수 없습니다!");
            return;
        }

        ownedBookmarks.Add(bookmark);
        Debug.Log($"[BookMarkManager] 책갈피 추가: {bookmark}");
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
                    Debug.LogWarning($"[BookMarkManager] 장착된 책갈피는 제거할 수 없습니다: {ownedBookmarks[i]}");
                    return false;
                }

                Debug.Log($"[BookMarkManager] 책갈피 제거: {ownedBookmarks[i]}");
                ownedBookmarks.RemoveAt(i);
                return true;
            }
        }

        Debug.LogWarning($"[BookMarkManager] 존재하지 않는 책갈피 ID: {uniqueID}");
        return false;
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
            bookmarkDataID: 2001,
            name: "테스트 스킬 책갈피",
            grade: Grade.Rare,
            optionType: 1,
            optionValue: 5,
            effectID: 101
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
        Debug.Log($"=== 보유 책갈피 목록 ({ownedBookmarks.Count}개) ===");
        for (int i = 0; i < ownedBookmarks.Count; i++)
        {
            Debug.Log(ownedBookmarks[i]);
        }
    }

    #endregion
}
