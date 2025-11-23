using UnityEngine;

/// <summary>
/// JML: Craft Success Type
/// 제작 성공 타입
/// </summary>
public enum CraftSuccessType
{
    Success,        // JML: Normal Success / 일반 성공
    GreatSuccess,   // JML: Great Success / 대성공
    Fail            // JML: Fail (for future use) / 실패 (나중에 사용)
}

/// <summary>
/// JML: Craft Result Data
/// 제작 결과 데이터
/// </summary>
public class BookMarkCraftResult
{
    public bool IsSuccess { get; private set; }
    public BookMark CraftedBookmark { get; private set; }
    public string Message { get; private set; }
    public CraftSuccessType SuccessType { get; private set; }

    public BookMarkCraftResult(bool isSuccess, BookMark bookmark, string message, CraftSuccessType successType = CraftSuccessType.Success)
    {
        IsSuccess = isSuccess;
        CraftedBookmark = bookmark;
        Message = message;
        SuccessType = successType;
    }
}
