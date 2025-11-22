using UnityEngine;

public class BookMarkCraft : MonoBehaviour
{
    private static BookMarkCraft instance;
    public static BookMarkCraft Instance => instance;

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
    /// 책갈피 제작
    /// </summary>
    /// <param name="recipeID">BookmarkCraftData의 Recipe_ID</param>
    /// <returns>제작 결과</returns>
    public BookMarkCraftResult CraftBookmark(int recipeID)
    {
        // JML: Recipe Data Load
        var recipeData = CSVLoader.Instance.GetData<BookmarkCraftData>(recipeID);
        if (recipeData == null)
        {
            Debug.LogError($"[BookMarkCraft] 존재하지 않는 레시피 ID: {recipeID}");
            return new BookMarkCraftResult(false, null, "레시피를 찾을 수 없습니다.");
        }

        // JML: Material Count Check
        if (!CheckMaterials(recipeData))
        {
            Debug.LogWarning($"[BookMarkCraft] 재료 부족: {recipeData.Recipe_Name}");
            return new BookMarkCraftResult(false, null, "재료가 부족합니다.");
        }

        // JML: Currency Check
        if (!CheckCurrency(recipeData))
        {
            Debug.LogWarning($"[BookMarkCraft] 골드 부족: {recipeData.Recipe_Name}");
            return new BookMarkCraftResult(false, null, "골드가 부족합니다.");
        }

        // JML: Material Consumption
        if (!ConsumeMaterials(recipeData))
        {
            Debug.LogError($"[BookMarkCraft] 재료 소모 실패: {recipeData.Recipe_Name}");
            return new BookMarkCraftResult(false, null, "재료 소모 중 오류가 발생했습니다.");
        }

        // JML: Currency Consumption
        if (!ConsumeCurrency(recipeData))
        {
            Debug.LogError($"[BookMarkCraft] 골드 소모 실패: {recipeData.Recipe_Name}");
            return new BookMarkCraftResult(false, null, "골드 소모 중 오류가 발생했습니다.");
        }

        // JML: Success Determination
        CraftSuccessType successType = RollCraftSuccess(recipeData.Success_Rate, recipeData.Great_Success_Rate);

        // JML: Result Bookmark Creation
        int resultID = successType == CraftSuccessType.GreatSuccess ? recipeData.Great_Result_ID : recipeData.Result_ID;
        BookMark craftedBookmark = CreateBookmarkFromResult(resultID);

        if (craftedBookmark == null)
        {
            Debug.LogError($"[BookMarkCraft] 책갈피 생성 실패: Result_ID {resultID}");
            return new BookMarkCraftResult(false, null, "책갈피 생성 중 오류가 발생했습니다.");
        }

        // JML: Add to BookMarkManager
        BookMarkManager.Instance.AddBookmark(craftedBookmark);

        string message = successType == CraftSuccessType.GreatSuccess ? "대성공!" : "성공!";
        Debug.Log($"[BookMarkCraft] 제작 {message}: {craftedBookmark}");

        return new BookMarkCraftResult(true, craftedBookmark, message, successType);
    }

