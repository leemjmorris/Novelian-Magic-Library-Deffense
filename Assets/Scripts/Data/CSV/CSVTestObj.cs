using Cysharp.Threading.Tasks;
using UnityEngine;

public class CSVTestObj : MonoBehaviour
{
    TestData data;
    TestData2 data2;



    private async UniTaskVoid Start()
    {
        // Wait until CSVLoader completes LoadAll
        await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);

        // Method 1: Use GetTable
        data = CSVLoader.Instance.GetTable<TestData>().GetId(1);
        data2 = CSVLoader.Instance.GetTable<TestData2>().GetId(1);
        var itemData = CSVLoader.Instance.GetTable<ItemData>().GetId(101611);

        var BookmarkCraftData = CSVLoader.Instance.GetTable<BookmarkCraftData>().GetId(1001);
        var BookmarkResultData = CSVLoader.Instance.GetTable<BookmarkResultData>().GetId(1211);
        var BookmarkOptionData = CSVLoader.Instance.GetTable<BookmarkOptionData>().GetId(1311);
        var BookmarkItemData = CSVLoader.Instance.GetTable<BookmarkItemData>().GetId(1411);

        // Method 2: Use GetData helper method (shorter)
        // data = CSVLoader.Instance.GetData<TestData>(1);
        // data2 = CSVLoader.Instance.GetData<TestData2>(1);

        if (data != null && data2 != null)
        {
            Debug.Log($"Name: {data.NAME}");
            Debug.Log($"Name2: {data2.NAME}");
            Debug.Log($"Item Name: {itemData.Item_Name}");
            Debug.Log("===============================");
            Debug.Log($"Bookmark Craft Success Rate: {BookmarkCraftData.Success_Rate}");
            Debug.Log($"Bookmark Result Grade: {BookmarkResultData.Grade}");
            Debug.Log($"Bookmark Option Name: {BookmarkOptionData.Option_Name}");
            Debug.Log($"Bookmark Item Option Value: {BookmarkItemData.Option_Value}");
        }
        else
        {
            Debug.LogError("Data not found!");
        }

        // Additional usage examples
        var allData = CSVLoader.Instance.GetTable<TestData>().GetAll();
        Debug.Log($"Total rows in TestTable: {allData.Count}");

        // Conditional search
        var filtered = CSVLoader.Instance.GetTable<TestData>().FindAll(x => x.ID > 5);
        Debug.Log($"Filtered count: {filtered.Count}");
    }
}
