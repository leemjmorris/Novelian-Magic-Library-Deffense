using UnityEngine;

/// <summary>
/// JML: Bookmark Crafting Utility Class (static)
/// 책갈피 제작 유틸리티 클래스 (static)
/// MonoBehaviour 상속 없이 순수 제작 로직만 제공
/// </summary>
public static class BookMarkCraft
{
    /// <summary>
    /// JML: Craft Bookmark
    /// 책갈피 제작
    /// </summary>
    /// <param name="recipeID">BookmarkCraftData의 Recipe_ID</param>
    /// <returns>제작 결과</returns>
    public static BookMarkCraftResult CraftBookmark(int recipeID)
    {
        // JML: Recipe Data Load
        // 레시피 데이터 로드
        var recipeData = CSVLoader.Instance.GetData<BookmarkCraftData>(recipeID);
        if (recipeData == null)
        {
            Debug.LogError($"[BookMarkCraft] 존재하지 않는 레시피 ID: {recipeID}");
            return new BookMarkCraftResult(false, null, "레시피를 찾을 수 없습니다.");
        }

        // JML: Material Check
        // 재료 확인
        if (!CheckMaterials(recipeData))
        {
            Debug.LogWarning($"[BookMarkCraft] 재료 부족: {CSVLoader.Instance.GetData<StringTable>(recipeData.Recipe_Name_ID)?.Text ?? "Unknown"}");
            return new BookMarkCraftResult(false, null, "재료가 부족합니다.");
        }

        // JML: Currency Check
        // 골드 확인
        if (!CheckCurrency(recipeData))
        {
            Debug.LogWarning($"[BookMarkCraft] 골드 부족: {CSVLoader.Instance.GetData<StringTable>(recipeData.Recipe_Name_ID)?.Text ?? "Unknown"}");
            return new BookMarkCraftResult(false, null, "골드가 부족합니다.");
        }

        // JML: Material Consumption
        // 재료 소모
        if (!ConsumeMaterials(recipeData))
        {
            Debug.LogError($"[BookMarkCraft] 재료 소모 실패: {CSVLoader.Instance.GetData<StringTable>(recipeData.Recipe_Name_ID)?.Text ?? "Unknown"}");
            return new BookMarkCraftResult(false, null, "재료 소모 중 오류가 발생했습니다.");
        }

        // JML: Currency Consumption
        // 골드 소모
        if (!ConsumeCurrency(recipeData))
        {
            Debug.LogError($"[BookMarkCraft] 골드 소모 실패: {CSVLoader.Instance.GetData<StringTable>(recipeData.Recipe_Name_ID)?.Text ?? "Unknown"}");
            return new BookMarkCraftResult(false, null, "골드 소모 중 오류가 발생했습니다.");
        }

        // JML: Success Determination
        // 성공 판정
        CraftSuccessType successType = RollCraftSuccess(recipeData.Great_Success_Rate);

        // JML: Result Bookmark Creation
        // 결과 책갈피 생성
        int resultID = successType == CraftSuccessType.GreatSuccess ? recipeData.Great_Result_ID : recipeData.Result_ID;
        BookMark craftedBookmark = CreateBookmarkFromResult(resultID, recipeData.Recipe_Type);

        if (craftedBookmark == null)
        {
            Debug.LogError($"[BookMarkCraft] 책갈피 생성 실패: Result_ID {resultID}");
            return new BookMarkCraftResult(false, null, "책갈피 생성 중 오류가 발생했습니다.");
        }

        // JML: Add to BookMarkManager
        // BookMarkManager에 추가
        BookMarkManager.Instance.AddBookmark(craftedBookmark);

        string message = successType == CraftSuccessType.GreatSuccess ? "대성공!" : "성공!";
        Debug.Log($"[BookMarkCraft] 제작 {message}: {craftedBookmark}");

        return new BookMarkCraftResult(true, craftedBookmark, message, successType);
    }

    #region Material & Currency Check

    /// <summary>
    /// JML: Check if materials are sufficient
    /// 재료가 충분한지 확인
    /// </summary>
    private static bool CheckMaterials(BookmarkCraftData recipeData)
    {
        var ingredientMgr = IngredientManager.Instance;
        if (ingredientMgr == null)
        {
            Debug.LogError("[BookMarkCraft] IngredientManager가 없습니다!");
            return false;
        }

        // JML: Material_1 Check
        // 재료 1 확인
        if (recipeData.Material_1_ID > 0 && recipeData.Material_1_Count > 0)
        {
            if (!ingredientMgr.HasIngredient(recipeData.Material_1_ID, recipeData.Material_1_Count))
                return false;
        }

        // JML: Material_2 Check
        // 재료 2 확인
        if (recipeData.Material_2_ID > 0 && recipeData.Material_2_Count > 0)
        {
            if (!ingredientMgr.HasIngredient(recipeData.Material_2_ID, recipeData.Material_2_Count))
                return false;
        }

        // JML: Material_3 Check
        // 재료 3 확인
        if (recipeData.Material_3_ID > 0 && recipeData.Material_3_Count > 0)
        {
            if (!ingredientMgr.HasIngredient(recipeData.Material_3_ID, recipeData.Material_3_Count))
                return false;
        }

        return true;
    }