    /// <summary>
    /// JML: Material Count Check
    /// </summary>
    private bool CheckMaterials(BookmarkCraftData recipeData)
    {
        var ingredientMgr = IngredientManager.Instance;
        if (ingredientMgr == null)
        {
            Debug.LogError("[BookMarkCraft] IngredientManager가 없습니다!");
            {
                return false;
            }
        }

        // Material_1
        if (recipeData.Material_1_ID > 0 && recipeData.Material_1_Count > 0)
        {
            if (!ingredientMgr.HasIngredient(recipeData.Material_1_ID, recipeData.Material_1_Count))
            {
                return false;
            }
        }

        // Material_2
        if (recipeData.Material_2_ID > 0 && recipeData.Material_2_Count > 0)
        {
            if (!ingredientMgr.HasIngredient(recipeData.Material_2_ID, recipeData.Material_2_Count))
            {
                return false;
            }
        }

        // Material_3
        if (recipeData.Material_3_ID > 0 && recipeData.Material_3_Count > 0)
        {
            if (!ingredientMgr.HasIngredient(recipeData.Material_3_ID, recipeData.Material_3_Count))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 골드가 충분한지 확인
    /// </summary>
    private bool CheckCurrency(BookmarkCraftData recipeData)
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

    /// <summary>
    /// 재료 소모
    /// </summary>
    private bool ConsumeMaterials(BookmarkCraftData recipeData)
    {
        var ingredientMgr = IngredientManager.Instance;

        // Material_1
        if (recipeData.Material_1_ID > 0 && recipeData.Material_1_Count > 0)
        {
            if (!ingredientMgr.RemoveIngredient(recipeData.Material_1_ID, recipeData.Material_1_Count))
                return false;
        }

        // Material_2
        if (recipeData.Material_2_ID > 0 && recipeData.Material_2_Count > 0)
        {
            if (!ingredientMgr.RemoveIngredient(recipeData.Material_2_ID, recipeData.Material_2_Count))
                return false;
        }

        // Material_3
        if (recipeData.Material_3_ID > 0 && recipeData.Material_3_Count > 0)
        {
            if (!ingredientMgr.RemoveIngredient(recipeData.Material_3_ID, recipeData.Material_3_Count))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 골드 소모
    /// </summary>
    private bool ConsumeCurrency(BookmarkCraftData recipeData)
    {
        var currencyMgr = CurrencyManager.Instance;

        if (recipeData.Currency_Count > 0)
        {
            return currencyMgr.SpendGold(recipeData.Currency_Count);
        }

        return true;
    }

    /// <summary>
    /// JML: Crafting Success Roll
    /// </summary>
    private CraftSuccessType RollCraftSuccess(float successRate, float greatSuccessRate)
    {
        float roll = Random.Range(0f, 1f);

        // JML: Great Success
        if (roll < greatSuccessRate)    // 0.05
        {
            return CraftSuccessType.GreatSuccess;
        }
        //JML: Normal Success
        return CraftSuccessType.Success;
    }

    /// <summary>   
    /// JML: Create Bookmark from Result_ID
    /// </summary>
    private BookMark CreateBookmarkFromResult(int resultID)
    {
        // BookMarkSkillData Check
        var skillData = CSVLoader.Instance.GetData<BookmarkSkillData>(resultID);

        if (skillData != null)
        {
            // JML: Create Skill Bookmark
            Debug.Log($"[BookMarkCraft] 스킬 북마크 생성: {skillData.Bookmark_Skill_Name}");
            var skillBookmark = new BookMark(
                bookmarkDataID: resultID,
                name: skillData.Bookmark_Skill_Name,
                grade: (Grade)skillData.Grade,
                optionType: skillData.Option_Type,
                optionValue: skillData.Option_Value,
                effectID: skillData.Effect_ID
            );
            return skillBookmark;
        }

        // BookMarkOptionData Check
        var optionData = CSVLoader.Instance.GetData<BookmarkOptionData>(resultID);
        if (optionData != null)
        {
            // JML: Create Stat Bookmark
            Debug.Log($"[BookMarkCraft] 스탯 북마크 생성: {optionData.Option_Name}");
            var statBookmark = new BookMark(
                bookmarkDataID: resultID,
                name: optionData.Option_Name,
                grade: (Grade)optionData.Grade,
                optionType: optionData.Option_Type,
                optionValue: optionData.Option_Value
            );
            return statBookmark;
        }

        // JML: Neither Data Found
        Debug.LogError($"[BookMarkCraft] Result_ID {resultID}에 해당하는 BookmarkSkillData 또는 BookmarkOptionData를 찾을 수 없습니다!");
        return null;
    }
}

/// <summary>
/// 제작 성공 타입
/// </summary>
public enum CraftSuccessType
{
    Success,        // 일반 성공
    GreatSuccess,   // 대성공
}

