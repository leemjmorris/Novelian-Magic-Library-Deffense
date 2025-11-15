using Cysharp.Threading.Tasks;
using UnityEngine;

public class CSVTestObj : MonoBehaviour
{
    TestData data;
    TestData2 data2;

    private async UniTaskVoid Start()
    {
        // CSVManager가 초기화될 때까지 대기
        await UniTask.WaitUntil(() => CSVManager.Instance != null && CSVManager.Instance.IsInit);

        data = CSVManager.Instance.testTable.GetId(1);
        data2 = CSVManager.Instance.testTable2.GetId(1);

        if (data != null)
        {
            Debug.Log($"Name: {data.NAME}");
            Debug.Log($"Name2: {data2.NAME}");
        }
        else
        {
            Debug.LogError("Data not found1!");
        }
    }
}