    /// <summary>
    /// JML: Check if currency is sufficient
    /// 골드가 충분한지 확인
    /// </summary>
    private static bool CheckCurrency(BookmarkCraftData recipeData)
    {
        var currencyMgr = CurrencyManager.Instance;
        if (currencyMgr == null)
        {
            Debug.LogError("[BookMarkCraft] CurrencyManager가 없습니다!");
            return false;
        }

        if (recipeData.Currency_Count > 0)
        {
            return currencyMgr.Gold >= recipeData.Currency_Count;
        }

        return true;
    }

    #endregion

    #region Material & Currency Consumption

    /// <summary>
    /// JML: Consume materials
    /// 재료 소모
    /// </summary>
    private static bool ConsumeMaterials(BookmarkCraftData recipeData)
    {
        var ingredientMgr = IngredientManager.Instance;

        // JML: Consume Material_1
        // 재료 1 소모
        if (recipeData.Material_1_ID > 0 && recipeData.Material_1_Count > 0)
        {
            if (!ingredientMgr.RemoveIngredient(recipeData.Material_1_ID, recipeData.Material_1_Count))
                return false;
        }

        // JML: Consume Material_2
        // 재료 2 소모
        if (recipeData.Material_2_ID > 0 && recipeData.Material_2_Count > 0)
        {
            if (!ingredientMgr.RemoveIngredient(recipeData.Material_2_ID, recipeData.Material_2_Count))
                return false;
        }

        // JML: Consume Material_3
        // 재료 3 소모
        if (recipeData.Material_3_ID > 0 && recipeData.Material_3_Count > 0)
        {
            if (!ingredientMgr.RemoveIngredient(recipeData.Material_3_ID, recipeData.Material_3_Count))
                return false;
        }

        return true;
    }

    /// <summary>
    /// JML: Consume currency
    /// 골드 소모
    /// </summary>
    private static bool ConsumeCurrency(BookmarkCraftData recipeData)
    {
        var currencyMgr = CurrencyManager.Instance;

        if (recipeData.Currency_Count > 0)
        {
            return currencyMgr.SpendGold(recipeData.Currency_Count);
        }

        return true;
    }

    #endregion

    #region Success Roll & Bookmark Creation

    /// <summary>
    /// JML: Roll crafting success
    /// 제작 성공 판정
    /// </summary>
    private static CraftSuccessType RollCraftSuccess(float greatSuccessRate)
    {
        float roll = Random.Range(0f, 1f);

        // JML: Great Success Check (great success rate is the probability of great success)
        // 대성공 확률 체크 (greatSuccessRate는 대성공 확률)
        if (roll < greatSuccessRate)
        {
            return CraftSuccessType.GreatSuccess;
        }

        // JML: Normal Success (always success if not great success)
        // 일반 성공 (대성공이 아니면 무조건 일반 성공)
        return CraftSuccessType.Success;
    }

    /// <summary>
    /// JML: Create Bookmark from Result_ID
    /// Result_ID로부터 BookMark 생성
    /// </summary>
    /// <param name="resultID">스탯: BookmarkStatListData의 List_ID / 스킬: Grade_ID</param>
    /// <param name="bookmarkType">제작할 책갈피 타입 (Stat 또는 Skill)</param>
    private static BookMark CreateBookmarkFromResult(int resultID, BookmarkType bookmarkType)
    {
        if (bookmarkType == BookmarkType.Stat)
        {
            return CreateStatBookmark(resultID);
        }
        else if (bookmarkType == BookmarkType.Skill)
        {
            return CreateSkillBookmark(resultID);
        }
        else
        {
            Debug.LogError($"[BookMarkCraft] 유효하지 않은 BookmarkType: {bookmarkType}");
            return null;
        }
    }

