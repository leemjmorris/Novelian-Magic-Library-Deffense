using UnityEngine;

/// <summary>
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
