using System.Collections.Generic;
using UnityEngine;

//TODO JML: 2차 빌드 후 삭제 예정 - 임시 책갈피 저장 매니저
/// <summary>
/// 제작된 책갈피를 임시로 저장하는 매니저
/// 2차 빌드 데모용으로만 사용, 이후 삭제 예정
/// </summary>
public class TempBookMarkManager : MonoBehaviour
{
    private static TempBookMarkManager instance;
    public static TempBookMarkManager Instance => instance;

    //TODO JML: 2차 빌드 후 삭제 예정 - 책갈피 저장소
    private List<BookMark> bookmarks = new List<BookMark>();
    private int instanceCounter = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[TempBookMarkManager] 싱글톤 인스턴스 생성 (DontDestroyOnLoad)");
        }
        else if (instance != this)
        {
            Debug.Log("[TempBookMarkManager] 중복 인스턴스 파괴");
            Destroy(gameObject);
        }
    }

    //TODO JML: 2차 빌드 후 삭제 예정 - 책갈피 추가
    /// <summary>
    /// 제작된 책갈피를 저장소에 추가
    /// </summary>
    public void AddBookmark(BookMark bookmark)
    {
        if (bookmark == null)
        {
            Debug.LogWarning("[TempBookMarkManager] null 책갈피는 추가할 수 없습니다.");
            return;
        }

        bookmarks.Add(bookmark);
        Debug.Log($"[TempBookMarkManager] 책갈피 추가: {bookmark.GetName()} (등급: {bookmark.GetGrade()}) - 총 {bookmarks.Count}개");
    }

    //TODO JML: 2차 빌드 후 삭제 예정 - 책갈피 목록 반환
    /// <summary>
    /// 저장된 모든 책갈피 목록 반환 (복사본)
    /// </summary>
    public List<BookMark> GetAllBookmarks()
    {
        return new List<BookMark>(bookmarks);
    }

    //TODO JML: 2차 빌드 후 삭제 예정 - 고유 ID 생성
    /// <summary>
    /// 인벤토리 표시용 고유 인스턴스 ID 생성
    /// </summary>
    public int GetNextInstanceId()
    {
        return ++instanceCounter;
    }

    //TODO JML: 2차 빌드 후 삭제 예정 - 저장소 초기화
    /// <summary>
    /// 테스트용: 모든 책갈피 삭제
    /// </summary>
    public void ClearAll()
    {
        bookmarks.Clear();
        instanceCounter = 0;
        Debug.Log("[TempBookMarkManager] 모든 책갈피 삭제됨");
    }
}
