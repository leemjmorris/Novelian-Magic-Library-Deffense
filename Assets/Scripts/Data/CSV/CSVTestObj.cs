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


        var currencyTable = CSVLoader.Instance.GetTable<CurrencyData>();
        var ingredientTable = CSVLoader.Instance.GetTable<IngredientData>();
        var gradeTable = CSVLoader.Instance.GetTable<GradeData>();

    
        Debug.Log("=== CSV Load Test ===");

        Debug.Log("=== BookMark Table===");
        Debug.Log($"Bookmark Name: {CSVLoader.Instance.GetData<StringTable>(bookmarkTable.GetId(111).Bookmark_Name_ID)?.Text ?? "Unknown"}");
        Debug.Log($"Bookmark Craft Bookmark_Name: {CSVLoader.Instance.GetData<StringTable>(bookmarkCraftTable.GetId(121).Recipe_Name_ID)?.Text ?? "Unknown"}");
        Debug.Log($"Bookmark Option Name: {CSVLoader.Instance.GetData<StringTable>(bookmarkOptionTable.GetId(1311).Option_Name_ID)?.Text ?? "Unknown"}");
        Debug.Log($"Bookmark List Name: {CSVLoader.Instance.GetData<StringTable>(bookmarkListTable.GetId(141).List_Name_ID)?.Text ?? "Unknown"}");

        Debug.Log("=== Currency & Ingredient Table ===");
        Debug.Log($"Ingredient Name: {CSVLoader.Instance.GetData<StringTable>(ingredientTable.GetId(1011).Ingredient_Name_ID)?.Text ?? "Unknown"}");
        Debug.Log($"Currency Name: {CSVLoader.Instance.GetData<StringTable>(currencyTable.GetId(161).Currency_Name_ID)?.Text ?? "Unknown"}");
        Debug.Log($"Grde Name: {CSVLoader.Instance.GetData<StringTable>(gradeTable.GetId(151).Grade_Name_ID)?.Text ?? "Unknown"}");

        Debug.Log("===============");


        // Additional usage examples
        var allData = CSVLoader.Instance.GetTable<BookmarkCraftData>().GetAll();
        Debug.Log($"Total rows in TestTable: {allData.Count}");

        // Conditional search
        var filtered = CSVLoader.Instance.GetTable<BookmarkCraftData>().FindAll(x => x.Recipe_ID > 5);
        Debug.Log($"Filtered count: {filtered.Count}");
    }
}
