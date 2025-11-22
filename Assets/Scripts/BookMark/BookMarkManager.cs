using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BookMarkManager : MonoBehaviour
{
    private static BookMarkManager instance;
    public static BookMarkManager Instance => instance;

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
    /// 책갈피 제거 (삭제 시 사용)
    /// </summary>
    public bool RemoveBookmark(int uniqueID)
    {
        var bookmark = ownedBookmarks.FirstOrDefault(b => b.UniqueID == uniqueID);
        if (bookmark == null)
        {
            Debug.LogWarning($"[BookMarkManager] 존재하지 않는 책갈피 ID: {uniqueID}");
            return false;
        }

        if (bookmark.IsEquipped)
        {
            Debug.LogWarning($"[BookMarkManager] 장착된 책갈피는 제거할 수 없습니다: {bookmark}");
            return false;
        }

        ownedBookmarks.Remove(bookmark);
        Debug.Log($"[BookMarkManager] 책갈피 제거: {bookmark}");
        return true;
    }

    /// <summary>
    /// 모든 책갈피 가져오기
    /// </summary>
    public List<BookMark> GetAllBookmarks()
    {
        return new List<BookMark>(ownedBookmarks);
    }

    /// <summary>
    /// 특정 UniqueID의 책갈피 가져오기
    /// </summary>
    public BookMark GetBookmark(int uniqueID)
    {
        return ownedBookmarks.FirstOrDefault(b => b.UniqueID == uniqueID);
    }

    /// <summary>
    /// 미장착 책갈피만 가져오기 (사서 관리 UI에서 사용)
    /// </summary>
    public List<BookMark> GetUnequippedBookmarks()
    {
        return ownedBookmarks.Where(b => !b.IsEquipped).ToList();
    }

    /// <summary>
    /// 특정 사서에게 장착된 책갈피 가져오기 (사서 관리 UI에서 사용)
    /// </summary>
    public List<BookMark> GetEquippedBookmarks(int librarianID)
    {
        return ownedBookmarks.Where(b => b.IsEquipped && b.EquippedLibrarianID == librarianID).ToList();
    }

    /// <summary>
    /// 등급별 필터링
    /// </summary>
    public List<BookMark> GetBookmarksByGrade(Grade grade)
    {
        return ownedBookmarks.Where(b => b.Grade == grade).ToList();
    }

    /// <summary>
    /// 옵션 타입별 필터링
    /// </summary>
    public List<BookMark> GetBookmarksByOptionType(int optionType)
    {
        return ownedBookmarks.Where(b => b.OptionType == optionType).ToList();
    }

    /// <summary>
    /// 북마크 타입별 필터링 (스탯/스킬)
    /// </summary>
    public List<BookMark> GetBookmarksByType(BookmarkType type)
    {
        return ownedBookmarks.Where(b => b.Type == type).ToList();
    }

    /// <summary>
    /// 스탯 북마크만 가져오기
    /// </summary>
    public List<BookMark> GetStatBookmarks()
    {
        return GetBookmarksByType(BookmarkType.Stat);
    }

    /// <summary>
    /// 스킬 북마크만 가져오기
    /// </summary>
    public List<BookMark> GetSkillBookmarks()
    {
        return GetBookmarksByType(BookmarkType.Skill);
    }

    #region Debug Methods

    /// <summary>
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
    /// 보유 책갈피 목록 출력
    /// </summary>
    [ContextMenu("보유 책갈피 목록 출력")]
    private void PrintAllBookmarks()
    {
        Debug.Log($"=== 보유 책갈피 목록 ({ownedBookmarks.Count}개) ===");
        foreach (var bookmark in ownedBookmarks)
        {
            Debug.Log(bookmark);
        }
    }

    #endregion
}
