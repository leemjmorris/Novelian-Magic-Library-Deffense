using Cysharp.Threading.Tasks;
using UnityEngine;

public class CSVTestObj : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        // Wait until CSVLoader completes LoadAll
        await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);

        // Method 1: Use GetTable
        var bookmarkTable = CSVLoader.Instance.GetTable<BookmarkData>();
        var bookmarkCraftTable = CSVLoader.Instance.GetTable<BookmarkCraftData>();
        var bookmarkOptionTable = CSVLoader.Instance.GetTable<BookmarkOptionData>();
        var bookmarkListTable = CSVLoader.Instance.GetTable<BookmarkListData>();
        var bookmarkSkillTable = CSVLoader.Instance.GetTable<BookmarkSkillData>();

        var currencyTable = CSVLoader.Instance.GetTable<CurrencyData>();
        var ingredientTable = CSVLoader.Instance.GetTable<IngredientData>();
        var gradeTable = CSVLoader.Instance.GetTable<GradeData>();

    
        Debug.Log("=== CSV Load Test ===");

        Debug.Log("=== BookMark Table===");
        Debug.Log($"Bookmark Name: {bookmarkTable.GetId(111).Bookmark_Name}");
        Debug.Log($"Bookmark Craft Bookmark_Name: {bookmarkCraftTable.GetId(121).Recipe_Name}");
        Debug.Log($"Bookmark Option Name: {bookmarkOptionTable.GetId(1311).Option_Name}");
        Debug.Log($"Bookmark List Name: {bookmarkListTable.GetId(141).List_Name}");
        Debug.Log($"Bookmark Skill Name: {bookmarkSkillTable.GetId(1711).Bookmark_Skill_Name}");

        Debug.Log("=== Currency & Ingredient Table ===");
        Debug.Log($"Ingredient Name: {ingredientTable.GetId(1011).Ingredient_Name}");
        Debug.Log($"Currency Name: {currencyTable.GetId(161).Currency_Name}");
        Debug.Log($"Grde Name: {gradeTable.GetId(151).Grade_Name}");

        Debug.Log("===============");


        // Additional usage examples
        var allData = CSVLoader.Instance.GetTable<BookmarkCraftData>().GetAll();
        Debug.Log($"Total rows in TestTable: {allData.Count}");

        // Conditional search
        var filtered = CSVLoader.Instance.GetTable<BookmarkCraftData>().FindAll(x => x.Recipe_ID > 5);
        Debug.Log($"Filtered count: {filtered.Count}");
    }
}
