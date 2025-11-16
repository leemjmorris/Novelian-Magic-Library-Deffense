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

        // Method 2: Use GetData helper method (shorter)
        // data = CSVLoader.Instance.GetData<TestData>(1);
        // data2 = CSVLoader.Instance.GetData<TestData2>(1);

        if (data != null && data2 != null)
        {
            Debug.Log($"Name: {data.NAME}");
            Debug.Log($"Name2: {data2.NAME}");
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