    /// <summary>
    /// JML: Create Stat Bookmark from BookmarkStatListTable
    /// 스탯 책갈피 생성 (BookmarkStatListTable 사용)
    /// </summary>
    /// <param name="listID">BookmarkStatListData의 List_ID</param>
    private static BookMark CreateStatBookmark(int listID)
    {
        // JML: Load BookmarkStatListData
        var listData = CSVLoader.Instance.GetData<BookmarkStatListData>(listID);
        if (listData == null)
        {
            Debug.LogError($"[BookMarkCraft] BookmarkStatListData를 찾을 수 없음: {listID}");
            return null;
        }

        // JML: Collect Option_1~4 (without LINQ)
        int[] optionIDs = new int[]
        {
            listData.Option_1_ID,
            listData.Option_2_ID,
            listData.Option_3_ID,
            listData.Option_4_ID
        };

        // JML: Filter valid options (exclude 0)
        int[] validOptions = new int[4];
        int validCount = 0;

        for (int i = 0; i < optionIDs.Length; i++)
        {
            if (optionIDs[i] > 0)
            {
                validOptions[validCount] = optionIDs[i];
                validCount++;
            }
        }

        if (validCount == 0)
        {
            Debug.LogError($"[BookMarkCraft] BookmarkStatListData {listID}에 유효한 옵션이 없습니다!");
            return null;
        }

        // JML: Random selection
        int selectedOptionID = validOptions[Random.Range(0, validCount)];
        Debug.Log($"[BookMarkCraft] 스탯 옵션 랜덤 선택: {selectedOptionID} (총 {validCount}개 중)");

        // JML: Find BookmarkData by Option_ID
        var allBookmarks = CSVLoader.Instance.GetTable<BookmarkData>().GetAll();
        BookmarkData bookmarkData = allBookmarks.Find(b => b.Option_ID == selectedOptionID);
        if (bookmarkData == null)
        {
            Debug.LogError($"[BookMarkCraft] Option_ID {selectedOptionID}에 해당하는 BookmarkData를 찾을 수 없음");
            return null;
        }

        // JML: Load BookmarkOptionData
        var optionData = CSVLoader.Instance.GetData<BookmarkOptionData>(bookmarkData.Option_ID);
        if (optionData == null)
        {
            Debug.LogError($"[BookMarkCraft] BookmarkOptionData를 찾을 수 없음: {bookmarkData.Option_ID}");
            return null;
        }

        // JML: Create Stat Bookmark
        string statBookmarkName = CSVLoader.Instance.GetData<StringTable>(bookmarkData.Bookmark_Name_ID)?.Text ?? "Unknown";
        Debug.Log($"[BookMarkCraft] 스탯 북마크 생성: {statBookmarkName}");
        var statBookmark = new BookMark(
            bookmarkDataID: bookmarkData.Bookmark_ID,
            name: statBookmarkName,
            grade: CSVLoader.Instance.GetData<GradeData>(bookmarkData.Grade_ID)?.Grade_Type ?? Grade.Common,
            optionType: (int)optionData.Option_Type,
            optionValue: optionData.Option_Value
        );
        return statBookmark;
    }

    /// <summary>
    /// JML: Create Skill Bookmark by Grade_ID
    /// 스킬 책갈피 생성 (등급별 필터링)
    /// </summary>
    /// <param name="gradeID">Grade_ID (1501~1505)</param>
    private static BookMark CreateSkillBookmark(int gradeID)
    {
        // JML: Load all BookmarkSkillListData
        var skillListTable = CSVLoader.Instance.GetTable<BookmarkSkillListData>();
        if (skillListTable == null)
        {
            Debug.LogError("[BookMarkCraft] BookmarkSkillListData 테이블을 찾을 수 없음");
            return null;
        }

        var allSkillList = skillListTable.GetAll();
        if (allSkillList == null || allSkillList.Count == 0)
        {
            Debug.LogError("[BookMarkCraft] BookmarkSkillListData가 비어있음");
            return null;
        }

        // JML: Filter by Grade_ID
        // 각 Bookmark_ID로 BookmarkTable 조회해서 Grade_ID 확인
        var matchingBookmarkIDs = new System.Collections.Generic.List<int>();

        for (int i = 0; i < allSkillList.Count; i++)
        {
            int bookmarkID = allSkillList[i].Bookmark_ID;
            var bookmarkData = CSVLoader.Instance.GetData<BookmarkData>(bookmarkID);

            if (bookmarkData != null && bookmarkData.Grade_ID == gradeID && bookmarkData.Skill_ID > 0)
            {
                matchingBookmarkIDs.Add(bookmarkID);
            }
        }

        if (matchingBookmarkIDs.Count == 0)
        {
            Debug.LogError($"[BookMarkCraft] Grade_ID {gradeID}에 해당하는 스킬 책갈피가 없습니다!");
            return null;
        }

        // JML: Random selection
        int selectedBookmarkID = matchingBookmarkIDs[Random.Range(0, matchingBookmarkIDs.Count)];
        Debug.Log($"[BookMarkCraft] 스킬 책갈피 랜덤 선택: {selectedBookmarkID} (등급 {gradeID}, 총 {matchingBookmarkIDs.Count}개 중)");

        // JML: Get BookmarkData
        var selectedBookmarkData = CSVLoader.Instance.GetData<BookmarkData>(selectedBookmarkID);
        if (selectedBookmarkData == null)
        {
            Debug.LogError($"[BookMarkCraft] Bookmark_ID {selectedBookmarkID}에 해당하는 BookmarkData를 찾을 수 없음");
            return null;
        }

        // JML: Create Skill Bookmark
        string bookmarkName = CSVLoader.Instance.GetData<StringTable>(selectedBookmarkData.Bookmark_Name_ID)?.Text ?? "Unknown";
        Debug.Log($"[BookMarkCraft] 스킬 북마크 생성: {bookmarkName}, Skill_ID: {selectedBookmarkData.Skill_ID}");

        var skillBookmark = new BookMark(
            bookmarkDataID: selectedBookmarkData.Bookmark_ID,
            name: bookmarkName,
            grade: CSVLoader.Instance.GetData<GradeData>(selectedBookmarkData.Grade_ID)?.Grade_Type ?? Grade.Common,
            optionType: 0,
            optionValue: 0,
            skillID: selectedBookmarkData.Skill_ID
        );
        return skillBookmark;
    }

    #endregion
}
