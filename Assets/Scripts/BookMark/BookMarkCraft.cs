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
    /// <param name="resultID">BookmarkListData의 List_ID</param>
    /// <param name="bookmarkType">제작할 책갈피 타입 (Stat 또는 Skill)</param>
    private static BookMark CreateBookmarkFromResult(int resultID, BookmarkType bookmarkType)
    {
        // JML: Load BookmarkListData
        // BookmarkListData 로드
        var listData = CSVLoader.Instance.GetData<BookmarkListData>(resultID);
        if (listData == null)
        {
            Debug.LogError($"[BookMarkCraft] BookmarkListData를 찾을 수 없음: {resultID}");
            return null;
        }

        // JML: Collect Option_1~4 (without LINQ)
        // Option_1~4 수집 (LINQ 없이)
        int[] optionIDs = new int[]
        {
            listData.Option_1_ID,
            listData.Option_2_ID,
            listData.Option_3_ID,
            listData.Option_4_ID
        };

        // JML: Filter valid options (exclude 0)
        // 유효한 옵션만 필터링 (0 제외)
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
            Debug.LogError($"[BookMarkCraft] BookmarkListData {resultID}에 유효한 옵션이 없습니다!");
            return null;
        }

        // JML: Random selection
        // 랜덤으로 하나 선택
        int selectedID = validOptions[Random.Range(0, validCount)];
        Debug.Log($"[BookMarkCraft] 랜덤 선택: {selectedID} (총 {validCount}개 중)");

        // JML: Load BookmarkData
        // 스탯 책갈피: selectedID는 Option_ID → BookmarkTable에서 Option_ID로 찾기
        // 스킬 책갈피: selectedID는 Bookmark_ID → BookmarkTable에서 Bookmark_ID로 찾기
        BookmarkData bookmarkData = null;

        if (bookmarkType == BookmarkType.Stat)
        {
            // 스탯 책갈피: Option_ID로 검색
            var allBookmarks = CSVLoader.Instance.GetTable<BookmarkData>().GetAll();
            bookmarkData = allBookmarks.Find(b => b.Option_ID == selectedID);
            if (bookmarkData == null)
            {
                Debug.LogError($"[BookMarkCraft] Option_ID {selectedID}에 해당하는 BookmarkData를 찾을 수 없음");
                return null;
            }
        }
        else if (bookmarkType == BookmarkType.Skill)
        {
            // 스킬 책갈피: Bookmark_ID로 검색
            bookmarkData = CSVLoader.Instance.GetData<BookmarkData>(selectedID);
            if (bookmarkData == null)
            {
                Debug.LogError($"[BookMarkCraft] Bookmark_ID {selectedID}에 해당하는 BookmarkData를 찾을 수 없음");
                return null;
            }
        }
        else
        {
            Debug.LogError($"[BookMarkCraft] 유효하지 않은 BookmarkType: {bookmarkType}");
            return null;
        }

        // JML: Create bookmark based on Recipe_Type
        // Recipe_Type에 따라 책갈피 생성
        if (bookmarkType == BookmarkType.Skill)
        {
            // JML: Skill Bookmark
            // 스킬 북마크
            if (bookmarkData.Skill_ID <= 0)
            {
                Debug.LogError($"[BookMarkCraft] BookmarkData {bookmarkData.Bookmark_ID}에 유효한 Skill_ID가 없습니다!");
                return null;
            }

            string bookmarkName = CSVLoader.Instance.GetData<StringTable>(bookmarkData.Bookmark_Name_ID)?.Text ?? "Unknown";
            Debug.Log($"[BookMarkCraft] 스킬 북마크 생성: {bookmarkName}, Skill_ID: {bookmarkData.Skill_ID}");
            var skillBookmark = new BookMark(
                bookmarkDataID: bookmarkData.Bookmark_ID,
                name: bookmarkName,
                grade: (Grade)bookmarkData.Grade_ID,
                optionType: 0,
                optionValue: 0,
                skillID: bookmarkData.Skill_ID
            );
            return skillBookmark;
        }
        else if (bookmarkType == BookmarkType.Stat)
        {
            // JML: Stat Bookmark
            // 스탯 북마크
            if (bookmarkData.Option_ID <= 0)
            {
                Debug.LogError($"[BookMarkCraft] BookmarkData {bookmarkData.Bookmark_ID}에 유효한 Option_ID가 없습니다!");
                return null;
            }

            // JML: Load BookmarkOptionData (Stat Bookmark)
            // BookmarkOptionData 로드 (스탯 북마크)
            var optionData = CSVLoader.Instance.GetData<BookmarkOptionData>(bookmarkData.Option_ID);

            if (optionData == null)
            {
                Debug.LogError($"[BookMarkCraft] BookmarkOptionData를 찾을 수 없음: {bookmarkData.Option_ID}");
                return null;
            }

            // JML: Create Stat Bookmark
            // 스탯 북마크 생성
            string statBookmarkName = CSVLoader.Instance.GetData<StringTable>(bookmarkData.Bookmark_Name_ID)?.Text ?? "Unknown";
            Debug.Log($"[BookMarkCraft] 스탯 북마크 생성: {statBookmarkName}");
            var statBookmark = new BookMark(
                bookmarkDataID: bookmarkData.Bookmark_ID,
                name: statBookmarkName,
                grade: (Grade)bookmarkData.Grade_ID,
                optionType: (int)optionData.Option_Type,
                optionValue: optionData.Option_Value
            );
            return statBookmark;
        }
        else
        {
            // JML: Error if invalid type
            // 유효하지 않은 타입이면 에러
            Debug.LogError($"[BookMarkCraft] 유효하지 않은 BookmarkType: {bookmarkType}");
            return null;
        }
    }

    #endregion
}
